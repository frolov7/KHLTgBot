import fs from "fs";
import path from "path";
import sql from "mssql/msnodesqlv8.js";
import { fileURLToPath } from "url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const pool = new sql.ConnectionPool({
    server: "LAPTOP-34F82EN1",
    database: "TelegramBOT",
    driver: "msnodesqlv8",
    options: { trustedConnection: true },
});

// список файлов с прогнозами
const predictionFiles = [
    "betzona.json",
    "legalbet.json",
    "livesport.json",
    "metaratings.json",
    "stavkatv.json",
    "vprognoze.json",
    "vseprosport.json",
];

async function importPredictions() {
    try {
        await pool.connect();
        console.log("Connected to DB");

        // Чистим таблицу
        await pool.request().query("TRUNCATE TABLE Predictions;");
        console.log("Таблица Predictions очищена");

        let totalInserted = 0;

        for (const file of predictionFiles) {
            const filePath = path.join(__dirname, "../../data", file);

            if (!fs.existsSync(filePath)) {
                console.warn(`Файл ${file} не найден, пропускаем`);
                continue;
            }

            const raw = fs.readFileSync(filePath, "utf-8");
            const predictions = JSON.parse(raw);

            for (const pred of predictions) {
                try {
                    // ищем матч по ID
                    const matchRes = await pool
                        .request()
                        .input("id", sql.VarChar, pred.id || pred.match)
                        .query("SELECT match_id FROM Matches WHERE match_id = @id");

                    if (matchRes.recordset.length === 0) {
                        console.warn(
                            `Прогноз пропущен: матч ${pred.match} (${pred.id}) не найден в Matches`
                        );
                        continue;
                    }

                    await pool
                        .request()
                        .input("match_id", sql.VarChar, pred.id || pred.match)
                        .input("source", sql.NVarChar, pred.source)
                        .input("main_prediction", sql.NVarChar, pred.prediction.main || null)
                        .input("alt_prediction", sql.NVarChar, pred.prediction.alt || null)
                        .input("score", sql.NVarChar, pred.prediction.score || null)
                        .input("general_text", sql.NVarChar, pred.prediction.text || null)
                        .input("result", sql.NVarChar, pred.prediction.result || null)
                        .input("home_team_text", sql.NVarChar, pred.teams.home.text || pred.teams.home.analysis || null)
                        .input("away_team_text", sql.NVarChar, pred.teams.away.text || pred.teams.away.analysis || null)
                        .query(`
                            INSERT INTO Predictions (
                                match_id, source,
                                main_prediction, alt_prediction, score, general_text, result,
                                home_team_text, away_team_text
                            )
                            VALUES (
                                @match_id, @source,
                                @main_prediction, @alt_prediction, @score, @general_text, @result,
                                @home_team_text, @away_team_text
                            );
                        `);

                    totalInserted++;
                } catch (err) {
                    console.error(`Ошибка при вставке прогноза для ${pred.match}:`, err.message);
                }
            }
        }

        console.log(`Загружено ${totalInserted} прогнозов в БД`);
        await pool.close();
    } catch (err) {
        console.error("Ошибка импорта:", err);
        await pool.close();
    }
}

importPredictions();
