import * as cheerio from "cheerio";
import fs from "fs";
import path from "path";
import { OUTPUT_PATH } from "../../../constants/constants.js";
import { findMatchId } from "../utils/teamMapUtils.js";
import { appendUniqueJson } from "../utils/fileUtils.js";

const BASE_URL = "https://meta-ratings.kz";

// загрузка календаря
const calendarPath = path.join(OUTPUT_PATH, "russia_khl_all.json");
const calendar = JSON.parse(fs.readFileSync(calendarPath, "utf-8"));

/**
 * Проверяет прогноз на основе фактического результата матча.
 * @param {Object} prediction Объект прогноза
 * @param {Object} match Данные матча из календаря
 * @returns {boolean|null} Результат проверки (true/false) или null, если проверить нельзя
 */
function checkPrediction(prediction, match) {
    if (!match || match.status !== "FINISHED") return null;

    const home = parseInt(match.result.home, 10);
    const away = parseInt(match.result.away, 10);
    const total = home + away;

    const main = prediction.main;
    if (!main) return null;

    // Общие тоталы
    if (main.startsWith("ТБ")) {
        const num = parseFloat(main.replace(/[^\d.]/g, ""));
        return total > num;
    }
    if (main.startsWith("ТМ")) {
        const num = parseFloat(main.replace(/[^\d.]/g, ""));
        return total < num;
    }

    // Победы
    if (main === "П1") return home > away;
    if (main === "П2") return away > home;

    // Индивидуальные тоталы
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

/**
 * Загружает HTML по указанному URL.
 * @param {string} url Адрес страницы
 * @returns {Promise<string>} HTML содержимое
 */
async function fetchHtml(url) {
    const res = await fetch(url, {
        headers: {
            "User-Agent":
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36",
        },
    });
    return await res.text();
}

/**
 * Парсит страницу отдельного матча с meta-ratings.kz.
 * @param {string} url URL страницы матча
 * @returns {Promise<Object>} Объект прогноза
 */
async function parseMatchPage(url) {
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    const h2 = $("h2").filter((_, el) => $(el).text().includes("Прогноз на матч")).first().text().trim();
    let match = h2.replace("Прогноз на матч", "").trim();
    match = match.replace(/\d{1,2}\s[а-я]+\s?\d{0,4}\s?(года)?/gi, "").trim();
    const [home, away] = match.split("–").map(s => s.trim());

    let prediction = {
        main: null,
        alt: null,
        score: null,
        text: "",
        result: null,
    };

    const paras = [];
    $("h2").filter((_, el) => $(el).text().includes("Прогноз на матч"))
        .first()
        .nextAll("p")
        .each((_, el) => {
            paras.push($(el).text().trim());
        });

    const altBets = [];
    for (const p of paras) {
        if (p.startsWith("Прогноз —")) {
            prediction.main = p.replace("Прогноз —", "").trim();
        }
        if (p.startsWith("Ставка —")) {
            altBets.push(p.replace("Ставка —", "").trim());
        }
    }
    if (altBets.length) prediction.alt = altBets.join(", ");

    prediction.text = paras.join("\n\n");

    const matchId = findMatchId(home, away, calendar);
    if (matchId) {
        prediction.result = checkPrediction(prediction, calendar[matchId]);
    }

    return {
        source: "meta-ratings.kz",
        match: `${home} – ${away}`,
        teams: {
            home: { name: home },
            away: { name: away },
        },
        prediction,
        id: matchId || null,
    };
}

/**
 * Основной запуск парсинга прогнозов с meta-ratings.kz
 * @returns {Promise<Array>} Массив прогнозов
 */
export async function scrapePredictions() {
    const listUrl = `${BASE_URL}/prognozy/hokkey/khl/`;
    const html = await fetchHtml(listUrl);
    const $ = cheerio.load(html);

    const links = [];
    $("a.TipsList_TipsBoxTitle__c8YUz").each((_, el) => {
        const href = $(el).attr("href");
        if (href && href.includes("/prognozy/hokkey/")) {
            links.push(BASE_URL + href);
        }
    });

    const results = [];
    for (const link of links.slice(0, 10)) {
        try {
            const data = await parseMatchPage(link);
            results.push(data);
        } catch (err) {
            console.error(`Ошибка при парсинге ${link}:`, err.message);
        }
    }

    const savePath = path.join(OUTPUT_PATH, "metaratings.json");

    const { merged, added } = appendUniqueJson(
        savePath,
        results,
        item => `${item.source}_${item.id || item.match}`
    );
    console.log(`Добавлено новых прогнозов: ${added}`);
    console.log(`Прогнозы с meta-ratings.kz сохранены в ${savePath}`);

    return merged;
}
