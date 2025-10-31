import fs from "fs";
import sql from "mssql/msnodesqlv8.js";
import { createLogger } from "../../scraper/services/utils/core/logger.js";
import { FILES } from "../../constants/constants.js";

const logger = createLogger("importMatchEvents");

const pool = new sql.ConnectionPool({
    server: "LAPTOP-34F82EN1",
    database: "TelegramBOT",
    driver: "msnodesqlv8",
    options: { trustedConnection: true },
});

/// <summary>
/// Импортирует все события матчей КХЛ (голы, удаления, буллиты, замены вратарей и т.д.)
/// из JSON-файла KHL_EVENTS.json в таблицы MatchEvents, GoalDetails, GoalieChanges и т.д.
/// </summary>
async function importMatchEvents() {
    const startTime = Date.now();
    logger.info("=== Импорт событий матчей КХЛ ===");

    try {
        await pool.connect();
        logger.info("Соединение с базой данных установлено.");

        const eventsPath = FILES.KHL_EVENTS;

        if (!fs.existsSync(eventsPath)) {
            logger.error("Файл KHL_EVENTS.json не найден. Импорт прерван.");
            return;
        }

        const data = JSON.parse(fs.readFileSync(eventsPath, "utf-8"));
        const matches = Object.entries(data);

        if (!matches.length) {
            logger.error("Файл KHL_EVENTS.json пуст или имеет неверный формат.");
            return;
        }

        // Очищаем таблицы перед импортом (если хочешь сохранять историю — удали эти строки)
        await pool.request().query(`
            DELETE FROM GoalDetails;
            DELETE FROM GoalieChanges;
            DELETE FROM ShootoutDetails;
            DELETE FROM Penalties;
            DELETE FROM MatchEvents;
        `);
        logger.info("Таблицы событий очищены.");

        let insertedEvents = 0;
        let skippedMatches = 0;

        for (const [matchId, matchData] of matches) {
            const matchExists = await pool
                .request()
                .input("match_id", sql.VarChar, matchId)
                .query("SELECT match_id FROM Matches WHERE match_id = @match_id");

            if (matchExists.recordset.length === 0) {
                logger.warn(`Матч ${matchId} (${matchData.home} – ${matchData.away}) не найден в Matches. Пропускаем.`);
                skippedMatches++;
                continue;
            }

            const events = matchData.events || [];
            if (events.length === 0) continue;

            for (const ev of events) {
                try {
                    // === Получаем event_type_id ===
                    const { recordset: eventTypeSet } = await pool
                        .request()
                        .input("name", sql.NVarChar, ev.eventType)
                        .query("SELECT event_type_id FROM EventTypes WHERE name = @name");

                    if (eventTypeSet.length === 0) {
                        logger.warn(`Тип события "${ev.eventType}" не найден. Пропуск.`);
                        continue;
                    }

                    const eventTypeId = eventTypeSet[0].event_type_id;

                    // === Получаем team_id ===
                    let teamId = null;
                    if (ev.team) {
                        const { recordset: teamSet } = await pool
                            .request()
                            .input("name", sql.NVarChar, ev.team)
                            .query("SELECT team_id FROM Teams WHERE name = @name");
                        if (teamSet.length > 0) teamId = teamSet[0].team_id;
                    }

                    // === Вставляем основное событие ===
                    const insertEvent = await pool
                        .request()
                        .input("match_id", sql.VarChar, matchId)
                        .input("team_id", sql.Int, teamId)
                        .input("event_type_id", sql.Int, eventTypeId)
                        .input("period", sql.NVarChar, ev.period || null)
                        .input("time", sql.NVarChar, ev.time || null)
                        .input("details", sql.NVarChar, ev.details || null)
                        .input("player", sql.NVarChar, ev.player || null)
                        .query(`
                            INSERT INTO MatchEvents (match_id, team_id, event_type_id, period, time, details, player)
                            OUTPUT INSERTED.event_id AS event_id
                            VALUES (@match_id, @team_id, @event_type_id, @period, @time, @details, @player);
                        `);

                    const eventId = insertEvent.recordset[0].event_id;
                    insertedEvents++;

                    // === Специфические таблицы по типу ===
                    if (ev.eventType === "Goal") {
                        await pool
                            .request()
                            .input("event_id", sql.Int, eventId)
                            .input("scorer", sql.NVarChar, ev.scorer || null)
                            .input("assistants", sql.NVarChar, ev.assistants ? ev.assistants.join(", ") : null)
                            .input("goal_type", sql.NVarChar, ev.goalType || null)
                            .input("score", sql.NVarChar, ev.score || null)
                            .query(`
                                INSERT INTO GoalDetails (event_id, scorer, assistants, goal_type, score)
                                VALUES (@event_id, @scorer, @assistants, @goal_type, @score);
                            `);
                    }
                    else if (ev.eventType === "Goalkeeper change") {
                        await pool
                            .request()
                            .input("event_id", sql.Int, eventId)
                            .input("goalie_out", sql.NVarChar, ev.goalieOut || null)
                            .input("goalie_in", sql.NVarChar, ev.goalieIn || null)
                            .query(`
                                INSERT INTO GoalieChanges (event_id, goalie_out, goalie_in)
                                VALUES (@event_id, @goalie_out, @goalie_in);
                            `);
                    }
                    else if (ev.eventType === "Shootout missed") {
                        await pool
                            .request()
                            .input("event_id", sql.Int, eventId)
                            .input("result", sql.NVarChar, "Missed")
                            .input("shooter", sql.NVarChar, ev.player || null)
                            .query(`
                                INSERT INTO ShootoutDetails (event_id, result, shooter)
                                VALUES (@event_id, @result, @shooter);
                            `);
                    }
                    else if (ev.eventType === "Goal disallowed") {
                        // можно тоже хранить в MatchEvents.details
                        continue;
                    }
                    else if (ev.eventType === "Penalty") {
                        await pool
                            .request()
                            .input("event_id", sql.Int, eventId)
                            .input("player", sql.NVarChar, ev.player || null)
                            .input("reason", sql.NVarChar, ev.details || null)
                            .query(`
                                INSERT INTO Penalties (event_id, player, reason)
                                VALUES (@event_id, @player, @reason);
                            `);
                    }

                } catch (err) {
                    logger.error(`Ошибка при вставке события (${ev.eventType}) в матче ${matchId}: ${err.message}`);
                }
            }
        }

        logger.info(`Импорт завершён. Всего добавлено событий: ${insertedEvents}, пропущено матчей: ${skippedMatches}.`);
    } catch (err) {
        logger.error("Ошибка при импорте событий:", err.message);
    } finally {
        await pool.close();
    }
}

/// <summary>
/// Точка входа при прямом запуске (node src/db/import/importMatchEvents.js)
/// </summary>
importMatchEvents();
