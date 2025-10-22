// src/scraper/services/predictions/vprognozeParse.js
import * as cheerio from "cheerio";
import fs from "fs";
import path from "path";
import { OUTPUT_PATH } from "../../../constants/constants.js";
import {
    TEAM_MAP,
    normalizeTeamName,
    findMatchId
} from "../utils/teamMapUtils.js";
import { appendUniqueJson } from "../utils/fileUtils.js";
import { createLogger } from "../utils/logger.js";

const logger = createLogger("vprognoze");
const BASE_URL = "https://vprognoze.kz/user/Андрей+Шарафутдинов/";

export { scrapePredictionsVprognoze as scrapePredictions };

/// <summary>
/// Загружает HTML-страницу по указанному URL.
/// </summary>
async function fetchHtml(url) {
    const res = await fetch(url, { headers: { "User-Agent": "Mozilla/5.0" } });
    return await res.text();
}

/// <summary>
/// Очищает текст от лишних пробелов и мусорных фраз.
/// </summary>
function cleanText(text) {
    if (!text) return "";
    return text
        .replace(/\s+/g, " ")
        .replace(/\n+/g, " ")
        .replace(/ +/g, " ")
        .replace(/Другие матчи КХЛ.*$/i, "")
        .trim();
}

/// <summary>
/// Удаляет лишние блоки (статистика, очные встречи и т.д.).
/// </summary>
function removeGarbage(text) {
    if (!text) return "";
    return text
        .replace(/📊.*?(?=Magnitogorsk|Nizhny|Lada|Sochi|Bars|Barys|Spartak|SKA|$)/gis, "")
        .replace(/🤝 Очные встречи.*?(?=Magnitogorsk|Nizhny|Lada|Sochi|Bars|Barys|Spartak|SKA|$)/gis, "")
        .replace(/Другие матчи КХЛ.*$/gis, "")
        .replace(/\s+/g, " ")
        .trim();
}

/// <summary>
/// Парсит русскую дату формата "22 окт 19:00".
/// Возвращает объект Date или null, если матч завершён.
/// </summary>
function parseRuDateVprognoze(dateStr, timeStr) {
    if (!dateStr || dateStr.includes("Завершен")) return null;

    const months = {
        янв: 0, фев: 1, мар: 2, апр: 3, май: 4, июн: 5,
        июл: 6, авг: 7, сен: 8, окт: 9, ноя: 10, дек: 11
    };

    const parts = dateStr.split(" ");
    if (parts.length === 2) {
        const [dayStr, monthStr] = parts;
        const day = parseInt(dayStr, 10);
        const month = months[monthStr?.toLowerCase?.()];
        const [hours, minutes] = (timeStr || "00:00").split(":").map(Number);
        const year = new Date().getFullYear();

        if (!isNaN(day) && month !== undefined) {
            const d = new Date(year, month, day, hours, minutes);
            if (!isNaN(d.getTime())) return d;
        }
    }
    return null;
}

