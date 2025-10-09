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
 */
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

/**
 * Загружает HTML по указанному URL.
 */
async function fetchHtml(url) {
    const res = await fetch(url, {
        headers: { "User-Agent": "Mozilla/5.0" },
    });
    return await res.text();
}

function cleanText(text) {
    if (!text) return "";
    return text
        .replace(/\s+/g, " ")   // убираем лишние пробелы и переносы
        .replace(/ /g, " ")     // убираем неразрывные пробелы
        .trim();
}


/**
 * Парсит страницу отдельного матча с meta-ratings.kz.
 */
async function parseMatchPage(url, meta) {
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    let prediction = { main: null, alt: null, score: null, text: "", result: null };

    const paras = [];
    $("h2")
        .filter((_, el) => $(el).text().includes("Прогноз на матч"))
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
    prediction.text = cleanText(paras.join(" "));

    const dateLog = meta.matchDate ? meta.matchDate.toISOString() : "дата не найдена";
    console.log(`Дата матча для ${meta.home} – ${meta.away}: ${dateLog}`);

    const matchId = findMatchId(meta.home, meta.away, calendar, meta.matchDate);
    if (matchId) {
        prediction.result = checkPrediction(prediction, calendar[matchId]);
    }

    return {
        source: "metaratings",
        url: url,
        match: `${meta.home} – ${meta.away}`,
        teams: {
            home: { name: meta.home },
            away: { name: meta.away },
        },
        prediction,
        id: matchId || null,
    };
}

export async function scrapePredictions() {
    const listUrl = `${BASE_URL}/prognozy/hokkey/khl/`;
    const html = await fetchHtml(listUrl);
    const $ = cheerio.load(html);

    const matches = [];

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

        // команды из заголовка
        const title = linkEl.text().trim();
        const cleanTitle = title.replace("Прогноз на матч", "").trim();
        const [homeRaw, awayRaw] = cleanTitle.split("–").map(s => s.trim());

        const home = homeRaw.split(".")[0].trim();
        const away = awayRaw.split(".")[0].trim();

        matches.push({ url, home, away, matchDate });
    });

    const results = [];
    for (const m of matches.slice(0, 10)) {
        try {
            const data = await parseMatchPage(m.url, m);
            results.push(data);
        } catch (err) {
            console.error(`Ошибка при парсинге ${m.url}:`, err.message);
        }
    }

    const savePath = path.join(OUTPUT_PATH, "metaratings.json");
    const { merged, added } = appendUniqueJson(
        savePath,
        results,
        (item) => `${item.source}_${item.id || item.match}`
    );
    console.log(`Добавлено новых прогнозов: ${added}`);
    console.log(`Прогнозы с meta-ratings.kz сохранены в ${savePath}`);

    return merged;
}


