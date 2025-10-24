import fs from "fs";
import dayjs from "dayjs";
import path from "path";
import { fileURLToPath } from "url";
import { exec } from "child_process";

import { BASE_URL, FILES } from "../../../constants/constants.js";
import { openPageAndNavigate, waitForSelectorSafe } from "../utils/core/pageUtils.js";
import { parseDate } from "../utils/core/dateUtils.js";
import { createLogger } from "../utils/core/logger.js";

// поддержка __dirname в ESM
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const logger = createLogger("updateMatches");

const CHECK_PREDICTIONS_SCRIPT = path.join(__dirname, "../../../db/import/importPredictions.js");

/// <summary>
/// Нормализует статус матча, приводя различные варианты статусов (например: "OT", "PEN", "ЗАВЕРШ")
/// к стандартным значениям: "FINISHED", "AFTER OVERTIME", "AFTER PENALTIES" или "SCHEDULED".
/// </summary>
/// <param name="rawStatus">Исходный текст статуса матча, полученный с сайта Flashscore.</param>
/// <param name="homeScore">Счёт домашней команды (число или null).</param>
/// <param name="awayScore">Счёт гостевой команды (число или null).</param>
/// <returns>Нормализованный статус матча в виде строки.</returns>
function normalizeStatus(rawStatus, homeScore, awayScore) {
    const status = (rawStatus || "").toUpperCase();

    if (status.includes("PEN") || status.includes("БУЛ")) return "AFTER PENALTIES";
    if (status.includes("OT") || status.includes("ОВЕР")) return "AFTER OVERTIME";
    if (status.includes("FINISHED") || status.includes("ЗАВЕРШ")) return "FINISHED";
    if (!rawStatus && homeScore !== null && awayScore !== null) return "FINISHED";

    return status || "SCHEDULED";
}

/// <summary>
/// Обновляет результаты последних матчей КХЛ (за вчера и сегодня) на основе данных с сайта Flashscore.
/// Выполняет парсинг страниц "results" и "live", обновляет локальный JSON-файл russia_khl_all.json,
/// и запускает проверку прогнозов через отдельный скрипт.
/// </summary>
/// <param name="browser">Экземпляр Puppeteer, используемый для парсинга страниц.</param>
/// <returns>Обновлённый объект матчей после синхронизации данных.</returns>
export const updateRecentMatches = async (browser) => {
    const startTime = Date.now(); // начало замера времени

    logger.info("=== Обновление матчей КХЛ ===");

    const filePath = FILES.KHL_MATCHES;
    if (!fs.existsSync(filePath)) {
        throw new Error("Файл russia_khl_all.json не найден. Сначала нужно выполнить полное обновление (--all).");
    }

    const matches = JSON.parse(fs.readFileSync(filePath, "utf-8"));
    const today = dayjs();
    const yesterday = today.subtract(1, "day");

    // === 1. Парсим завершённые (results) ===
    const resultsUrl = `${BASE_URL}/hockey/russia/khl/results`;
    const resultsPage = await openPageAndNavigate(browser, resultsUrl);
    await waitForSelectorSafe(resultsPage, ".event__match");

    const scrapedResults = await resultsPage.evaluate(() =>
        Array.from(document.querySelectorAll(".event__match")).map((el) => ({
            id: el.id?.replace("g_4_", ""),
            homeScore: el.querySelector(".event__score--home")?.innerText.trim() || null,
            awayScore: el.querySelector(".event__score--away")?.innerText.trim() || null,
            rawStatus: el.querySelector(".event__stage")?.innerText.trim() || "",
        }))
    );
    await resultsPage.close();

    // === 2. Парсим LIVE (overview) ===
    const liveUrl = `${BASE_URL}/hockey/russia/khl/`;
    const livePage = await openPageAndNavigate(browser, liveUrl);
    await waitForSelectorSafe(livePage, ".event__match");

    const scrapedLive = await livePage.evaluate(() =>
        Array.from(document.querySelectorAll(".event__match")).map((el) => ({
            id: el.id?.replace("g_4_", ""),
            homeScore: el.querySelector(".event__score--home")?.innerText.trim() || null,
            awayScore: el.querySelector(".event__score--away")?.innerText.trim() || null,
            rawStatus: el.querySelector(".event__stage")?.innerText.trim() || "",
        }))
    );
    await livePage.close();

    // === 3. Объединяем и нормализуем ===
    const allScraped = [...scrapedResults, ...scrapedLive].map((m) => ({
        id: m.id,
        status: normalizeStatus(m.rawStatus, m.homeScore, m.awayScore),
        result: {
            home: m.homeScore,
            away: m.awayScore,
        },
    }));

    // === 4. Фильтруем матчи вчера + сегодня ===
    const recentIds = Object.entries(matches)
        .filter(([_, match]) => {
            const d = dayjs(parseDate(match.date));
            return d.isSame(today, "day") || d.isSame(yesterday, "day");
        })
        .map(([id]) => id);

    const recent = allScraped.filter((m) => recentIds.includes(m.id) || m.status === "LIVE");

    if (recent.length === 0) {
        logger.info("Нет актуальных матчей для обновления.");
        return;
    }

    logger.info(`Найдено ${recent.length} актуальных матчей:`);

    const updatedIds = [];

    // === 5. Обновляем данные в календаре ===
    for (const match of recent) {
        const prev = matches[match.id];
        if (!prev) continue;

        matches[match.id] = {
            ...prev,
            status: match.status !== "SCHEDULED" ? match.status : prev.status,
            result: {
                home: match.result.home ?? prev.result.home,
                away: match.result.away ?? prev.result.away,
            },
        };

        updatedIds.push(match.id);

        logger.info(
            `Матч: ${prev.home?.name} – ${prev.away?.name} | matchID: ${match.id} | Дата: ${prev.date}`
        );
    }

    // === 6. Сохраняем ===
    fs.writeFileSync(filePath, JSON.stringify(matches, null, 2), "utf-8");

    // === 7. Проверка прогнозов ===
    logger.info("\nПроверка прогнозов...");
    exec(`node "${CHECK_PREDICTIONS_SCRIPT}"`, (error, stdout, stderr) => {
        if (error) logger.error("Ошибка при проверке прогнозов:", error.message);
        if (stderr) logger.error(stderr);
        if (stdout.trim()) logger.info(stdout.trim());
    });

    // === 8. Итог ===
    const duration = ((Date.now() - startTime) / 1000).toFixed(2);
    logger.info(`\nОбновлено матчей: ${updatedIds.length}`);
    logger.info(`✅ Обновление завершено за ${duration} сек.\n`);

    return matches;
};
