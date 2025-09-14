// src/db/import/import_matches.js
import fs from "fs";
import sql from "mssql/msnodesqlv8.js";
import dayjs from "dayjs";
import customParseFormat from "dayjs/plugin/customParseFormat.js";

dayjs.extend(customParseFormat);

const raw = fs.readFileSync("src/data/russia_khl_all.json", "utf-8");
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
        console.log("✅ Connected to DB");

        let count = 0;

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
                        `⚠️ Пропущен матч ${key} (${match.home.name} vs ${match.away.name}) — команда не найдена`
                    );
                    continue;
                }

                // дата
                let dateStr = match.date;
                if (dateStr && dateStr.match(/^\d{2}\.\d{2}\.\s/)) {
                    const year = new Date().getFullYear();
                    dateStr = dateStr.replace(/(\d{2}\.\d{2}\.)/, `$1${year} `);
                }
                const dateObj = dateStr
                    ? dayjs(dateStr, "DD.MM.YYYY HH:mm").toDate()
                    : null;

                // UPSERT
                await pool
                    .request()
                    .input("id", sql.VarChar, key)
                    .input("date", sql.DateTime, dateObj)
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
                        MERGE Matches AS target
                        USING (SELECT @id AS match_id) AS source
                        ON target.match_id = source.match_id
                        WHEN MATCHED THEN 
                            UPDATE SET 
                                match_date = @date,
                                status = @status,
                                home_team_name = @homeName,
                                home_team_id = @homeId,
                                away_team_name = @awayName,
                                away_team_id = @awayId,
                                home_score = @homeScore,
                                away_score = @awayScore
                        WHEN NOT MATCHED THEN
                            INSERT (match_id, match_date, status, home_team_name, home_team_id, away_team_name, away_team_id, home_score, away_score)
                            VALUES (@id, @date, @status, @homeName, @homeId, @awayName, @awayId, @homeScore, @awayScore);
                    `);

                count++;
            } catch (err) {
                console.error(`❌ Ошибка при вставке/обновлении матча ${key}:`, err.message);
            }
        }

        console.log(`✅ Imported/Updated ${count}/${Object.keys(matches).length} matches into DB`);
        await pool.close();
    } catch (err) {
        console.error("❌ Error:", err);
        await pool.close();
    }
}

importMatches();
