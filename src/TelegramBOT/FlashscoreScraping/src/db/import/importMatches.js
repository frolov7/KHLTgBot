// src/db/import/importMatches.js

import fs from "fs";
import sql from "mssql/msnodesqlv8.js";
import dayjs from "dayjs";
import customParseFormat from "dayjs/plugin/customParseFormat.js";
import utc from "dayjs/plugin/utc.js";
import timezone from "dayjs/plugin/timezone.js";
import { createLogger } from "../../scraper/services/utils/core/logger.js";
import { FILES } from "../../constants/constants.js";

dayjs.extend(customParseFormat);
dayjs.extend(utc);
dayjs.extend(timezone);

const logger = createLogger("importMatches");

const pool = new sql.ConnectionPool({
    server: "LAPTOP-34F82EN1",
    database: "TelegramBOT",
    driver: "msnodesqlv8",
    options: { trustedConnection: true },
});

/// <summary>
/// Импортирует и синхронизирует матчи КХЛ из JSON в таблицу Matches базы данных.
/// Использует MERGE (upsert): если запись существует — обновляет, иначе вставляет новую.
/// Также выполняет автоматическое сопоставление команд и преобразование даты по часовому поясу МСК.
/// </summary>
async function importMatches() {
    const startTime = Date.now();
    logger.info("=== Импорт матчей КХЛ ===");

    const matchesPath = FILES.KHL_MATCHES;

    if (!fs.existsSync(matchesPath)) {
        logger.error("Файл russia_khl_all.json не найден. Операция прервана.");
        return;
    }

    const matches = JSON.parse(fs.readFileSync(matchesPath, "utf-8"));
    let added = 0;
    let updated = 0;
    let skipped = 0;

    try {
        await pool.connect();
        logger.info("Соединение с базой данных установлено.");

        for (const [matchId, match] of Object.entries(matches)) {
            try {
                // === Поиск ID команд ===
                const homeRes = await pool
                    .request()
                    .input("name", sql.VarChar, match.home.name)
                    .query("SELECT team_id FROM Teams WHERE name = @name");

                const awayRes = await pool
                    .request()
                    .input("name", sql.VarChar, match.away.name)
                    .query("SELECT team_id FROM Teams WHERE name = @name");

                if (homeRes.recordset.length === 0 || awayRes.recordset.length === 0) {
                    logger.info(`Пропущен матч: ${match.home.name} vs ${match.away.name} — команда не найдена.`);
                    skipped++;
                    continue;
                }

                // === Парсинг даты по МСК ===
                let matchDate = null;
                if (match.date) {
                    let dateStr = match.date;
                    if (dateStr.match(/^\d{2}\.\d{2}\.\s/)) {
                        const year = new Date().getFullYear();
                        dateStr = dateStr.replace(/(\d{2}\.\д{2}\.)/, `$1${year} `);
                    }
                    matchDate = dayjs.tz(dateStr, "DD.MM.YYYY HH:mm", "Europe/Moscow").toDate();
                }

                // === MERGE с OUTPUT ===
                const result = await pool
                    .request()
                    .input("id", sql.VarChar, matchId)
                    .input("date", sql.DateTime, matchDate ? dayjs(matchDate).add(3, "hour").toDate() : null)
                    .input("status", sql.VarChar, match.status || "SCHEDULED")
                    .input("homeName", sql.VarChar, match.home.name)
                    .input("homeId", sql.Int, homeRes.recordset[0].team_id)
                    .input("awayName", sql.VarChar, match.away.name)
                    .input("awayId", sql.Int, awayRes.recordset[0].team_id)
                    .input("homeScore", sql.Int, match.result?.home ? parseInt(match.result.home, 10) : null)
                    .input("awayScore", sql.Int, match.result?.away ? parseInt(match.result.away, 10) : null)
                    .query(`
                        MERGE Matches AS target
                        USING (SELECT 
                            @id AS match_id, 
                            @date AS match_date, 
                            @status AS status,
                            @homeName AS home_team_name, 
                            @homeId AS home_team_id,
                            @awayName AS away_team_name, 
                            @awayId AS away_team_id,
                            @homeScore AS home_score,
                            @awayScore AS away_score
                        ) AS source
                        ON (target.match_id = source.match_id)
                        WHEN MATCHED AND (
                            ISNULL(target.status, '') <> ISNULL(source.status, '') OR
                            ISNULL(target.home_score, -1) <> ISNULL(source.home_score, -1) OR
                            ISNULL(target.away_score, -1) <> ISNULL(source.away_score, -1)
                        ) THEN
                            UPDATE SET 
                                match_date = source.match_date,
                                status = source.status,
                                home_team_name = source.home_team_name,
                                home_team_id = source.home_team_id,
                                away_team_name = source.away_team_name,
                                away_team_id = source.away_team_id,
                                home_score = source.home_score,
                                away_score = source.away_score
                        WHEN NOT MATCHED THEN
                            INSERT (
                                match_id, match_date, status,
                                home_team_name, home_team_id,
                                away_team_name, away_team_id,
                                home_score, away_score
                            )
                            VALUES (
                                source.match_id, source.match_date, source.status,
                                source.home_team_name, source.home_team_id,
                                source.away_team_name, source.away_team_id,
                                source.home_score, source.away_score
                            )
                        OUTPUT $action AS Action;
                    `);

                const action = result.recordset?.[0]?.Action;
                if (action === "INSERT") {
                    logger.info(`🟢 Добавлен новый матч: ${match.home.name} vs ${match.away.name} (${match.status})`);
                    added++;
                } else if (action === "UPDATE") {
                    logger.info(`🟡 Обновлён матч: ${match.home.name} vs ${match.away.name} (${match.status})`);
                    updated++;
                }

            } catch (err) {
                logger.error(`Ошибка при обработке матча ${matchId}: ${err.message}`);
                skipped++;
            }
        }

        logger.info(`Импорт завершён. Добавлено: ${added}, обновлено: ${updated}, пропущено: ${skipped}.`);
    } catch (err) {
        logger.error("Ошибка при импорте матчей:", err.message);
    } finally {
        await pool.close();
    }
}

/// <summary>
/// Точка входа при запуске файла напрямую (node src/db/import/importMatches.js).
/// </summary>
importMatches();
