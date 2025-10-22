// src/scraper/services/predictions/stavkatvParse.js
import * as cheerio from "cheerio";
import fs from "fs";
import path from "path";
import { OUTPUT_PATH } from "../../../constants/constants.js";
import { findMatchId, normalizeTeamName } from "../utils/teamMapUtils.js";
import { appendUniqueJson } from "../utils/fileUtils.js";
import { createLogger } from "../utils/logger.js";

const logger = createLogger("stavkatv");
const BASE_URL = "https://stavka.tv";

export { scrapePredictionsStavka as scrapePredictions };

/// <summary>
/// Загружает HTML-страницу по указанному URL.
/// </summary>
async function fetchHtml(url) {
    const res = await fetch(url, {
        headers: {
            "User-Agent":
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36",
        },
    });
    if (!res.ok) throw new Error(`Ошибка загрузки ${url}: ${res.status}`);
    return await res.text();
}

/// <summary>
/// Заменяет кавычки на русские «ёлочки».
/// </summary>
function replaceQuotes(text) {
    if (!text) return text;
    return text
        .replace(/"([^"]+)"/g, "«$1»")
        .replace(/“([^”]+)”/g, "«$1»")
        .replace(/”/g, "»")
        .replace(/“/g, "«");
}

/// <summary>
/// Проверяет исход прогноза на основе результата из календаря.
/// </summary>
function checkPrediction(prediction, match) {
    if (!match || match.status !== "FINISHED") return null;

    const home = parseInt(match.result.home, 10);
    const away = parseInt(match.result.away, 10);
    const total = home + away;
    const main = prediction.main;
    if (!main) return null;

    if (main.startsWith("ТБ")) return total > parseFloat(main.replace(/[^\d.]/g, ""));
    if (main.startsWith("ТМ")) return total < parseFloat(main.replace(/[^\d.]/g, ""));
    if (main === "П1") return home > away;
    if (main === "П2") return away > home;
    if (main.startsWith("ИТБ1")) return home > parseFloat(main.replace(/[^\d.]/g, ""));
    if (main.startsWith("ИТМ1")) return home < parseFloat(main.replace(/[^\d.]/g, ""));
    if (main.startsWith("ИТБ2")) return away > parseFloat(main.replace(/[^\d.]/g, ""));
    if (main.startsWith("ИТМ2")) return away < parseFloat(main.replace(/[^\d.]/g, ""));
    return null;
}

/// <summary>
/// Преобразует дату из блоков сайта в объект Date.
/// </summary>
function parseStavkaDate(dateStr, timeStr) {
    const months = {
        янв: 0, фев: 1, мар: 2, апр: 3, май: 4, июн: 5,
        июл: 6, авг: 7, сен: 8, окт: 9, ноя: 10, дек: 11,
    };

    if (!dateStr) return null;

    const [dayStr, monStr] = dateStr.split(" ");
    const day = parseInt(dayStr, 10);
    const month = months[monStr?.toLowerCase?.()] ?? null;
    const year = new Date().getFullYear();

    // если нет времени или месяц неизвестен — возвращаем null
    if (!month || !timeStr || !timeStr.includes(":")) return null;

    const [h, m] = timeStr.split(":").map(Number);
    const date = new Date(year, month, day, h, m);
    return isNaN(date.getTime()) ? null : date;
}

/// <summary>
/// Парсит страницу конкретного матча со stavka.tv.
/// </summary>
async function parseMatchPage(url) {
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    const prediction = {
        main: null,
        alt: null,
        score: null,
        text: "",
        result: null,
    };

    // Основная ставка
    const outcome = $(".EditorsChoice .choice .outcome").first().text().trim();
    if (outcome) prediction.main = replaceQuotes(outcome);

    // Извлечение текстов прогнозов
    const texts = [];
    $("li, p, h2, h3").each((_, el) => {
        const t = $(el).text().trim();
        if (/^(Основной прогноз|Прогноз на|Прогноз с)/i.test(t)) {
            texts.push(replaceQuotes(t));
            const scoreMatch = t.match(/(\d+:\d+)/);
            if (scoreMatch) prediction.score = scoreMatch[1];

            if (/^Прогноз на тотал/i.test(t)) {
                const boldText = $(el).find("strong, b").last().text().trim();
                if (boldText) {
                    const cleanAlt = boldText.replace(/\s+за\s+[0-9.,]+$/, "").trim();
                    prediction.alt = replaceQuotes(cleanAlt);
                }
            }
        }
    });

    prediction.text = texts.join("\n\n");
    return prediction;
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
/// Главная функция: парсит КХЛ прогнозы со stavka.tv.
/// </summary>
export async function scrapePredictionsStavka() {
    const listUrl = `${BASE_URL}/matches/ice-hockey`;
    const html = await fetchHtml(listUrl);
    const $ = cheerio.load(html);

    const calendarPath = path.join(OUTPUT_PATH, "russia_khl_all.json");
    const calendar = JSON.parse(fs.readFileSync(calendarPath, "utf-8"));

    const results = [];
    const seen = new Set();
    let duplicates = 0;

    const khlSection = $("section.MatchesTable")
        .filter((_, el) => {
            const href = $(el).find(".MatchesTableHeader .title a.title-link").attr("href");
            return href && href.includes("russia-khl");
        })
        .first();

    if (!khlSection.length) {
        logger.error("Не найдена секция КХЛ на странице!");
        return [];
    }

    const rows = khlSection.find(".MatchesRow");
    const totalRows = rows.length;
    logger.info(`Найдено ${totalRows} матчей КХЛ.`);

    for (const el of rows.toArray()) {
        const link = $(el).find("a.match-link").attr("href");
        if (!link) continue;

        const fullUrl = BASE_URL + link;
        const teams = $(el).find(".team-name");
        const home = normalizeTeamName($(teams[0]).text().trim());
        const away = normalizeTeamName($(teams[1]).text().trim());

        const dateStr = $(el).find(".event-date").text().trim();
        const timeStr = $(el).find(".event-status").text().trim();

        let matchDate = null;
        if (dateStr && timeStr) matchDate = parseStavkaDate(dateStr, timeStr);

        const key = `${fullUrl}_${home}_${away}`;
        if (seen.has(key)) {
            duplicates++;
            continue;
        }
        seen.add(key);

        const matchId = findMatchId(home, away, calendar, matchDate);
        if (!matchId) {
            logger.warn(`Не найден матч для ${home} – ${away} (${dateStr} ${timeStr})`);
            continue;
        }

        logger.info(`Матч: ${home} – ${away} | matchID: ${matchId || "не найден"} | Дата: ${matchDate?.toISOString?.() || "нет даты"}`);

        try {
            const prediction = await parseMatchPage(fullUrl);
            prediction.result = checkPrediction(prediction, calendar[matchId]);

            results.push({
                source: "stavkatv",
                url: fullUrl,
                match: `${home} – ${away}`,
                date: matchDate,
                teams: {
                    home: { name: home },
                    away: { name: away },
                },
                prediction,
                id: matchId,
            });
        } catch (err) {
            logger.error(`Ошибка при парсинге ${fullUrl}`, err);
        }
    }

    const added = saveResults(results, "stavkatv.json");
    logger.info(`Итог: добавлено новых прогнозов ${added}/${results.length}`);

    return results;
}
