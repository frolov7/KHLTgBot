import puppeteer from "puppeteer";
import fs from "fs";
import { FILES } from "../../constants/constants.js";
import { TEAM_MAP } from "../services/utils/matches/teamMapUtils.js";
import { createLogger } from "../services/utils/core/logger.js";

const MATCHES_FILE = FILES.KHL_MATCHES;
const OUTPUT_FILE = FILES.RESULT_VIDEOS;

const logger = createLogger("khlYoutubeParser");

/// <summary>
/// Парсит последние видеообзоры матчей КХЛ с YouTube-канала КХЛ,
/// сопоставляет их с матчами из календаря и сохраняет результат в JSON-файл.
///
/// Алгоритм работы:
/// 1. Загружает страницу https://www.youtube.com/@khl/videos.
/// 2. Находит видео с заголовками, содержащими фразу "Обзор матча Фонбет КХЛ сезон 2025/2026".
/// 3. Сопоставляет по дате и названию команд с календарём матчей (`khl_all_matches.json`).
/// 4. Добавляет только новые видеообзоры (без дублей).
/// 5. Сортирует итог по дате (по возрастанию).
/// </summary>
/// <param name="limit">Количество последних видео, которые нужно просканировать. По умолчанию — 20.</param>
/// <returns>Массив всех найденных и сохранённых видеообзоров.</returns>
export async function scrapeKhlYoutubeVideos(limit = 20) {
    const browser = await puppeteer.launch({
        headless: true,
        args: ["--no-sandbox", "--disable-setuid-sandbox"],
    });

    const page = await browser.newPage();
    await page.goto("https://www.youtube.com/@khl/videos", { waitUntil: "networkidle2" });

    await page.waitForSelector("ytd-rich-grid-media", { timeout: 15000 });

    const videos = await page.evaluate((limit) => {
        const items = Array.from(document.querySelectorAll("ytd-rich-grid-media"));
        return items.slice(0, limit).map((el) => {
            const titleEl = el.querySelector("#video-title");
            const linkEl = el.querySelector("#video-title-link");
            return {
                title: titleEl?.textContent.trim() || "",
                url: linkEl ? `https://www.youtube.com${linkEl.getAttribute("href")}` : null,
            };
        });
    }, limit);

    await browser.close();

    // Фильтруем только обзоры КХЛ
    const filtered = videos.filter(v =>
        v.title.includes("Обзор матча Фонбет КХЛ сезон 2025/2026")
    );

    logger.info(`Найдено ${filtered.length} видеообзоров матчей КХЛ`);

    if (!fs.existsSync(MATCHES_FILE)) {
        logger.error("Не найден файл календаря khl_all_matches.json");
        return;
    }

    const matches = JSON.parse(fs.readFileSync(MATCHES_FILE, "utf-8"));
    const newResults = [];

    for (const video of filtered) {
        const matchTitle = video.title.split("|")[0].trim();
        const dateMatch = video.title.match(/\d{2}\.\d{2}\.\d{4}/)?.[0];
        let foundId = null;

        for (const [id, match] of Object.entries(matches)) {
            if (!match.date || !match.home?.name || !match.away?.name) continue;

            const datePart = match.date.split(" ")[0];
            if (datePart !== dateMatch) continue;

            const home = match.home.name.toLowerCase();
            const away = match.away.name.toLowerCase();
            const titleLow = matchTitle.toLowerCase();

            if (
                titleLow.includes(home) ||
                titleLow.includes(away) ||
                teamMatchFromUtils(titleLow, home, away)
            ) {
                foundId = id;
                break;
            }
        }

        newResults.push({
            title: video.title,
            url: normalizeYoutubeUrl(video.url),
            id: foundId || "NOT_FOUND",
        });
    }

    // Загружаем старые записи (если есть)
    let existing = [];
    if (fs.existsSync(OUTPUT_FILE)) {
        try {
            existing = JSON.parse(fs.readFileSync(OUTPUT_FILE, "utf-8"));
        } catch {
            logger.warn("Не удалось прочитать старый resultVideos.json, создаём заново");
        }
    }

    // Удаляем дубли по нормализованным ссылкам
    const urlSet = new Set(existing.map(v => normalizeYoutubeUrl(v.url)));
    const trulyNew = newResults.filter(v => !urlSet.has(normalizeYoutubeUrl(v.url)));
    const merged = [...existing, ...trulyNew];

    // Сортировка по дате (по возрастанию)
    merged.sort((a, b) => {
        const dateA = a.title.match(/\d{2}\.\d{2}\.\d{4}/)?.[0];
        const dateB = b.title.match(/\d{2}\.\d{2}\.\д{4}/)?.[0];
        if (!dateA || !dateB) return 0;
        const [dA, mA, yA] = dateA.split(".").map(Number);
        const [dB, mB, yB] = dateB.split(".").map(Number);
        return new Date(yA, mA - 1, dA) - new Date(yB, mB - 1, dB);
    });

    fs.writeFileSync(OUTPUT_FILE, JSON.stringify(merged, null, 2), "utf-8");

    logger.info(`Добавлено ${trulyNew.length} новых видео. Всего ${merged.length}.`);
    if (trulyNew.length > 0) {
        logger.info("Новые видео:");
        for (const v of trulyNew) logger.info(`  - ${v.title} (${v.url})`);
    }

    return merged;
}

/// <summary>
/// Нормализует ссылку YouTube — удаляет дополнительные параметры
/// (например, "&pp=..." или "&ab_channel=...") для корректного сравнения.
/// </summary>
/// <param name="url">Исходный URL видео YouTube.</param>
/// <returns>Нормализованный URL без параметров и лишних слэшей.</returns>
function normalizeYoutubeUrl(url) {
    if (!url) return url;
    const base = url.split("&")[0];
    return base.replace(/\/+$/, "");
}

/// <summary>
/// Проверяет, совпадает ли название команд в заголовке видео
/// с командами из TEAM_MAP (рус/англ соответствие).
/// Используется для сопоставления видео с матчами.
/// </summary>
/// <param name="titleLow">Название видео в нижнем регистре.</param>
/// <param name="home">Название домашней команды (англ.).</param>
/// <param name="away">Название гостевой команды (англ.).</param>
/// <returns>true, если найдено совпадение по командам; иначе false.</returns>
function teamMatchFromUtils(titleLow, home, away) {
    for (const [rus, eng] of Object.entries(TEAM_MAP)) {
        const rusLower = rus.toLowerCase();
        const engLower = eng.toLowerCase();
        if (titleLow.includes(rusLower) && (home.includes(engLower) || away.includes(engLower))) {
            return true;
        }
    }
    return false;
}
