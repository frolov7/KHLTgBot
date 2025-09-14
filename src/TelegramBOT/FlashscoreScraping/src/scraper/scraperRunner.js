// src/scraper/scraperRunner.js
import puppeteer from "puppeteer";
import { exec } from "child_process";
import { BASE_URL, OUTPUT_PATH } from "../constants/constants.js";

import { getAllMatches, updateRecentMatches } from "./services/matches/getMatches.js";
import { handleFileType } from "../files/handle/handle.js";
import { initializeProgressbar } from "../cli/progressbar/progressbar.js";
import { start as startLoading, stop as stopLoading } from "../cli/loader/loader.js";
import { parseDate } from "./services/utils/dateUtils.js";

export default async function main(args) {
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

    const leagueUrl = `${BASE_URL}/hockey/russia/khl`;

    //
    // --- Полный парсинг (календарь + результаты)
    //
    if (args.includes("--all")) {
        startLoading();

        const matches = await getAllMatches(browser, leagueUrl);

        stopLoading();
        const total = Object.keys(matches).length;
        console.log("Найдено матчей всего:", total);

        const progressbar = initializeProgressbar(total);
        const orderedData = {};

        // --- Сначала результаты (старые → новые)
        const results = Object.entries(matches)
            .filter(([_, match]) => match.status && match.status !== "SCHEDULED")
            .sort((a, b) => parseDate(a[1].date) - parseDate(b[1].date));

        for (const [id, match] of results) {
            orderedData[id] = match;
            progressbar.increment();
        }

        // --- Потом будущие матчи (по дате вперёд)
        const upcoming = Object.entries(matches)
            .filter(([_, match]) => !match.status || match.status === "SCHEDULED")
            .sort((a, b) => parseDate(a[1].date) - parseDate(b[1].date));

        for (const [id, match] of upcoming) {
            orderedData[id] = match;
            progressbar.increment();
        }

        handleFileType(orderedData, "json", "russia_khl_all");

        progressbar.stop();
        console.info(`\n✅ Импортировано матчей: ${total}`);
        console.info(`Сохранили в: ${OUTPUT_PATH}/russia_khl_all.json\n`);

        // импорт в БД
        runImport();
    }

    //
    // --- Только обновление вчерашних и сегодняшних матчей
    //
    if (args.includes("--update")) {
        startLoading();

        const updatedMatches = await updateRecentMatches(browser);

        stopLoading();
        console.info(`\n✅ Обновили только вчерашние и сегодняшние матчи`);
        console.info(`Сохранили в: ${OUTPUT_PATH}/russia_khl_all.json\n`);

        // импорт в БД
        runImport();
    }

    await browser.close();
    console.log("✅ Браузер закрыт");
}

//
// Вызов импорта в БД
//
function runImport() {
    console.log("📥 Запускаем импорт в базу...");
    exec("node src/db/import/import_matches.js", (error, stdout, stderr) => {
        if (error) {
            console.error(`❌ Ошибка запуска импорта: ${error.message}`);
            return;
        }
        if (stderr) {
            console.error(`⚠️ STDERR: ${stderr}`);
        }
        console.log(stdout);
    });
}

// если файл запущен напрямую → вызываем main()
if (process.argv[1] && process.argv[1].endsWith("scraperRunner.js")) {
    const args = process.argv.slice(2);
    main(args);
}
