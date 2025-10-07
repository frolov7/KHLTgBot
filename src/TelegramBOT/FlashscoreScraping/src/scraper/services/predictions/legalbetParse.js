import * as cheerio from "cheerio";
import fs from "fs";
import path from "path";
import { OUTPUT_PATH } from "../../../constants/constants.js";
import { TEAM_MAP, normalizeTeamName, findMatchId, parseRuDateLegalbet } from "../utils/teamMapUtils.js";
import { appendUniqueJson } from "../utils/fileUtils.js";

const BASE_URL = "https://legalbet.kz";

/**
 * Загружает HTML-страницу по указанному URL.
 */
async function fetchHtml(url) {
    const res = await fetch(url, {
        headers: { "User-Agent": "Mozilla/5.0" },
    });
    return await res.text();
}

/**
 * Проверяет, относится ли заголовок к команде (с учётом TEAM_MAP).
 */
function matchesTeam(title, teamName) {
    const clean = (s) => (s || "").toLowerCase().replace(/[«»"']/g, "").trim();
    const normalizedTitle = clean(title);

    const teamBase = TEAM_MAP[teamName] || normalizeTeamName(teamName);

    for (const [ru, eng] of Object.entries(TEAM_MAP)) {
        if (eng === teamBase) {
            if (normalizedTitle.includes(clean(ru))) {
                return true;
            }
        }
    }
    return false;
}

/**
 * Парсит страницу матча legalbet.kz
 */
async function parseMatchPage(url, calendar, matchInfo) {
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    const home = normalizeTeamName(matchInfo.home);
    const away = normalizeTeamName(matchInfo.away);

    // Дата матча
    const dateStr = $(".match-head__info-date").first().text().trim();
    const timeStr = $(".match-head__info-time").first().text().trim();
    const matchDate = parseRuDateLegalbet(dateStr, timeStr);

    if (!matchDate)
        return null; // завершает выполнение без ошибки

    console.log(`Дата матча для ${home} – ${away}: ${dateStr} ${timeStr} → ${matchDate}`);


    let homeText = "";
    let awayText = "";
    let commonText = "";
    let mainBet = null;

    $("h2, h3").each((_, el) => {
        const title = $(el).text().trim();

        // Анализ хозяев
        if (matchesTeam(title, home)) {
            let sibling = $(el).next();
            while (sibling.length && !sibling.is("h2, h3")) {
                homeText += sibling.text().trim() + "\n\n";
                sibling = sibling.next();
            }
        }

        // Анализ гостей
        if (matchesTeam(title, away)) {
            let sibling = $(el).next();
            while (sibling.length && !sibling.is("h2, h3")) {
                awayText += sibling.text().trim() + "\n\n";
                sibling = sibling.next();
            }
        }

        // Общий прогноз
        if (/прогноз/i.test(title)) {
            let sibling = $(el).next();
            while (sibling.length && !sibling.is("p:has(strong)")) {
                commonText += sibling.text().trim() + "\n\n";
                sibling = sibling.next();
            }
            const betLine = sibling.find("strong").text().trim();
            if (betLine) {
                mainBet = betLine;
            }
        }
    });

    // Поиск "Мой прогноз"
    $("p").each((_, el) => {
        const text = $(el).text().trim();
        if (/Мой прогноз:/i.test(text)) {
            const strong = $(el).find("strong").text().trim();
            if (strong) {
                mainBet = strong;
            }
        }
    });

    const matchId = home && away ? findMatchId(home, away, calendar, matchDate) : null;

    return {
        source: "legalbet",
        url: url,
        match: `${home} – ${away}`,
        teams: {
            home: { name: home, analysis: homeText.trim() },
            away: { name: away, analysis: awayText.trim() },
        },
        prediction: {
            main: mainBet,
            alt: null,
            score: null,
            text: commonText.trim(),
            result: null,
        },
        id: matchId,
    };
}

/**
 * Основная функция: парсит список матчей КХЛ на legalbet.kz
 */
export async function scrapePredictionsLegalbet() {
    const listUrl = `${BASE_URL}/hockey/tournaments/khl/`;

    const html = await fetchHtml(listUrl);
    const $ = cheerio.load(html);

    const calendarPath = path.join(OUTPUT_PATH, "russia_khl_all.json");
    const calendar = JSON.parse(fs.readFileSync(calendarPath, "utf-8"));

    const links = [];
    let stopParsing = false;

    $(".match-table__item").each((_, el) => {
        if (stopParsing) return false; // сразу стоп

        const href = $(el).find("a.match-table__teams").attr("href");
        const home = $(el).find("meta[itemprop='homeTeam']").attr("content");
        const away = $(el).find("meta[itemprop='awayTeam']").attr("content");
        const dateStr = $(el).find("meta[itemprop='startDate']").attr("content");

        if (!href || !home || !away || !dateStr) {
            stopParsing = true;
            return false; // полностью прерываем .each
        }

        const matchDate = new Date(dateStr);
        if (isNaN(matchDate.getTime())) {
            console.log(`⏭ Пропуск ${home} – ${away}: невалидная дата`);
            return;
        }

        links.push({
            url: BASE_URL + href,
            home,
            away,
            date: matchDate.toISOString(),
        });
    });


    const results = [];
    try {
        for (const { url, home, away } of links) {
            const data = await parseMatchPage(url, calendar, { home, away });
            if (data) results.push(data);
            else break;
        }
    } catch (err) {
        console.error(err.message);
    }


    const savePath = path.join(OUTPUT_PATH, "legalbet.json");

    const { merged, added } = appendUniqueJson(
        savePath,
        results,
        (item) => `${item.source}_${item.id || item.match}`
    );

    console.log(`Добавлено новых прогнозов: ${added}`);
    console.log(`Прогнозы с legalbet.kz сохранены в ${savePath}`);

    return merged;
}

export { scrapePredictionsLegalbet as scrapePredictions };
