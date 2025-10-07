import fs from "fs";
import path from "path";
import * as cheerio from "cheerio";
import { OUTPUT_PATH } from "../../../constants/constants.js";
import { findMatchId, normalizeTeamName, TEAM_MAP } from "../utils/teamMapUtils.js";
import { appendUniqueJson } from "../utils/fileUtils.js";

/// <summary>
/// Загружает HTML страницу по указанному URL.
/// Используется для получения страниц прогнозов с сайта.
/// </summary>
/// <param name="url">URL страницы</param>
/// <returns>HTML в виде строки</returns>
async function fetchHtml(url) {
    const res = await fetch(url, {
        headers: { "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)" }
    });
    return await res.text();
}

/// <summary>
/// Заменяет все алиасы команд на нормализованные имена из TEAM_MAP.
/// Например: "Шанхай Дрэгонс" → "Shanghai".
/// </summary>
/// <param name="text">Исходный текст прогноза</param>
/// <returns>Текст с нормализованными названиями команд</returns>
function replaceTeamAliases(text) {
    for (const [alias, normalized] of Object.entries(TEAM_MAP)) {
        const regex = new RegExp(alias, "gi");
        text = text.replace(regex, normalized);
    }
    return text;
}

/// <summary>
/// Разбирает строку даты и времени матча в объект Date.
/// Поддерживает форматы "dd mon" и "dd-mm-yyyy".
/// </summary>
/// <param name="dateStr">Строка с датой</param>
/// <param name="timeStr">Строка с временем</param>
/// <param name="matchTitle">Название матча (для логов)</param>
/// <returns>Объект Date или строка "FINISHED"</returns>
function parseMatchDate(dateStr, timeStr, matchTitle) {
    if (!dateStr) return null;

    if (dateStr.includes("Завершен")) return null; 

    const months = {
        янв: 0, фев: 1, мар: 2, апр: 3, май: 4, июн: 5,
        июл: 6, авг: 7, сен: 8, окт: 9, ноя: 10, дек: 11
    };

    const parts = dateStr.split(" ");
    if (parts.length === 2) {
        const [dayStr, monthStr] = parts;
        const day = parseInt(dayStr, 10);
        const month = months[monthStr.toLowerCase()];
        const [hours, minutes] = timeStr.split(":").map(Number);
        const year = new Date().getFullYear();

        if (!isNaN(day) && month !== undefined) {
            const d = new Date(year, month, day, hours, minutes);
            console.log(`📅 Parsed date (dd mon hh:mm): ${d.toISOString()} | raw: "${dateStr} ${timeStr}" для ${matchTitle}`);
            return d;
        }
    }

    if (dateStr.includes("-")) {
        const [d, m, y] = dateStr.split("-").map(Number);
        const [h, min] = (timeStr || "00:00").split(":").map(Number);
        const dt = new Date(y, m - 1, d, h, min);
        console.log(`📅 Parsed date (dd-mm-yyyy): ${dt.toISOString()} | raw: "${dateStr} ${timeStr}" для ${matchTitle}`);
        return dt;
    }

    console.warn(`⚠️ Не удалось разобрать дату: ${dateStr} ${timeStr} для ${matchTitle}`);
    return null;
}

