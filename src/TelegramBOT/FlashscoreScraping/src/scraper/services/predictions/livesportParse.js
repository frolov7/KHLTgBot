// src/scraper/services/predictions/livesportParse.js
import * as cheerio from "cheerio";
import fs from "fs";
import path from "path";
import { OUTPUT_PATH } from "../../../constants/constants.js";
import { findMatchId, cleanText, normalizeTeamName } from "../utils/teamMapUtils.js";
import { appendUniqueJson } from "../utils/fileUtils.js";
import { createLogger } from "../utils/logger.js";

const logger = createLogger("livesport");
const BASE_URL = "https://www.livesport.ru";

export { scrapePredictionsLivesport as scrapePredictions };

/// <summary>
/// Загружает HTML-страницу по указанному URL.
/// </summary>
async function fetchHtml(url) {
    const res = await fetch(url, { headers: { "User-Agent": "Mozilla/5.0" } });
    if (!res.ok) throw new Error(`Ошибка загрузки ${url}: ${res.status}`);
    return await res.text();
}

/// <summary>
/// Парсит страницу конкретного матча и извлекает прогноз.
/// </summary>
async function parseMatchPage(url, calendar) {
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    // Проверяем, что это прогноз по КХЛ
    const league = $(".article-match-tour").first().text().trim();
    if (!league.includes("КХЛ")) return null;

    // Названия команд
    const home = normalizeTeamName($(".article-match-info a").first().find("b").text().trim());
    const away = normalizeTeamName($(".article-match-info a").last().find("b").text().trim());

    // Дата и время
    const timeStr = $(".article-match-info u").first().text().trim();
    const leadText = $(".article-lead.article-lead-tips").text();
    const dateMatch = leadText.match(/(\d{1,2})\s([а-я]+)(?:\s(\d{4}))?/i);

    // статус матча (например: "Перерыв", "Завершен", "3-й период")
    const matchStatus = $(".article-match-info span, .article-match-info b.status, .event-status")
        .first()
        .text()
        .trim();

    let matchDate = null;
    let rawDateLabel = "";
    if (dateMatch && timeStr) {
        const day = parseInt(dateMatch[1], 10);
        const monthName = dateMatch[2].toLowerCase();
        const year = dateMatch[3] ? parseInt(dateMatch[3], 10) : new Date().getFullYear();
        const months = {
            января: 0, февраля: 1, марта: 2, апреля: 3, мая: 4, июня: 5,
            июля: 6, августа: 7, сентября: 8, октября: 9, ноября: 10, декабря: 11
        };
        const [h, m] = (timeStr || "00:00").split(":").map(Number);
        matchDate = new Date(year, months[monthName], day, h, m);
        rawDateLabel = `${day} ${monthName}`;
    } else {
        rawDateLabel = dateMatch?.[0] || timeStr || "Неизвестно";
    }

    // если не удалось получить дату или матч уже идёт
    if (!matchDate || isNaN(matchDate)) {
        const statusLabel = matchStatus ? `, ${matchStatus}` : "";
        logger.warn(`Пропуск: ${home} – ${away} (${rawDateLabel} Завршен)`);
        return null;
    }

    const matchId = findMatchId(home, away, calendar, matchDate);
    logger.info(`Матч: ${home} – ${away} | matchID: ${matchId || "не найден"} | Дата: ${matchDate.toISOString()}`);

    // Анализ по командам
    let homeText = "";
    let awayText = "";

    $("h2").each((_, el) => {
        const header = $(el).text().replace(/«|»/g, "").trim();
        const paras = [];
        $(el).nextUntil("h2").each((_, p) => {
            if ($(p).is("p")) paras.push(cleanText($(p).text().trim()));
        });

        if (header.toLowerCase().includes(home.toLowerCase())) {
            homeText = paras.join(" ");
        } else if (header.toLowerCase().includes(away.toLowerCase())) {
            awayText = paras.join(" ");
        }
    });

    // Общий прогноз
    const forecastHeader = $("h2:contains('Прогноз')");
    let commonText = "";
    if (forecastHeader.length) {
        forecastHeader.nextUntil("h2").each((_, el) => {
            if ($(el).is("p")) {
                const txt = cleanText($(el).text().trim());
                if (txt) commonText += txt + " ";
            }
        });
    }

    // Основная ставка
    let mainBet = null;
    $("p:contains('Ставка')").each((_, el) => {
        const txt = $(el).text().replace(/Ставка:/i, "").trim();
        if (!mainBet) mainBet = txt;
    });

    return {
        source: "livesport",
        url,
        match: `${home} – ${away}`,
        date: matchDate,
        teams: {
            home: { name: home, text: homeText },
            away: { name: away, text: awayText },
        },
        prediction: {
            main: mainBet,
            text: commonText,
            alt: null,
            result: null,
        },
        id: matchId,
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
/// Главная функция Livesport: парсит список прогнозов и сохраняет их.
/// </summary>
export async function scrapePredictionsLivesport() {
    const listUrl = `${BASE_URL}/tips/hockey/`;
    const html = await fetchHtml(listUrl);
    const $ = cheerio.load(html);

    const calendarPath = path.join(OUTPUT_PATH, "russia_khl_all.json");
    const calendar = JSON.parse(fs.readFileSync(calendarPath, "utf-8"));

    const links = [];
    const seen = new Set();
    let duplicates = 0;

    // Верхний блок "ставка дня"
    $("div.r_tips_t_block a.r_tips_t").each((_, el) => {
        const href = $(el).attr("href");
        if (href && href.includes("/tips/hockey/")) {
            const full = BASE_URL + href;
            if (seen.has(full)) duplicates++;
            else { seen.add(full); links.push(full); }
        }
    });

    // Основные прогнозы
    $("div.r_tips_l_one a, div.r_tips_l a").each((_, el) => {
        const href = $(el).attr("href");
        if (href && href.includes("/tips/hockey/") && !href.includes("/express/")) {
            const full = BASE_URL + href;
            if (seen.has(full)) duplicates++;
            else { seen.add(full); links.push(full); }
        }
    });

    logger.info(`Найдено ${links.length} матчей.`);

    const rawResults = [];
    for (const url of links) {
        try {
            const data = await parseMatchPage(url, calendar);
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

    const added = saveResults(results, "livesport.json");
    logger.info(`Итог: добавлено новых прогнозов ${added}/${results.length}`);

    return results;
}