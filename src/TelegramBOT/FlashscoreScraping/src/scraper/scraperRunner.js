// src/scraper/scraperRunner.js
process.stdout.write('\uFEFF');
process.stdout.setEncoding('utf8');

import puppeteer from "puppeteer";
import { exec } from "child_process";
import iconv from "iconv-lite";
import { FILES } from "../constants/constants.js";

import { updateRecentMatches } from "./services/matches/updateMatches.js";

import { scrapePredictions as scrapeBetzona } from "./services/predictions/betzonaParse.js";
import { scrapePredictions as scrapeLegalbet } from "./services/predictions/legalbetParse.js";
import { scrapePredictions as scrapeLivesport } from "./services/predictions/livesportParse.js";
import { scrapePredictions as scrapeMetaRatings } from "./services/predictions/metaRatingsParse.js";
import { scrapePredictions as scrapeStavkatv } from "./services/predictions/stavkatvParse.js";
import { scrapePredictions as scrapeVprognoze } from "./services/predictions/vprognozeParse.js";
import { scrapePredictions as scrapeVseprosport } from "./services/predictions/vseprosportParse.js";

import { scrapeKhlYoutubeVideos as scrapeKhlVideos } from "./parsers/khlYoutubeParser.js";
import { createLogger } from "./services/utils/core/logger.js";

import fs from "fs";
import path from "path";

// путь к JSON
const MATCHES_PATH = FILES.KHL_MATCHES;

// путь к validateJson.js
const VALIDATE_SCRIPT = path.join(
    process.cwd(),
    "src/scraper/services/matches/validateJson.js"
);

const IMPORT_MATCHES_SCRIPT = path.join(process.cwd(), "src/db/import/importMatches.js");
const IMPORT_PREDICTIONS_SCRIPT = path.join(process.cwd(), "src/db/import/importPredictions.js");
const IMPORT_VIDEOS_SCRIPT = path.join(process.cwd(), "src/db/import/importMatchVideos.js");

const logger = createLogger("scraperRunner");

/// <summary>
/// Главная функция запуска парсеров и сервисов обновления данных.
/// Обрабатывает аргументы командной строки (--validate, --import, --updateResults, --predictions, --resultvideos)
/// и выполняет соответствующие задачи: обновление матчей, парсинг прогнозов, загрузку видеообзоров и т.д.
/// </summary>
export default async function main(args) {
    if (args.includes("--validate")) {
        exec(`node "${VALIDATE_SCRIPT}"`, (error, stdout, stderr) => {
            if (error) {
                logger.error(`Ошибка запуска validate: ${error.message}`);
                return;
            }
            if (stderr) logger.error(stderr);
            logger.info(stdout);
        });
        return;
    }

    if (args.includes("--import")) {
        runImport(IMPORT_MATCHES_SCRIPT, "импорт матчей");
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

    // === Обновление результатов матчей ===
    if (args.includes("--updateResults")) {
        await updateRecentMatches(browser);
        console.info(`Сохранили в: ${FILES.KHL_MATCHES}`);

        runImport(IMPORT_MATCHES_SCRIPT, "импорт матчей");
    }

    // === Парсинг прогнозов ===
    if (args.includes("--predictions")) {
        const scrapers = [
            { name: "betzona", fn: scrapeBetzona },
            { name: "legalbet", fn: scrapeLegalbet },
            { name: "livesport", fn: scrapeLivesport },
            { name: "metaratings", fn: scrapeMetaRatings },
            { name: "stavkatv", fn: scrapeStavkatv },
            { name: "vprognoze", fn: scrapeVprognoze },
            { name: "vseprosport", fn: scrapeVseprosport },
        ];

        const logsMap = {};
        const resultsMap = {};

        const runWithLogger = async (name, fn) => {
            const prefix = `[${name}]`;
            logsMap[name] = [];
            const buffer = logsMap[name];

            const pushLog = (msg, isError = false) => {
                buffer.push(msg);
                if (isError) logger.error(msg);
                else logger.info(msg);
            };

            pushLog(`--- Начало парсинга: ${name} ---`);

            const localLogger = {
                log: (...args) => pushLog(`${prefix} ${args.join(" ")}`),
                info: (...args) => pushLog(`${prefix} ${args.join(" ")}`),
                warn: (...args) => pushLog(`${prefix} ⚠️ ${args.join(" ")}`),
                error: (...args) => pushLog(`${prefix} ❌ ${args.join(" ")}`, true),
                start: () => pushLog(`--- Начало парсинга: ${name} ---`),
                end: () => pushLog(`--- Завершён парсинг: ${name} ---`),
            };

            const start = Date.now();

            try {
                await fn({ logger: localLogger });
                const duration = ((Date.now() - start) / 1000).toFixed(2);
                pushLog(`✅ Завершён парсинг ${name} (${duration} сек.)\n`);
                resultsMap[name] = { status: "ok" };
            } catch (err) {
                pushLog(`❌ Ошибка в ${name}: ${err.message}`, true);
                resultsMap[name] = { status: "error", error: err };
            }
        };

        logger.info("Запускаем парсеры по очереди...\n");
        const totalStart = Date.now();

        for (const { name, fn } of scrapers) {
            logger.info(`=== ▶ Запуск парсера: ${name} ===`);
            await runWithLogger(name, fn);
        }

        const totalDuration = ((Date.now() - totalStart) / 1000).toFixed(2);
        logger.info(`✅ Парсеры прогнозов завершили работу за ${totalDuration} сек.\n`);

        runImport(IMPORT_PREDICTIONS_SCRIPT, "импорт прогнозов");
    }

    // === Видеообзоры ===
    if (args.includes("--resultvideos")) {
        logger.info("Запускаем парсинг видеообзоров КХЛ...");
        await scrapeKhlVideos();
        logger.info("Импортируем результаты в БД...");
        runImport(IMPORT_VIDEOS_SCRIPT, "импорт видеообзоров");
    }

    await browser.close();
    logger.info("Браузер закрыт");
}

/// <summary>
/// Выполняет запуск внешнего Node.js-скрипта (импорта данных) и отображает лог его выполнения.
/// Используется для импортирования матчей, прогнозов и видеообзоров в базу данных.
/// </summary>
function runImport(scriptPath, label = "импорт") {
    logger.info(`Запускаем ${label}...`);
    exec(`node "${scriptPath}"`, { encoding: "buffer" }, (error, stdout, stderr) => {
        const out = iconv.decode(stdout, "utf8");
        const err = iconv.decode(stderr, "utf8");

        if (error) {
            logger.error(`Ошибка запуска ${label}: ${error.message}`);
            return;
        }

        if (err.trim()) {
            logger.warn(`STDERR:\n${err}`);
        }

        logger.info(out);
    });
}

// Точка входа
if (process.argv[1] && process.argv[1].endsWith("scraperRunner.js")) {
    const args = process.argv.slice(2);
    main(args);
}
