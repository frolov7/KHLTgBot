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
        args: [
            "--no-sandbox",
            "--disable-setuid-sandbox",
            "--disable-dev-shm-usage",
            "--disable-gpu",
            "--disable-features=site-per-process",
        ],
        defaultViewport: { width: 1366, height: 768 },
    });

    const page = await browser.newPage();
    page.setDefaultNavigationTimeout(60000); // увеличен таймаут до 60 секунд

    // 🧭 Загружаем YouTube с 3 попытками
    for (let attempt = 1; attempt <= 3; attempt++) {
        try {
            logger.info(`🌐 Открываем YouTube (попытка ${attempt}/3)...`);
            await page.goto("https://www.youtube.com/@khl/videos", {
                waitUntil: "networkidle2",
            });
            await page.waitForSelector("ytd-rich-grid-media", { timeout: 20000 });
            break;
        } catch (err) {
            logger.warn(`⚠️ Не удалось загрузить страницу YouTube (попытка ${attempt}): ${err.message}`);
            if (attempt === 3) {
                logger.error("❌ YouTube не загрузился после 3 попыток. Завершаем парсинг.");
                await browser.close();
                return;
            }
        }
    }

    // Извлекаем видео
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

    // 🎯 Фильтруем только обзоры КХЛ за последние 2 дня
    const filtered = videos
        .filter(v => v.title.includes("Обзор матча Фонбет КХЛ сезон 2025/2026"))
        .filter(v => {
            const dateStr = v.title.match(/\d{1,2}\.\d{1,2}\.\d{4}/)?.[0];
            if (!dateStr) return false;
            const [d, m, y] = dateStr.split(".").map(Number);
            const videoDate = new Date(y, m - 1, d);
            const now = new Date();
            const diffDays = (now - videoDate) / (1000 * 60 * 60 * 24);
            return diffDays <= 3 && diffDays >= 0; // только последние 2 дня
        });

    logger.info(`🎥 Найдено ${filtered.length} видеообзоров матчей КХЛ за последние 2 дня.`);

    // 📂 Проверяем наличие календаря
    if (!fs.existsSync(MATCHES_FILE)) {
        logger.error("❌ Не найден файл календаря khl_all_matches.json");
        return;
    }

    const matches = JSON.parse(fs.readFileSync(MATCHES_FILE, "utf-8"));
    const newResults = [];

    for (const video of filtered) {
        const titleLow = video.title.toLowerCase();
        const matchTitle = video.title.split("|")[0].trim();
        const rawDateMatch = video.title.match(/(\d{1,2})\.(\d{1,2})\.(\d{4})/);
        if (!rawDateMatch) {
            logger.warn(`[NO_DATE] Не найдена дата в названии: ${video.title}`);
            continue;
        }
        const [_, d, m, y] = rawDateMatch;
        const dateMatch = `${d.padStart(2, "0")}.${m.padStart(2, "0")}.${y}`;

        let foundId = null;

        // 🔍 Сопоставляем по дате и командам
        for (const [id, match] of Object.entries(matches)) {
            if (!match.date || !match.home?.name || !match.away?.name) continue;
            const matchDate = normalizeDate(match.date.split(" ")[0]);
            if (matchDate !== dateMatch) continue;

            const home = match.home.name.toLowerCase();
            const away = match.away.name.toLowerCase();
            if (teamMatchFromUtils(titleLow, home, away)) {
                foundId = id;
                break;
            }
        }

        if (foundId) {
            newResults.push({
                title: video.title,
                url: normalizeYoutubeUrl(video.url),
                id: foundId,
            });
        } else {
            const sameDate = Object.entries(matches)
                .filter(([_, m]) => normalizeDate(m.date?.split(" ")[0]) === dateMatch)
                .map(([_, m]) => `${m.home?.name} vs ${m.away?.name}`);

            logger.warn(`[NOT_FOUND] ${video.title}`);
            if (sameDate.length) {
                logger.warn(`  ⚙️ Матчи в календаре на ${dateMatch}:`);
                sameDate.forEach(m => logger.warn(`   - ${m}`));
            } else {
                logger.warn(`  ⚙️ В календаре нет матчей на ${dateMatch}`);
            }

            newResults.push({
                title: video.title,
                url: normalizeYoutubeUrl(video.url),
                id: "NOT_FOUND",
            });
        }
    }

    // 📦 Объединяем с предыдущими результатами
    let existing = [];
    if (fs.existsSync(OUTPUT_FILE)) {
        try {
            existing = JSON.parse(fs.readFileSync(OUTPUT_FILE, "utf-8"));
            existing = existing.filter(v => v.id !== "NOT_FOUND");
        } catch {
            logger.warn("⚠️ Ошибка чтения старого resultVideos.json, создаём заново.");
        }
    }

    const urlSet = new Set(existing.map(v => normalizeYoutubeUrl(v.url)));
    const trulyNew = newResults.filter(v => !urlSet.has(normalizeYoutubeUrl(v.url)));
    const merged = [...existing, ...trulyNew];

    // Сортировка по дате (по возрастанию)
    merged.sort((a, b) => {
        const dateA = a.title.match(/(\d{1,2})\.(\d{1,2})\.(\d{4})/)?.[0];
        const dateB = b.title.match(/(\d{1,2})\.(\d{1,2})\.(\d{4})/)?.[0];
        if (!dateA || !dateB) return 0;
        const [dA, mA, yA] = normalizeDate(dateA).split(".").map(Number);
        const [dB, mB, yB] = normalizeDate(dateB).split(".").map(Number);
        return new Date(yA, mA - 1, dA) - new Date(yB, mB - 1, dB);
    });

    fs.writeFileSync(OUTPUT_FILE, JSON.stringify(merged, null, 2), "utf-8");

    logger.info(`✅ Добавлено ${trulyNew.length} новых видео. Всего ${merged.length}.`);
    if (trulyNew.length > 0) {
        trulyNew.forEach(v => logger.info(`  - ${v.title} (${v.url})`));
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
    let clean = url.trim();

    // Приводим все варианты (watch, shorts, embed) к одному формату
    clean = clean
        .replace("https://www.youtube.com/shorts/", "https://www.youtube.com/watch?v=")
        .replace("https://youtube.com/shorts/", "https://www.youtube.com/watch?v=")
        .replace("https://youtu.be/", "https://www.youtube.com/watch?v=")
        .replace("https://youtube.com/watch?v=", "https://www.youtube.com/watch?v=")
        .replace("https://m.youtube.com/watch?v=", "https://www.youtube.com/watch?v=");

    // Убираем все параметры (?si=, &pp=, &ab_channel= и прочие)
    const idx = clean.indexOf("&");
    if (idx !== -1) clean = clean.substring(0, idx);
    const siIdx = clean.indexOf("?si=");
    if (siIdx !== -1) clean = clean.substring(0, siIdx);

    // Убираем возможные дублирующиеся слэши
    clean = clean.replace(/\/+$/, "");

    return clean;
}
/// <summary>
/// Приводит строку даты к формату "DD.MM.YYYY",
/// добавляя ведущие нули для дня и месяца при необходимости.
/// Используется для унификации формата дат перед сравнением.
/// </summary>
/// <param name="dateStr">Исходная строка даты, например "5.11.2025" или "05.11.2025".</param>
/// <returns>
/// Строка даты в нормализованном виде "DD.MM.YYYY", либо null,
/// если исходное значение не задано.
/// </returns>
function normalizeDate(dateStr) {
    if (!dateStr) return null;
    const [d, m, y] = dateStr.split(".");
    const dd = d.padStart(2, "0");
    const mm = m.padStart(2, "0");
    return `${dd}.${mm}.${y}`;
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
    const rusHome = Object.keys(TEAM_MAP).filter(
        r => TEAM_MAP[r].toLowerCase() === home
    );
    const rusAway = Object.keys(TEAM_MAP).filter(
        r => TEAM_MAP[r].toLowerCase() === away
    );

    const hasHome = rusHome.some(r => titleLow.includes(r.toLowerCase()));
    const hasAway = rusAway.some(r => titleLow.includes(r.toLowerCase()));
    return hasHome && hasAway;
}
