import fs from "fs";
import path from "path";
import sql from "mssql/msnodesqlv8.js";
import { fileURLToPath } from "url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Конфигурация подключения
const pool = new sql.ConnectionPool({
    server: "LAPTOP-34F82EN1",
    database: "TelegramBOT",
    driver: "msnodesqlv8",
    options: { trustedConnection: true },
});

// Путь к JSON с видеообзорами
const videosPath = path.join(__dirname, "../../data/resultVideos.json");

async function importVideos() {
    try {
        await pool.connect();
        console.log("Подключено к базе данных");

        if (!fs.existsSync(videosPath)) {
            console.error("Файл resultVideos.json не найден");
            return;
        }

        const raw = fs.readFileSync(videosPath, "utf-8");
        const videos = JSON.parse(raw);

        // Можно очистить таблицу перед импортом
        await pool.request().query("TRUNCATE TABLE MatchVideos;");
        console.log("Таблица MatchVideos очищена");

        let inserted = 0;

        for (const video of videos) {
            try {
                // Проверяем, что матч найден в Matches
                const matchRes = await pool
                    .request()
                    .input("id", sql.VarChar, video.id)
                    .query("SELECT match_id FROM Matches WHERE match_id = @id");

                if (matchRes.recordset.length === 0) {
                    console.warn(`Пропущено: матч ${video.match} (${video.id}) не найден в Matches`);
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

                inserted++;
            } catch (err) {
                console.error(`Ошибка при вставке видео "${video.title}":`, err.message);
            }
        }

        console.log(`Импортировано ${inserted} видеообзоров в таблицу MatchVideos`);
        await pool.close();
    } catch (err) {
        console.error("Ошибка импорта:", err);
        await pool.close();
    }
}

importVideos();
