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
export async function scrapeMatchEvents({ browser, matchId, matchUrl, homeTeam, awayTeam, logger }) {
    try {
        const page = await openPageAndNavigate(browser, matchUrl);
        await waitForSelectorSafe(page, ".smv__incident");
        const html = await page.content();
        await page.close();

        const $ = cheerio.load(html);
        const events = [];
        let currentPeriod = null;

        const mainContainer = $(".smv__verticalSections.section");
        if (!mainContainer.length) return events;

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

            // --- Событие ---
            if ($el.hasClass("smv__participantRow")) {
                const $incident = $el.find(".smv__incident, .smv__incidentIconSub");
                if ($incident.length === 0) return;

                const team = $el.hasClass("smv__homeParticipant") ? homeTeam : awayTeam;
                const time = $incident.find(".smv__timeBox").text().trim() || null;
                let event = { period: currentPeriod, time, team };

                // === ГОЛ ===
                if ($incident.find("svg.hockeyGoal-ico, svg.icon--goal").length > 0) {
                    const eventType = "Goal";
                    const scorer = $incident.find(".smv__playerName").text().trim();
                    const assistants = $incident
                        .find(".smv__assist a")
                        .map((_, a) => $(a).text().trim())
                        .get()
                        .filter(Boolean);
                    const goalTypeRaw = $incident.find(".smv__subIncident").text().replace(/[()]/g, "").trim();
                    const goalType = goalTypeRaw || "Even strength";
                    const score = $incident
                        .find(".smv__score, .smv__incidentHomeScore, .smv__incidentAwayScore")
                        .text()
                        .trim();

                    event.eventType = eventType;
                    if (scorer) event.scorer = scorer;
                    if (assistants.length) event.assistants = assistants;
                    if (score) event.score = score;
                    event.goalType = goalType;
                }

                // === УДАЛЕНИЕ ===
                else if ($incident.find("svg[class*='penalty']").length > 0) {
                    const eventType = "Penalty";
                    const player = $incident.find(".smv__playerName").text().trim() || null;

                    // --- Причина удаления ---
                    const reason =
                        $incident.find(".smv__subIncident").text().replace(/[()]/g, "").trim() ||
                        $incident.find("[title]").attr("title")?.trim() ||
                        null;

                    // --- Извлекаем длительность штрафа (2, 5, 10, 2+10, 2+2 и т.д.) ---
                    const penaltyIcons = $incident.find("svg[class*='penalty']");
                    const durations = [];

                    penaltyIcons.each((_, icon) => {
                        const cls = $(icon).attr("class") || "";
                        if (cls.includes("penalty-2-min")) durations.push("2");
                        else if (cls.includes("penalty-5-min")) durations.push("5");
                        else if (cls.includes("penalty-10-min")) durations.push("10");
                    });

                    // Если несколько штрафов подряд (например, 2+10), соединяем через "+"
                    const duration = durations.length > 1 ? durations.join("+") : durations[0] || null;

                    // --- Формируем объект события ---
                    event.eventType = eventType;
                    if (player) event.player = player;
                    if (reason) event.reason = reason;
                    if (duration) event.duration = duration;
                }

                // === ГОЛ НЕ ЗАСЧИТАН ===
                else if ($incident.find("svg.whistle-ico").length > 0) {
                    const eventType = "Goal disallowed";
                    const details =
                        $incident.find("svg.whistle-ico title").text().trim() ||
                        $incident.text().trim();

                    const player =
                        $incident.find(".smv__playerName").text().trim() ||
                        $incident.find(".smv__assist a").text().trim() ||
                        null;

                    event.eventType = eventType;
                    if (details) event.details = details;
                    if (player) event.player = player;
                }

                // === БУЛЛИТ НЕ РЕАЛИЗОВАН ===
                else if (currentPeriod === "SO" && $incident.find('svg[data-testid="wcl-icon-incidents-warning"]').length > 0) {
                    const eventType = "Shootout missed";

                    const details =
                        $incident.find("svg[data-testid='wcl-icon-incidents-warning'] title").text().trim() ||
                        $incident.find(".smv__subIncident").text().replace(/[()]/g, "").trim() ||
                        "Shootout missed";

                    const player =
                        $incident.find(".smv__playerName").text().trim() || null;

                    event.eventType = eventType;
                    if (details) event.details = details;
                    if (player) event.player = player;
                }

                // === ЗАМЕНА ВРАТАРЯ ===
                else if ($incident.find("svg.substitution").length > 0) {
                    const eventType = "Goalkeeper change";

                    // кто уходит (out) — всегда внутри контейнера SubOut
                    const goalieOut =
                        $incident
                            .find(".smv__incidentSubOut .smv__playerName, .smv__incidentSubOut a")
                            .first()
                            .text()
                            .trim() || null;

                    // кто выходит (in) — основной .smv__playerName рядом с иконкой замены
                    // (НЕ внутри SubOut)
                    const goalieIn =
                        $incident
                            .children("a.smv__playerName, .smv__playerName")
                            .first()
                            .text()
                            .trim() || null;

                    event.eventType = eventType;
                    if (goalieOut) event.goalieOut = goalieOut;
                    if (goalieIn) event.goalieIn = goalieIn;
                }

                // --- Добавляем, если тип события определён ---
                if (event.eventType) {
                    // Убираем пустые поля
                    for (const key in event) {
                        if (
                            event[key] === null ||
                            event[key] === undefined ||
                            (Array.isArray(event[key]) && event[key].length === 0)
                        ) {
                            delete event[key];
                        }
                    }
                    events.push(event);
                }
            }
        });

        // --- Сохранение ---
        let allEvents = {};
        try {
            if (fs.existsSync(FILES.KHL_EVENTS)) {
                const raw = fs.readFileSync(FILES.KHL_EVENTS, "utf-8").trim();
                if (raw) allEvents = JSON.parse(raw);
            }
        } catch {
            allEvents = {};
        }

        allEvents[matchId] = { id: matchId, home: homeTeam, away: awayTeam, events };
        fs.writeFileSync(FILES.KHL_EVENTS, JSON.stringify(allEvents, null, 2), "utf-8");

        return events;
    } catch (err) {
        logger.error(`[matchEventsParse] Ошибка при парсинге ${homeTeam} – ${awayTeam}: ${err.message}`);
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
    logger.info(`[scraperRunner] === ▶ Запуск скрипта: matchEventsParse ===`);
    logger.info(`[scraperRunner] --- Начало парсинга: matchEventsParse ---`);

    const matches = JSON.parse(fs.readFileSync(FILES.KHL_MATCHES, "utf-8"));

    const today = dayjs();
    const startDate = today.subtract(1, "day");
    //const startDate = dayjs("2025-09-05", "YYYY-MM-DD"); // Парсинг всех матчей с начала сезона

    logger.info(`[matchEventsParse] Диапазон: ${startDate.format("DD.MM.YYYY")} – ${today.format("DD.MM.YYYY")}`);

    const matchEntries = Object.entries(matches).filter(([_, match]) => {
        if (!match.date) return false;
        const date = dayjs(parseDate(match.date));
        return !date.isBefore(startDate, "day") && !date.isAfter(today, "day");
    });

    logger.info(`[matchEventsParse] Найдено ${matchEntries.length} матчей КХЛ за последние 2 дня.`);

    let updated = 0;

    for (const [matchId, match] of matchEntries) {
        const matchDate = dayjs(parseDate(match.date)).toISOString();
        logger.info(`[matchEventsParse] Матч: ${match.home?.name} – ${match.away?.name} | matchID: ${matchId} | Дата: ${matchDate}`);

        const events = await scrapeMatchEvents({
            browser,
            matchId,
            matchUrl: `${BASE_URL}/match/${matchId}/#/match-summary/match-summary`,
            homeTeam: match.home?.name,
            awayTeam: match.away?.name,
            logger,
        });

        if (events.length > 0) updated++;
    }

    const elapsed = ((Date.now() - startTime) / 1000).toFixed(1);
    logger.info(`[matchEventsParse] Итог: обновлено ${updated}/${matchEntries.length} матчей`);
    logger.info(`[scraperRunner] ✅ Завершён скрипт matchEventsParse (${elapsed} сек.)`);
}
