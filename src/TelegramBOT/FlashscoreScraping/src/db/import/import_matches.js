import fs from "fs";
import sql from "mssql/msnodesqlv8.js";
import dayjs from "dayjs";
import path from "path";
import { fileURLToPath } from "url";
import customParseFormat from "dayjs/plugin/customParseFormat.js";
import utc from "dayjs/plugin/utc.js";
import timezone from "dayjs/plugin/timezone.js";

dayjs.extend(customParseFormat);
dayjs.extend(utc);
dayjs.extend(timezone);

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const raw = fs.readFileSync(
    path.join(__dirname, "../../data/russia_khl_all.json"),
    "utf-8"
);
const matches = JSON.parse(raw);

const pool = new sql.ConnectionPool({
    server: "LAPTOP-34F82EN1",
    database: "TelegramBOT",
    driver: "msnodesqlv8",
    options: { trustedConnection: true },
});

async function importMatches() {
    try {
        await pool.connect();
        console.log("Connected to DB");

        // 1. Чистим таблицу
        await pool.request().query("DELETE FROM Matches");
        console.log("Все старые матчи удалены");

        let count = 0;

        // 2. Вставляем все матчи из JSON
        for (const [key, match] of Object.entries(matches)) {
            try {
                // ищем ID команд
                const homeTeamRes = await pool
                    .request()
                    .input("name", sql.VarChar, match.home.name)
                    .query("SELECT team_id FROM Teams WHERE name = @name");

                const awayTeamRes = await pool
                    .request()
                    .input("name", sql.VarChar, match.away.name)
                    .query("SELECT team_id FROM Teams WHERE name = @name");

                if (
                    homeTeamRes.recordset.length === 0 ||
                    awayTeamRes.recordset.length === 0
                ) {
                    console.warn(
                        `Пропущен матч ${key} (${match.home.name} vs ${match.away.name}) — команда не найдена`
                    );
                    continue;
                }

                // дата по МСК
                let dateObj = null;
                if (match.date) {
                    let dateStr = match.date;
                    if (dateStr.match(/^\d{2}\.\d{2}\.\s/)) {
                        const year = new Date().getFullYear();
                        dateStr = dateStr.replace(/(\d{2}\.\d{2}\.)/, `$1${year} `);
                    }
                    dateObj = dayjs.tz(dateStr, "DD.MM.YYYY HH:mm", "Europe/Moscow").toDate();
                }

                await pool
                    .request()
                    .input("id", sql.VarChar, key)
                    .input("date", sql.DateTime, dayjs(dateObj).add(3, "hour").toDate())
                    .input("status", sql.VarChar, match.status || "SCHEDULED")
                    .input("homeName", sql.VarChar, match.home.name)
                    .input("homeId", sql.Int, homeTeamRes.recordset[0].team_id)
                    .input("awayName", sql.VarChar, match.away.name)
                    .input("awayId", sql.Int, awayTeamRes.recordset[0].team_id)
                    .input(
                        "homeScore",
                        sql.Int,
                        match.result?.home ? parseInt(match.result.home, 10) : null
                    )
                    .input(
                        "awayScore",
                        sql.Int,
                        match.result?.away ? parseInt(match.result.away, 10) : null
                    )
                    .query(`
                        INSERT INTO Matches (
                            match_id, match_date, status, 
                            home_team_name, home_team_id, 
                            away_team_name, away_team_id, 
                            home_score, away_score
                        )
                        VALUES (
                            @id, @date, @status, 
                            @homeName, @homeId, 
                            @awayName, @awayId, 
                            @homeScore, @awayScore
                        );
                    `);

                count++;
            } catch (err) {
                console.error(`Ошибка при вставке матча ${key}:`, err.message);
            }
        }

        console.log(`Загружено ${count}/${Object.keys(matches).length} матчей в БД`);
        await pool.close();
    } catch (err) {
        console.error("Error:", err);
        await pool.close();
    }
}

importMatches();
