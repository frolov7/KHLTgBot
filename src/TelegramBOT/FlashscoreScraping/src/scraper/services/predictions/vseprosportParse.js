// src/scraper/services/predictions/vseprosportParse.js
import * as cheerio from "cheerio";
import fs from "fs";
import path from "path";
import { OUTPUT_PATH } from "../../../constants/constants.js";
import {
    normalizeTeamName,
    findMatchId
} from "../utils/teamMapUtils.js";
import { appendUniqueJson } from "../utils/fileUtils.js";
import { createLogger } from "../utils/logger.js";

const logger = createLogger("vseprosport");
const BASE_URL = "https://www.vseprosport.kz";

export { scrapePredictionsVseprosport as scrapePredictions };

/// <summary>
/// Загружает HTML-страницу по указанному URL.
/// </summary>
async function fetchHtml(url) {
    const res = await fetch(url, { headers: { "User-Agent": "Mozilla/5.0" } });
    return await res.text();
}

/// <summary>
/// Очищает текст от лишних слов и коэффициентов.
/// </summary>
function cleanText(text) {
    if (!text) return "";
    return text
        .replace(/\s*с коэффициентом\s*[\d.,]+/gi, "")
        .replace(/\s*за\s*[\d.,]+/gi, "")
        .replace(/([.!?])\s{2,}/g, "$1 ")
        .replace(/\s+/g, " ")
        .trim();
}

/// <summary>
/// Парсит страницу конкретного прогноза vseprosport.kz.
/// </summary>
async function parseMatchPage(url, calendar) {
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    // названия команд
    let home = $("#prediction-teams-1 p.h3").first().text().trim();
    let away = $("#prediction-teams-2 p.h3").first().text().trim();

    home = normalizeTeamName(home);
    away = normalizeTeamName(away);

    const match = `${home} – ${away}`;

    // дата
    const matchDateStr = $("time.matchdate").attr("datetime");
    const matchDate = matchDateStr ? new Date(matchDateStr) : null;
    if (!matchDate || isNaN(matchDate.getTime())) return null;

    const matchId = findMatchId(home, away, calendar, matchDate);
    logger.info(`Матч: ${home} – ${away} | matchID: ${matchId || "не найден"} | Дата: ${matchDate.toISOString()}`);

    // основной прогноз
    let mainBet = $(".bonus-item-bet span.fw-medium").first().text().trim();
    mainBet = cleanText(mainBet);

    // полный текст прогноза
    let textBlock = $("#prediction-section .default-content").first().find("p")
        .map((_, el) => $(el).text())
        .get()
        .join(" ");
    textBlock = cleanText(textBlock);

    // текущая форма (анализ)
    let homeForm = $("#prediction-teams-1").next(".default-content").text().replace("Текущая форма", "").trim();
    let awayForm = $("#prediction-teams-2").next(".default-content").text().replace("Текущая форма", "").trim();

    homeForm = homeForm.replace(/\s+/g, " ");
    awayForm = awayForm.replace(/\s+/g, " ");

    return {
        source: "vseprosport",
        url,
        match,
        date: matchDate,
        teams: {
            home: { name: home, text: homeForm },
            away: { name: away, text: awayForm },
        },
        prediction: {
            main: mainBet || null,
            text: textBlock || null,
            alt: null,
            result: null,
        },
        id: matchId,
    };
}

/// <summary>
/// Сохраняет результаты парсинга в JSON.
/// Удаляет поле date перед записью.
/// </summary>
function saveResults(results, fileName) {
    const cleanedResults = results.map(r => {
        const { date, ...rest } = r;
        return rest;
    });

    const savePath = path.join(OUTPUT_PATH, fileName);
    const { merged, added } = appendUniqueJson(
        savePath,
        cleanedResults,
        i => `${i.source}_${i.id || i.match}`
    );

    logger.info(`Прогнозы сохранены в ${savePath}`);
    return added;
}

/// <summary>
/// Главная функция парсера Vseprosport.
/// </summary>
export async function scrapePredictionsVseprosport() {
    const listUrl = `${BASE_URL}/news/hockey`;
    const html = await fetchHtml(listUrl);
    const $ = cheerio.load(html);

    const calendarPath = path.join(OUTPUT_PATH, "russia_khl_all.json");
    const calendar = JSON.parse(fs.readFileSync(calendarPath, "utf-8"));

    const links = [];
    const seen = new Set();
    let duplicates = 0;

    $("#forecast-list-ajax .forecast").each((_, el) => {
        const type = $(el).find(".forecast-body .headgrey").first().text();
        if (type.includes("KHL")) {
            const href = $(el).find("a").attr("href");
            if (!href) return;
            const full = BASE_URL + href;
            if (seen.has(full)) {
                duplicates++;
                return;
            }
            seen.add(full);
            links.push(full);
        }
    });

    logger.info(`Найдено ${links.length} матчей.`);

    const rawResults = [];
    for (const link of links) {
        try {
            const data = await parseMatchPage(link, calendar);
            if (data) rawResults.push(data);
        } catch (err) {
            logger.error(`Ошибка при парсинге ${link}`, err);
        }
    }

    const results = Object.values(
        rawResults.reduce((acc, item) => {
            const key = `${item.source}_${item.id || item.match}`;
            if (!acc[key]) acc[key] = { ...item };
            else {
                const ex = acc[key];
                if (ex.prediction.alt) ex.prediction.alt += `, ${item.prediction.main}`;
                else ex.prediction.alt = item.prediction.main;
            }
            return acc;
        }, {})
    );

    const added = saveResults(results, "vseprosport.json");
    logger.info(`Итог: добавлено новых прогнозов ${added}/${results.length}`);

    return results;
}
