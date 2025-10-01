import { TEAM_MAP, normalizeTeamName } from "./teamMapUtils.js";

const toNum = (s) => parseFloat(String(s).replace(",", "."));

const norm = (s) =>
    (s || "")
        .trim()
        .replace(/[\u00AB\u00BB“”"]/g, '"')   // «»“” -> "
        .replace(/[—–]/g, "-")
        .replace(/\s+/g, " ")
        .toLowerCase();

// словарь падежных форм
const TEAM_CASES = {
    "сибири": "Сибирь",
    "лады": "Лада",
    "автомобилиста": "Автомобилист",
    "металлурга": "Металлург",
    "авангарда": "Авангард",
    "торпедо": "Торпедо",
    "динамо": "Динамо", // общий случай
    "цска": "ЦСКА",
    "спартака": "Спартак",
    "салавата": "Салават Юлаев",
    "ак барса": "Ак Барс",
};

/**
 * Разбор текста прогноза в структурированный вид
 */
export function parsePrediction(text) {
    if (!text) return { type: "UNKNOWN", raw: text };
    const src = text;
    const s = norm(text);

    let m;

    // ===== Победа короткая =====
    if (/^п1$/.test(s)) return { type: "WINNER", team: "home", condition: "WIN", raw: src };
    if (/^п2$/.test(s)) return { type: "WINNER", team: "away", condition: "WIN", raw: src };
    if (/^(x|ничья)$/.test(s)) return { type: "WINNER", condition: "DRAW", raw: src };

    // ===== Победа X в основное время =====
    if ((m = s.match(/победа\s+[«"]?([^"»]+)[»"]?\s+в\s+основное\s+время/))) {
        return { type: "WINNER", teamByName: m[1], condition: "WIN_FT", raw: src };
    }

    // ===== Победа X в матче =====
    if ((m = s.match(/победа\s+[«"]?([^"»]+)[»"]?\s+в\s+матче/))) {
        return { type: "WINNER", teamByName: m[1], condition: "WIN", raw: src };
    }

    // ===== Победа X (в конце строки, с точкой или без) =====
    if ((m = s.match(/победа\s+[«"]?([^"»]+)[»"]?\.?$/))) {
        return { type: "WINNER", teamByName: m[1], condition: "WIN", raw: src };
    }

    // ===== X не проиграет =====
    if ((m = s.match(/"?(.*?)"?\s+не\s+проиграет/))) {
        return { type: "DOUBLE_CHANCE", teamByName: m[1], dcMode: "NOLOSE", raw: src };
    }

    // ===== Тоталы =====
    if ((m = s.match(/^тб\s*\(?\s*([0-9]+(?:[.,][0-9]+)?)\s*\)?$/)))
        return { type: "TOTAL", condition: ">", value: toNum(m[1]), raw: src };
    if ((m = s.match(/^тм\s*\(?\s*([0-9]+(?:[.,][0-9]+)?)\s*\)?$/)))
        return { type: "TOTAL", condition: "<", value: toNum(m[1]), raw: src };

    // ===== Инд. тоталы =====
    if ((m = s.match(/^итб\s*([12])\s*\(?\s*([0-9]+(?:[.,][0-9]+)?)\s*\)?$/)))
        return { type: "INDIVIDUAL_TOTAL", team: m[1] === "1" ? "home" : "away", condition: ">", value: toNum(m[2]), raw: src };
    if ((m = s.match(/^итм\s*([12])\s*\(?\s*([0-9]+(?:[.,][0-9]+)?)\s*\)?$/)))
        return { type: "INDIVIDUAL_TOTAL", team: m[1] === "1" ? "home" : "away", condition: "<", value: toNum(m[2]), raw: src };

    // ===== Форы =====
    if ((m = s.match(/^ф\s*([12])\s*\(?\s*([+-]?[0-9]+(?:[.,][0-9]+)?)\s*\)?$/)))
        return { type: "HANDICAP", team: m[1] === "1" ? "home" : "away", value: toNum(m[2]), raw: src };

    if ((m = s.match(/"?(.*?)"?\s*с\s*форой\s*\(?\s*([+-]?[0-9]+(?:[.,][0-9]+)?)\s*\)?/)))
        return { type: "HANDICAP", teamByName: m[1], value: toNum(m[2]), raw: src };

    // ===== Двойной шанс =====
    if ((m = s.match(/^(1x|x2|12)$/))) return { type: "DOUBLE_CHANCE", variant: m[1].toUpperCase(), raw: src };

    return { type: "UNKNOWN", raw: src };
}

/**
 * Оценка прогноза по факту матча
 */
export function evaluatePrediction(pred, match) {
    if (!pred || pred.type === "UNKNOWN" || !match || !match.result) return "UNKNOWN";

    const home = parseInt(match.result.home, 10);
    const away = parseInt(match.result.away, 10);
    const total = home + away;

    const sideByName = (name, match) => {
        if (!name || !match) return null;
        let clean = normalizeTeamName(name);

        // пробуем падежные формы
        if (TEAM_CASES[clean.toLowerCase()]) {
            clean = TEAM_CASES[clean.toLowerCase()];
        }

        let mapped = TEAM_MAP[clean];
        if (!mapped) {
            for (const key in TEAM_MAP) {
                const keyNorm = key.toLowerCase();
                const cleanNorm = clean.toLowerCase();
                if (
                    cleanNorm === keyNorm ||
                    cleanNorm.startsWith(keyNorm) ||
                    cleanNorm.endsWith(keyNorm) ||
                    cleanNorm.includes(keyNorm) ||
                    keyNorm.includes(cleanNorm)
                ) {
                    mapped = TEAM_MAP[key];
                    break;
                }
            }
        }
        if (!mapped) return null;

        if (match.home?.name === mapped) return "home";
        if (match.away?.name === mapped) return "away";
        return null;
    };

    switch (pred.type) {
        case "WINNER": {
            const team = pred.team || sideByName(pred.teamByName, match);
            if (!team) return "UNKNOWN";

            if (pred.condition === "DRAW") return home === away ? "WIN" : "LOSE";

            if (pred.condition === "WIN_FT") {
                if (match.status !== "FINISHED") return "LOSE"; // только основное время
                return team === "home"
                    ? home > away ? "WIN" : "LOSE"
                    : away > home ? "WIN" : "LOSE";
            }

            // обычная победа (учитываем ОТ/буллиты)
            return team === "home"
                ? home > away ? "WIN" : "LOSE"
                : away > home ? "WIN" : "LOSE";
        }

        case "TOTAL": {
            let mainTimeTotal;
            if (match.status === "FINISHED") {
                mainTimeTotal = total;
            } else if (match.status === "AFTER OVERTIME" || match.status === "AFTER PENALTIES") {
                mainTimeTotal = total - 1; // вычитаем 1 гол из ОТ/буллитов
            } else {
                return "UNKNOWN";
            }

            return pred.condition === ">"
                ? mainTimeTotal > pred.value ? "WIN" : "LOSE"
                : mainTimeTotal < pred.value ? "WIN" : "LOSE";
        }

        case "INDIVIDUAL_TOTAL": {
            let goals;
            if (pred.team === "home") {
                goals = home;
            } else if (pred.team === "away") {
                goals = away;
            } else {
                const team = sideByName(pred.teamByName, match);
                if (!team) return "UNKNOWN";
                goals = team === "home" ? home : away;
            }

            let mainTimeGoals;
            if (match.status === "FINISHED") {
                mainTimeGoals = goals;
            } else if (match.status === "AFTER OVERTIME" || match.status === "AFTER PENALTIES") {
                const isWinner =
                    (goals === home && home > away) || (goals === away && away > home);
                mainTimeGoals = isWinner ? goals - 1 : goals;
            } else {
                return "UNKNOWN";
            }

            return pred.condition === ">"
                ? mainTimeGoals > pred.value ? "WIN" : "LOSE"
                : mainTimeGoals < pred.value ? "WIN" : "LOSE";
        }

        case "HANDICAP": {
            const team = pred.team || sideByName(pred.teamByName, match);
            if (!team) return "UNKNOWN";

            let homeFT = home;
            let awayFT = away;

            // убираем гол из ОТ/буллитов
            if (match.status === "AFTER OVERTIME" || match.status === "AFTER PENALTIES") {
                if (home > away) homeFT -= 1;
                else if (away > home) awayFT -= 1;
            }

            let diff = homeFT - awayFT;
            if (team === "away") diff = -diff;

            console.log(
                `DEBUG HANDICAP: ${pred.raw} | status=${match.status} | home=${homeFT} | away=${awayFT} | diff=${diff} | handicap=${pred.value}`
            );

            return checkHandicap(diff, pred.value, match.status);
        }


        case "DOUBLE_CHANCE": {
            if (pred.variant) {
                if (pred.variant === "1X") return home >= away ? "WIN" : "LOSE";
                if (pred.variant === "X2") return away >= home ? "WIN" : "LOSE";
                if (pred.variant === "12") return home !== away ? "WIN" : "LOSE";
            }
            const team = sideByName(pred.teamByName, match);
            if (!team) return "UNKNOWN";

            if (match.status !== "FINISHED") return "WIN"; // в ОТ/буллитах всегда WIN
            return team === "home"
                ? home >= away ? "WIN" : "LOSE"
                : away >= home ? "WIN" : "LOSE";
        }

        default:
            return "UNKNOWN";
    }
}

/**
 * Проверка форы
 */
function checkHandicap(diff, handicap, status) {
    const adjusted = diff + handicap;

    console.log(
        `CHECK HANDICAP: diff=${diff}, handicap=${handicap}, adjusted=${adjusted}`
    );

    if (adjusted > 0) {
        console.log("→ RESULT = WIN");
        return "WIN";
    }
    if (adjusted === 0) {
        console.log("→ RESULT = DRAW");
        return "DRAW";
    }
    console.log("→ RESULT = LOSE");
    return "LOSE";
}
