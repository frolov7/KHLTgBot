import fs from "fs";
import dayjs from "dayjs";
import { BASE_URL, OUTPUT_PATH } from "../../../constants/constants.js";
import { openPageAndNavigate, waitForSelectorSafe } from "../utils/pageUtils.js";
import { parseDate } from "../utils/dateUtils.js";

function normalizeStatus(rawStatus, homeScore, awayScore) {
    const status = (rawStatus || "").toUpperCase();

    if (status.includes("PEN") || status.includes("БУЛ")) return "AFTER PENALTIES";
    if (status.includes("OT") || status.includes("ОВЕР")) return "AFTER OVERTIME";
    if (status.includes("FINISHED") || status.includes("ЗАВЕРШ")) return "FINISHED";

    // если статус пустой, но счёт есть → матч завершён
    if (!rawStatus && homeScore !== null && awayScore !== null) {
        return "FINISHED";
    }

    return status || "SCHEDULED";
}

export const updateRecentMatches = async (browser) => {
    const path = `${OUTPUT_PATH}/russia_khl_all.json`;
    if (!fs.existsSync(path)) {
        throw new Error("Файл russia_khl_all.json не найден. Сначала нужно сделать --all");
    }
    const matches = JSON.parse(fs.readFileSync(path, "utf-8"));

    const today = dayjs();
    const yesterday = dayjs().subtract(1, "day");

    // 1. Парсим завершённые (results)
    const resultsUrl = `${BASE_URL}/hockey/russia/khl/results`;
    const resultsPage = await openPageAndNavigate(browser, resultsUrl);
    await waitForSelectorSafe(resultsPage, ".event__match");

    const scrapedResults = await resultsPage.evaluate(() => {
        return Array.from(document.querySelectorAll(".event__match")).map((el) => {
            const id = el.id?.replace("g_4_", "");
            const homeScore = el.querySelector(".event__score--home")?.innerText.trim() || null;
            const awayScore = el.querySelector(".event__score--away")?.innerText.trim() || null;
            const rawStatus = el.querySelector(".event__stage")?.innerText.trim() || "";
            return { id, rawStatus, homeScore, awayScore };
        });
    });

    await resultsPage.close();

    // 2. Парсим LIVE (overview)
    const liveUrl = `${BASE_URL}/hockey/russia/khl/`;
    const livePage = await openPageAndNavigate(browser, liveUrl);
    await waitForSelectorSafe(livePage, ".event__match");

    const scrapedLive = await livePage.evaluate(() => {
        return Array.from(document.querySelectorAll(".event__match")).map((el) => {
            const id = el.id?.replace("g_4_", "");
            const homeScore = el.querySelector(".event__score--home")?.innerText.trim() || null;
            const awayScore = el.querySelector(".event__score--away")?.innerText.trim() || null;
            const rawStatus = el.querySelector(".event__stage")?.innerText.trim() || "";
            return { id, rawStatus, homeScore, awayScore };
        });
    });

    await livePage.close();

    // 3. Объединяем
    const scrapedMatches = [...scrapedResults, ...scrapedLive];

    // 4. Нормализуем
    const normalizedMatches = scrapedMatches.map((m) => {
        const status = normalizeStatus(m.rawStatus, m.homeScore, m.awayScore);
        return {
            id: m.id,
            status,
            result: {
                home: status === "SCHEDULED" ? null : m.homeScore,
                away: status === "SCHEDULED" ? null : m.awayScore,
            },
        };
    });

    // 5. Фильтруем вчера + сегодня
    const recentIds = Object.entries(matches)
        .filter(([_, match]) => {
            const d = dayjs(parseDate(match.date));
            return d.isSame(today, "day") || d.isSame(yesterday, "day");
        })
        .map(([id]) => id);

    const recent = normalizedMatches.filter(
        (m) => recentIds.includes(m.id) || m.status === "LIVE"
    );

    console.log("Матчи за вчера и сегодня:");
    recent.forEach((m) => {
        const prev = matches[m.id];
        if (m.status === "LIVE") {
            console.log(`${prev?.date || "???"} | ${prev?.home?.name} vs ${prev?.away?.name} | LIVE ${m.result.home}:${m.result.away}`);
        } else {
            console.log(`${prev?.date || "???"} | ${prev?.home?.name} vs ${prev?.away?.name} | ${m.status} | ${m.result.home}:${m.result.away}`);
        }
    });

    // 6. Обновляем JSON
    const updatedIds = [];
    for (const match of recent) {
        if (matches[match.id]) {
            const prev = matches[match.id];
            const homeScore = match.result.home ?? prev.result.home;
            const awayScore = match.result.away ?? prev.result.away;
            const status = match.status !== "SCHEDULED" ? match.status : prev.status;

            matches[match.id] = {
                ...prev,
                status,
                result: { home: homeScore, away: awayScore },
            };

            updatedIds.push(match.id);
        }
    }

    fs.writeFileSync(path, JSON.stringify(matches, null, 2), "utf-8");

    if (updatedIds.length > 0) {
        console.log("Обновлены матчи:", updatedIds.join(", "));
    } else {
        console.log("Нет матчей для обновления за вчера/сегодня");
    }

    return matches;
};

