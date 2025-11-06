import fs from "fs";
import sql from "mssql/msnodesqlv8.js";
import { createLogger } from "../../scraper/services/utils/core/logger.js";
import { FILES } from "../../constants/constants.js";

const logger = createLogger("importMatchEvents");

/// <summary>
/// Подключение к базе данных Microsoft SQL Server.
/// Используется драйвер msnodesqlv8 с доверенным подключением.
/// </summary>
const pool = new sql.ConnectionPool({
    server: "LAPTOP-34F82EN1",
    database: "TelegramBOT",
    driver: "msnodesqlv8",
    options: { trustedConnection: true },
});

/// <summary>
/// Получает аргументы командной строки для определения режима работы:
/// полный импорт или импорт одного конкретного матча.
/// </summary>
const args = process.argv.slice(2);
const singleMode = args.includes("--single");
const matchId = singleMode ? args[args.indexOf("--single") + 1] : null;

/// <summary>
/// Импортирует события матчей КХЛ (голы, удаления, буллиты, замены вратарей и т.д.)
/// из JSON-файла KHL_EVENTS.json в таблицы MatchEvents, GoalDetails, GoalieChanges, ShootoutDetails и Penalties.
/// Поддерживает два режима работы:
/// 1. Полный импорт всех матчей — очистка всех таблиц и загрузка всех событий.
/// 2. Импорт одного матча (--single matchId) — обновление существующих событий и добавление новых без удаления таблиц.
/// </summary>
async function importMatchEvents(singleMode, matchId) {
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
        let insertedEvents = 0;
        let skippedMatches = 0;

        // ---------------------------------------------------------------
        // Режим: Импорт только одного матча (--single matchId)
        // ---------------------------------------------------------------
        if (singleMode && matchId) {
            logger.info(`Импорт событий только для матча ${matchId}`);

            const matchData = data[matchId];
            if (!matchData || !matchData.events?.length) {
                logger.warn(`Нет данных о событиях для матча ${matchId}.`);
                return;
            }

            const count = await importSingleMatch(matchId, matchData, pool);
            const elapsed = ((Date.now() - startTime) / 1000).toFixed(2);
            logger.info(`✅ Импорт завершён для ${matchId} (${count} событий, ${elapsed} сек.)`);
            return;
        }

        // ---------------------------------------------------------------
        // Режим: Полный импорт всех матчей
        // ---------------------------------------------------------------
        const matches = Object.entries(data);
        if (!matches.length) {
            logger.error("Файл KHL_EVENTS.json пуст или имеет неверный формат.");
            return;
        }

        // Очистка таблиц перед полным импортом
        await pool.request().query(`
            DELETE FROM GoalDetails;
            DELETE FROM GoalieChanges;
            DELETE FROM ShootoutDetails;
            DELETE FROM Penalties;
            DELETE FROM MatchEvents;
        `);
        logger.info("Таблицы событий очищены.");

        for (const [mId, matchData] of matches) {
            const matchExists = await pool
                .request()
                .input("match_id", sql.VarChar, mId)
                .query("SELECT match_id FROM Matches WHERE match_id = @match_id");

            if (matchExists.recordset.length === 0) {
                logger.warn(`Матч ${mId} (${matchData.home} – ${matchData.away}) не найден в Matches. Пропускаем.`);
                skippedMatches++;
                continue;
            }

            const count = await importSingleMatch(mId, matchData, pool);
            insertedEvents += count;
        }

        const elapsed = ((Date.now() - startTime) / 1000).toFixed(2);
        logger.info(`✅ Импорт завершён. Добавлено/обновлено событий: ${insertedEvents}, пропущено матчей: ${skippedMatches}. (${elapsed} сек.)`);
    } catch (err) {
        logger.error("Ошибка при импорте событий:", err.message);
    } finally {
        await pool.close();
        logger.info("Соединение с базой данных закрыто.");
    }
}

