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
async function fetchHtml(url) {
    const res = await fetch(url, { headers: { "User-Agent": "Mozilla/5.0" } });
    return await res.text();
}

/// <summary>
/// Очищает текст прогноза.
/// </summary>
function cleanText(text) {
    if (!text) return null;
    return text
        .replace(/\s*с коэффициентом\s*[\d.,]+/gi, "")
        .replace(/\s*за\s*[\d.,]+/gi, "")
        .replace(/([.!?])\s{2,}/g, "$1 ")
        .trim();
}

/// <summary>
/// Парсит страницу конкретного матча vseprosport.kz и извлекает прогноз.
/// </summary>
/// <param name="url">URL страницы матча</param>
/// <param name="calendar">Календарь матчей КХЛ</param>
/// <returns>Объект прогноза</returns>
async function parseMatchPage(url, calendar) {
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    // 🔹 Названия команд
    let home = $("#prediction-teams-1 p.h3").first().text().trim();
    let away = $("#prediction-teams-2 p.h3").first().text().trim();

    home = normalizeTeamName(home);
    away = normalizeTeamName(away);

    // 🔹 Дата матча
    let matchDateStr = $("time.matchdate").attr("datetime");
    let matchDate = matchDateStr ? new Date(matchDateStr) : null;

    // 🔹 Основной прогноз (короткий, например "ЦСКА с форой (-1.5)")
    let mainBet = $(".bonus-item-bet span.fw-medium").first().text().trim();
    mainBet = cleanText(mainBet);

    // 🔹 Альтернативный прогноз (например "Тотал больше пяти шайб")
    let altBet = $(".expert-predictions .item .d-flex.align-items-center.gap-6.pb-2 span.fw-semibold.lh-1")
        .last()
        .text()
        .trim();
    altBet = cleanText(altBet);


    // 🔹 Полный текст прогноза (берём из блока <section id="prediction-section">)
    let textBlock = $("#prediction-section .default-content").first().find("p")
        .map((_, el) => $(el).text())
        .get()
        .join(" ");
    textBlock = textBlock.replace(/\s+/g, " ").trim();

    // 🔹 Текущая форма → analysis
    let homeForm = $("#prediction-teams-1").next(".default-content").text().replace("Текущая форма", "").trim();
    let awayForm = $("#prediction-teams-2").next(".default-content").text().replace("Текущая форма", "").trim();

    homeForm = homeForm.replace(/\s+/g, " ");
    awayForm = awayForm.replace(/\s+/g, " ");

    const prediction = {
        main: mainBet || null,
        alt: altBet || null,   // теперь будет "Тотал больше пяти шайб"
        score: null,
        text: textBlock || null,
        result: null,
    };


    const teams = {
        home: { name: home, analysis: homeForm },
        away: { name: away, analysis: awayForm }
    };

    // 🔹 Ищем matchId по командам + дате
    const matchId = findMatchId(home, away, calendar, matchDate);

    return {
        source: url,
        match: `${home} – ${away}`,
        teams,
        prediction,
        id: matchId,
    };
}



/// <summary>
/// Основная функция: парсит список прогнозов КХЛ с vseprosport.kz.
/// </summary>
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
