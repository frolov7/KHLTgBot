import * as cheerio from "cheerio";
import fs from "fs";
import path from "path";
import { OUTPUT_PATH } from "../../../constants/constants.js";
import { appendUniqueJson } from "../utils/fileUtils.js";
import { normalizeTeamName, findMatchId, parseRuDate } from "../utils/teamMapUtils.js";

export { scrapePredictionsBetzona as scrapePredictions };

const BASE_URL = "https://betzona.ru";

/// <summary>
/// Загружает HTML-страницу по указанному URL.
/// </summary>
async function fetchHtml(url) {
    const res = await fetch(url, {
        headers: { "User-Agent": "Mozilla/5.0" },
    });
    return await res.text();
}

/// <summary>
/// Извлекает тексты анализа для home/away и общий прогноз.
/// </summary>
function extractTeamAndPredictionTexts($, home, away) {
    // Тексты команд
    const teamBlocks = $(".head-team");
    let homeText = null;
    let awayText = null;

    teamBlocks.each((_, el) => {
        const teamName = $(el).find("h2").text().trim();
        const infoBlock = $(el).next(".team-info").find(".info").text().trim();

        if (teamName && infoBlock) {
            if (teamName.toLowerCase() === home.toLowerCase()) {
                homeText = infoBlock;
            } else if (teamName.toLowerCase() === away.toLowerCase()) {
                awayText = infoBlock;
            }
        }
    });

    // Общий прогноз
    let commonText = "";
    const forecastHeader = $("h2").filter((_, el) => $(el).text().trim() === "Прогноз");
    if (forecastHeader.length) {
        const forecastBlock = forecastHeader.next(".forecast-info");
        if (forecastBlock.length) {
            commonText = forecastBlock.text().trim();
        } else {
            let sibling = forecastHeader.next();
            const texts = [];
            while (sibling.length) {
                if (sibling.is("h2") || sibling.is("h3")) break;
                if (sibling.is("p") || sibling.is("div")) {
                    const t = sibling.text().trim();
                    if (t) texts.push(t);
                }
                sibling = sibling.next();
            }
            commonText = texts.join("\n\n");
        }
    }

    return { homeText, awayText, commonText };
}

/// <summary>
/// Парсит страницу конкретного матча и извлекает прогноз.
/// </summary>
async function parseMatchPage(url, calendar, matchInfo) {
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    const home = normalizeTeamName(matchInfo.home);
    const away = normalizeTeamName(matchInfo.away);

    // ✅ дата матча
    const dateRaw = $(".match-review-head-date").first().text().trim() || null;
    const matchDate = parseRuDate(dateRaw);
    console.log(`Дата матча для ${home} – ${away}: ${dateRaw} → ${matchDate}`);

    let mainBet = $(".bet_name").first().text().trim() || null;
    const { homeText, awayText, commonText } = extractTeamAndPredictionTexts($, home, away);

    const matchId = findMatchId(home, away, calendar, matchDate);

    return {
        source: url,
        match: `${home} – ${away}`,
        date: dateRaw, // можно сохранить и "сырую" строку
        teams: {
            home: { name: home, text: homeText },
            away: { name: away, text: awayText },
        },
        prediction: {
            main: mainBet,
            alt: null,
            score: null,
            text: commonText,
            result: null,
        },
        id: matchId,
    };
}


/// <summary>
/// Основная функция: собирает прогнозы КХЛ с betzona.ru,
/// группирует дублирующиеся прогнозы и сохраняет в JSON.
/// </summary>
export async function scrapePredictionsBetzona() {
    const listUrl = `${BASE_URL}/prognozy-khl.html`;

    const html = await fetchHtml(listUrl);
    const $ = cheerio.load(html);

    const calendarPath = path.join(OUTPUT_PATH, "russia_khl_all.json");
    const calendar = JSON.parse(fs.readFileSync(calendarPath, "utf-8"));

    const links = [];
    $(".bets-description-card").each((_, el) => {
        const href = $(el).attr("href");
        const tournament = $(el).attr("data-tournament") || "";
        const matchTitle = $(el).attr("data-match-name") || "";
        const matchDate = $(el).attr("data-date") || null;

        if (href && tournament.includes("КХЛ") && matchTitle) {
            const [home, away] = matchTitle.split(/[-–]/).map((s) => s.trim());
            links.push({ url: BASE_URL + href, home, away, date: matchDate });
        }
    });

    const rawResults = [];
    for (const { url, home, away } of links) {
        try {
            const data = await parseMatchPage(url, calendar, { home, away });
            if (data) rawResults.push(data);
        } catch (err) {
            console.error(`Ошибка при парсинге ${url}:`, err.message);
        }
    }

    const grouped = {};
    for (const item of rawResults) {
        const key = `${item.source}_${item.id || item.match}`;
        if (!grouped[key]) {
            grouped[key] = { ...item };
        } else {
            const existing = grouped[key];
            if (!existing.prediction.alt) {
                existing.prediction.alt = item.prediction.main;
            } else {
                existing.prediction.alt += `, ${item.prediction.main}`;
            }
        }
    }

    const results = Object.values(grouped);

    const savePath = path.join(OUTPUT_PATH, "betzona.json");

    const { merged, added } = appendUniqueJson(
        savePath,
        results,
        (item) => `${item.source}_${item.id || item.match}`
    );

    console.log(`Добавлено новых прогнозов: ${added}`);
    console.log(`Прогнозы с betzona.ru сохранены в ${savePath}`);

    return merged;
}
