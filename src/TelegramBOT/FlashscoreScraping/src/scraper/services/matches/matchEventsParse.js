import fs from "fs";
import * as cheerio from "cheerio";
import { BASE_URL, FILES } from "../../../constants/constants.js";
import { openPageAndNavigate, waitForSelectorSafe } from "../utils/core/pageUtils.js";

/// <summary>
/// Преобразует название периода матча в стандартный формат (строковое обозначение).
/// </summary>
/// <param name="text">Текстовое название периода, извлечённое из HTML (например, "1-й период", "Overtime").</param>
/// <returns>
/// Строка, соответствующая стандартному обозначению периода: 
/// "1st period", "2nd period", "3rd period", "OT", "SO"
/// </returns>
function parsePeriodTitle(text) {
    const lower = text.toLowerCase();
    if (lower.includes("1-й") || lower.includes("1st")) return "1st period";
    if (lower.includes("2-й") || lower.includes("2nd")) return "2nd period";
    if (lower.includes("3-й") || lower.includes("3rd")) return "3rd period";
    if (lower.includes("овертайм") || lower.includes("overtime")) return "OT";
    if (lower.includes("буллит") || lower.includes("penalties")) return "SO";
    return null;
}

/// <summary>
/// Выполняет парсинг голов одного конкретного матча КХЛ с сайта Flashscore.
/// </summary>
/// <param name="browser">Экземпляр Puppeteer Browser, используемый для навигации по страницам.</param>
/// <param name="matchId">Уникальный идентификатор матча Flashscore (например, "boRXxDHG").</param>
/// <param name="matchUrl">Полная ссылка на страницу матча для парсинга.</param>
/// <param name="homeTeam">Название домашней команды.</param>
/// <param name="awayTeam">Название гостевой команды.</param>
/// <param name="logger">Экземпляр логгера для записи процесса выполнения.</param>
/// <returns>
/// Массив объектов с информацией о каждом голе, включая:
/// период, время, счёт, автора, ассистентов, команду и тип гола.
/// </returns>
/// <remarks>
/// Функция открывает страницу матча в Puppeteer, парсит DOM через Cheerio,
/// извлекает события голов (включая буллиты), сохраняет результаты в JSON (`KHL_EVENTS.json`)
/// и возвращает массив найденных голов.
/// </remarks>
export async function scrapeGoals({ browser, matchId, matchUrl, homeTeam, awayTeam, logger }) {
    try {
        const page = await openPageAndNavigate(browser, matchUrl);
        await waitForSelectorSafe(page, ".smv__incident");
        const html = await page.content();
        await page.close();

        const $ = cheerio.load(html);
        const goals = [];
        let currentPeriod = null;

        const mainContainer = $(".smv__verticalSections.section");
        if (!mainContainer.length) return goals;

        mainContainer.find("div").each((_, el) => {
            const $el = $(el);
            const classAttr = $el.attr("class") || "";

            // --- Заголовок периода ---
            if (classAttr.includes("wclHeaderSection--summary")) {
                const titleText = $el.text().trim();
                const parsed = parsePeriodTitle(titleText);
                if (parsed) currentPeriod = parsed;
                return;
            }

            // --- Событие: гол ---
            if ($el.hasClass("smv__participantRow")) {
                const $incident = $el.find(".smv__incident");
                if ($incident.length === 0) return;

                const isGoal = $incident.find("svg.hockeyGoal-ico, svg.icon--goal").length > 0;
                if (!isGoal) return;

                const team = $el.hasClass("smv__homeParticipant") ? homeTeam : awayTeam;
                const time = $incident.find(".smv__timeBox").text().trim() || null;
                const score =
                    $incident.find(".smv__score, .smv__incidentHomeScore, .smv__incidentAwayScore").text().trim() || null;
                const scorer = $incident.find(".smv__playerName").text().trim() || null;

                const assistants = $incident
                    .find(".smv__assist a")
                    .map((_, a) => $(a).text().trim())
                    .get()
                    .filter(Boolean);

                const goalTypeRaw = $incident.find(".smv__subIncident").text().replace(/[()]/g, "").trim();
                const goalType = goalTypeRaw || "Even strength";

                goals.push({
                    period: currentPeriod,
                    time,
                    score: score || null,
                    scorer,
                    assistants: assistants.length ? assistants : null,
                    team,
                    goalType
                });
            }
        });

        // --- Обработка буллитов (SO) ---
        const shootoutGoals = goals.filter((g) => g.period === "SO");
        if (shootoutGoals.length > 0) {
            let homeCount = 0;
            let awayCount = 0;
            shootoutGoals.forEach((g) => {
                if (g.team === homeTeam) homeCount++;
                else if (g.team === awayTeam) awayCount++;
                g.score = `${homeCount}-${awayCount}`;
            });
        }

        // --- Сохранение результатов ---
        let allGoals = {};
        try {
            if (fs.existsSync(FILES.KHL_EVENTS)) {
                const raw = fs.readFileSync(FILES.KHL_EVENTS, "utf-8").trim();
                if (raw) allGoals = JSON.parse(raw);
            }
        } catch {
            allGoals = {};
        }

        allGoals[matchId] = { id: matchId, home: homeTeam, away: awayTeam, goals };
        fs.writeFileSync(FILES.KHL_EVENTS, JSON.stringify(allGoals, null, 2), "utf-8");

        return goals;
    } catch (err) {
        logger.error(`[parseEvents] Ошибка при парсинге ${homeTeam} – ${awayTeam}: ${err.message}`);
        return [];
    }
}

