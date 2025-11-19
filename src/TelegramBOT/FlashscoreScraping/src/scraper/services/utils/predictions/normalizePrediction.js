/**
 * Функция normalizePrediction — ядро нормализации текстовых прогнозов.
 *
 * Что делает:
 *  - Принимает текст прогноза и названия команд.
 *  - Преобразует любой «живой» текст (например: "победа Северстали",
 *    "Металлург с форой (-1.5)", "тотал больше 5.5", "индивидуальный тотал
 *    Салавата Юлаева больше (2.5)" и др.) в короткую стандартизированную форму.
 *
 * Краткая форма включает:
 *  - Победа:       П1 / П2
 *  - Ничья:        X
 *  - Двойной шанс: 1X / X2
 *  - Фора:         Ф1 (+1.5) / Ф2 (-1)
 *  - Тоталы:       ТБ (5.5) / ТМ (4)
 *  - Инд. тоталы:  ИТБ1 (2.5) / ИТМ2 (1.5)
 *
 * Особенности:
 *  - Умеет распознавать команды по названию или склонениям.
 *  - Автоматически ставит пробел в форe: "Ф1 (-1.5)".
 *  - Корректно определяет команду для ИТБ/ИТМ по тексту.
 *  - Отлавливает краткие нотации ("П1", "ТБ(5.5)", "Ф2(0)") и приводит к нормализованному виду.
 *  - Использует словарь normalizationDictionary.json для распознавания
 *    паттернов: победа, тотал, не проиграет и т.д.
 */

import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const DICT_PATH = path.join(__dirname, "normalizationDictionary.json");

// грузим словарь один раз
let dictionary = [];
try {
    const raw = fs.readFileSync(DICT_PATH, "utf-8");
    dictionary = JSON.parse(raw);
    console.log("[normalizePrediction] ✓ Dictionary loaded:", dictionary.length, "groups");
} catch (e) {
    console.error("[normalizePrediction] ❌ Can't load normalizationDictionary.json");
}

/* -------- ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ -------- */

// упоминание команды в тексте + учёт склонений ("Салават" -> "Салавата")
function textMentionsTeam(lowerText, teamName) {
    if (!teamName) return false;

    const teamLower = teamName.toLowerCase();

    if (lowerText.includes(teamLower)) return true;

    const tokens = teamLower.split(/\s+/).filter(Boolean);
    for (const token of tokens) {
        const stem = token.replace(/[аеёиоуыэюя]$/i, "");
        if (stem.length >= 3 && lowerText.includes(stem)) return true;
    }
    return false;
}

function extractNumber(text) {
    const inBrackets = text.match(/\((-?\+?\d+(?:[.,]\d+)?)\)/);
    if (inBrackets) {
        return inBrackets[1].replace(",", ".");
    }

    const m = text.match(/[-+]?\d+(?:[.,]\d+)?/);
    return m ? m[0].replace(",", ".") : null;
}

function formatTotal(prefix, num) {
    // нужно "ТБ (5.5)", "ТМ (5.5)"
    return `${prefix} (${num})`;
}

function extractTotal(text, prefix) {
    const num = extractNumber(text);
    if (!num) return prefix;
    return formatTotal(prefix, num);
}

// ИТ тотал команды: пытаемся вытащить 1/2 из текста, потом по названию
function extractTeamTotal(text, homeTeam, awayTeam, prefix) {
    const num = extractNumber(text) || "";
    const lower = text.toLowerCase();

    // 1) Если уже явно написано ИТБ1 / ИТБ2 / ИТМ1 / ИТМ2
    const explicitSide = lower.match(/ит[бм]\s*([12])/i);
    if (explicitSide) {
        const side = explicitSide[1];
        return `${prefix}${side} (${num})`;
    }

    // 2) По названиям команд
    const home = textMentionsTeam(lower, homeTeam);
    const away = textMentionsTeam(lower, awayTeam);

    if (home) return `${prefix}1 (${num})`;
    if (away) return `${prefix}2 (${num})`;

    // 3) fallback
    return `${prefix}1 (${num})`;
}

/**
 * Если строка УЖЕ в нормализованном виде (П2, ТМ (5.5), ИТБ1 (3.5), Ф1(-1.5)),
 * просто возвращаем её как есть, без словаря.
 */
