// src/scraper/services/predictions/metaratingsParse.js
import * as cheerio from "cheerio";
import fs from "fs";
import path from "path";
import { OUTPUT_PATH } from "../../../constants/constants.js";
import { findMatchId, normalizeTeamName } from "../utils/teamMapUtils.js";
import { appendUniqueJson } from "../utils/fileUtils.js";
import { createLogger } from "../utils/logger.js";

const logger = createLogger("meta-ratings");
const BASE_URL = "https://meta-ratings.kz";

export { scrapePredictionsMetaratings as scrapePredictions };

/// <summary>
/// Загружает HTML-страницу по указанному URL.
/// </summary>
async function fetchHtml(url) {
    const res = await fetch(url, { headers: { "User-Agent": "Mozilla/5.0" } });
    if (!res.ok) throw new Error(`Ошибка загрузки ${url}: ${res.status}`);
    return await res.text();
}

/// <summary>
/// Очищает текст от лишних пробелов и символов.
/// </summary>
function cleanText(text) {
    if (!text) return "";
    return text.replace(/\s+/g, " ").replace(/&nbsp;/g, " ").trim();
}

/// <summary>
/// Проверяет исход прогноза на основе календаря и результата матча.
/// </summary>
function checkPrediction(prediction, match) {
    if (!match || match.status !== "FINISHED") return null;

    const home = parseInt(match.result.home, 10);
    const away = parseInt(match.result.away, 10);
    const total = home + away;
    const main = prediction.main;
    if (!main) return null;

    if (main.startsWith("ТБ")) {
        const num = parseFloat(main.replace(/[^\d.]/g, ""));
        return total > num;
    }
    if (main.startsWith("ТМ")) {
        const num = parseFloat(main.replace(/[^\d.]/g, ""));
        return total < num;
    }
    if (main === "П1") return home > away;
    if (main === "П2") return away > home;
    if (main.startsWith("ИТБ1")) {
        const num = parseFloat(main.replace(/[^\d.]/g, ""));
        return home > num;
    }
    if (main.startsWith("ИТМ1")) {
        const num = parseFloat(main.replace(/[^\d.]/g, ""));
        return home < num;
    }
    if (main.startsWith("ИТБ2")) {
        const num = parseFloat(main.replace(/[^\d.]/g, ""));
        return away > num;
    }
    if (main.startsWith("ИТМ2")) {
        const num = parseFloat(main.replace(/[^\d.]/g, ""));
        return away < num;
    }

    return null;
}

/// <summary>
/// Парсит страницу отдельного матча на meta-ratings.kz.
/// </summary>
async function parseMatchPage(url, calendar, matchInfo) {
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    const home = normalizeTeamName(matchInfo.home);
    const away = normalizeTeamName(matchInfo.away);
    const matchDate = matchInfo.matchDate || null;

    if (!matchDate) {
        logger.warn(`⏭ Пропуск ${home} – ${away}: отсутствует дата матча`);
        return null;
    }

    const matchId = findMatchId(home, away, calendar, matchDate);
    logger.info(`Матч: ${home} – ${away} | matchID: ${matchId || "не найден"} | Дата: ${matchDate.toISOString()}`);

    const prediction = {
        main: null,
        alt: null,
        text: "",
        result: null,
    };

    // Извлечение текста прогноза
    const paras = [];
    $("h2")
        .filter((_, el) => $(el).text().includes("Прогноз на матч"))
        .first()
        .nextAll("p")
        .each((_, el) => {
            const txt = cleanText($(el).text());
            if (txt) paras.push(txt);
        });

    const altBets = [];
    for (const p of paras) {
        if (p.startsWith("Прогноз —")) {
            prediction.main = p.replace("Прогноз —", "").trim();
        } else if (p.startsWith("Ставка —")) {
            altBets.push(p.replace("Ставка —", "").trim());
        }
    }
    if (altBets.length) prediction.alt = altBets.join(", ");
    prediction.text = cleanText(paras.join(" "));

    if (matchId) prediction.result = checkPrediction(prediction, calendar[matchId]);

    return {
        source: "metaratings",
        url,
        match: `${home} – ${away}`,
        date: matchDate,
        teams: {
            home: { name: home },
            away: { name: away },
        },
        prediction,
        id: matchId || null,
    };
}

/// <summary>
/// Сохраняет результаты парсинга в JSON.
/// </summary>
function saveResults(results, fileName) {
    const cleanedResults = results.map(r => {
        const { date, ...rest } = r;
        return rest;
    });

    const savePath = path.join(OUTPUT_PATH, fileName);
    const { merged, added } = appendUniqueJson(savePath, cleanedResults, i => `${i.source}_${i.id || i.match}`);

    logger.info(`Прогнозы сохранены в ${savePath}`);
    return added;
}

/// <summary>
/// Главная функция Metaratings: парсит список прогнозов, обрабатывает и сохраняет.
/// </summary>
export async function scrapePredictionsMetaratings() {
    const listUrl = `${BASE_URL}/prognozy/hokkey/khl/`;
    const html = await fetchHtml(listUrl);
    const $ = cheerio.load(html);

    const calendarPath = path.join(OUTPUT_PATH, "russia_khl_all.json");
    const calendar = JSON.parse(fs.readFileSync(calendarPath, "utf-8"));

    const links = [];
    const seen = new Set();
    let duplicates = 0;

    $(".TipsList_TipsBox___jUgx").each((_, el) => {
        const linkEl = $(el).find("a.TipsList_TipsBoxTitle__c8YUz");
        const href = linkEl.attr("href");
        if (!href) return;

        const url = BASE_URL + href;

        // дата и время
        const dateStr = $(el).find(".TipsList_TipsBoxDate__ZW4Q5").text().trim();
        const timeStr = $(el).find(".TipsList_TipsBoxClock__qCJyW").text().trim();
        let matchDate = null;
        if (dateStr && timeStr) {
            const [day, month, year] = dateStr.split(".").map(Number);
            const [hours, minutes] = timeStr.split(":").map(Number);
            matchDate = new Date(year, month - 1, day, hours, minutes);
        }

        // команды
        const title = linkEl.text().trim().replace("Прогноз на матч", "").trim();
        const [homeRaw, awayRaw] = title.split("–").map(s => s.trim());
        const home = normalizeTeamName(homeRaw.split(".")[0].trim());
        const away = normalizeTeamName(awayRaw.split(".")[0].trim());

        const key = `${url}_${home}_${away}`;
        if (seen.has(key)) {
            duplicates++;
            return;
        }
        seen.add(key);

        links.push({ url, home, away, matchDate });
    });

    logger.info(`Найдено ${links.length} матчей.`);

    const rawResults = [];
    for (const { url, home, away, matchDate } of links) {
        try {
            const data = await parseMatchPage(url, calendar, { home, away, matchDate });
            if (data) rawResults.push(data);
        } catch (err) {
            logger.error(`Ошибка при парсинге ${url}`, err);
        }
    }

    const results = Object.values(rawResults.reduce((acc, item) => {
        const key = `${item.source}_${item.id || item.match}`;
        if (!acc[key]) acc[key] = { ...item };
        else {
            const ex = acc[key];
            if (ex.prediction.alt) ex.prediction.alt += `, ${item.prediction.main}`;
            else ex.prediction.alt = item.prediction.main;
        }
        return acc;
    }, {}));

    const added = saveResults(results, "metaratings.json");
    logger.info(`Итог: добавлено новых прогнозов ${added}/${results.length}`);

    return results;
}
