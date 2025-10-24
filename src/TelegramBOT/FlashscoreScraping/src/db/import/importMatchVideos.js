// src/db/import/importMatchVideos.js

import fs from "fs";
import sql from "mssql/msnodesqlv8.js";
import { createLogger } from "../../scraper/services/utils/core/logger.js";
import { FILES } from "../../constants/constants.js";

const logger = createLogger("importMatchVideos");

const pool = new sql.ConnectionPool({
    server: "LAPTOP-34F82EN1",
    database: "TelegramBOT",
    driver: "msnodesqlv8",
    options: { trustedConnection: true },
});

/// <summary>
/// Импортирует видеообзоры КХЛ из JSON-файла в таблицу MatchVideos базы данных.
/// Перед вставкой очищает таблицу и добавляет только видео для матчей, существующих в Matches.
/// </summary>
async function importMatchVideos() {
    const startTime = Date.now();
    logger.info("=== Импорт видеообзоров КХЛ ===");

    try {
        await pool.connect();
        logger.info("Соединение с базой данных установлено.");

        const videosPath = FILES.RESULT_VIDEOS;

        if (!fs.existsSync(videosPath)) {
            logger.error("Файл resultVideos.json не найден. Импорт прерван.");
            return;
        }

        const videos = JSON.parse(fs.readFileSync(videosPath, "utf-8"));
        if (!Array.isArray(videos) || videos.length === 0) {
            logger.error("Файл resultVideos.json пуст или имеет неверный формат.");
            return;
        }

        // Очистка таблицы перед импортом
        await pool.request().query("TRUNCATE TABLE MatchVideos;");
        logger.info("Таблица MatchVideos очищена.");

        let inserted = 0;
        let skipped = 0;

        for (const video of videos) {
            try {
                // Проверяем, существует ли матч с таким ID
                const { recordset } = await pool
                    .request()
                    .input("id", sql.VarChar, video.id)
                    .query("SELECT match_id FROM Matches WHERE match_id = @id");

                if (recordset.length === 0) {
                    logger.info(`Пропущено: матч ${video.title} (${video.id}) не найден в Matches.`);
                    skipped++;
                    continue;
                }

                await pool
                    .request()
                    .input("match_id", sql.VarChar, video.id)
                    .input("title", sql.NVarChar, video.title)
                    .input("url", sql.NVarChar, video.url)
                    .query(`
                        INSERT INTO MatchVideos (match_id, title, url)
                        VALUES (@match_id, @title, @url);
                    `);

                logger.info(`Добавлено видео: ${video.title} | matchID: ${video.id}`);
                inserted++;
            } catch (err) {
                logger.error(`Ошибка при вставке видео "${video.title}": ${err.message}`);
            }
        }

        logger.info(`Импорт завершён. Добавлено: ${inserted}, пропущено: ${skipped}.`);
    } catch (err) {
        logger.error("Ошибка при импорте видео:", err.message);
    } finally {
        await pool.close();
    }
}

/// <summary>
/// Точка входа при запуске файла напрямую (node src/db/import/importMatchVideos.js).
/// </summary>
importMatchVideos();
