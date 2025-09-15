import fs from "fs";
import dayjs from "dayjs";
import { BASE_URL, OUTPUT_PATH } from "../../../constants/constants.js";
import { openPageAndNavigate, waitForSelectorSafe } from "../utils/pageUtils.js";
import { formatDateWithYear, parseDate } from "../utils/dateUtils.js";

//
// 👉 Получаем результат матча (один матч)
//
export const getMatchResult = async (browser, matchId) => {
    const page = await browser.newPage();
    await page.goto(`${BASE_URL}/match/${matchId}/`, {
        waitUntil: "domcontentloaded",
    });

    await waitForSelectorSafe(page, ".duelParticipant__startTime");

    // статус
    let status = null;
    try {
        status = await page.$eval(".fixedHeaderDuel__detailStatus", el => el.innerText.trim());
    } catch {
        status = null;
    }
    if (!status) {
        status = "SCHEDULED"; // пустая строка или null → матч ещё не начался
    }

    // счёт
    let homeScore = null;
    let awayScore = null;
    try {
        const scoreSpans = await page.$$eval(".detailScore__wrapper span", els =>
            els.map(e => e.innerText.trim())
        );
        if (scoreSpans.length >= 3) {
            homeScore = scoreSpans[0] || null;
            awayScore = scoreSpans[2] || null;
        }
    } catch {
        // счёта нет — матч не начался
    }

    const matchData = await page.evaluate(() => {
        return {
            date: document.querySelector(".duelParticipant__startTime")?.innerText.trim() || null,
            home: {
                name: document.querySelector(
                    ".duelParticipant__home .participant__participantName.participant__overflow"
                )?.innerText.trim() || null,
            },
            away: {
                name: document.querySelector(
                    ".duelParticipant__away .participant__participantName.participant__overflow"
                )?.innerText.trim() || null,
            },
        };
    });

    await page.close();

    return {
        ...matchData,
        status,
        result: { home: homeScore, away: awayScore },
    };
};



//
// 👉 Полный парсинг (результаты + календарь)
//
export const getAllMatches = async (browser, leagueSeasonUrl) => {
    const page = await openPageAndNavigate(browser, `${leagueSeasonUrl}/results`);

    // жмем "Показать ещё", пока есть
    while (true) {
        try {
            const moreBtn = await page.$("a.event__more");
            if (!moreBtn) break;
            await moreBtn.click();
            await new Promise((res) => setTimeout(res, 2000));
        } catch {
            break;
        }
    }

    await waitForSelectorSafe(page, ".event__match");

    const matchIds = await page.evaluate(() =>
        Array.from(document.querySelectorAll(".event__match"))
            .map((el) => el.id?.replace("g_4_", ""))
            .filter(Boolean)
    );

    const matches = {};
    for (const id of matchIds) {
        try {
            const result = await getMatchResult(browser, id);
            matches[id] = {
                ...result,
                date: formatDateWithYear(result.date),
            };
        } catch (err) {
            console.warn(`⚠️ Ошибка загрузки матча ${id}:`, err.message);
        }
    }

    await page.close();
    return matches;
};

//
// 👉 Быстрое обновление: только вчера и сегодня
//
export const updateRecentMatches = async (browser) => {
    // читаем уже сохранённый календарь
    const path = `${OUTPUT_PATH}/russia_khl_all.json`;
    if (!fs.existsSync(path)) {
        throw new Error("❌ Файл russia_khl_all.json не найден. Сначала нужно сделать --all");
    }
    const matches = JSON.parse(fs.readFileSync(path, "utf-8"));

    const today = dayjs();
    const yesterday = dayjs().subtract(1, "day");

    const isRecent = (dateStr) => {
        const d = dayjs(parseDate(dateStr));
        return d.isSame(today, "day") || d.isSame(yesterday, "day");
    };

    const idsToUpdate = Object.entries(matches)
        .filter(([_, match]) => isRecent(match.date))
        .map(([id]) => id);

    for (const id of idsToUpdate) {
        try {
            const result = await getMatchResult(browser, id);
            matches[id] = {
                ...matches[id],
                ...result,
                date: formatDateWithYear(result.date),
            };
        } catch (err) {
            console.warn(`⚠️ Ошибка обновления матча ${id}:`, err.message);
        }
    }

    // сохраняем обратно
    fs.writeFileSync(path, JSON.stringify(matches, null, 2), "utf-8");
    return matches;
};
