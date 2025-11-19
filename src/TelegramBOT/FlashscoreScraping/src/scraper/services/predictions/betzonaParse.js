import * as cheerio from "cheerio";
import fs from "fs";
import { FILES } from "../../../constants/constants.js";
import { appendUniqueJson } from "../utils/core/jsonUtils.js";
import { normalizeTeamName, findMatchId } from "../utils/matches/teamMapUtils.js";
import { normalizePrediction } from "../utils/predictions/normalizePrediction.js";

const BASE_URL = "https://betzona.ru";

export { scrapePredictionsBetzona as scrapePredictions };

/// <summary>
/// Загружает HTML-страницу по указанному URL.
/// </summary>
/// <param name="url">Адрес страницы для загрузки.</param>
/// <returns>HTML-код страницы в виде строки.</returns>
async function fetchHtml(url) {
    const res = await fetch(url, { headers: { "User-Agent": "Mozilla/5.0" } });
    return await res.text();
}

/// <summary>
/// Заменяет обычные кавычки на типографские («»).
/// </summary>
/// <param name="str">Исходная строка с текстом.</param>
/// <returns>Строка с заменёнными кавычками.</returns>
function normalizeQuotes(str) {
    if (!str) return str;
    return str
        .replace(/"([^"]+)"/g, "«$1»")
        .replace(/««/g, "«")
        .replace(/»»/g, "»");
}

/// <summary>
/// Очищает текст HTML-узла от лишних элементов и пробелов.
/// </summary>
/// <param name="$">Объект cheerio для работы с DOM.</param>
/// <param name="node">HTML-узел, текст которого нужно извлечь.</param>
/// <returns>Очищенный текст узла.</returns>
function cleanNodeText($, node) {
    const $node = $(node);
    if ($node.is(".scores, .standing, .row, .white-block, .position-table")) return "";
    if (/Личные встречи/i.test($node.text()) || /Реклама/i.test($node.text())) return "";
    return $node.text().replace(/\s+/g, " ").trim();
}

/// <summary>
/// Извлекает текст прогнозов для домашней и гостевой команд, а также общий текст прогнозов.
/// </summary>
/// <param name="$">Объект cheerio для анализа HTML.</param>
/// <param name="home">Название домашней команды.</param>
/// <param name="away">Название гостевой команды.</param>
/// <returns>Объект с текстами: homeText, awayText и commonText.</returns>
function extractTeamAndPredictionTexts($, home, away) {
    let homeText = null;
    let awayText = null;

    $(".head-team").each((_, el) => {
        const teamName = $(el).find("h2").text().trim();
        const infoBlock = $(el).next(".team-info").find(".info");
        let clean = cleanNodeText($, infoBlock);
        if (teamName && clean) {
            if (teamName.toLowerCase() === home.toLowerCase()) homeText = clean;
            else if (teamName.toLowerCase() === away.toLowerCase()) awayText = clean;
        }
    });

    const commonParts = [];
    const forecastHeader = $("h2").filter((_, el) => $(el).text().trim() === "Прогноз");
    if (forecastHeader.length) {
        let sibling = forecastHeader.next();
        while (sibling.length) {
            if (sibling.is("h2") || sibling.is("h3")) break;
            if (sibling.is("p")) {
                const clean = cleanNodeText($, sibling);
                if (clean) commonParts.push(clean);
            }
            sibling = sibling.next();
        }
    }

    return { homeText, awayText, commonText: commonParts.join(" ") };
}

function extractForecastText($) {
    const parts = [];

    $(".forecast-info p").each((_, el) => {
        const text = $(el).text().replace(/\s+/g, " ").trim();
        if (text) parts.push(text);
    });

    return parts.join(" ");
}


