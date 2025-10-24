import { TEAM_MAP, normalizeTeamName } from "./teamMapUtils.js";

const toNum = (s) => parseFloat(String(s).replace(",", "."));
const norm = (s) =>
    (s || "")
        .trim()
        .replace(/[\u00AB\u00BB“”"]/g, '"') // кавычки «»“” → "
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

/// <summary>
/// Разбирает текст прогноза в структурированный объект:
/// тип прогноза, команда, значение, условие и т.п.
/// </summary>
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
    if ((m = s.match(/победа\s+[«"]?([^"»]+)[»"]?\s+в\s+основное\s+время/)))
        return { type: "WINNER", teamByName: m[1], condition: "WIN_FT", raw: src };

    // Победа команды в матче
    if ((m = s.match(/победа\s+[«"]?([^"»]+)[»"]?\s+в\s+матче/)))
        return { type: "WINNER", teamByName: m[1], condition: "WIN", raw: src };

    // Победа команды в конце строки
    if ((m = s.match(/победа\s+[«"]?([^"»]+)[»"]?\.?$/)))
        return { type: "WINNER", teamByName: m[1], condition: "WIN", raw: src };

    // X не проиграет
    if ((m = s.match(/"?(.*?)"?\s+не\s+проиграет/)))
        return { type: "DOUBLE_CHANCE", teamByName: m[1], dcMode: "NOLOSE", raw: src };

    // тоталы
    if ((m = s.match(/^тб\s*\(?\s*([\d.,]+)\s*\)?$/)))
        return { type: "TOTAL", condition: ">", value: toNum(m[1]), raw: src };
    if ((m = s.match(/^тм\s*\(?\s*([\d.,]+)\s*\)?$/)))
        return { type: "TOTAL", condition: "<", value: toNum(m[1]), raw: src };

    // индивидуальные тоталы
    if ((m = s.match(/^итб\s*([12])\s*\(?\s*([\d.,]+)\s*\)?$/)))
        return { type: "INDIVIDUAL_TOTAL", team: m[1] === "1" ? "home" : "away", condition: ">", value: toNum(m[2]), raw: src };
    if ((m = s.match(/^итм\s*([12])\s*\(?\s*([\d.,]+)\s*\)?$/)))
        return { type: "INDIVIDUAL_TOTAL", team: m[1] === "1" ? "home" : "away", condition: "<", value: toNum(m[2]), raw: src };

    // форы
    if ((m = s.match(/^ф\s*([12])\s*\(?\s*([+-]?[\d.,]+)\s*\)?$/)))
        return { type: "HANDICAP", team: m[1] === "1" ? "home" : "away", value: toNum(m[2]), raw: src };
    if ((m = s.match(/"?(.*?)"?\s*с\s*форой\s*\(?\s*([+-]?[\d.,]+)\s*\)?/)))
        return { type: "HANDICAP", teamByName: m[1], value: toNum(m[2]), raw: src };

    // двойной шанс
    if ((m = s.match(/^(1x|x2|12)$/i)))
        return { type: "DOUBLE_CHANCE", variant: m[1].toUpperCase(), raw: src };

    return { type: "UNKNOWN", raw: src };
}

/// <summary>
/// Оценивает прогноз по фактическому результату матча.
/// Возвращает WIN / LOSE / DRAW / UNKNOWN.
/// </summary>
export function evaluatePrediction(pred, match) {
    if (!pred || pred.type === "UNKNOWN" || !match?.result) return "UNKNOWN";

    const home = parseInt(match.result.home, 10);
    const away = parseInt(match.result.away, 10);
    const total = home + away;

    const resolveTeam = (name) => {
        if (!name) return null;
        let clean = normalizeTeamName(name);
        if (TEAM_CASES[clean.toLowerCase()]) clean = TEAM_CASES[clean.toLowerCase()];

        let mapped = TEAM_MAP[clean];
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

        if (match.home?.name === mapped) return "home";
        if (match.away?.name === mapped) return "away";
        return null;
    };

    const checkHandicap = (diff, handicap) => {
        const adjusted = diff + handicap;
        if (adjusted > 0) return "WIN";
        if (adjusted === 0) return "DRAW";
        return "LOSE";
    };

    switch (pred.type) {
        case "WINNER": {
            const team = pred.team || resolveTeam(pred.teamByName);
            if (!team) return "UNKNOWN";

            if (pred.condition === "DRAW") return home === away ? "WIN" : "LOSE";

            if (pred.condition === "WIN_FT") {
                if (match.status !== "FINISHED") return "LOSE";
                return team === "home" ? (home > away ? "WIN" : "LOSE") : (away > home ? "WIN" : "LOSE");
            }

            return team === "home" ? (home > away ? "WIN" : "LOSE") : (away > home ? "WIN" : "LOSE");
        }

        case "TOTAL": {
            let mainTimeTotal =
                match.status === "FINISHED"
                    ? total
                    : (match.status === "AFTER OVERTIME" || match.status === "AFTER PENALTIES")
                        ? total - 1
                        : null;

            if (mainTimeTotal === null) return "UNKNOWN";
            return pred.condition === ">"
                ? mainTimeTotal > pred.value ? "WIN" : "LOSE"
                : mainTimeTotal < pred.value ? "WIN" : "LOSE";
        }

        case "INDIVIDUAL_TOTAL": {
            const team = pred.team || resolveTeam(pred.teamByName);
            if (!team) return "UNKNOWN";
            const goals = team === "home" ? home : away;
            return pred.condition === ">"
                ? goals > pred.value ? "WIN" : "LOSE"
                : goals < pred.value ? "WIN" : "LOSE";
        }

        case "HANDICAP": {
            const team = pred.team || resolveTeam(pred.teamByName);
            if (!team) return "UNKNOWN";
            let diff = home - away;
            if (team === "away") diff = -diff;
            return checkHandicap(diff, pred.value);
        }

        case "DOUBLE_CHANCE": {
            if (pred.variant === "1X") return home >= away ? "WIN" : "LOSE";
            if (pred.variant === "X2") return away >= home ? "WIN" : "LOSE";
            if (pred.variant === "12") return home !== away ? "WIN" : "LOSE";
            const team = resolveTeam(pred.teamByName);
            if (!team) return "UNKNOWN";
            return team === "home" ? (home >= away ? "WIN" : "LOSE") : (away >= home ? "WIN" : "LOSE");
        }

        default:
            return "UNKNOWN";
    }
}
