// src/db/import/importPredictions.js
import { Buffer } from "buffer";

// Принудительно устанавливаем UTF-8 для вывода
if (process.stdout && process.stdout.setDefaultEncoding) {
    process.stdout.setDefaultEncoding("utf8");
}
if (process.stderr && process.stderr.setDefaultEncoding) {
    process.stderr.setDefaultEncoding("utf8");
}

import { spawnSync } from "child_process";
import { fileURLToPath } from "url";
import path from "path";
import fs from "fs";
import sql from "mssql/msnodesqlv8.js";
import { FILES } from "../../constants/constants.js";
import { createLogger } from "../../scraper/services/utils/core/logger.js";

/// <summary>
/// Настройка путей и логгера.
/// Поддержка __dirname в ES-модулях.
/// </summary>
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const VALIDATE_SCRIPT = path.join(
    __dirname,
    "../../scraper/services/utils/predictions/validatePredictionsData.js"
);

const logger = createLogger("importPredictions");

/// <summary>
/// Проверка JSON-файлов прогнозов перед импортом.
/// Если валидатор возвращает ошибки, импорт прерывается.
/// </summary>
logger.info("=== Проверка JSON-файлов прогнозов перед импортом ===");

const validation = spawnSync("node", [VALIDATE_SCRIPT], { stdio: "inherit" });

if (validation.error) {
    logger.error(`Ошибка при запуске валидатора: ${validation.error.message}`);
    process.exit(1);
}

if (validation.status !== 0) {
    logger.error("❌ Валидатор обнаружил ошибки. Импорт прогнозов остановлен.\n");
    process.exit(1);
}

logger.info("✅ Проверка завершена успешно. Ошибок не обнаружено.\n");

/// <summary>
/// Настройки подключения к MSSQL.
/// Используется trustedConnection.
/// </summary>
const pool = new sql.ConnectionPool({
    server: "LAPTOP-34F82EN1",
    database: "TelegramBOT",
    driver: "msnodesqlv8",
    options: { trustedConnection: true },
});

/// <summary>
/// Пути ко всем файлам прогнозов.
/// </summary>
const PREDICTION_PATHS = [
    FILES.BETZONA,
    FILES.LEGALBET,
    FILES.LIVESPORT,
    FILES.METARATINGS,
    FILES.STAVKATV,
    FILES.VPROGNOZE,
    FILES.VSEPROSPORT,
];

/// <summary>
/// Импортирует прогнозы КХЛ в таблицу Predictions базы данных.
/// Предварительно очищает таблицу, затем добавляет новые записи.
/// </summary>
async function importPredictions() {
    const startTime = Date.now();
    logger.info("=== Импорт прогнозов КХЛ ===");

    try {
        await pool.connect();
        logger.info("Соединение с базой данных установлено.");

        // Очистка таблицы перед импортом
        await pool.request().query("TRUNCATE TABLE Predictions;");
        logger.info("Таблица Predictions очищена.\n");

        let totalInserted = 0;
        let totalSkipped = 0;

        for (const filePath of PREDICTION_PATHS) {
            const file = path.basename(filePath);

            if (!fs.existsSync(filePath)) {
                logger.warn(`${file}: файл не найден. Пропускаем.`);
                continue;
            }

            const raw = fs.readFileSync(filePath, "utf-8").trim();
            if (!raw) {
                logger.warn(`${file}: пустой файл. Пропускаем.`);
                continue;
            }

            let predictions;
            try {
                predictions = JSON.parse(raw);
            } catch (err) {
                logger.error(`${file}: ошибка парсинга JSON — ${err.message}`);
                continue;
            }

            if (!Array.isArray(predictions) || predictions.length === 0) {
                logger.warn(`${file}: пустой или некорректный JSON. Пропускаем.`);
                continue;
            }

            /// <summary>
            /// Основной цикл вставки прогнозов в базу.
            /// Проверяется наличие матча, затем вставляется запись.
            /// </summary>
            for (const pred of predictions) {
                try {
                    const matchId = pred.id || pred.match;
                    if (!matchId) {
                        logger.warn(`${file}: прогноз без match_id (source: ${pred.source})`);
                        totalSkipped++;
                        continue;
                    }

                    // Проверяем, существует ли матч
                    const matchRes = await pool
                        .request()
                        .input("id", sql.VarChar, matchId)
                        .query("SELECT match_id FROM Matches WHERE match_id = @id");

                    if (matchRes.recordset.length === 0) {
                        logger.warn(`${file}: матч ${matchId} не найден в Matches.`);
                        totalSkipped++;
                        continue;
                    }

                    // Вставляем прогноз
                    await pool
                        .request()
                        .input("match_id", sql.VarChar, matchId)
                        .input("source", sql.NVarChar, pred.source)
                        .input("url", sql.NVarChar, pred.url || null)
                        .input("main_prediction", sql.NVarChar, pred.prediction?.main || null)
                        .input("alt_prediction", sql.NVarChar, pred.prediction?.alt || null)
                        .input("score", sql.NVarChar, pred.prediction?.score || null)
                        .input("general_text", sql.NVarChar, pred.prediction?.text || null)
                        .input("result", sql.NVarChar, pred.prediction?.result || null)
                        .input(
                            "home_team_text",
                            sql.NVarChar,
                            pred.teams?.home?.text || pred.teams?.home?.analysis || null
                        )
                        .input(
                            "away_team_text",
                            sql.NVarChar,
                            pred.teams?.away?.text || pred.teams?.away?.analysis || null
                        )
                        .query(`
                            INSERT INTO Predictions (
                                match_id, source, url,
                                main_prediction, alt_prediction, score,
                                general_text, result,
                                home_team_text, away_team_text
                            )
                            VALUES (
                                @match_id, @source, @url,
                                @main_prediction, @alt_prediction, @score,
                                @general_text, @result,
                                @home_team_text, @away_team_text
                            );
                        `);

                    totalInserted++;
                } catch (err) {
                    logger.error(
                        `${file}: ошибка при вставке (${pred.source || "unknown"}, match ${pred.id}) — ${err.message}`
                    );
                    totalSkipped++;
                }
            }
        }

        logger.info(`✅ Импорт завершён.`);
        logger.info(`Добавлено: ${totalInserted}, пропущено: ${totalSkipped}.`);
    } catch (err) {
        logger.error(`Ошибка при импорте прогнозов: ${err.message}`);
    } finally {
        await pool.close();
    }
}

/// <summary>
/// Точка входа при прямом запуске файла.
/// </summary>
importPredictions();