/// <summary>
/// Импортирует события для одного конкретного матча КХЛ.
/// Используется как при быстром режиме (--single), так и при полном импорте.
/// </summary>
/// <param name="matchId">Уникальный идентификатор матча (например, boRXxDHG).</param>
/// <param name="matchData">Объект с данными о матче и списком событий.</param>
/// <returns>Количество успешно добавленных событий.</returns>
async function importSingleMatch(matchId, matchData, pool) {
    const events = matchData.events || [];
    let insertedEvents = 0;

    // --- получаем все event_id для этого матча ---
    const existingEvents = await pool.request()
        .input("match_id", sql.VarChar, matchId)
        .query("SELECT event_id, event_type_id, period, time, player, details FROM MatchEvents WHERE match_id = @match_id");

    const existingMap = new Map(
        existingEvents.recordset.map(ev => [
            `${ev.event_type_id}|${ev.period || ""}|${ev.time || ""}|${ev.player || ""}|${ev.details || ""}`,
            ev.event_id
        ])
    );

    const seenEventIds = new Set();

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

            const key = `${eventTypeId}|${ev.period || ""}|${ev.time || ""}|${ev.player || ""}|${ev.details || ""}`;
            let eventId = existingMap.get(key);

            // --- MERGE-логика для MatchEvents ---
            if (eventId) {
                // обновляем
                await pool.request()
                    .input("event_id", sql.Int, eventId)
                    .input("team_id", sql.Int, teamId)
                    .query(`
                        UPDATE MatchEvents
                        SET team_id = @team_id
                        WHERE event_id = @event_id;
                    `);
                logger.info(`🔄 Обновлено событие (${ev.eventType}) для матча ${matchId}`);
            } else {
                // вставляем
                const insertEvent = await pool.request()
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
                eventId = insertEvent.recordset[0].event_id;
                insertedEvents++;
                logger.info(`➕ Добавлено новое событие (${ev.eventType}) для матча ${matchId}`);
            }

            seenEventIds.add(eventId);

            // --- Обработка подтаблиц (MERGE сохраняем как есть) ---
            switch (ev.eventType) {
                case "Goal":
                    await pool.request()
                        .input("event_id", sql.Int, eventId)
                        .input("scorer", sql.NVarChar, ev.scorer || null)
                        .input("assistants", sql.NVarChar, ev.assistants ? ev.assistants.join(", ") : null)
                        .input("goal_type", sql.NVarChar, ev.goalType || null)
                        .input("score", sql.NVarChar, ev.score || null)
                        .query(`
                            MERGE GoalDetails AS target
                            USING (SELECT @event_id AS event_id) AS src
                            ON target.event_id = src.event_id
                            WHEN MATCHED THEN
                                UPDATE SET scorer=@scorer, assistants=@assistants, goal_type=@goal_type, score=@score
                            WHEN NOT MATCHED THEN
                                INSERT (event_id, scorer, assistants, goal_type, score)
                                VALUES (@event_id, @scorer, @assistants, @goal_type, @score);
                        `);
                    break;

                case "Penalty":
                    await pool.request()
                        .input("event_id", sql.Int, eventId)
                        .input("player", sql.NVarChar, ev.player || null)
                        .input("reason", sql.NVarChar, ev.reason || ev.details || null)
                        .input("duration", sql.NVarChar, ev.duration || null)
                        .query(`
                            MERGE Penalties AS target
                            USING (SELECT @event_id AS event_id) AS src
                            ON target.event_id = src.event_id
                            WHEN MATCHED THEN
                                UPDATE SET player=@player, reason=@reason, duration=@duration
                            WHEN NOT MATCHED THEN
                                INSERT (event_id, player, reason, duration)
                                VALUES (@event_id, @player, @reason, @duration);
                        `);
                    break;

                case "Goalkeeper change":
                    await pool.request()
                        .input("event_id", sql.Int, eventId)
                        .input("goalie_out", sql.NVarChar, ev.goalieOut || null)
                        .input("goalie_in", sql.NVarChar, ev.goalieIn || null)
                        .query(`
                            MERGE GoalieChanges AS target
                            USING (SELECT @event_id AS event_id) AS src
                            ON target.event_id = src.event_id
                            WHEN MATCHED THEN
                                UPDATE SET goalie_out=@goalie_out, goalie_in=@goalie_in
                            WHEN NOT MATCHED THEN
                                INSERT (event_id, goalie_out, goalie_in)
                                VALUES (@event_id, @goalie_out, @goalie_in);
                        `);
                    break;

                // Буллит не реализован — "Shootout missed"
                case "Shootout missed":
                    await pool.request()
                        .input("event_id", sql.Int, eventId)
                        .input("shooter", sql.NVarChar, ev.player || null)
                        .input("result", sql.NVarChar, "Missed")
                        .query(`
                            MERGE ShootoutDetails AS target
                            USING (SELECT @event_id AS event_id) AS src
                            ON target.event_id = src.event_id
                            WHEN MATCHED THEN
                                UPDATE SET shooter=@shooter, result=@result
                            WHEN NOT MATCHED THEN
                                INSERT (event_id, shooter, result)
                                VALUES (@event_id, @shooter, @result);
                        `);
                    break;

                // Гол не засчитан — "Goal disallowed"
                case "Goal disallowed":
                    await pool.request()
                        .input("event_id", sql.Int, eventId)
                        .input("scorer", sql.NVarChar, ev.player || null)
                        .input("goal_type", sql.NVarChar, ev.reason || ev.details || "Disallowed")
                        .query(`
                            MERGE GoalDetails AS target
                            USING (SELECT @event_id AS event_id) AS src
                            ON target.event_id = src.event_id
                            WHEN MATCHED THEN
                                UPDATE SET scorer=@scorer, goal_type=@goal_type
                            WHEN NOT MATCHED THEN
                                INSERT (event_id, scorer, goal_type)
                                VALUES (@event_id, @scorer, @goal_type);
                        `);
                    break;

                default:
                    logger.info(`(i) Пропущен дополнительный парсинг для типа "${ev.eventType}"`);
                    break;
            }

        } catch (err) {
            logger.error(`Ошибка при обработке события (${ev.eventType}) в матче ${matchId}: ${err.message}`);
        }
    }

    // --- Удаляем устаревшие события ---
    for (const [_, evId] of existingMap) {
        if (!seenEventIds.has(evId)) {
            await pool.request()
                .input("event_id", sql.Int, evId)
                .query(`
                    DELETE FROM GoalDetails WHERE event_id = @event_id;
                    DELETE FROM GoalieChanges WHERE event_id = @event_id;
                    DELETE FROM ShootoutDetails WHERE event_id = @event_id;
                    DELETE FROM Penalties WHERE event_id = @event_id;
                    DELETE FROM MatchEvents WHERE event_id = @event_id;
                `);
            logger.info(`Удалено устаревшее событие event_id=${evId} для матча ${matchId}`);
        }
    }

    return insertedEvents;
}

/// <summary>
/// Точка входа при прямом запуске скрипта.
/// Пример:
/// node src/db/import/importMatchEvents.js
/// node src/db/import/importMatchEvents.js --single boRXxDHG
/// </summary>
importMatchEvents(singleMode, matchId);
