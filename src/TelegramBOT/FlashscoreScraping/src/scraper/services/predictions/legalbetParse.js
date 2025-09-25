import * as cheerio from "cheerio";
import fs from "fs";
import path from "path";
import { OUTPUT_PATH } from "../../../constants/constants.js";
import { normalizeTeamName, findMatchId } from "../utils/teamMapUtils.js";
import { appendUniqueJson } from "../utils/fileUtils.js";

const BASE_URL = "https://legalbet.kz";

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
/// Очищает название команды/заголовка:
/// - убирает кавычки (« » " ')
/// - приводит к нижнему регистру
/// - убирает лишние пробелы
/// </summary>
function cleanName(name) {
    return (name || "")
        .replace(/[«»"']/g, "")
        .trim()
        .toLowerCase();
}

/// <summary>
/// Парсит страницу матча legalbet.kz и делит текст на блоки:
/// - homeText — анализ хозяев
/// - awayText — анализ гостей
/// - text — общий текст прогноза перед финальной ставкой
/// - mainBet — сам прогноз
/// </summary>
async function parseMatchPage(url, calendar, matchInfo) {
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    const home = normalizeTeamName(matchInfo.home);
    const away = normalizeTeamName(matchInfo.away);

    let homeText = "";
    let awayText = "";
    let commonText = "";
    let mainBet = null;
    let collectingCommon = false;

    $("h3").each((_, el) => {
        const title = $(el).text().trim();
        const cleanTitle = cleanName(title);

        if (cleanTitle.includes(cleanName(home))) {
            let sibling = $(el).next();
            while (sibling.length && !sibling.is("h3")) {
                homeText += sibling.text().trim() + "\n\n";
                sibling = sibling.next();
            }
        }

        if (cleanTitle.includes(cleanName(away))) {
            let sibling = $(el).next();
            while (sibling.length && !sibling.is("h3")) {
                awayText += sibling.text().trim() + "\n\n";
                sibling = sibling.next();
            }
        }

        if (cleanTitle.includes(`${cleanName(home)} – ${cleanName(away)}: прогноз на матч`)) {
            collectingCommon = true;
            let sibling = $(el).next();
            while (sibling.length && !sibling.is("p:has(strong)")) {
                commonText += sibling.text().trim() + "\n\n";
                sibling = sibling.next();
            }
            const betLine = sibling.find("strong").text().trim();
            if (betLine) {
                mainBet = betLine;
            }
            collectingCommon = false;
        }
    });

    $("p").each((_, el) => {
        const text = $(el).text().trim();
        if (/Мой прогноз:/i.test(text)) {
            const strong = $(el).find("strong").text().trim();
            if (strong) {
                mainBet = strong;
            }
        } else if (collectingCommon) {
            commonText += text + "\n\n";
        }
    });

    const matchId = home && away ? findMatchId(home, away, calendar) : null;

    return {
        source: "legalbet.kz",
        match: `${home} – ${away}`,
        teams: {
            home: { name: home, text: homeText.trim() },
            away: { name: away, text: awayText.trim() },
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

/// <summary>
/// Основная функция: парсит список матчей с legalbet.kz,
/// заходит в каждый матч, достаёт прогнозы и сохраняет.
/// </summary>
export async function scrapePredictionsLegalbet() {
    const listUrl = `${BASE_URL}/hockey/tournaments/khl/`;

    const html = await fetchHtml(listUrl);
    const $ = cheerio.load(html);

    const calendarPath = path.join(OUTPUT_PATH, "russia_khl_all.json");
    const calendar = JSON.parse(fs.readFileSync(calendarPath, "utf-8"));

    const links = [];
    $(".match-table__item").each((_, el) => {
        const href = $(el).find("a.match-table__teams").attr("href");
        const home = $(el).find("meta[itemprop='homeTeam']").attr("content");
        const away = $(el).find("meta[itemprop='awayTeam']").attr("content");
        const dateStr = $(el).find("meta[itemprop='startDate']").attr("content");

        if (href && home && away && dateStr) {
            const matchDate = new Date(dateStr);
            const today = new Date();
            const tomorrow = new Date();
            tomorrow.setDate(today.getDate() + 1);

            const normalize = (d) => new Date(d.getFullYear(), d.getMonth(), d.getDate());
            const matchDay = normalize(matchDate);
            const todayDay = normalize(today);
            const tomorrowDay = normalize(tomorrow);

            if (matchDay.getTime() === todayDay.getTime() || matchDay.getTime() === tomorrowDay.getTime()) {
                links.push({
                    url: BASE_URL + href,
                    home,
                    away,
                    date: matchDate.toISOString(),
                });
            }
        }
    });

    const results = [];
    for (const { url, home, away } of links) {
        try {
            const data = await parseMatchPage(url, calendar, { home, away });
            if (data) results.push(data);
        } catch (err) {
            console.error(`Ошибка при парсинге ${url}:`, err.message);
        }
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
