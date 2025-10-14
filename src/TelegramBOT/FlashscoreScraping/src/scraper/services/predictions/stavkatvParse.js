import * as cheerio from "cheerio";
import fs from "fs";
import path from "path";
import { OUTPUT_PATH } from "../../../constants/constants.js";
import { findMatchId } from "../utils/teamMapUtils.js";
import { appendUniqueJson } from "../utils/fileUtils.js";

const BASE_URL = "https://stavka.tv";

// загрузка календаря КХЛ
const calendarPath = path.join(OUTPUT_PATH, "russia_khl_all.json");
const calendar = JSON.parse(fs.readFileSync(calendarPath, "utf-8"));

function replaceQuotes(text) {
    if (!text) return text;
    return text
        .replace(/"([^"]+)"/g, "«$1»") // "..." → «...»
        .replace(/“([^”]+)”/g, "«$1»") // англ. кавычки
        .replace(/”/g, "»")
        .replace(/“/g, "«");
}

/**
 * Загружает HTML-страницу
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
 * Проверка прогноза на основе результата
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
 * Парсит страницу матча
 */
async function parseMatchPage(url) {
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    let prediction = {
        main: null,
        alt: null,
        score: null,
        text: "",
        result: null,
    };

    // основной исход (главная ставка)
    const outcome = $(".EditorsChoice .choice .outcome").first().text().trim();
    if (outcome) {
        prediction.main = replaceQuotes(outcome);
    }

    // собираем текст прогнозов
    const texts = [];
    $("li, p, h2, h3").each((_, el) => {
        const t = $(el).text().trim();

        if (/^(Основной прогноз|Прогноз на|Прогноз с)/i.test(t)) {
            texts.push(t);

            const scoreMatch = t.match(/(\d+:\d+)/);
            if (scoreMatch) {
                prediction.score = scoreMatch[1];
            }

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

/**
 * Преобразует дату из блока на сайте в объект Date
 */
function parseStavkaDate(dateStr, timeStr) {
    const months = {
        "янв": 0, "фев": 1, "мар": 2, "апр": 3, "май": 4, "июн": 5,
        "июл": 6, "авг": 7, "сен": 8, "окт": 9, "ноя": 10, "дек": 11,
    };
    const [dayStr, monStr] = dateStr.split(" ");
    const day = parseInt(dayStr, 10);
    const month = months[monStr.toLowerCase()];
    const year = new Date().getFullYear();
    const [h, m] = timeStr.split(":").map(Number);

    return new Date(year, month, day, h, m);
}

/**
 * Основная функция: парсинг прогнозов stavka.tv
 */
export async function scrapePredictions() {
    const listUrl = `${BASE_URL}/matches/ice-hockey`;

    const html = await fetchHtml(listUrl);
    const $ = cheerio.load(html);

    const results = [];

    // 1️⃣ Находим секцию с заголовком "КХЛ"
    const khlSection = $("section.MatchesTable")
        .filter((_, el) => {
            const titleLink = $(el).find(".MatchesTableHeader .title a.title-link").attr("href");
            return titleLink && titleLink.includes("russia-khl");
        })
        .first();

    if (!khlSection.length) {
        console.error("❌ Не найдена секция КХЛ на странице!");
        return [];
    }

    // 2️⃣ Берём только матчи из КХЛ
    const rows = khlSection.find(".MatchesRow");

    for (const el of rows.toArray()) {
        const link = $(el).find("a.match-link").attr("href");
        if (!link) continue;

        const fullUrl = BASE_URL + link;
        const teams = $(el).find(".team-name");
        const home = $(teams[0]).text().trim();
        const away = $(teams[1]).text().trim();

        // дата и время матча
        const dateStr = $(el).find(".event-date").text().trim();
        const timeStr = $(el).find(".event-status").text().trim();
        let matchDate = null;
        if (dateStr && timeStr) {
            matchDate = parseStavkaDate(dateStr, timeStr);
        }

        const matchId = findMatchId(home, away, calendar, matchDate);
        if (!matchId) {
            console.log(`⚠️ Не найден матч для ${home} – ${away} (${dateStr} ${timeStr})`);
            continue;
        }

        const prediction = await parseMatchPage(fullUrl);
        prediction.result = checkPrediction(prediction, calendar[matchId]);

        results.push({
            source: "stavkatv",
            url: fullUrl,
            match: `${home} – ${away}`,
            teams: {
                home: { name: home },
                away: { name: away },
            },
            prediction,
            id: matchId,
        });
    }

    const savePath = path.join(OUTPUT_PATH, "stavkatv.json");

    const { merged, added } = appendUniqueJson(
        savePath,
        results,
        (item) => `${item.source}_${item.id || item.match}`
    );

    console.log(`Добавлено новых прогнозов: ${added}`);
    console.log(`Прогнозы КХЛ со stavka.tv сохранены в ${savePath}`);

    return merged;
}