/// <summary>
/// Парсит страницу отдельного прогноза на vprognoze.kz.
/// </summary>
async function parseMatchPage(url, calendar) {
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    const names = $(".v3-top-forecast-header__match-name span")
        .map((_, el) => normalizeTeamName($(el).text()))
        .get();

    if (names.length !== 2) return null;

    const home = names[0];
    const away = names[1];
    const match = `${home} – ${away}`;

    const spans = $(".v3-top-forecast-header__match-timer span");
    const timeStr = spans.first().text().trim();
    const dateStr = spans.last().text().trim();
    const matchDate = parseRuDateVprognoze(dateStr, timeStr);
    if (!matchDate) return null;

    const matchId = findMatchId(home, away, calendar, matchDate);
    logger.info(`Матч: ${home} – ${away} | matchID: ${matchId || "не найден"} | Дата: ${matchDate.toISOString()}`);

    let homeText = "";
    let awayText = "";
    let commonText = "";
    let mainBet = null;
    let altBet = null;
    let score = null;

    // анализ по командам
    const homeHeader = $("h3").filter((_, el) =>
        $(el).text().trim().toLowerCase().includes(home.toLowerCase())
    );
    const awayHeader = $("h3").filter((_, el) =>
        $(el).text().trim().toLowerCase().includes(away.toLowerCase())
    );

    if (homeHeader.length) {
        const textParts = [];
        homeHeader.nextUntil("h3").each((_, el) => {
            if ($(el).is("p")) textParts.push($(el).text().trim());
        });
        homeText = removeGarbage(cleanText(textParts.join(" ")));
    }

    if (awayHeader.length) {
        const textParts = [];
        awayHeader.nextUntil("h3").each((_, el) => {
            if ($(el).is("p")) textParts.push($(el).text().trim());
        });
        awayText = removeGarbage(cleanText(textParts.join(" ")));
    }

    // общий прогноз (текст)
    const forecastHeader = $("h3").filter((_, el) =>
        $(el).text().trim().startsWith("Прогноз на матч")
    );

    if (forecastHeader.length) {
        let sibling = forecastHeader.next();
        while (sibling.length && sibling.is("p")) {
            const txt = sibling.text().trim();
            if (/^(✅|💡|📊)/.test(txt)) break;
            commonText += txt + " ";
            sibling = sibling.next();
        }
    }

    // прогнозы
    $(".v3-forecast-card-description__text p").each((_, el) => {
        const txt = $(el).text().trim();
        if (txt.startsWith("✅ Основной прогноз")) {
            mainBet = cleanText(txt.replace("✅ Основной прогноз:", ""));
        } else if (txt.startsWith("💡 Альтернатива")) {
            altBet = cleanText(txt.replace("💡 Альтернатива:", ""));
        } else if (txt.startsWith("📊 Примерный счёт")) {
            score = cleanText(txt.replace("📊 Примерный счёт:", ""));
        }
    });

    return {
        source: "vprognoze",
        url,
        match,
        date: matchDate,
        teams: {
            home: { name: home, text: homeText },
            away: { name: away, text: awayText },
        },
        prediction: {
            main: mainBet,
            text: commonText.trim(),
            alt: altBet,
            result: score,
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
    const { merged, added } = appendUniqueJson(
        savePath,
        cleanedResults,
        i => `${i.source}_${i.id || i.match}`
    );

    logger.info(`Прогнозы сохранены в ${savePath}`);
    return added;
}

/// <summary>
/// Главная функция парсера Vprognoze.
/// </summary>
export async function scrapePredictionsVprognoze() {
    const html = await fetchHtml(BASE_URL);
    const $ = cheerio.load(html);

    const calendarPath = path.join(OUTPUT_PATH, "russia_khl_all.json");
    const calendar = JSON.parse(fs.readFileSync(calendarPath, "utf-8"));

    const links = [];
    const seen = new Set();
    let duplicates = 0;

    $(".mini-tip-list .mini-tip").each((_, el) => {
        const league = $(el).find(".mini-tip__league").text().trim();
        if (!/КХЛ/i.test(league)) return; // берём только КХЛ

        const href = $(el).find(".mini-tip__teams").attr("href");
        const dayStr = $(el).find(".ui-date__day").text().trim(); 
        const timeStr = $(el).find(".ui-date__hour").text().trim();
        if (!href || !dayStr) return;

        // преобразуем "22-10" → дата текущего года
        const [day, month] = dayStr.split("-").map(Number);
        const now = new Date();
        const matchDate = new Date(now.getFullYear(), month - 1, day);
        matchDate.setHours(...(timeStr.split(":").map(Number)));

        // фильтр — только сегодня и завтра
        const tomorrow = new Date(now);
        tomorrow.setDate(now.getDate() + 1);
        if (matchDate < new Date(now.setHours(0, 0, 0, 0)) || matchDate > new Date(tomorrow.setHours(23, 59, 59, 999))) return;

        if (seen.has(href)) return;
        seen.add(href);

        // добавляем именно строку, не объект
        links.push(href);
    });

    logger.info(`Найдено ${links.length} матчей.`);

    // дальше всё как было:
    const rawResults = [];
    for (const href of links) {
        try {
            const data = await parseMatchPage(href, calendar);
            if (data) rawResults.push(data);
        } catch (err) {
            logger.error(`Ошибка при парсинге ${href}`, err);
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

    const added = saveResults(results, "vprognoze.json");
    logger.info(`Итог: добавлено новых прогнозов ${added}/${results.length}`);

    return results;
}
