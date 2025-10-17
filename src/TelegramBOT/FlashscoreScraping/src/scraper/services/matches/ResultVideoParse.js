import puppeteer from "puppeteer";
import fs from "fs";
import path from "path";
import { OUTPUT_PATH } from "../../../constants/constants.js";
import { TEAM_MAP } from "../utils/teamMapUtils.js";

const MATCHES_FILE = path.join(OUTPUT_PATH, "russia_khl_all.json");
const OUTPUT_FILE = path.join(OUTPUT_PATH, "resultVideos.json");

/**
 * Парсит последние видеообзоры матчей КХЛ с YouTube-канала КХЛ,
 * сопоставляет их с матчами из календаря и сохраняет результат в JSON-файл.
 *
 *  - Находит все видео с текстом "Обзор матча Фонбет КХЛ сезон 2025/2026"
 *  - Сопоставляет по дате и названию команд с файла `russia_khl_all.json`
 *  - Добавляет только новые видео (без дублей)
 *  - Сортирует по дате (по возрастанию)
 *
 * @async
 * @param {number} [limit=20] — Количество последних видео, которые нужно просканировать.
 * @returns {Promise<Array>} Массив всех найденных и сохранённых видеообзоров.
 */
export async function scrapeKhlYoutubeVideos(limit = 20) {
    const browser = await puppeteer.launch({
        headless: true,
        args: ["--no-sandbox", "--disable-setuid-sandbox"],
    });

    const page = await browser.newPage();
    await page.goto("https://www.youtube.com/@khl/videos", {
        waitUntil: "networkidle2",
    });

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

    // Берём только обзоры
    const filtered = videos.filter(v =>
        v.title.includes("Обзор матча Фонбет КХЛ сезон 2025/2026")
    );

    console.log(`Найдено ${filtered.length} видеообзоров матчей КХЛ`);

    if (!fs.existsSync(MATCHES_FILE)) {
        console.error("Не найден russia_khl_all.json");
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

    // Загружаем старые записи
    let existing = [];
    if (fs.existsSync(OUTPUT_FILE)) {
        try {
            existing = JSON.parse(fs.readFileSync(OUTPUT_FILE, "utf-8"));
        } catch {
            console.warn("Не удалось прочитать старый resultVideos.json, создаём заново");
        }
    }

    // Удаляем дубли (по нормализованным URL)
    const urlSet = new Set(existing.map(v => normalizeYoutubeUrl(v.url)));

    const trulyNew = newResults.filter(v => !urlSet.has(normalizeYoutubeUrl(v.url))); // ✅ реально новые
    const merged = [...existing, ...trulyNew];

    // Сортируем по дате (по возрастанию)
    merged.sort((a, b) => {
        const dateA = a.title.match(/\d{2}\.\d{2}\.\d{4}/)?.[0];
        const dateB = b.title.match(/\d{2}\.\d{2}\.\d{4}/)?.[0];
        if (!dateA || !dateB) return 0;
        const [dA, mA, yA] = dateA.split(".").map(Number);
        const [dB, mB, yB] = dateB.split(".").map(Number);
        return new Date(yA, mA - 1, dA) - new Date(yB, mB - 1, dB);
    });

    fs.writeFileSync(OUTPUT_FILE, JSON.stringify(merged, null, 2), "utf-8");

    console.log(`Добавлено ${trulyNew.length} новых видео. Всего ${merged.length}.`);
    if (trulyNew.length > 0) {
        console.log("Новые видео:");
        for (const v of trulyNew) {
            console.log(`  - ${v.title} (${v.url})`);
        }
    }
    return merged;
}

/**
 * Нормализует ссылку YouTube — удаляет все дополнительные параметры
 * (например, "&pp=..." или "&ab_channel=..."), чтобы корректно проверять дубли.
 *
 * @param {string} url — Исходный URL-адрес видео YouTube.
 * @returns {string} Нормализованный URL без параметров и лишних слэшей.
 */
function normalizeYoutubeUrl(url) {
    if (!url) return url;
    const base = url.split("&")[0]; // убираем параметры
    return base.replace(/\/+$/, ""); // удаляем лишний слеш на конце
}

/**
 * Проверяет, совпадает ли название команд в названии видео
 * с данными из TEAM_MAP (рус/англ соответствие).
 *
 * Используется для определения, к какому матчу относится видео.
 *
 * @param {string} titleLow — Название видео в нижнем регистре.
 * @param {string} home — Название домашней команды (англ. или рус.).
 * @param {string} away — Название гостевой команды (англ. или рус.).
 * @returns {boolean} true — если найдено соответствие команд; иначе false.
 */
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
