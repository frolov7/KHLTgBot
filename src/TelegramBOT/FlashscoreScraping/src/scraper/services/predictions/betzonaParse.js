import * as cheerio from "cheerio";
import fs from "fs";
import path from "path";
import { OUTPUT_PATH } from "../../../constants/constants.js";
import { appendUniqueJson } from "../utils/fileUtils.js";
import { normalizeTeamName, findMatchId, parseRuDate } from "../utils/teamMapUtils.js";

export { scrapePredictionsBetzona as scrapePredictions };

const BASE_URL = "https://betzona.ru";


function normalizeQuotes(str) {
    if (!str) return str;
    return str
        .replace(/"([^"]+)"/g, "«$1»")  // заменяет "текст" → «текст»
        .replace(/««/g, "«")            // на всякий случай чистим двойные
        .replace(/»»/g, "»");
}

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
/// Очищает текст узла: убирает табы/переносы, рекламу, таблицы.
/// </summary>
function cleanNodeText($, node) {
    const $node = $(node);

    // выкидываем таблицы, блоки статистики, рекламу
    if ($node.is(".scores, .standing, .row, .white-block, .position-table")) return "";
    if (/Личные встречи/i.test($node.text())) return "";
    if (/Реклама/i.test($node.text())) return "";

    // нормализация текста
    let text = $node.text().replace(/\s+/g, " ").trim();
    return text;
}

function extractTeamAndPredictionTexts($, home, away) {
    let homeText = null;
    let awayText = null;

    // Анализ команд
    $(".head-team").each((_, el) => {
        const teamName = $(el).find("h2").text().trim();
        const infoBlock = $(el).next(".team-info").find(".info");
        let clean = cleanNodeText($, infoBlock);
        if (teamName && clean) {
            if (teamName.toLowerCase() === home.toLowerCase()) {
                homeText = clean;
            } else if (teamName.toLowerCase() === away.toLowerCase()) {
                awayText = clean;
            }
        }
    });

    // Общий прогноз
    let commonParts = [];
    const forecastHeader = $("h2").filter((_, el) => $(el).text().trim() === "Прогноз");
    if (forecastHeader.length) {
        let sibling = forecastHeader.next();
        while (sibling.length) {
            if (sibling.is("h2") || sibling.is("h3")) break;

            // берём только <p>
            if (sibling.is("p")) {
                const clean = cleanNodeText($, sibling);
                if (clean) commonParts.push(clean);
            }

            sibling = sibling.next();
        }
    }

    return {
        homeText,
        awayText,
        commonText: commonParts.join(" ")
    };
}



/// <summary>
/// Парсит страницу конкретного матча и извлекает прогноз.
/// </summary>
async function parseMatchPage(url, calendar, matchInfo) {
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    const home = normalizeTeamName(matchInfo.home);
    const away = normalizeTeamName(matchInfo.away);

    let matchDate = null;

    if (url.includes("betzona")) {
        // --- формат Betzona: "09.10.2025 19:00"
        const dateBlock = $(".match-review-head-date").first().text().trim();
        if (dateBlock) {
            const [dateStr, timeStr] = dateBlock.split(" ");
            if (dateStr && timeStr) {
                const [day, month, year] = dateStr.split(".").map(Number);
                const [hours, minutes] = timeStr.split(":").map(Number);
                matchDate = new Date(year, month - 1, day, hours, minutes);
            }
        }
    } else {
        // --- формат Legalbet: отдельно дата и время
        const dateStr = $(".match-head__info-date").first().text().trim();
        const timeStr = $(".match-head__info-time").first().text().trim();
        if (dateStr && timeStr) {
            const [day, month, year] = dateStr.split(".").map(Number);
            const [hours, minutes] = timeStr.split(":").map(Number);
            matchDate = new Date(year, month - 1, day, hours, minutes);
        }
    }

    if (!matchDate) return null;

    console.log(`Дата матча для ${home} – ${away}: ${matchDate}`);

    // --- Тексты по командам ---
    let homeText = "";
    let awayText = "";

    $(".head-team").each((_, el) => {
        const teamName = $(el).find("h2").text().trim();
        const infoBlock = $(el).next(".team-info").find(".info p");
        let teamText = infoBlock.map((_, p) => $(p).text().trim()).get().join(" ");

        if (teamName && teamText) {
            if (normalizeTeamName(teamName) === home) {
                homeText = teamText;
            } else if (normalizeTeamName(teamName) === away) {
                awayText = teamText;
            }
        }
    });

    // --- Общий прогноз ---
    let commonText = $(".forecast-info > p")
        .map((_, el) => $(el).text().replace(/\s+/g, " ").trim())
        .get()
        .filter(Boolean)
        .join(" ");

    // --- Основная ставка ---
    let mainBet = $(".forecast-info .bet_name").first().text().trim() || null;

    // Подстраховка — берём последние абзацы, если прогноз не нашёлся
    if (!commonText) {
        const allParas = $("p").map((_, el) => $(el).text().trim()).get().filter(Boolean);
        if (allParas.length > 0) {
            commonText = allParas.slice(-2).join(" ");
        }
    }

    // --- Заменяем кавычки на ёлочки ---
    homeText = normalizeQuotes(homeText);
    awayText = normalizeQuotes(awayText);
    commonText = normalizeQuotes(commonText);
    if (mainBet) mainBet = normalizeQuotes(mainBet);

    const matchId = home && away ? findMatchId(home, away, calendar, matchDate) : null;

    return {
        source: "betzona",
        url: url,
        match: `${home} – ${away}`,
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
