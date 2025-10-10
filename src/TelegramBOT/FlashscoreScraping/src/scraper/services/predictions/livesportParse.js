import * as cheerio from "cheerio";
import fs from "fs";
import path from "path";
import { OUTPUT_PATH } from "../../../constants/constants.js";
import { findMatchId, cleanText } from "../utils/teamMapUtils.js";
import { appendUniqueJson } from "../utils/fileUtils.js";

/**
 * Загружает HTML-страницу по указанному URL.
 * @param {string} url URL страницы для загрузки
 * @returns {Promise<string>} HTML содержимое страницы
 */
async function fetchHtml(url) {
    try {
        const res = await fetch(url, {
            headers: { "User-Agent": "Mozilla/5.0" },
            signal: AbortSignal.timeout(15000)
        });
        if (!res.ok) {
            throw new Error(`HTTP ${res.status}`);
        }
        return await res.text();
    } catch (err) {
        throw new Error(`Ошибка загрузки ${url}: ${err.message}`);
    }
}

/**
 * Парсит страницу конкретного матча и извлекает прогноз.
 */
async function parseMatchPage(url, calendar) {
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    // проверяем что матч КХЛ
    const league = $(".article-match-tour").first().text().trim();
    if (!league.includes("КХЛ"))
        return null;

    // команды
    const home = $(".article-match-info a").first().find("b").text().trim();
    const away = $(".article-match-info a").last().find("b").text().trim();

    // время матча
    const timeStr = $(".article-match-info u").first().text().trim();

    // дата матча (берём из блока lead)
    const leadText = $(".article-lead.article-lead-tips").text();
    const dateMatch = leadText.match(/(\d{1,2})\s([а-я]+)(?:\s(\d{4}))?/i);

    let matchDate = null;
    if (dateMatch && timeStr) {
        const day = parseInt(dateMatch[1], 10);
        const monthName = dateMatch[2].toLowerCase();
        const year = dateMatch[3] ? parseInt(dateMatch[3], 10) : new Date().getFullYear();

        const months = {
            января: 0, февраля: 1, марта: 2, апреля: 3, мая: 4, июня: 5,
            июля: 6, августа: 7, сентября: 8, октября: 9, ноября: 10, декабря: 11
        };

        const [h, m] = timeStr.split(":").map(Number);
        matchDate = new Date(year, months[monthName], day, h, m);
    }

    // анализ по командам
    let homeAnalysis = "";
    let awayAnalysis = "";

    $("h2").each((_, el) => {
        const header = $(el).text().replace(/«|»/g, "").trim();
        const paras = [];
        $(el).nextUntil("h2").each((_, p) => {
            if ($(p).is("p")) {
                paras.push($(p).text().trim());
            }
        });

        if (header.toLowerCase().includes(home.toLowerCase())) {
            homeAnalysis = paras.join(" ");
        } else if (header.toLowerCase().includes(away.toLowerCase())) {
            awayAnalysis = paras.join(" ");
        }
    });

    // прогноз
    const prediction = { main: null, alt: null, score: null, text: "", result: null };

    const forecastSection = $("h2:contains('Прогноз и ставка')");
    const paras = [];
    forecastSection.nextUntil("h2").each((_, el) => {
        if ($(el).is("p")) paras.push($(el).text().trim());
    });

    // собираем все абзацы, которые начинаются с "Прогноз:"
    const forecastParas = paras.filter(p => p.startsWith("Прогноз:"));

    // объединяем с пустой строкой между ними
    prediction.text = forecastParas.map(cleanText).join("\n\n");

    // ставки
    $("p:contains('Ставка')").each((i, el) => {
        const txt = $(el).text().replace("Ставка:", "").trim();
        if (i === 0) prediction.main = cleanText(txt);
        else prediction.alt = (prediction.alt ? prediction.alt + ", " : "") + cleanText(txt);
    });

    const matchId = findMatchId(home, away, calendar, matchDate);
    if (!matchId) {
        console.warn(`⚠️ Не найден ID: ${home} – ${away} (${matchDate})`);
    }

    return {
        source: "livesport",
        url,
        match: `${home} – ${away}`,
        teams: {
            home: { name: home, analysis: cleanText(homeAnalysis) },
            away: { name: away, analysis: cleanText(awayAnalysis) }
        },
        prediction,
        id: matchId,
    };
}

/**
 * Основной метод: парсит список прогнозов с livesport.ru
 */
export async function scrapePredictions() {
    const listUrl = "https://www.livesport.ru/tips/hockey/";

    let html;
    try {
        html = await fetchHtml(listUrl);
    } catch (err) {
        console.error(`Livesport недоступен: ${err.message}`);
        return [];
    }

    const $ = cheerio.load(html);

    const calendarPath = path.join(OUTPUT_PATH, "russia_khl_all.json");
    const calendar = JSON.parse(fs.readFileSync(calendarPath, "utf-8"));

    const links = [];

    // верхний блок "ставка дня"
    $("div.r_tips_t_block a.r_tips_t").each((_, el) => {
        const href = $(el).attr("href");
        if (href && href.includes("/tips/hockey/")) {
            links.push("https://www.livesport.ru" + href);
        }
    });

    // обычные прогнозы (несколько подряд)
    $("div.r_tips_l_one a, div.r_tips_l a").each((_, el) => {
        const href = $(el).attr("href");
        if (href && href.includes("/tips/hockey/") && !href.includes("/tips/express/")) {
            links.push("https://www.livesport.ru" + href);
        }
    });

    const results = [];
    for (const link of links.slice(0, 10)) {
        try {
            const data = await parseMatchPage(link, calendar);
            if (data) results.push(data); // отфильтрованные только КХЛ
        } catch (err) {
            console.error(`Ошибка при парсинге ${link}:`, err.message);
        }
    }

    const savePath = path.join(OUTPUT_PATH, "livesport.json");

    const { merged, added } = appendUniqueJson(
        savePath,
        results,
        (item) => `${item.source}_${item.id || item.match}`
    );
    console.log(`Добавлено новых прогнозов: ${added}`);
    console.log(`Прогнозы с livesport.ru сохранены в ${savePath}`);

    return merged;
}
