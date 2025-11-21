// updatePredictionResults.js
import fs from "fs";
import sql from "mssql/msnodesqlv8.js";
import path from "path";
import { fileURLToPath } from "url";

import {
    parsePrediction,
    evaluatePrediction,
} from "../../scraper/services/utils/predictions/predictionParser.js";

import { createLogger } from "../../scraper/services/utils/core/logger.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Путь к JSON прогнозов
const DATA_PATH = path.join(__dirname, "../../data/predictions");

// Путь к JSON матчей
const MATCHES_FILE = path.join(__dirname, "../../data/matches/khl_all_matches.json");

// Загружаем матчи
const matches = JSON.parse(fs.readFileSync(MATCHES_FILE, "utf-8"));

const logger = createLogger("updatePredictionResults");

// Подключение к БД
const pool = new sql.ConnectionPool({
    server: "LAPTOP-34F82EN1",
    database: "TelegramBOT",
    driver: "msnodesqlv8",
    options: { trustedConnection: true },
});

// Helper: преобразовать дату из JSON
function parseMatchDate(str) {
    if (!str) return null;
    // Формат: "06.09.2025 14:30"
    const [datePart, timePart] = str.split(" ");
    const [d, m, y] = datePart.split(".").map(Number);
    const [hh, mm] = timePart.split(":").map(Number);
    return new Date(y, m - 1, d, hh, mm);
}

// Фильтр: матч был за последние 2 дня
function isRecentMatch(matchDate) {
    if (!matchDate) return false;
    const now = new Date();
    const diffMs = now - matchDate;
    const diffDays = diffMs / 1000 / 60 / 60 / 24;
    return diffDays <= 2; // последние 2 дня
}

/// Основная логика
async function updatePredictionResults() {
    const startTime = Date.now();
    logger.info("=== Проверка прогнозов (последние 2 дня) ===");

    try {
        await pool.connect();
        logger.info("Соединение с базой установлено.");

        // Загружаем прогнозы
        const { recordset: predictions } = await pool.request()
            .query("SELECT prediction_id, match_id, main_prediction, source FROM Predictions");

        // Загружаем все JSON с прогнозами
        const predictionFiles = fs.readdirSync(DATA_PATH)
            .filter(f => f.endsWith(".json"));

        const jsonData = {};
        for (const file of predictionFiles) {
            const full = path.join(DATA_PATH, file);
            jsonData[file] = JSON.parse(fs.readFileSync(full, "utf-8"));
        }

        const modifiedFiles = new Set();
        let updatedCount = 0;

        for (const row of predictions) {
            const match = matches[row.match_id];
            if (!match) continue;

            // превращаем дату матча в Date()
            const matchDate = parseMatchDate(match.date);

            // ОБРАБАТЫВАЕМ ТОЛЬКО МАТЧИ ЗА ПОСЛЕДНИЕ 2 ДНЯ
            if (!isRecentMatch(matchDate)) continue;

            // И только завершённые
            if (!["FINISHED", "AFTER OVERTIME", "AFTER PENALTIES"].includes(match.status))
                continue;

            const parsed = parsePrediction(row.main_prediction);
            const result = evaluatePrediction(parsed, match);

            // Обновляем БД
            await pool.request()
                .input("id", sql.Int, row.prediction_id)
                .input("res", sql.NVarChar, result)
                .query("UPDATE Predictions SET result = @res WHERE prediction_id = @id");

            // Обновление в JSON
            for (const file of predictionFiles) {
                const arr = jsonData[file];
                if (!Array.isArray(arr)) continue;

                let changed = false;

                for (const item of arr) {
                    if (item.id === row.match_id && item.source === row.source) {
                        if (item.prediction && item.prediction.result !== result) {
                            item.prediction.result = result;
                            changed = true;
                        }
                    }
                }

                if (changed) modifiedFiles.add(file);
            }

            logger.info(
                `Прогноз #${row.prediction_id} | ${row.source} | ${match.home?.name} – ${match.away?.name} | ${row.main_prediction} → ${result}`
            );

            updatedCount++;
        }

        // Сохраняем только изменённые файлы
        for (const file of modifiedFiles) {
            const full = path.join(DATA_PATH, file);
            fs.writeFileSync(full, JSON.stringify(jsonData[file], null, 2), "utf-8");
            logger.info(`✔ JSON сохранён: ${file}`);
        }

        logger.info(`Готово. Обновлено прогнозов: ${updatedCount}`);
    } catch (err) {
        logger.error("Ошибка:", err);
    } finally {
        await pool.close();
        const sec = ((Date.now() - startTime) / 1000).toFixed(2);
        logger.info(`=== Завершено за ${sec} сек ===`);
    }
}

updatePredictionResults();