function detectShortNotation(text) {
    if (!text || typeof text !== "string") return null;

    const trimmed = text.trim();

    // П1 / П2
    if (/^п[12]$/i.test(trimmed)) {
        return trimmed.toUpperCase();
    }

    // ТБ(5.5), ТМ(5.5)  →  ТБ (5.5)
    if (/^т[бм]\s*\(\s*[-+]?\d+(\.\d+)?\s*\)$/i.test(trimmed)) {
        return trimmed.replace(/^([Тт][БбМм])\s*\(/, "$1 (");
    }

    // ИТБ1(3.5), ИТМ2(2.5) → ИТБ1 (3.5)
    if (/^ит[бм][12]\s*\(\s*[-+]?\d+(\.\d+)?\s*\)$/i.test(trimmed)) {
        return trimmed.replace(/^(ИТ[БМ][12])\s*\(/i, (m, p1) => `${p1} (`);
    }

    // Ф1(-1.5), Ф2(1) → Ф1 (-1.5)
    if (/^ф[12]\s*\(\s*[-+]?\d+(\.\d+)?\s*\)$/i.test(trimmed)) {
        return trimmed.replace(/^(Ф[12])\s*\(/i, (m, p1) => `${p1} (`);
    }

    // 1X, X2, 12 — двойной шанс
    if (/^(1x|x2|12)$/i.test(trimmed)) {
        return trimmed.toUpperCase();
    }

    return null;
}

/* -------- ОСНОВНАЯ ФУНКЦИЯ -------- */

export function normalizePrediction(text, homeTeam, awayTeam) {
    console.log("\n================ NORMALIZE START ================");
    console.log("INPUT text:", text);

    if (!text || typeof text !== "string") {
        console.log("[normalizePrediction] text undefined");
        return null;
    }

    // 0) Уже краткая запись? Тогда просто вернуть.
    const short = detectShortNotation(text);
    if (short) {
        console.log("[normalizePrediction] → Short notation detected:", short);
        return short;
    }

    const lowerText = text.toLowerCase();
    console.log("Lower:", lowerText);

    /* === 1. ФОРА (приоритет 1) === */
    /* === 1. ФОРА (приоритет 1) === */
    if (/фор[а-яё]*/i.test(text) || /ф[12]/i.test(lowerText)) {
        console.log("[normalizePrediction] → HANDICAP detected");

        const num = extractNumber(text);
        if (!num) return null;

        // 🔥 ВСТАВЛЯЕМ СЮДА — до определения команд по тексту
        // detect explicit "фора1", "ф1", "фора 1"
        const explicitHandicap1 = lowerText.match(/фора?\s*1|ф1/);
        const explicitHandicap2 = lowerText.match(/фора?\s*2|ф2/);

        if (explicitHandicap1) return `Ф1 (${num})`;
        if (explicitHandicap2) return `Ф2 (${num})`;

        // Стандартная логика fallback при указании команды в тексте
        const isHome = textMentionsTeam(lowerText, homeTeam);
        const isAway = textMentionsTeam(lowerText, awayTeam);

        if (isHome) return `Ф1 (${num})`;
        if (isAway) return `Ф2 (${num})`;

        return `Ф1 (${num})`;
    }

    /* === 2. Индивидуальный тотал (приоритет 2) ===
       Ловим и фразы, и короткие префиксы "итб", "итм" без цифры команды.
    */
    if (
        lowerText.includes("индивидуальный тотал") ||
        /\bит[бм]/i.test(lowerText)
    ) {
        console.log("[normalizePrediction] → Individual Total detected");
        if (lowerText.includes("больше")) {
            return extractTeamTotal(text, homeTeam, awayTeam, "ИТБ");
        }
        if (lowerText.includes("меньше")) {
            return extractTeamTotal(text, homeTeam, awayTeam, "ИТМ");
        }
        // если нет слова "больше/меньше", но есть ИТБ / ИТМ в самом начале
        const overMatch = lowerText.match(/^итб/);
        const underMatch = lowerText.match(/^итм/);
        if (overMatch) return extractTeamTotal(text, homeTeam, awayTeam, "ИТБ");
        if (underMatch) return extractTeamTotal(text, homeTeam, awayTeam, "ИТМ");
    }

    /* === 3. Проход по словарю === */
    for (const group of dictionary) {
        for (const pattern of group.patterns) {
            if (!lowerText.includes(pattern.toLowerCase())) continue;

            console.log(`[normalizePrediction] → Pattern matched: "${pattern}" (type ${group.type})`);

            switch (group.type) {
                case "Win": {
                    const home = textMentionsTeam(lowerText, homeTeam);
                    const away = textMentionsTeam(lowerText, awayTeam);

                    if (home) return "П1";
                    if (away) return "П2";
                    return "П1";
                }

                case "Draw":
                    return "X";

                case "TotalOver":
                    return extractTotal(text, "ТБ");

                case "TotalUnder":
                    return extractTotal(text, "ТМ");

                case "ITOver":
                    return extractTeamTotal(text, homeTeam, awayTeam, "ИТБ");

                case "ITUnder":
                    return extractTeamTotal(text, homeTeam, awayTeam, "ИТМ");

                case "DoubleChance": {
                    const home = textMentionsTeam(lowerText, homeTeam);
                    const away = textMentionsTeam(lowerText, awayTeam);

                    if (home) return "1X";
                    if (away) return "2X";

                    return "1X";
                }

                default:
                    break;
            }
        }
    }

    console.log("[normalizePrediction] ❓ Nothing matched → null");
    return null;
}

export default { normalizePrediction };
