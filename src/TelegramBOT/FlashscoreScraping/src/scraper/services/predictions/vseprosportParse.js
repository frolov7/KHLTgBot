import * as cheerio from "cheerio";
import fs from "fs";
import path from "path";
import { OUTPUT_PATH } from "../../../constants/constants.js";
import { normalizeTeamName, findMatchId } from "../utils/teamMapUtils.js";
import { appendUniqueJson } from "../utils/fileUtils.js"; 

export { scrapePredictionsVseprosport as scrapePredictions };

/// <summary>
/// Загружает HTML-страницу по указанному URL.
/// </summary>
/// <param name="url">Адрес страницы</param>
/// <returns>HTML содержимое</returns>
async function fetchHtml(url) {
    const res = await fetch(url, { headers: { "User-Agent": "Mozilla/5.0" } });
    return await res.text();
}

/// <summary>
/// Очищает текст прогноза: удаляет коэффициенты, двойные пробелы и мусор.
/// </summary>
/// <param name="text">Исходный текст</param>
/// <returns>Очищенный текст</returns>
function cleanText(text) {
    if (!text) return null;
    return text
        .replace(/\s*с коэффициентом\s*[\d.,]+/gi, "")
        .replace(/\s*за\s*[\d.,]+/gi, "")
        .replace(/([.!?])\s{2,}/g, "$1 ")
        .trim();
}

/// <summary>
/// Парсит страницу конкретного матча на vseprosport.kz и извлекает прогноз.
/// </summary>
/// <param name="url">URL страницы матча</param>
/// <param name="calendar">Календарь КХЛ</param>
/// <returns>Объект прогноза с данными команд</returns>
async function parseMatchPage(url, calendar) {
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    // Названия команд
    let home = $("#prediction-teams-1 p.h3").first().text().trim();
    let away = $("#prediction-teams-2 p.h3").first().text().trim();

    home = normalizeTeamName(home);
    away = normalizeTeamName(away);

    // Основной прогноз 
    let mainBet = $(".bonus-item-bet span.fw-medium").first().text().trim();
    mainBet = cleanText(mainBet);

    // Текущая форма команд
    let homeForm = $("#prediction-teams-1")
        .next(".default-content")
        .text()
        .replace("Текущая форма", "")
        .trim();
    let awayForm = $("#prediction-teams-2")
        .next(".default-content")
        .text()
        .replace("Текущая форма", "")
        .trim();

    homeForm = homeForm.replace(/\s+/g, " ");
    awayForm = awayForm.replace(/\s+/g, " ");

    const text = `${home}: ${homeForm}\n\n${away}: ${awayForm}`;

    const prediction = {
        main: mainBet,
        alt: null,
        score: null,
        text,
        result: null,
    };

    const matchId = findMatchId(home, away, calendar);

    return {
        source: "vseprosport.kz",
        match: `${home} – ${away}`,
        teams: { home: { name: home }, away: { name: away } },
        prediction,
        id: matchId,
    };
}

/// <summary>
/// Основная функция: парсит список прогнозов КХЛ с vseprosport.kz,
/// добавляет новые прогнозы в JSON без перезаписи.
/// </summary>
/// <returns>Массив прогнозов (старые + новые)</returns>
export async function scrapePredictionsVseprosport() {
    const listUrl = "https://www.vseprosport.kz/news/hockey";
    const html = await fetchHtml(listUrl);
    const $ = cheerio.load(html);

    const calendarPath = path.join(OUTPUT_PATH, "russia_khl_all.json");
    const calendar = JSON.parse(fs.readFileSync(calendarPath, "utf-8"));

    // ссылки на КХЛ прогнозы
    const links = [];
    $("#forecast-list-ajax .forecast").each((_, el) => {
        const type = $(el).find(".forecast-body .headgrey").first().text();
        if (type.includes("KHL")) {
            const href = $(el).find("a").attr("href");
            if (href) links.push("https://www.vseprosport.kz" + href);
        }
    });

    const results = [];
    for (const link of links.slice(0, 10)) {
        try {
            const data = await parseMatchPage(link, calendar);
            results.push(data);
        } catch (err) {
            console.error(`Ошибка при парсинге ${link}:`, err.message);
        }
    }

    const savePath = path.join(OUTPUT_PATH, "vseprosport.json");

    const { merged, added } = appendUniqueJson(
        savePath,
        results,
        (item) => `${item.source}_${item.id || item.match}`
    );

    console.log(`Добавлено новых прогнозов: ${added}`);
    console.log(`Прогнозы с vseprosport.kz сохранены в ${savePath}`);

    return merged;
}