/// <summary>
/// Парсит данные конкретного матча и возвращает структурированный объект с прогнозом.
/// </summary>
/// <param name="url">Ссылка на страницу матча.</param>
/// <param name="calendar">JSON-объект с календарём КХЛ.</param>
/// <param name="matchInfo">Информация о командах матча (home, away).</param>
/// <param name="logger">Логгер для вывода информации и ошибок.</param>
/// <returns>Объект с данными прогноза или null, если матч не найден.</returns>
async function parseData(url, calendar, matchInfo, logger) {
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);
    const home = normalizeTeamName(matchInfo.home);
    const away = normalizeTeamName(matchInfo.away);
    let matchDate = null;

    const dateBlock = $(".match-review-head-date").first().text().trim();
    if (dateBlock) {
        const [dateStr, timeStr] = dateBlock.split(" ");
        if (dateStr && timeStr) {
            const [day, month, year] = dateStr.split(".").map(Number);
            const [hours, minutes] = timeStr.split(":").map(Number);
            matchDate = new Date(year, month - 1, day, hours, minutes);
        }
    }

    if (!matchDate) return null;

    const matchId = findMatchId(home, away, calendar, matchDate);
    logger.info(`Матч: ${home} – ${away} | matchID: ${matchId || "не найден"} | Дата: ${matchDate.toISOString()}`);

    const texts = extractTeamAndPredictionTexts($, home, away);
    const forecastText = extractForecastText($);
    const mainBet = $(".forecast-info .bet_name").first().text().trim() || null;

    return {
        source: "betzona",
        url,
        match: `${home} – ${away}`,
        teams: {
            home: { name: home, text: normalizeQuotes(texts.homeText || "") },
            away: { name: away, text: normalizeQuotes(texts.awayText || "") },
        },
        prediction: {
            main: normalizePrediction(mainBet, home, away),
            text: normalizeQuotes(forecastText || ""),
            result: null,
        },
        id: matchId,
    };
}


/// <summary>
/// Сохраняет результаты прогнозов в JSON-файл, исключая дубликаты.
/// </summary>
/// <param name="results">Массив объектов прогнозов для сохранения.</param>
/// <param name="logger">Логгер для вывода информации о сохранении.</param>
/// <returns>Количество добавленных новых прогнозов.</returns>
function saveResults(results, logger) {
    const savePath = FILES.BETZONA;
    const { merged, added } = appendUniqueJson(savePath, results, i => `${i.source}_${i.id || i.match}`);
    logger.info(`Прогнозы сохранены в ${savePath}`);
    return added;
}


/// <summary>
/// Основная функция парсера Betzona — извлекает все прогнозы КХЛ и сохраняет их в JSON.
/// </summary>
/// <param name="logger">Логгер для вывода информации (по умолчанию console).</param>
/// <returns>Массив объектов с прогнозами матчей.</returns>
export async function scrapePredictionsBetzona({ logger = console } = {}) {
    const listUrl = `${BASE_URL}/prognozy-khl.html`;
    const html = await fetchHtml(listUrl);
    const $ = cheerio.load(html);

    // путь к календарю теперь из FILES
    const calendar = JSON.parse(fs.readFileSync(FILES.KHL_MATCHES, "utf-8"));

    const links = [];
    const seen = new Set();

    $(".bets-description-card").each((_, el) => {
        const href = $(el).attr("href");
        const tournament = $(el).attr("data-tournament") || "";
        const matchTitle = $(el).attr("data-match-name") || "";
        const matchDate = $(el).attr("data-date") || null;

        if (!href || !matchTitle || !tournament.includes("КХЛ")) return;

        const key = `${href}_${matchTitle}`;
        if (seen.has(key)) return;
        seen.add(key);

        const [home, away] = matchTitle.split(/[-–]/).map(s => s.trim());
        links.push({ url: BASE_URL + href, home, away, date: matchDate });
    });

    logger.info(`Найдено ${links.length} матчей.`);

    const rawResults = [];
    for (const { url, home, away } of links) {
        try {
            const data = await parseData(url, calendar, { home, away }, logger);
            if (data) rawResults.push(data);
        } catch (err) {
            logger.error(`Ошибка при парсинге ${url}`, err);
        }
    }

    const results = Object.values(rawResults.reduce((acc, item) => {
        const key = `${item.source}_${item.id || item.match}`;
        if (!acc[key]) acc[key] = { ...item };
        else {
            const ex = acc[key];
            if (ex.prediction.alt) ex.prediction.alt += `, ${item.prediction.main}`;
            else ex.prediction.alt = item.prediction.main;
        }
        return acc;
    }, {}));

    const added = saveResults(results, logger);
    logger.info(`Итог: добавлено новых прогнозов ${added}/${results.length}`);

    return results;
}
