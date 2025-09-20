// src/scraper/scraperRunner.js
import puppeteer from "puppeteer";
import { exec } from "child_process";
import { OUTPUT_PATH } from "../constants/constants.js";

import { updateRecentMatches } from "./services/matches/getMatches.js";
import { start as startLoading, stop as stopLoading } from "../cli/loader/loader.js";

import fs from "fs";
import path from "path";

// путь к JSON
const MATCHES_PATH = path.join(OUTPUT_PATH, "russia_khl_all.json");

// путь к validateJson.js
const VALIDATE_SCRIPT = path.join(
    process.cwd(),
    "src/scraper/services/matches/validateJson.js"
);

// путь к import_matches.js
const IMPORT_SCRIPT = path.join(
    process.cwd(),
    "src/db/import/import_matches.js"
);

// Проверка и обновление статуса матчей на LIVE
async function updateLiveStatuses(browser) {
    if (!fs.existsSync(MATCHES_PATH)) return;

    const raw = fs.readFileSync(MATCHES_PATH, "utf-8");
    const matches = JSON.parse(raw);

    const page = await browser.newPage();
    await page.goto("https://www.flashscorekz.com/hockey/russia/khl/", {
        waitUntil: "domcontentloaded",
        timeout: 30000,
    });

    const liveMatches = await page.evaluate(() => {
        const rows = document.querySelectorAll(".event__match");
        const result = {};

        rows.forEach(row => {
            const id = row.getAttribute("id")?.replace("g_4_", "");
            if (!id) return;

            const statusEl = row.querySelector(".event__stage");
            const homeScoreEl = row.querySelector(".event__score--home");
            const awayScoreEl = row.querySelector(".event__score--away");

            const statusText = statusEl ? statusEl.textContent.trim() : "";
            const homeScore = homeScoreEl ? homeScoreEl.textContent.trim() : null;
            const awayScore = awayScoreEl ? awayScoreEl.textContent.trim() : null;

            result[id] = {
                status: statusText,
                homeScore,
                awayScore
            };
        });

        return result;
    });

    let updated = 0;

    for (const [id, match] of Object.entries(matches)) {
        if (!liveMatches[id]) continue;

        const { status, homeScore, awayScore } = liveMatches[id];

        if (homeScore !== null && awayScore !== null) {
            matches[id].result.home = homeScore;
            matches[id].result.away = awayScore;
        }

        if (
            status.includes("1") ||
            status.includes("2") ||
            status.includes("3") ||
            status.includes("OT") ||
            status.includes("Бул") ||
            status.toUpperCase().includes("LIVE")
        ) {
            matches[id].status = "LIVE";
            console.log(`⚡ ${match.home.name} vs ${match.away.name} → LIVE (${status})`);
            updated++;
        } else if (!status && homeScore !== null && awayScore !== null) {
            matches[id].status = "FINISHED";
            console.log(`${match.home.name} vs ${match.away.name} → FINISHED ${homeScore}:${awayScore}`);
            updated++;
        } else if (status.includes("После буллитов")) {
            matches[id].status = "AFTER PENALTIES";
            console.log(`${match.home.name} vs ${match.away.name} → AFTER PENALTIES`);
            updated++;
        } else if (status.includes("После овертайма")) {
            matches[id].status = "AFTER OVERTIME";
            console.log(`${match.home.name} vs ${match.away.name} → AFTER OVERTIME`);
            updated++;
        }
    }

    await page.close();

    if (updated > 0) {
        fs.writeFileSync(MATCHES_PATH, JSON.stringify(matches, null, 2), "utf-8");
        console.log(`Обновили ${updated} матч(ей) до статуса LIVE`);
    } else {
        console.log("Нет матчей для обновления статуса LIVE");
    }
}

export default async function main(args) {
    if (args.includes("--validate")) {
        exec(`node "${VALIDATE_SCRIPT}"`, (error, stdout, stderr) => {
            if (error) {
                console.error(`Ошибка запуска validate: ${error.message}`);
                return;
            }
            if (stderr) console.error(stderr);
            console.log(stdout);
        });
        return;
    }

    if (args.includes("--import")) {
        exec(`node "${IMPORT_SCRIPT}"`, (error, stdout, stderr) => {
            if (error) {
                console.error(`Ошибка запуска import: ${error.message}`);
                return;
            }
            if (stderr) console.error(stderr);
            console.log(stdout);
        });
        return;
    }

    const browser = await puppeteer.launch({
        headless: "new",
        executablePath: puppeteer.executablePath(),
        defaultViewport: null,
        args: [
            "--no-sandbox",
            "--disable-setuid-sandbox",
            "--disable-dev-shm-usage",
            "--disable-gpu",
        ],
    });

    if (args.includes("--update")) {
        startLoading();

        await updateRecentMatches(browser);
        await updateLiveStatuses(browser);

        stopLoading();
        console.info(`\nОбновили только вчерашние и сегодняшние матчи`);
        console.info(`Сохранили в: ${OUTPUT_PATH}/russia_khl_all.json\n`);

        runImport();
    }

    await browser.close();
    console.log("Браузер закрыт");
}

function runImport() {
    console.log("Запускаем импорт в базу...");
    exec(`node "${IMPORT_SCRIPT}"`, (error, stdout, stderr) => {
        if (error) {
            console.error(`Ошибка запуска импорта: ${error.message}`);
            return;
        }
        if (stderr) {
            console.error(`STDERR: ${stderr}`);
        }
        console.log(stdout);
    });
}

if (process.argv[1] && process.argv[1].endsWith("scraperRunner.js")) {
    const args = process.argv.slice(2);
    main(args);
}
