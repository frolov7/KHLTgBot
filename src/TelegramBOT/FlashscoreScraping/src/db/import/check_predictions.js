import fs from "fs";
import sql from "mssql/msnodesqlv8.js";
import path from "path";
import { fileURLToPath } from "url";
import { parsePrediction, evaluatePrediction } from "../../scraper/services/utils/predictionParser.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// JSON с результатами матчей
const matches = JSON.parse(
    fs.readFileSync(path.join(__dirname, "../../data/russia_khl_all.json"), "utf-8")
);

const OUTPUT_PATH = path.join(__dirname, "../../data");

const pool = new sql.ConnectionPool({
    server: "LAPTOP-34F82EN1",
    database: "TelegramBOT",
    driver: "msnodesqlv8",
    options: { trustedConnection: true },
});

/**
 * Обновляем прогнозы и в БД, и в JSON
 */
async function checkPredictions() {
    try {
        await pool.connect();
        console.log("Connected to DB");

        const predictionsRes = await pool.request()
            .query("SELECT prediction_id, match_id, main_prediction, source FROM Predictions");

        let updated = 0;

        // Загружаем все JSON-файлы с прогнозами
        const predictionFiles = fs.readdirSync(OUTPUT_PATH)
            .filter(f => f.endsWith(".json") && f !== "russia_khl_all.json");

        const jsonData = {};
        for (const file of predictionFiles) {
            const fullPath = path.join(OUTPUT_PATH, file);
            jsonData[file] = JSON.parse(fs.readFileSync(fullPath, "utf-8"));
        }

        for (const row of predictionsRes.recordset) {
            const match = matches[row.match_id];
            if (!match || !["FINISHED", "AFTER OVERTIME", "AFTER PENALTIES"].includes(match.status)) {
                continue; // матч ещё не завершён
            }

            const parsed = parsePrediction(row.main_prediction);
            const result = evaluatePrediction(parsed, match);

            // обновляем в БД
            await pool.request()
                .input("id", sql.Int, row.prediction_id)
                .input("res", sql.VarChar, result)
                .query("UPDATE Predictions SET result = @res WHERE prediction_id = @id");

            // обновляем в JSON
            for (const file of predictionFiles) {
                const arr = jsonData[file];
                if (!Array.isArray(arr)) continue;

                let changed = false;
                for (const item of arr) {
                    if (item.id === row.match_id && item.source === row.source) {
                        item.prediction.result = result;
                        changed = true;
                    }
                }

                if (changed) {
                    const fullPath = path.join(OUTPUT_PATH, file);
                    fs.writeFileSync(fullPath, JSON.stringify(arr, null, 2), "utf-8");
                }
            }

            // 👉 логируем в консоль
            console.log(
                `Prediction ${row.prediction_id} [${row.source}] (${match.home.name} vs ${match.away.name}): ${row.main_prediction} → ${result}`
            );

            updated++;
        }

        console.log(`✅ Проверено и обновлено ${updated} прогнозов`);
        await pool.close();
    } catch (err) {
        console.error("Error:", err);
        await pool.close();
    }
}

checkPredictions();