/// <summary>
/// Парсит страницу прогноза.
/// Извлекает названия команд, дату матча, полный текст прогноза,
/// анализ по командам и прогнозы (основной, альтернатива, счёт).
/// </summary>
/// <param name="url">URL страницы прогноза</param>
/// <returns>Объект с данными прогноза</returns>
async function parseMatchPage(url) {
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    const result = {
        match: null,
        teams: { home: { name: null, analysis: "" }, away: { name: null, analysis: "" } },
        prediction: { main: null, alt: null, score: null, text: "" },
        matchDate: null,
    };

    // названия команд
    const names = $(".v3-top-forecast-header__match-name span")
        .map((_, el) => normalizeTeamName($(el).text()))
        .get();

    if (names.length === 2) {
        result.teams.home.name = names[0];
        result.teams.away.name = names[1];
        result.match = `${names[0]} – ${names[1]}`;
    }

    // дата и время
    const spans = $(".v3-top-forecast-header__match-timer span");
    const timeStr = spans.first().text().trim();
    const dateStr = spans.last().text().trim();
    result.matchDate = parseMatchDate(dateStr, timeStr, result.match);

    if (!result.matchDate) return null; 

    // полный текст прогноза
    let fullText = $(".v3-forecast-card-description__text").text().trim();

    // приводим текст к нормализованным названиям команд
    fullText = replaceTeamAliases(fullText);
    result.prediction.text = fullText;

    // разбиваем текст на анализ по командам
    if (result.teams.home.name && result.teams.away.name) {
        const homeName = TEAM_MAP[result.teams.home.name] || result.teams.home.name;
        const awayName = TEAM_MAP[result.teams.away.name] || result.teams.away.name;

        const homeIdx = fullText.indexOf(homeName);
        const awayIdx = fullText.indexOf(awayName);

        if (homeIdx !== -1 && awayIdx !== -1) {
            if (homeIdx < awayIdx) {
                result.teams.home.analysis = fullText
                    .slice(homeIdx + homeName.length, awayIdx)
                    .split("📊")[0]
                    .trim();

                result.teams.away.analysis = fullText
                    .slice(awayIdx + awayName.length)
                    .split("📊")[0]
                    .trim();
            } else {
                result.teams.away.analysis = fullText
                    .slice(awayIdx + awayName.length, homeIdx)
                    .split("📊")[0]
                    .trim();

                result.teams.home.analysis = fullText
                    .slice(homeIdx + homeName.length)
                    .split("📊")[0]
                    .trim();
            }
        }
    }

    $(".v3-forecast-card-description__text p").each((_, el) => {
        const txt = $(el).text().trim();
        if (txt.startsWith("✅ Основной прогноз")) {
            result.prediction.main = txt.replace("✅ Основной прогноз:", "").trim();
        } else if (txt.startsWith("💡 Альтернатива")) {
            result.prediction.alt = txt.replace("💡 Альтернатива:", "").trim();
        } else if (txt.startsWith("📊 Примерный счёт")) {
            result.prediction.score = txt.replace("📊 Примерный счёт:", "").trim();
        }
    });

    return result;
}

/// Основная функция для сбора прогнозов.
/// Загружает список прогнозов пользователя, парсит каждую страницу,
/// ищет ID матча в календаре и сохраняет результаты в JSON.
/// </summary>
/// <returns>Массив объединённых прогнозов</returns>
export async function scrapePredictions() {
    const url = "https://vprognoze.kz/user/Андрей+Шарафутдинов/";
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    const calendarPath = path.join(OUTPUT_PATH, "russia_khl_all.json");
    const calendar = JSON.parse(fs.readFileSync(calendarPath, "utf-8"));

    const predictions = [];

    // список прогнозов
    const links = $(".mini-tip-list .mini-tip .mini-tip__teams")
        .map((_, el) => $(el).attr("href"))
        .get();

    for (const href of links) {
        try {
            const data = await parseMatchPage(href);

            if (data === null) {
                break;
            }

            if (!data.match) {
                console.warn(`⚠️ Пропускаем матч без названия: ${href}`);
                continue;
            }

            if (!data.matchDate)
                continue;

            const { home, away } = data.teams;
            const matchId = findMatchId(home.name, away.name, calendar, data.matchDate);
            if (matchId)
                data.id = matchId;
            else 
                console.warn(`⚠️ Не нашли ID для матча: ${data.match}`);

            predictions.push({
                source: "vprognoze",
                url: href,
                match: data.match,
                teams: data.teams,
                prediction: data.prediction,
                id: data.id
            });
        } catch (err) {
            console.error(`Ошибка при обработке ${href}:`, err.message);
        }
    }

    const savePath = path.join(OUTPUT_PATH, "vprognoze.json");
    const { merged, added } = appendUniqueJson(
        savePath,
        predictions,
        (item) =>
            `${item.source}_${item.id || (item.match + "_" + (item.matchDate ? item.matchDate.toISOString() : ""))}`
    );

    console.log(`Добавлено новых прогнозов: ${added}`);
    console.log(`Прогнозы сохранены в ${savePath}`);

    return merged;
}
