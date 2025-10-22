// src/scraper/services/predictions/legalbetParse.js
import * as cheerio from "cheerio";
import fs from "fs";
import path from "path";
import { OUTPUT_PATH } from "../../../constants/constants.js";
import {
    TEAM_MAP,
    normalizeTeamName,
    findMatchId,
    parseRuDateLegalbet
} from "../utils/teamMapUtils.js";
import { appendUniqueJson } from "../utils/fileUtils.js";
import { createLogger } from "../utils/logger.js";

const logger = createLogger("legalbet");
const BASE_URL = "https://legalbet.kz";

export { scrapePredictionsLegalbet as scrapePredictions };

/// <summary>
/// Загружает HTML-страницу по указанному URL.
/// </summary>
async function fetchHtml(url) {
    const res = await fetch(url, { headers: { "User-Agent": "Mozilla/5.0" } });
    return await res.text();
}

/// <summary>
/// Проверяет, соответствует ли заголовок названию команды (с учётом TEAM_MAP).
/// </summary>
function matchesTeam(title, teamName) {
    const clean = (s) => (s || "").toLowerCase().replace(/[«»"']/g, "").trim();
    const normalizedTitle = clean(title);
    const teamBase = TEAM_MAP[teamName] || normalizeTeamName(teamName);

    for (const [ru, eng] of Object.entries(TEAM_MAP)) {
        if (eng === teamBase && normalizedTitle.includes(clean(ru))) {
            return true;
        }
    }
    return false;
}

/// <summary>
/// Очищает текст HTML-узла от ненужных блоков, рекламы и мусора.
/// </summary>
function cleanNodeText($, node) {
    const $node = $(node);
    $node.find("style, script, .match-odds, .odds-tabs, .custom-link-widget").remove();
    $node.find("a.bk-tag-link span").each((_, el) => {
        const coef = $(el).text().trim();
        $(el).replaceWith(coef ? " " + coef : "");
    });

    let text = $node.text().replace(/\s+/g, " ").trim();
    if (!text || /Автор:/i.test(text) || /Все ставки/i.test(text)) return "";
    return text;
}

/// <summary>
/// Собирает текстовый блок между заголовками.
/// </summary>
function collectBlockText($, startHeader) {
    let text = "";
    let sibling = startHeader.next();
    while (sibling.length && !sibling.is("h2, h3")) {
        if (sibling.is("p, ul, ol")) {
            const clean = cleanNodeText($, sibling);
            if (clean) text += clean + " ";
        }
        if (sibling.is("style") || sibling.hasClass("match-odds") || sibling.hasClass("odds-tabs")) break;
        sibling = sibling.next();
    }
    return text.trim();
}

/// <summary>
/// Парсит страницу отдельного матча Legalbet.
/// </summary>
async function parseMatchPage(url, calendar, matchInfo) {
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    const home = normalizeTeamName(matchInfo.home);
    const away = normalizeTeamName(matchInfo.away);

    const dateStr = $(".match-head__info-date").first().text().trim();
    const timeStr = $(".match-head__info-time").first().text().trim();
    const matchDate = parseRuDateLegalbet(dateStr, timeStr);
    if (!matchDate) return null;

    const matchId = findMatchId(home, away, calendar, matchDate);
    logger.info(`Матч: ${home} – ${away} | matchID: ${matchId || "не найден"} | Дата: ${matchDate.toISOString()}`);


    let homeText = "";
    let awayText = "";
    let commonText = "";
    let mainBet = null;

    // тексты для команд
    const homeHeader = $("h2, h3").filter((_, el) => matchesTeam($(el).text().trim(), home));
    if (homeHeader.length) homeText = collectBlockText($, homeHeader.first());

    const awayHeader = $("h2, h3").filter((_, el) => matchesTeam($(el).text().trim(), away));
    if (awayHeader.length) awayText = collectBlockText($, awayHeader.first());

    // общий прогноз
    const forecastHeader = $("h2, h3").filter((_, el) => /прогноз/i.test($(el).text().trim()));
    if (forecastHeader.length) {
        let sibling = forecastHeader.first().next();
        while (sibling.length && !sibling.is("h2, h3")) {
            if (sibling.is("p, ul, ol")) {
                const clean = cleanNodeText($, sibling);
                if (clean && !/^Прогноз:/i.test(clean)) commonText += clean + " ";
            }
            sibling = sibling.next();
        }

        // ищем основную ставку
        const betLine = forecastHeader.nextAll("p").find("strong").first().text().trim();
        if (betLine) mainBet = betLine;
    }

    // fallback "Мой прогноз"
    $("p").each((_, el) => {
        const text = $(el).text().trim();
        if (/Мой прогноз:/i.test(text)) {
            const strong = $(el).find("strong").text().trim();
            if (strong) mainBet = strong;
        }
    });

    return {
        source: "legalbet",
        url,
        match: `${home} – ${away}`,
        date: matchDate,
        teams: {
            home: { name: home, text: homeText },
            away: { name: away, text: awayText },
        },
        prediction: {
            main: mainBet,
            text: commonText,
            alt: null,
            result: null,
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
/// Главная функция парсера Legalbet.
/// </summary>
export async function scrapePredictionsLegalbet() {
    const listUrl = `${BASE_URL}/hockey/tournaments/khl/`;
    const html = await fetchHtml(listUrl);
    const $ = cheerio.load(html);

    const calendarPath = path.join(OUTPUT_PATH, "russia_khl_all.json");
    const calendar = JSON.parse(fs.readFileSync(calendarPath, "utf-8"));

    const links = [];
    const seen = new Set();
    let duplicates = 0;

    $(".match-table__item").each((_, el) => {
        const tournamentId = $(el).find(".match_list_cfs-js").attr("data-tournament-id");
        if (tournamentId !== "20000006") return; // Только КХЛ

        const href = $(el).find("a.match-table__teams").attr("href");
        const home = $(el).find("meta[itemprop='homeTeam']").attr("content");
        const away = $(el).find("meta[itemprop='awayTeam']").attr("content");
        const dateStr = $(el).find("meta[itemprop='startDate']").attr("content");
        if (!href || !home || !away || !dateStr) return;

        const matchDate = new Date(dateStr);
        const now = new Date();
        const tomorrow = new Date(now);
        tomorrow.setDate(now.getDate() + 1);

        // фильтр только на сегодня и завтра
        if (matchDate > tomorrow) return;

        const key = `${href}_${home}_${away}`;
        if (seen.has(key)) return;
        seen.add(key);

        links.push({
            url: BASE_URL + href,
            home,
            away,
            date: matchDate.toISOString(),
        });
    });

    logger.info(`Найдено ${links.length} матчей.`);

    const rawResults = [];
    let emptyCount = 0;
    const MAX_EMPTY = 1;

    for (const { url, home, away } of links) {
        try {
            const data = await parseMatchPage(url, calendar, { home, away });

            if (data) {
                rawResults.push(data);
                emptyCount = 0; // сброс если найден прогноз
            } else {
                emptyCount++;
            }

            // если подряд много матчей без прогнозов — прекращаем
            if (emptyCount >= MAX_EMPTY) {
                break;
            }

        } catch (err) {
            logger.error(`[legalbet] Ошибка при парсинге ${url}`, err);
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

    const added = saveResults(results, "legalbet.json");
    logger.info(`Итог: добавлено новых прогнозов ${added}/${results.length}`);

    return results;
}