/// <summary>
/// Выполняет массовый парсинг голов за последние три дня (позавчера, вчера, сегодня).
/// </summary>
/// <param name="browser">Экземпляр Puppeteer Browser для загрузки страниц.</param>
/// <param name="logger">Объект логгера для записи статусов выполнения.</param>
/// <returns>
/// Асинхронная операция без возвращаемого значения. Все результаты сохраняются в JSON (`KHL_EVENTS.json`).
/// </returns>
/// <remarks>
/// 1. Загружает список всех матчей КХЛ из `KHL_MATCHES.json`.
/// 2. Фильтрует матчи по диапазону (текущая дата минус два дня → сегодня).
/// 3. Для каждого подходящего матча вызывает `scrapeGoals()`.
/// 4. Ведёт детальный лог хода выполнения и итоговую статистику.
/// </remarks>
export async function scrapeRecentEvents({ browser, logger }) {
    const dayjs = (await import("dayjs")).default;
    const { parseDate } = await import("../utils/core/dateUtils.js");

    const startTime = Date.now();
    logger.info(`[scraperRunner] === ▶ Запуск скрипта: parseGoals ===`);
    logger.info(`[scraperRunner] --- Начало парсинга: parseGoals ---`);

    const matches = JSON.parse(fs.readFileSync(FILES.KHL_MATCHES, "utf-8"));

    // Диапазон: позавчера → сегодня
    const today = dayjs();
    const startDate = today.subtract(2, "day");

    logger.info(`[parseEvents] Диапазон: ${startDate.format("DD.MM.YYYY")} – ${today.format("DD.MM.YYYY")}`);

    // --- Фильтрация матчей по датам ---
    const matchEntries = Object.entries(matches).filter(([_, match]) => {
        if (!match.date) return false;
        const date = dayjs(parseDate(match.date));
        return !date.isBefore(startDate, "day") && !date.isAfter(today, "day");
    });

    logger.info(`[parseEvents] Найдено ${matchEntries.length} матчей КХЛ за последние 3 дня.`);

    let updated = 0;

    // --- Обработка матчей ---
    for (const [matchId, match] of matchEntries) {
        const matchDate = dayjs(parseDate(match.date)).toISOString();
        logger.info(`[parseEvents] Матч: ${match.home?.name} – ${match.away?.name} | matchID: ${matchId} | Дата: ${matchDate}`);

        const goals = await scrapeGoals({
            browser,
            matchId,
            matchUrl: `${BASE_URL}/match/${matchId}/#/match-summary/match-summary`,
            homeTeam: match.home?.name,
            awayTeam: match.away?.name,
            logger,
        });

        if (goals.length > 0) updated++;
    }

    // --- Завершение ---
    const elapsed = ((Date.now() - startTime) / 1000).toFixed(1);
    logger.info(`[parseEvents] Итог: обновлено ${updated}/${matchEntries.length} матчей`);
    logger.info(`[scraperRunner] ✅ Завершён скрипт parseGoals (${elapsed} сек.)`);
}
