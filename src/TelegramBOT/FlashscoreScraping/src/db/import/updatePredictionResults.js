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

const DATA_PATH = path.join(__dirname, "../../data/predictions");
const MATCHES_FILE = path.join(__dirname, "../../data/matches/khl_all_matches.json");
const matches = JSON.parse(fs.readFileSync(MATCHES_FILE, "utf-8"));

const logger = createLogger("updatePredictionResults");

const pool = new sql.ConnectionPool({
    server: "LAPTOP-34F82EN1",
    database: "TelegramBOT",
    driver: "msnodesqlv8",
    options: { trustedConnection: true },
});

// === ПАРСИНГ ДАТЫ МАТЧА ===
function parseMatchDate(str) {
    if (!str) return null;
    const [datePart, timePart] = str.split(" ");
    const [d, m, y] = datePart.split(".").map(Number);
    const [hh, mm] = timePart.split(":").map(Number);
    return new Date(y, m - 1, d, hh, mm);
}

// Проверка — матч за последние 2 дня?
function isRecentMatch(matchDate) {
    if (!matchDate) return false;
    const now = new Date();
    const diff = (now - matchDate) / 1000 / 60 / 60 / 24;
    return diff <= 2;
}

/// ============================================================================
///   ОСНОВНАЯ ФУНКЦИЯ
/// ============================================================================
async function updatePredictionResults() {
    const startTime = Date.now();

    // === ВКЛЮЧЕНО ПО УМОЛЧАНИЮ: только последние 2 дня ===
    const UPDATE_ONLY_RECENT = true;

    // === ВАРИАНТ: обновлять ВСЕ прогнозы ===
    //const UPDATE_ONLY_RECENT = false;

    logger.info(
        UPDATE_ONLY_RECENT
            ? "=== Проверка прогнозов (последние 2 дня) ==="
            : "=== Проверка всех прогнозов ==="
    );

    try {
        await pool.connect();
        logger.info("Соединение с базой установлено.");

        const { recordset: predictions } = await pool.request()
            .query("SELECT prediction_id, match_id, main_prediction, source FROM Predictions");

        const predictionFiles = fs.readdirSync(DATA_PATH)
            .filter(f => f.endsWith(".json"));

        const jsonData = {};
        for (const file of predictionFiles) {
            jsonData[file] = JSON.parse(fs.readFileSync(path.join(DATA_PATH, file), "utf-8"));
        }

        const modifiedFiles = new Set();
        let updatedCount = 0;

        for (const row of predictions) {
            const match = matches[row.match_id];
            if (!match) continue;

            const matchDate = parseMatchDate(match.date);

            // === ФИЛЬТР ПО 2 ДНЯМ ===
            if (UPDATE_ONLY_RECENT && !isRecentMatch(matchDate)) continue;

            // === ТОЛЬКО ЗАКОНЧЕННЫЕ МАТЧИ ===
            if (!["FINISHED", "AFTER OVERTIME", "AFTER PENALTIES"].includes(match.status))
                continue;

            const parsed = parsePrediction(row.main_prediction);
            const result = evaluatePrediction(parsed, match);

            await pool.request()
                .input("id", sql.Int, row.prediction_id)
                .input("res", sql.NVarChar, result)
                .query("UPDATE Predictions SET result = @res WHERE prediction_id = @id");

            // === Обновляем JSON ===
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

        // === Сохранение JSON ===
        for (const file of modifiedFiles) {
            fs.writeFileSync(
                path.join(DATA_PATH, file),
                JSON.stringify(jsonData[file], null, 2),
                "utf-8"
            );
            logger.info(`✔ JSON обновлен: ${file}`);
        }

        logger.info(`Готово. Обновлено прогнозов: ${updatedCount}`);
    } catch (err) {
        logger.error("Ошибка:", err);
    } finally {
        await pool.close();
        logger.info(`=== Завершено за ${((Date.now() - startTime) / 1000).toFixed(2)} сек ===`);
    }
}

updatePredictionResults();
