import fs from "fs";
import sql from "mssql/msnodesqlv8.js";
import path from "path";
import { fileURLToPath } from "url";
import { parsePrediction, evaluatePrediction } from "../../scraper/services/utils/matches/predictionParser.js";
import { createLogger } from "../../scraper/services/utils/core/logger.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const DATA_PATH = path.join(__dirname, "../../data");
const MATCHES_FILE = path.join(DATA_PATH, "russia_khl_all.json");

const matches = JSON.parse(fs.readFileSync(MATCHES_FILE, "utf-8"));

const logger = createLogger("updatePredictionResults");

const pool = new sql.ConnectionPool({
    server: "LAPTOP-34F82EN1",
    database: "TelegramBOT",
    driver: "msnodesqlv8",
    options: { trustedConnection: true },
});

/// <summary>
/// Проверяет прогнозы из базы данных и JSON-файлов, сравнивая их с результатами завершённых матчей.
/// При совпадении матчей обновляет поле `result` в БД и JSON.
/// </summary>
async function updatePredictionResults() {
    const startTime = Date.now();
    logger.info("=== Проверка прогнозов ===");

    try {
        await pool.connect();
        logger.info("Соединение с базой данных установлено.");

        // Получаем все прогнозы из БД
        const { recordset: predictions } = await pool.request()
            .query("SELECT prediction_id, match_id, main_prediction, source FROM Predictions");

        // Загружаем все JSON-файлы с прогнозами
        const predictionFiles = fs.readdirSync(DATA_PATH)
            .filter(f => f.endsWith(".json") && f !== "russia_khl_all.json");

        const jsonData = {};
        for (const file of predictionFiles) {
            const fullPath = path.join(DATA_PATH, file);
            jsonData[file] = JSON.parse(fs.readFileSync(fullPath, "utf-8"));
        }

        let updatedCount = 0;

        for (const row of predictions) {
            const match = matches[row.match_id];
            if (!match) continue;

            // обновляем только завершённые матчи
            if (!["FINISHED", "AFTER OVERTIME", "AFTER PENALTIES"].includes(match.status)) continue;

            const parsed = parsePrediction(row.main_prediction);
            const result = evaluatePrediction(parsed, match);

            // Обновляем результат в БД
            await pool.request()
                .input("id", sql.Int, row.prediction_id)
                .input("res", sql.VarChar, result)
                .query("UPDATE Predictions SET result = @res WHERE prediction_id = @id");

            // Обновляем результат в JSON
            for (const file of predictionFiles) {
                const data = jsonData[file];
                if (!Array.isArray(data)) continue;

                let changed = false;
                for (const item of data) {
                    if (item.id === row.match_id && item.source === row.source) {
                        item.prediction.result = result;
                        changed = true;
                    }
                }

                if (changed) {
                    fs.writeFileSync(path.join(DATA_PATH, file), JSON.stringify(data, null, 2), "utf-8");
                }
            }

            logger.info(
                `Прогноз #${row.prediction_id} | Источник: ${row.source} | Матч: ${match.home.name} – ${match.away.name} | ${row.main_prediction} → ${result}`
            );

            updatedCount++;
        }

        logger.info(`\nПроверено и обновлено прогнозов: ${updatedCount}`);
    } catch (err) {
        logger.error("Ошибка при проверке прогнозов:", err.message);
    } finally {
        await pool.close();
        const duration = ((Date.now() - startTime) / 1000).toFixed(2);
        logger.info(`Проверка завершена за ${duration} сек.`);
    }
}

/// <summary>
/// Точка входа при запуске файла напрямую (node src/db/import/updatePredictionResults.js).
/// </summary>
updatePredictionResults();
