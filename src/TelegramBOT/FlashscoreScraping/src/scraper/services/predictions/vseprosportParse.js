// src/scraper/services/predictions/vseprosportParse.js
import * as cheerio from "cheerio";
import fs from "fs";
import { FILES } from "../../../constants/constants.js";
import { normalizeTeamName, findMatchId } from "../utils/matches/teamMapUtils.js";
import { createLogger } from "../utils/core/logger.js";
import { normalizePrediction } from "../utils/predictions/normalizePrediction.js";

const logger = createLogger("vseprosport");
const BASE_URL = "https://www.vseprosport.kz";

export { scrapePredictionsVseprosport as scrapePredictions };

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
/// Очищает текст от лишних слов, коэффициентов и пробелов.
/// </summary>
/// <param name="text">Исходный текст.</param>
/// <returns>Очищенный текст.</returns>
function cleanText(text) {
    if (!text) return "";
    return text
        .replace(/\s*с коэффициентом\s*[\d.,]+/gi, "")
        .replace(/\s*за\s*[\d.,]+/gi, "")
        .replace(/([.!?])\s{2,}/g, "$1 ")
        .replace(/\s+/g, " ")
        .trim();
}

/// <summary>
/// Парсит страницу конкретного прогноза с сайта vseprosport.kz.
/// </summary>
/// <param name="url">Ссылка на страницу прогноза.</param>
/// <param name="calendar">JSON-календарь матчей.</param>
/// <returns>Объект прогноза с данными матча.</returns>
async function parseMatchPage(url, calendar) {
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    // Извлечение названий команд
    let home = $("#prediction-teams-1 p.h3").first().text().trim();
    let away = $("#prediction-teams-2 p.h3").first().text().trim();

    home = normalizeTeamName(home);
    away = normalizeTeamName(away);
    const match = `${home} – ${away}`;

    // Дата матча
    const matchDateStr = $("time.matchdate").attr("datetime");
    const matchDate = matchDateStr ? new Date(matchDateStr) : null;
    if (!matchDate || isNaN(matchDate.getTime())) return null;

    const matchId = findMatchId(home, away, calendar, matchDate);
    logger.info(`Матч: ${home} – ${away} | matchID: ${matchId || "не найден"} | Дата: ${matchDate.toISOString()}`);

    // Основной прогноз
    let mainBet = $(".bonus-item-bet span.fw-medium").first().text().trim();
    mainBet = cleanText(mainBet);

    // Основной текст прогноза
    let textBlock = $("#prediction-section .default-content").first()
        .find("p")
        .map((_, el) => $(el).text())
        .get()
        .join(" ");
    textBlock = cleanText(textBlock);

    // Анализ по командам (текущая форма)
    let homeForm = $("#prediction-teams-1").next(".default-content").text().replace("Текущая форма", "").trim();
    let awayForm = $("#prediction-teams-2").next(".default-content").text().replace("Текущая форма", "").trim();

    homeForm = homeForm.replace(/\s+/g, " ");
    awayForm = awayForm.replace(/\s+/g, " ");

    return {
        source: "vseprosport",
        url,
        match,
        date: matchDate,
        teams: {
            home: { name: home, text: homeForm },
            away: { name: away, text: awayForm },
        },
        prediction: {
            main: mainBet
                ? normalizePrediction(mainBet, home, away)
                : null,
            text: textBlock || null,
            alt: null,
            result: null,
        },
        id: matchId,
    };
}

/// <summary>
/// Сохраняет результаты парсинга в JSON без дубликатов.
/// </summary>
/// <param name="results">Массив прогнозов.</param>
/// <returns>Количество добавленных новых прогнозов.</returns>
function saveResults(results) {
    const savePath = FILES.VSEPROSPORT;

    // Читаем уже сохранённые прогнозы
    let existing = [];
    if (fs.existsSync(savePath)) {
        try {
            existing = JSON.parse(fs.readFileSync(savePath, "utf-8"));
        } catch (err) {
            logger.error(`Ошибка чтения ${savePath}:`, err);
        }
    }

    // Убираем старые прогнозы без id, если пришёл новый с тем же матчем
    const updatedExisting = existing.filter(old => {
        if (!old.id && old.match) {
            const newer = results.find(r => r.match === old.match && r.id);
            return !newer; // если появился новый с id — старый отбрасываем
        }
        return true;
    });

    // Очищаем новые данные
    const cleanedResults = results
        .filter(r => r.id || r.match)
        .map(r => {
            const { date, ...rest } = r;
            return rest;
        });

    // Склеиваем всё и удаляем дубликаты по source + match
    const uniqueMap = {};
    for (const r of [...cleanedResults, ...updatedExisting]) {
        const key = `${r.source}_${r.match}`;
        if (r.id) uniqueMap[key] = r; // заменяем версию без id
        else if (!uniqueMap[key]) uniqueMap[key] = r;
    }

    const final = Object.values(uniqueMap);

    fs.writeFileSync(savePath, JSON.stringify(final, null, 2), "utf-8");
    logger.info(`Прогнозы сохранены в ${savePath}`);
    return final.length - existing.length;
}

/// <summary>
/// Главная функция парсера Vseprosport.  
/// Собирает список КХЛ-прогнозов, парсит каждую страницу и сохраняет результаты.
/// </summary>
/// <returns>Массив объектов прогнозов.</returns>
export async function scrapePredictionsVseprosport() {
    const listUrl = `${BASE_URL}/news/hockey`;
    const html = await fetchHtml(listUrl);
    const $ = cheerio.load(html);

    const calendar = JSON.parse(fs.readFileSync(FILES.KHL_MATCHES, "utf-8"));

    const links = [];
    const seen = new Set();

    $("#forecast-list-ajax .forecast").each((_, el) => {
        const type = $(el).find(".forecast-body .headgrey").first().text();
        if (type.includes("KHL")) {
            const href = $(el).find("a").attr("href");
            if (!href) return;
            const full = BASE_URL + href;
            if (seen.has(full)) return;
            seen.add(full);
            links.push(full);
        }
    });

    logger.info(`Найдено ${links.length} матчей.`);

    const rawResults = [];
    for (const link of links) {
        try {
            const data = await parseMatchPage(link, calendar);
            if (data) rawResults.push(data);
        } catch (err) {
            logger.error(`Ошибка при парсинге ${link}`, err);
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

    const added = saveResults(results);
    logger.info(`Итог: добавлено новых прогнозов ${added}/${results.length}`);

    return results;
}
