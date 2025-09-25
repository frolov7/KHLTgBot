import * as cheerio from "cheerio";
import fs from "fs";
import path from "path";
import { OUTPUT_PATH } from "../../../constants/constants.js";
import { findMatchId } from "../utils/teamMapUtils.js";   // ✅ словарь и поиск матчей из utils
import { appendUniqueJson } from "../utils/fileUtils.js"; // ✅ добавление без перезаписи

const BASE_URL = "https://stavka.tv";

// загрузка календаря КХЛ
const calendarPath = path.join(OUTPUT_PATH, "russia_khl_all.json");
const calendar = JSON.parse(fs.readFileSync(calendarPath, "utf-8"));

function replaceQuotes(text) {
    if (!text) return text;
    return text
        .replace(/"([^"]+)"/g, "«$1»")   // "..." → «...»
        .replace(/“([^”]+)”/g, "«$1»")   // англ. кавычки тоже
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

    // собираем прогнозы
    const texts = [];
    $("li, p, h2, h3").each((_, el) => {
        const t = $(el).text().trim();

        // оставляем только абзацы с прогнозами
        if (/^(Основной прогноз|Прогноз на|Прогноз с)/i.test(t)) {
            texts.push(t);

            // если есть счёт
            const scoreMatch = t.match(/(\d+:\d+)/);
            if (scoreMatch) {
                prediction.score = scoreMatch[1];
            }

            // если это блок "Прогноз на тотал"
            if (/^Прогноз на тотал/i.test(t)) {
                // ищем жирный текст внутри блока
                const boldText = $(el).find("strong, b").last().text().trim();
                if (boldText) {
                    // убираем "за ..." в конце (коэффициенты)
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
 * Основная функция: парсинг прогнозов stavka.tv
 */
export async function scrapePredictions() {
    const listUrl = `${BASE_URL}/matches/ice-hockey`;

    const html = await fetchHtml(listUrl);
    const $ = cheerio.load(html);

    const results = [];
    const rows = $(".MatchesRow");

    for (const el of rows.toArray()) {
        const link = $(el).find("a.match-link").attr("href");
        if (!link) continue;

        const fullUrl = BASE_URL + link;
        const teams = $(el).find(".team-name");
        const home = $(teams[0]).text().trim();
        const away = $(teams[1]).text().trim();

        const matchId = findMatchId(home, away, calendar);
        if (!matchId) continue;

        const prediction = await parseMatchPage(fullUrl);
        prediction.result = checkPrediction(prediction, calendar[matchId]);

        results.push({
            source: "stavka.tv",
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
