import fs from "fs";
import path from "path";
import * as cheerio from "cheerio";
import { OUTPUT_PATH } from "../../../constants/constants.js";
import { findMatchId } from "../utils/teamMapUtils.js";
import { appendUniqueJson } from "../utils/fileUtils.js";

/// <summary>
/// Проверяет прогноз на основе результата матча.
/// </summary>
/// <param name="prediction">Объект прогноза</param>
/// <param name="match">Данные матча из календаря</param>
/// <returns>
/// true — прогноз совпал, 
/// false — прогноз не совпал, 
/// null — нельзя проверить (например, матч не завершён)
/// </returns>
function checkPrediction(prediction, match) {
    if (!match || match.status !== "FINISHED") return null;

    const home = parseInt(match.result.home, 10);
    const away = parseInt(match.result.away, 10);
    const total = home + away;

    const main = prediction.main;
    if (!main) return null;

    if (main.startsWith("ТБ")) {
        const num = parseFloat(main.replace(/[^\d.]/g, ""));
        return total > num;
    }
    if (main.startsWith("ТМ")) {
        const num = parseFloat(main.replace(/[^\d.]/g, ""));
        return total < num;
    }
    if (main === "П1") return home > away;
    if (main === "П2") return away > home;

    if (main.startsWith("ИТБ1")) {
        const num = parseFloat(main.replace(/[^\d.]/g, ""));
        return home > num;
    }
    if (main.startsWith("ИТМ1")) {
        const num = parseFloat(main.replace(/[^\d.]/g, ""));
        return home < num;
    }
    if (main.startsWith("ИТБ2")) {
        const num = parseFloat(main.replace(/[^\d.]/g, ""));
        return away > num;
    }
    if (main.startsWith("ИТМ2")) {
        const num = parseFloat(main.replace(/[^\d.]/g, ""));
        return away < num;
    }

    return null;
}

/// <summary>
/// Загружает HTML-страницу по указанному URL.
/// </summary>
/// <param name="url">Ссылка на страницу</param>
/// <returns>HTML-содержимое в виде строки</returns>
async function fetchHtml(url) {
    const res = await fetch(url, {
        headers: { "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)" }
    });
    return await res.text();
}

/// <summary>
/// Парсит страницу матча и извлекает прогноз, текстовое описание и анализ по командам.
/// </summary>
/// <param name="url">Ссылка на страницу матча</param>
/// <returns>Объект прогноза с данными команд и текстом</returns>
async function parseMatchPage(url) {
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    const result = {
        source: "vprognoze",
        match: null,
        teams: {
            home: { name: null, analysis: "" },
            away: { name: null, analysis: "" }
        },
        prediction: { main: null, alt: null, score: null, text: "", result: null }
    };

    const h3Prediction = $("h3")
        .filter((_, el) => $(el).text().includes("Прогноз на матч"))
        .first();

    if (h3Prediction.length) {
        result.match = h3Prediction.text().replace("Прогноз на матч", "").trim();
        const [homeName, awayName] = result.match.split("–").map(s => s.trim());
        result.teams.home.name = homeName;
        result.teams.away.name = awayName;

        // Анализ по каждой команде
        ["home", "away"].forEach(side => {
            const teamName = result.teams[side].name;
            if (!teamName) return;

            const h3 = $("h3")
                .filter((_, el) => $(el).text().includes(teamName))
                .first();

            if (h3.length) {
                let analysis = [];
                let sibling = h3.next();
                while (sibling.length && sibling.is("p")) {
                    const t = sibling.text().trim();
                    if (t.startsWith("📊Статистика")) break;
                    analysis.push(t);
                    sibling = sibling.next();
                }
                result.teams[side].analysis = analysis.join("\n\n");
            }
        });

        // Блок с прогнозом
        let predictionText = [];
        let sibling = h3Prediction.next();
        while (sibling.length && sibling.is("p")) {
            const t = sibling.text().trim();
            predictionText.push(t);

            if (t.startsWith("✅ Основной прогноз")) {
                result.prediction.main = t.replace("✅ Основной прогноз:", "").trim();
            } else if (t.startsWith("💡 Альтернатива")) {
                result.prediction.alt = t.replace("💡 Альтернатива:", "").trim();
            } else if (t.startsWith("📊 Примерный счёт")) {
                result.prediction.score = t.replace("📊 Примерный счёт:", "").trim();
            }

            sibling = sibling.next();
        }
        result.prediction.text = predictionText.join("\n\n");
    }

    return result;
}

/// <summary>
/// Основная функция: парсит прогнозы с vprognoze.kz, оставляет только КХЛ,
/// сопоставляет с календарём и сохраняет новые прогнозы в JSON без перезаписи.
/// </summary>
/// <returns>Объединённый массив прогнозов (старые + новые)</returns>
export async function scrapePredictions() {
    const url =
        "https://vprognoze.kz/user/%D0%90%D0%BD%D0%B4%D1%80%D0%B5%D0%B9+%D0%A8%D0%B0%D1%80%D0%B0%D1%84%D1%83%D1%82%D0%B4%D0%B8%D0%BD%D0%BE%D0%B2/";
    const html = await fetchHtml(url);
    const $ = cheerio.load(html);

    // загружаем календарь матчей
    const calendarPath = path.join(OUTPUT_PATH, "russia_khl_all.json");
    const calendar = JSON.parse(fs.readFileSync(calendarPath, "utf-8"));

    const matchLinks = $(".mini-tip")
        .filter((_, el) => {
            const league = $(el).find(".mini-tip__league").text();
            return league.includes("КХЛ. Регулярный чемпионат");
        })
        .map((_, el) => $(el).find(".mini-tip__info a").attr("href"))
        .get()
        .filter(Boolean);

    const predictions = [];

    for (const href of matchLinks.slice(0, 10)) {
        try {
            const data = await parseMatchPage(href);

            if (data) {
                const { home, away } = data.teams;
                if (!home.name || !away.name) {
                    console.warn(`Пропускаем матч без названий: ${data.match}`);
                    continue;
                }

                const matchId = findMatchId(home.name, away.name, calendar);
                if (matchId) {
                    data.id = matchId;
                    const match = calendar[matchId];
                    data.prediction.result = checkPrediction(data.prediction, match);
                } else {
                    console.warn(`Не нашли ID для матча: ${data.match}`);
                }

                predictions.push(data);
            }
        } catch (err) {
            console.error(`Ошибка при обработке ${href}:`, err.message);
        }
    }

    const savePath = path.join(OUTPUT_PATH, "vprognoze.json");

    const { merged, added } = appendUniqueJson(
        savePath,
        predictions,
        (item) => `${item.source}_${item.id || item.match}`
    );

    console.log(`Добавлено новых прогнозов: ${added}`);
    console.log(`Прогнозы сохранены в ${savePath}`);

    return merged;
}
