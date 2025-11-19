// src/scraper/services/predictions/stavkatvParse.js
import * as cheerio from "cheerio";
import fs from "fs";
import { FILES } from "../../../constants/constants.js";
import { findMatchId, normalizeTeamName } from "../utils/matches/teamMapUtils.js";
import { appendUniqueJson } from "../utils/core/jsonUtils.js";
import { createLogger } from "../utils/core/logger.js";
import { normalizePrediction } from "../utils/predictions/normalizePrediction.js";

const logger = createLogger("stavkatv");
const BASE_URL = "https://stavka.tv";

export { scrapePredictionsStavka as scrapePredictions };

/// <summary>
/// Загружает HTML-страницу по указанному URL.
/// </summary>
/// <param name="url">Адрес страницы для загрузки.</param>
/// <returns>HTML-код страницы в виде строки.</returns>
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
/// <param name="text">Исходная строка.</param>
/// <returns>Строка с заменёнными кавычками.</returns>
function replaceQuotes(text) {
    if (!text) return text;
    return text
        .replace(/"([^"]+)"/g, "«$1»")
        .replace(/“([^”]+)”/g, "«$1»")
        .replace(/”/g, "»")
        .replace(/“/g, "«");
}

/// <summary>
/// Проверяет исход прогноза на основе результата матча.
/// </summary>
/// <param name="prediction">Объект с прогнозом (основная ставка и т.д.).</param>
/// <param name="match">Данные матча из календаря (результат, статус).</param>
/// <returns>
/// true — прогноз верен,  
/// false — неверен,  
/// null — невозможно определить.
/// </returns>
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
/// Преобразует дату со страницы StavkaTV в объект Date.
/// </summary>
/// <param name="dateStr">Дата в формате "21 окт".</param>
/// <param name="timeStr">Время в формате "19:30".</param>
/// <returns>Объект Date или null, если не удалось преобразовать.</returns>
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

    if (!month || !timeStr || !timeStr.includes(":")) return null;

    const [h, m] = timeStr.split(":").map(Number);
    const date = new Date(year, month, day, h, m);
    return isNaN(date.getTime()) ? null : date;
}

/// <summary>
/// Парсит страницу конкретного матча со stavka.tv и извлекает прогноз.
/// </summary>
/// <param name="url">Ссылка на страницу матча.</param>
/// <returns>Объект с информацией о прогнозе.</returns>
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

    // Извлекаем текст прогнозов
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
/// Сохраняет результаты парсинга в JSON без дубликатов.
/// </summary>
/// <param name="results">Массив прогнозов.</param>
/// <returns>Количество добавленных новых прогнозов.</returns>
function saveResults(results) {
    const cleanedResults = results.map(r => {
        const { date, ...rest } = r;
        return rest;
    });

    const savePath = FILES.STAVKATV;
    const { merged, added } = appendUniqueJson(savePath, cleanedResults, i => `${i.source}_${i.id || i.match}`);

    logger.info(`Прогнозы сохранены в ${savePath}`);
    return added;
}

/// <summary>
/// Главная функция: парсит КХЛ-прогнозы со stavka.tv.
/// </summary>
/// <returns>Массив объектов прогнозов.</returns>
export async function scrapePredictionsStavka() {
    const listUrl = `${BASE_URL}/matches/ice-hockey`;
    const html = await fetchHtml(listUrl);
    const $ = cheerio.load(html);

    const calendar = JSON.parse(fs.readFileSync(FILES.KHL_MATCHES, "utf-8"));

    const results = [];
    const seen = new Set();

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
    logger.info(`Найдено ${rows.length} матчей КХЛ.`);

    for (const el of rows.toArray()) {
        const link = $(el).find("a.match-link").attr("href");
        if (!link) continue;

        const fullUrl = BASE_URL + link;
        const teams = $(el).find(".team-name");
        const home = normalizeTeamName($(teams[0]).text().trim());
        const away = normalizeTeamName($(teams[1]).text().trim());

        const dateStr = $(el).find(".event-date").text().trim();
        const timeStr = $(el).find(".event-status").text().trim();
        const matchDate = dateStr && timeStr ? parseStavkaDate(dateStr, timeStr) : null;

        const key = `${fullUrl}_${home}_${away}`;
        if (seen.has(key)) continue;
        seen.add(key);

        const matchId = findMatchId(home, away, calendar, matchDate);
        if (!matchId) {
            logger.warn(`Не найден матч для ${home} – ${away} (${dateStr} ${timeStr})`);
            continue;
        }

        logger.info(`Матч: ${home} – ${away} | matchID: ${matchId || "не найден"} | Дата: ${matchDate?.toISOString?.() || "нет даты"}`);

        try {
            const prediction = await parseMatchPage(fullUrl);

            // Нормализуем прогноз (main + alt)
            // нормализуем только main и alt
            prediction.main = normalizePrediction(prediction.main, home, away);
            prediction.alt = normalizePrediction(prediction.alt, home, away);



            // Проверка исхода матча
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

    const added = saveResults(results);
    logger.info(`Итог: добавлено новых прогнозов ${added}/${results.length}`);

    return results;
}
