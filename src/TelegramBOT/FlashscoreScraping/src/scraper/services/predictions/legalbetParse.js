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

    // дата матча
    const dateStr = $(".match-head__info-date").first().text().trim();
    const timeStr = $(".match-head__info-time").first().text().trim();
    const matchDate = parseRuDateLegalbet(dateStr, timeStr);
    if (!matchDate) return null;

    console.log(`Дата матча для ${home} – ${away}: ${dateStr} ${timeStr} → ${matchDate}`);

    let homeText = "";
    let awayText = "";
    let commonText = "";
    let mainBet = null;

    // очистка текста от лишнего
    function cleanNodeText(node) {
        const $node = $(node);

        // убираем ненужные блоки
        $node.find("style, script, .match-odds, .odds-tabs, .custom-link-widget").remove();

        // оставляем коэффициенты числом
        $node.find("a.bk-tag-link span").each((_, el) => {
            const coef = $(el).text().trim();
            $(el).replaceWith(coef ? " " + coef : "");
        });

        let text = $node.text().replace(/\s+/g, " ").trim();

        if (!text) return "";
        if (/Автор:/i.test(text)) return "";
        if (/Все ставки/i.test(text)) return "";

        return text;
    }

    // вытаскиваем текст между заголовками
    function collectBlockText(startHeader) {
        let text = "";
        let sibling = startHeader.next();
        while (sibling.length && !sibling.is("h2, h3")) {
            if (sibling.is("p") || sibling.is("ul") || sibling.is("ol")) {
                const clean = cleanNodeText(sibling);
                if (clean) text += clean + " ";
            }
            if (
                sibling.is("style") ||
                sibling.hasClass("match-odds") ||
                sibling.hasClass("odds-tabs")
            ) break;
            sibling = sibling.next();
        }
        return text.trim();
    }

    // Анализ хозяев
    const homeHeader = $("h2, h3").filter((_, el) =>
        matchesTeam($(el).text().trim(), home)
    );
    if (homeHeader.length) {
        homeText = collectBlockText(homeHeader.first());
    }

    // Анализ гостей
    const awayHeader = $("h2, h3").filter((_, el) =>
        matchesTeam($(el).text().trim(), away)
    );
    if (awayHeader.length) {
        awayText = collectBlockText(awayHeader.first());
    }

    // Общий прогноз
    const forecastHeader = $("h2, h3").filter((_, el) =>
        /прогноз/i.test($(el).text().trim())
    );
    if (forecastHeader.length) {
        let sibling = forecastHeader.first().next();
        while (sibling.length && !sibling.is("h2, h3")) {
            if ((sibling.is("p") || sibling.is("ul") || sibling.is("ol"))) {
                const clean = cleanNodeText(sibling);
                // убираем абзацы, начинающиеся с "Прогноз:"
                if (clean && !/^Прогноз:/i.test(clean)) {
                    commonText += clean + " ";
                }
            }
            if (
                sibling.is("style") ||
                sibling.hasClass("match-odds") ||
                sibling.hasClass("odds-tabs")
            ) break;
            sibling = sibling.next();
        }

        // ищем основную ставку в <strong>
        const betLine = forecastHeader
            .nextAll("p")
            .find("strong")
            .first()
            .text()
            .trim();
        if (betLine) {
            mainBet = betLine;
        }
    }

    // fallback: "Мой прогноз"
    $("p").each((_, el) => {
        const text = $(el).text().trim();
        if (/Мой прогноз:/i.test(text)) {
            const strong = $(el).find("strong").text().trim();
            if (strong) {
                mainBet = strong;
            }
        }
    });

    const matchId =
        home && away ? findMatchId(home, away, calendar, matchDate) : null;

    return {
        source: "legalbet",
        url: url,
        match: `${home} – ${away}`,
        teams: {
            home: { name: home, analysis: homeText },
            away: { name: away, analysis: awayText },
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
