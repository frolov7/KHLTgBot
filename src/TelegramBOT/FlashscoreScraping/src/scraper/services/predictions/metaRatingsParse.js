// src/scraper/services/predictions/metaratingsParse.js
import * as cheerio from "cheerio";
import fs from "fs";
import { FILES } from "../../../constants/constants.js";
import { findMatchId, normalizeTeamName } from "../utils/matches/teamMapUtils.js";
import { appendUniqueJson } from "../utils/core/jsonUtils.js";
import { createLogger } from "../utils/core/logger.js";
import { normalizePrediction } from "../utils/predictions/normalizePrediction.js";

const logger = createLogger("metaratings");
const BASE_URL = "https://meta-ratings.kz";

export { scrapePredictionsMetaratings as scrapePredictions };

/// <summary>
/// Загружает HTML-страницу по указанному URL.
/// </summary>
/// <param name="url">Адрес страницы для загрузки.</param>
/// <returns>HTML-код страницы в виде строки.</returns>
async function fetchHtml(url) {
    const res = await fetch(url, { headers: { "User-Agent": "Mozilla/5.0" } });
    if (!res.ok) throw new Error(`Ошибка загрузки ${url}: ${res.status}`);
    return await res.text();
}

/// <summary>
/// Очищает текст от лишних пробелов, неразрывных пробелов и HTML-сущностей.
/// </summary>
/// <param name="text">Исходная строка.</param>
/// <returns>Очищенный текст.</returns>
function cleanText(text) {
    if (!text) return "";
    return text.replace(/\s+/g, " ").replace(/&nbsp;/g, " ").trim();
}

/// <summary>
/// Проверяет, прошёл ли прогноз на основе результата матча.
/// </summary>
/// <param name="prediction">Объект с прогнозом (основная ставка и т.п.).</param>
/// <param name="match">Объект матча из календаря, содержащий результат.</param>
/// <returns>
/// true — прогноз угадал,  
/// false — не угадал,  
/// null — невозможно определить.
/// </returns>
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
/// Парсит страницу отдельного матча на meta-ratings.kz и извлекает прогноз.
/// </summary>
/// <param name="url">Ссылка на страницу прогноза.</param>
/// <param name="calendar">JSON-календарь матчей.</param>
/// <param name="matchInfo">Информация о матче (home, away, matchDate).</param>
/// <returns>Объект прогноза или null, если матч не найден.</returns>
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

    // Извлекаем параграфы с текстом прогноза
    const paras = [];
    $("h2")
        .filter((_, el) => $(el).text().includes("Прогноз на матч"))
        .first()
        .nextAll("p")
        .each((_, el) => {
            const txt = cleanText($(el).text());
            if (txt) paras.push(txt);
        });

    // Основной и альтернативные прогнозы
    const altBets = [];
    for (const p of paras) {
        if (p.startsWith("Прогноз —")) {
            prediction.main = p.replace("Прогноз —", "").trim();
        } else if (p.startsWith("Ставка —")) {
            altBets.push(p.replace("Ставка —", "").trim());
        }
    }

    // Основной прогноз
    if (prediction.main) {
        prediction.main = normalizePrediction(prediction.main, home, away);
    }

    // Альтернативные ставки
    if (altBets.length) {
        prediction.alt = altBets
            .map(b => normalizePrediction(b, home, away))
            .join(", ");
    }

    prediction.text = cleanText(paras.join(" "));

    // Проверяем исход прогноза
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
/// Сохраняет результаты парсинга в JSON-файл без дубликатов.
/// </summary>
/// <param name="results">Массив объектов прогнозов.</param>
/// <returns>Количество добавленных новых прогнозов.</returns>
function saveResults(results) {
    const cleanedResults = results
        .filter(r => r.id || r.match) // защита от полностью пустых
        .map(r => {
            const { date, ...rest } = r;
            return rest;
        });

    const savePath = FILES.METARATINGS;

    // 🔧 устраняем дубликаты с null-id
    const uniqueMap = {};
    for (const r of cleanedResults) {
        const key = `${r.source}_${r.match}`;
        if (r.id) {
            // если пришёл прогноз с id — перезаписываем версию без id
            uniqueMap[key] = r;
        } else if (!uniqueMap[key]) {
            // если раньше не было — добавляем
            uniqueMap[key] = r;
        }
    }

    const dedupedResults = Object.values(uniqueMap);

    const { merged, added } = appendUniqueJson(
        savePath,
        dedupedResults,
        i => `${i.source}_${i.id || i.match}`
    );

    logger.info(`Прогнозы сохранены в ${savePath}`);
    return added;
}

/// <summary>
/// Главная функция парсера Metaratings.  
/// Собирает список матчей КХЛ, парсит каждую страницу и сохраняет результаты.
/// </summary>
/// <returns>Массив объектов с прогнозами матчей.</returns>
export async function scrapePredictionsMetaratings() {
    const listUrl = `${BASE_URL}/prognozy/hokkey/khl/`;
    const html = await fetchHtml(listUrl);
    const $ = cheerio.load(html);

    // Календарь матчей КХЛ
    const calendar = JSON.parse(fs.readFileSync(FILES.KHL_MATCHES, "utf-8"));

    const links = [];
    const seen = new Set();
    let duplicates = 0;

    $(".TipsList_TipsBox___jUgx").each((_, el) => {
        const linkEl = $(el).find("a.TipsList_TipsBoxTitle__c8YUz");
        const href = linkEl.attr("href");
        if (!href) return;

        const url = BASE_URL + href;

        // Дата и время матча
        const dateStr = $(el).find(".TipsList_TipsBoxDate__ZW4Q5").text().trim();
        const timeStr = $(el).find(".TipsList_TipsBoxClock__qCJyW").text().trim();
        let matchDate = null;
        if (dateStr && timeStr) {
            const [day, month, year] = dateStr.split(".").map(Number);
            const [hours, minutes] = timeStr.split(":").map(Number);
            matchDate = new Date(year, month - 1, day, hours, minutes);
        }

        // Названия команд
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
            logger.error(`[metaratings] Ошибка при парсинге ${url}`, err);
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

    const added = saveResults(results);
    logger.info(`Итог: добавлено новых прогнозов ${added}/${results.length}`);

    return results;
}
