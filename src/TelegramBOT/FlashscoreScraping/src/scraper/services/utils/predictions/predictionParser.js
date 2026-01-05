// predictionParser.js
import { TEAM_MAP, normalizeTeamName } from "../matches/teamMapUtils.js";

const toNum = (s) => parseFloat(String(s).replace(",", "."));

// Нормализация строки прогноза
const norm = (s) =>
    (s || "")
        .trim()
        .replace(/[\u00AB\u00BB“”]/g, '"') // «»“” → "
        .replace(/[—–]/g, "-")
        .replace(/\s+/g, " ")
        .toLowerCase();

// падежные формы названий команд
const TEAM_CASES = {
    "сибири": "Сибирь",
    "лады": "Лада",
    "автомобилиста": "Автомобилист",
    "металлурга": "Металлург",
    "авангарда": "Авангард",
    "торпедо": "Торпедо",
    "динамо": "Динамо",
    "цска": "ЦСКА",
    "спартака": "Спартак",
    "салавата": "Салават Юлаев",
    "ак барса": "Ак Барс",
};

/// Разбирает текст прогноза в структурированный объект
export function parsePrediction(text) {
    if (!text) return { type: "UNKNOWN", raw: text };

    const src = text;
    const s = norm(text);
    let m;

    // короткие обозначения побед
    if (/^п1$/.test(s)) return { type: "WINNER", team: "home", condition: "WIN", raw: src };
    if (/^п2$/.test(s)) return { type: "WINNER", team: "away", condition: "WIN", raw: src };
    if (/^(x|ничья)$/.test(s)) return { type: "WINNER", condition: "DRAW", raw: src };

    // Победа команды в основное время
    if ((m = s.match(/победа\s+[«"]?([^"»]+)[»"]?\s+в\s+основное\s+время/))) {
        return { type: "WINNER", teamByName: m[1], condition: "WIN_FT", raw: src };
    }

    // Победа команды в матче
    if ((m = s.match(/победа\s+[«"]?([^"»]+)[»"]?\s+в\s+матче/))) {
        return { type: "WINNER", teamByName: m[1], condition: "WIN", raw: src };
    }

    // Победа команды в конце строки
    if ((m = s.match(/победа\s+[«"]?([^"»]+)[»"]?\.?$/))) {
        return { type: "WINNER", teamByName: m[1], condition: "WIN", raw: src };
    }

    // двойной шанс: 1X, X2, 12, 1х, х2, 1Х, Х2
    if ((m = s.match(/^(1x|1х|x1|х1)$/i)))
        return { type: "DOUBLE_CHANCE", variant: "1X", raw: src };

    if ((m = s.match(/^(x2|х2|2x|2х)$/i)))
        return { type: "DOUBLE_CHANCE", variant: "X2", raw: src };

    if ((m = s.match(/^(12)$/i)))
        return { type: "DOUBLE_CHANCE", variant: "12", raw: src };

    // “Команда не проиграет”
    if ((m = s.match(/"?(.*?)"?\s+не\s+проиграет/))) {
        return {
            type: "DOUBLE_CHANCE",
            teamByName: m[1],
            dcMode: "NOLOSE",
            raw: src
        };
    }

    // ОБЕ ЗАБЬЮТ — расширенная версия с числом
    if ((m = s.match(/^оз\s*\(?\s*([\d.,]+)\s*\)?$/))) {
        return {
            type: "BOTH_TEAMS_SCORE",
            value: toNum(m[1]),
            raw: src
        };
    }

    // ОБЕ ЗАБЬЮТ БОЛЬШЕ X – Да/Нет
    if ((m = s.match(/каждая команда забь[её]т.*больше\s*\(?\s*([\d.,]+)\s*\)?\s*[-–]?\s*(да|нет)?/))) {
        return {
            type: "BOTH_TEAMS_SCORE",
            value: toNum(m[1]),
            answer: m[2] ? m[2].toLowerCase() : "да",
            raw: src
        };
    }

    // тоталы матча
    if ((m = s.match(/^тб\s*\(?\s*([\d.,]+)\s*\)?$/))) {
        return { type: "TOTAL", condition: ">", value: toNum(m[1]), raw: src };
    }
    if ((m = s.match(/^тм\s*\(?\s*([\d.,]+)\s*\)?$/))) {
        return { type: "TOTAL", condition: "<", value: toNum(m[1]), raw: src };
    }

    // индивидуальные тоталы
    if ((m = s.match(/^итб\s*([12])\s*\(?\s*([\d.,]+)\s*\)?$/))) {
        return {
            type: "INDIVIDUAL_TOTAL",
            team: m[1] === "1" ? "home" : "away",
            condition: ">",
            value: toNum(m[2]),
            raw: src,
        };
    }
    if ((m = s.match(/^итм\s*([12])\s*\(?\s*([\d.,]+)\s*\)?$/))) {
        return {
            type: "INDIVIDUAL_TOTAL",
            team: m[1] === "1" ? "home" : "away",
            condition: "<",
            value: toNum(m[2]),
            raw: src,
        };
    }

    // форы
    if ((m = s.match(/^ф\s*([12])\s*\(?\s*([+-]?[\d.,]+)\s*\)?$/))) {
        return {
            type: "HANDICAP",
            team: m[1] === "1" ? "home" : "away",
            value: toNum(m[2]),
            raw: src,
        };
    }
    if ((m = s.match(/"?(.*?)"?\s*с\s*форой\s*\(?\s*([+-]?[\d.,]+)\s*\)?/))) {
        return {
            type: "HANDICAP",
            teamByName: m[1],
            value: toNum(m[2]),
            raw: src,
        };
    }

    // двойной шанс 1X / X2 / 12
    if ((m = s.match(/^(1x|x2|12)$/i))) {
        return { type: "DOUBLE_CHANCE", variant: m[1].toUpperCase(), raw: src };
    }

    return { type: "UNKNOWN", raw: src };
}

// =================== ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ ===================

// нормализованный матч может прийти либо из JSON (result.home),
// либо из БД (home_score). Поддерживаем оба варианта.
function getFullTimeScore(match) {
    const home =
        match?.result?.home != null ? parseInt(match.result.home, 10) : parseInt(match.home_score, 10);
    const away =
        match?.result?.away != null ? parseInt(match.result.away, 10) : parseInt(match.away_score, 10);

    if (!Number.isFinite(home) || !Number.isFinite(away)) {
        return { home: null, away: null };
    }
    return { home, away };
}

// Счёт в основное время (без ОТ и буллитов)
function getMainTimeScore(match) {
    const { home, away } = getFullTimeScore(match);
    const status = match?.status;

    if (home === null || away === null) {
        return { home: null, away: null };
    }

    // Обычный оконченный матч — всё в основное время
    if (status === "FINISHED") {
        return { home, away };
    }

    // ОТ / буллиты — отнимаем один гол у победителя,
    // если разница счёта = 1 (типичная ситуация для КХЛ)
    if (status === "AFTER OVERTIME" || status === "AFTER PENALTIES") {
        const diff = home - away;

        if (Math.abs(diff) === 1) {
            if (diff > 0) {
                // выиграл хозяин
                return { home: home - 1, away };
            } else {
                // выиграл гость
                return { home, away: away - 1 };
            }
        }

        // на всякий случай fallback — считаем как есть
        return { home, away };
    }

    // Остальные статусы нам не интересны
    return { home: null, away: null };
}

// Определение стороны по названию команды
function resolveTeamByName(name, match) {
    if (!name || !match) return null;

    let clean = normalizeTeamName(name);
    if (TEAM_CASES[clean.toLowerCase()]) {
        clean = TEAM_CASES[clean.toLowerCase()];
    }

    let mapped = TEAM_MAP[clean];

    // Пытаемся найти по частичному совпадению
    if (!mapped) {
        for (const key in TEAM_MAP) {
            const keyNorm = key.toLowerCase();
            const cleanNorm = clean.toLowerCase();
            if (
                cleanNorm === keyNorm ||
                cleanNorm.includes(keyNorm) ||
                keyNorm.includes(cleanNorm)
            ) {
                mapped = TEAM_MAP[key];
                break;
            }
        }
    }

    const homeName = match.home?.name || match.home_team_name;
    const awayName = match.away?.name || match.away_team_name;

    if (homeName === mapped) return "home";
    if (awayName === mapped) return "away";

    return null;
}

// Проверка форы
function checkHandicap(diff, handicap) {
    const adjusted = diff + handicap;
    if (adjusted > 0) return "WIN";
    if (adjusted === 0) return "DRAW"; // возврат
    return "LOSE";
}

/// Оценивает прогноз по фактическому результату матча.
/// Возвращает WIN / LOSE / DRAW / UNKNOWN.
export function evaluatePrediction(pred, match) {
    if (!pred || pred.type === "UNKNOWN" || !match) return "UNKNOWN";

    const status = match.status;
    const { home: fullHome, away: fullAway } = getFullTimeScore(match);
    const { home: mainHome, away: mainAway } = getMainTimeScore(match);

    if (fullHome === null || fullAway === null) return "UNKNOWN";

    const totalFull = fullHome + fullAway;
    const totalMain =
        mainHome != null && mainAway != null ? mainHome + mainAway : null;

    switch (pred.type) {
        // ======= ПОБЕДЫ =======
        case "WINNER": {
            const team =
                pred.team || resolveTeamByName(pred.teamByName, match);

            if (pred.condition === "DRAW") {
                return fullHome === fullAway ? "WIN" : "LOSE";
            }

            if (!team) return "UNKNOWN";

            // Победа в основное время
            if (pred.condition === "WIN_FT") {
                // Если матч дошёл до ОТ/буллитов, то в основное время была ничья → ставка проиграла
                if (status === "AFTER OVERTIME" || status === "AFTER PENALTIES") {
                    return "LOSE";
                }
                // Оценка по финальному счёту, т.к. он же и есть счёт в основное время
                if (status !== "FINISHED") return "UNKNOWN";
                return team === "home"
                    ? fullHome > fullAway
                        ? "WIN"
                        : "LOSE"
                    : fullAway > fullHome
                        ? "WIN"
                        : "LOSE";
            }

            // Обычная победа по итогам матча (с учётом ОТ/буллитов)
            return team === "home"
                ? fullHome > fullAway
                    ? "WIN"
                    : "LOSE"
                : fullAway > fullHome
                    ? "WIN"
                    : "LOSE";
        }

        // ======= ТОТАЛ МАТЧА =======
        case "TOTAL": {
            if (totalMain === null) return "UNKNOWN";

            if (pred.condition === ">") {
                if (totalMain > pred.value) return "WIN";
                if (totalMain === pred.value) return "DRAW";
                return "LOSE";
            } else {
                if (totalMain < pred.value) return "WIN";
                if (totalMain === pred.value) return "DRAW";
                return "LOSE";
            }
        }

        // ======= ИНДИВИДУАЛЬНЫЕ ТОТАЛЫ =======
        case "INDIVIDUAL_TOTAL": {
            const team =
                pred.team || resolveTeamByName(pred.teamByName, match);
            if (!team) return "UNKNOWN";
            if (mainHome == null || mainAway == null) return "UNKNOWN";

            const goals = team === "home" ? mainHome : mainAway;

            if (pred.condition === ">") {
                if (goals > pred.value) return "WIN";
                if (goals === pred.value) return "DRAW";
                return "LOSE";
            } else {
                if (goals < pred.value) return "WIN";
                if (goals === pred.value) return "DRAW";
                return "LOSE";
            }
        }

        // ======= ФОРЫ =======
        case "HANDICAP": {
            const team = pred.team || resolveTeam(pred.teamByName);
            if (!team) return "UNKNOWN";

            // Забитые голы (всего)
            const homeGoals = Number(match.result.home);
            const awayGoals = Number(match.result.away);

            // Определяем голы в основное время
            let home60 = homeGoals;
            let away60 = awayGoals;

            // Если матч завершился после ОТ или буллитов — отнимаем единственный решающий гол
            if (match.status === "AFTER OVERTIME" || match.status === "AFTER PENALTIES") {
                if (homeGoals > awayGoals) home60--;
                else if (awayGoals > homeGoals) away60--;
            }

            // Разница счёта
            let diff = home60 - away60;

            // Если ставка на гостевую команду — разворачиваем разницу
            if (team === "away") diff = -diff;

            const handicap = pred.value;
            const adjusted = diff + handicap;

            if (adjusted > 0) return "WIN";
            if (adjusted === 0) return "DRAW";
            return "LOSE";
        }

        // ======= ДВОЙНОЙ ШАНС =======
        case "DOUBLE_CHANCE": {
            if (mainHome == null || mainAway == null) return "UNKNOWN";

            const home = mainHome;
            const away = mainAway;

            // 1X — хозяин не проиграет в основное время
            if (pred.variant === "1X")
                return home >= away ? "WIN" : "LOSE";

            // X2 — гость не проиграет в основное время
            if (pred.variant === "X2")
                return away >= home ? "WIN" : "LOSE";

            // 12 — не будет ничьей в основное время
            if (pred.variant === "12")
                return home !== away ? "WIN" : "LOSE";

            // “Команда не проиграет”
            if (pred.dcMode === "NOLOSE") {
                const team = resolveTeam(pred.teamByName);
                if (!team) return "UNKNOWN";

                return team === "home"
                    ? home >= away ? "WIN" : "LOSE"
                    : away >= home ? "WIN" : "LOSE";
            }

            return "UNKNOWN";
        }

        case "BOTH_TEAMS_SCORE": {
            const homeFT = Number(match.result.home);
            const awayFT = Number(match.result.away);

            // ГОЛЫ В ОСНОВНОЕ ВРЕМЯ
            const adjHome =
                match.status === "FINISHED"
                    ? homeFT
                    : (match.status === "AFTER OVERTIME" || match.status === "AFTER PENALTIES")
                        ? homeFT - 1
                        : null;

            const adjAway =
                match.status === "FINISHED"
                    ? awayFT
                    : (match.status === "AFTER OVERTIME" || match.status === "AFTER PENALTIES")
                        ? awayFT - 1
                        : null;

            if (adjHome === null || adjAway === null) return "UNKNOWN";

            // Требование: обе команды > value
            const need = pred.value; // например 1.5

            const ok = adjHome > need && adjAway > need;

            return ok ? "WIN" : "LOSE";
        }

        default:
            return "UNKNOWN";
    }
}
