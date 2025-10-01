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

    let title = $("h1").first().text().trim();
    title = title.replace("Прогноз на матч", "").trim();
    title = title.replace(/:.*$/gi, "").trim();
    title = title.replace(/\d{1,2}\s[а-я]+\s?\d{0,4}/gi, "").trim();
    title = title.replace(/КХЛ/gi, "").trim();
    title = title.replace(/ставка за.*$/gi, "").trim();

    const [home, away] = title.split(/[-–—]/).map((s) => s.trim());

    const section = $("h2").filter((_, el) => $(el).text().includes("Прогноз и ставка")).first();
    const paras = [];
    section.nextAll("p").each((_, el) => {
        const t = $(el).text().trim();
        if (t) paras.push(t);
    });

    const prediction = { main: null, alt: null, score: null, text: "", result: null };
    let textBlock = paras.join("\n\n");

    const idx = textBlock.indexOf("Прогноз:");
    if (idx !== -1) {
        textBlock = textBlock.slice(idx).trim();
    }
    prediction.text = cleanText(textBlock);

    const bets = paras.filter((p) => p.startsWith("Ставка"));
    if (bets.length > 0) {
        prediction.main = cleanText(bets[0].replace("Ставка:", "").trim());
        if (bets.length > 1) {
            prediction.alt = bets
                .slice(1)
                .map((b) => cleanText(b.replace("Ставка:", "").trim()))
                .filter(Boolean)
                .join(", ");
        }
    }

    return {
        source: "livesport.ru",
        match: `${home} – ${away}`,
        teams: { home: { name: home }, away: { name: away } },
        prediction,
        id: findMatchId(home, away, calendar),
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
    $("a.r_tips_t, .r_tips_l_one a").each((_, el) => {
        const href = $(el).attr("href");
        if (href && href.includes("/tips/hockey/") && !href.includes("/tips/express/")) {
            links.push("https://www.livesport.ru" + href);
        }
    });

    const results = [];
    for (const link of links.slice(0, 10)) {
        try {
            const data = await parseMatchPage(link, calendar);
            results.push(data);
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
