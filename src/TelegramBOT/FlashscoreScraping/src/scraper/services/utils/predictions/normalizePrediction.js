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
} catch (e) {
    console.error("[normalizePrediction] Can't load normalizationDictionary.json");
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

/**
 * Извлечение числа:
 *  1) (5.5) в скобках
 *  2) словесные числительные: "пяти", "шести" и т.п.
 *  3) обычные цифры 5, 5.5, 2,5
 */
function extractNumber(text) {
    // (5.5) или (+1,5)
    const numInBrackets = text.match(/\((-?\+?\d+(?:[.,]\d+)?)\)/);
    if (numInBrackets) {
        return numInBrackets[1].replace(",", ".");
    }

    // словесные числительные в родительном падеже
    const wordsMap = {
        "одной": 1, "одна": 1, "одно": 1,
        "двух": 2, "две": 2,
        "трех": 3, "трёх": 3,
        "четырех": 4, "четырёх": 4,
        "пяти": 5,
        "шести": 6,
        "семи": 7,
        "восьми": 8,
        "девяти": 9,
        "десяти": 10
    };

    const tokens = text.toLowerCase().split(/\s+/);
    for (const word of tokens) {
        if (wordsMap[word] !== undefined) {
            return String(wordsMap[word]);
        }
    }

    // обычные цифры
    const m = text.match(/[-+]?\d+(?:[.,]\d+)?/);
    return m ? m[0].replace(",", ".") : null;
}

function formatTotal(prefix, num) {
    return `${prefix} (${num})`;
}

function extractTotal(text, prefix) {
    const num = extractNumber(text);
    if (!num) return prefix;
    return `${prefix} (${num})`;
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
    return null;
}

/**
 * Если строка УЖЕ в нормализованном виде (П2, ТМ (5.5), ИТБ1 (3.5), Ф1(-1.5), 1X, X2, 12, ОЗ (1.5) - Да/Нет),
 * просто возвращаем её как есть, без словаря.
 */
function detectShortNotation(text) {
    if (!text || typeof text !== "string") return null;

    const trimmed = text.trim();

    // П1 / П2
    if (/^п[12]$/i.test(trimmed)) {
        return trimmed.toUpperCase();
    }

    // X
    if (/^x$/i.test(trimmed)) {
        return "X";
    }

    // 1X, X2, 12 — двойной шанс
    if (/^(1x|x2|2x|12)$/i.test(trimmed)) {
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

    // ОЗ (1.5), ОЗ (1.5) - Да/Нет
    if (/^оз\s*\(\s*[-+]?\d+(\.\d+)?\s*\)(\s*-\s*(да|нет))?$/i.test(trimmed)) {
        return trimmed
            .replace(/^оз/i, "ОЗ")
            .replace(/\(\s*/g, "( ")
            .replace(/\s*\)/g, " )")
            .replace(/\s*-\s*да$/i, " - Да")
            .replace(/\s*-\s*нет$/i, " - Нет");
    }

    return null;
}

/**
 * Нормализация "обе забьют" / "каждая команда забьёт ..."
 */
function normalizeBothTeams(text, lowerText) {
    const hasPhrase =
        lowerText.includes("каждая команда заб") ||
        lowerText.includes("обе забьют") ||
        lowerText.includes("обе команды заб") ||
        lowerText.includes("команды забьют") ||
        lowerText.includes("обе команды");

    if (!hasPhrase) return null;

    const num = extractNumber(text) || "";

    // смотрим на хвост после последнего тире — там обычно "Да." / "Нет."
    const parts = lowerText.split(/[–—-]/);
    const tail = parts.length > 1 ? parts[parts.length - 1] : lowerText;

    let yes = false;
    let no = false;

    if (tail.includes("нет")) no = true;
    if (tail.includes("да")) yes = true;

    if (no) return `ОЗ (${num}) - Нет`;
    if (yes) return `ОЗ (${num}) - Да`;
    return `ОЗ (${num})`;
}

/* -------- ОСНОВНАЯ ФУНКЦИЯ -------- */

export function normalizePrediction(text, homeTeam, awayTeam, ctx = {}) {
    const { fileName, matchId, source, field } = ctx;

    if (!text || typeof text !== "string") {
        return null;
    }

    const trimmed = text.trim();
    const lowerText = trimmed.toLowerCase();

    // прогнозы по таймам/периодам/сетам вообще не трогаем
    if (/тайм|период|половин|четверт|квартал|сет|раунд/i.test(lowerText)) {
        return trimmed;
    }

    // уже краткая запись? Тогда просто вернуть.
    const short = detectShortNotation(trimmed);
    if (short) {
        return short;
    }

    // ОБЕ ЗАБЬЮТ (явно текстом)
    const bothTeamsNorm = normalizeBothTeams(trimmed, lowerText);
    if (bothTeamsNorm) {
        return bothTeamsNorm;
    }

    // Любые конструкции "... или ничья" / "ничья или ..." → двойной шанс 1X / X2
    if (/(или\s+ничья|ничья\s+или)/i.test(lowerText)) {
        const home = textMentionsTeam(lowerText, homeTeam);
        const away = textMentionsTeam(lowerText, awayTeam);

        if (home && !away) return "1X";
        if (away && !home) return "X2";

        // если не получилось однозначно, по умолчанию 1X
        return "1X";
    }

    /* === 1. ФОРА (приоритет 1) === */
    if (/фор[а-яё]*/i.test(trimmed) || /ф[12]/i.test(lowerText)) {
        const num = extractNumber(trimmed);
        if (!num) return null;

        // явные "фора1", "ф1", "фора 1"
        const explicitHandicap1 = lowerText.match(/фора?\s*1|ф1/);
        const explicitHandicap2 = lowerText.match(/фора?\s*2|ф2/);

        if (explicitHandicap1) return `Ф1 (${num})`;
        if (explicitHandicap2) return `Ф2 (${num})`;

        // по командам
        const isHome = textMentionsTeam(lowerText, homeTeam);
        const isAway = textMentionsTeam(lowerText, awayTeam);

        if (isHome) return `Ф1 (${num})`;
        if (isAway) return `Ф2 (${num})`;

        // fallback
        return `Ф1 (${num})`;
    }

    /* === 2. Индивидуальный тотал (приоритет 2, до словаря) ===
       Ловим:
         - "индивидуальный тотал ..."
         - "индивидуальному тотае ..." (опечатка на сайте — тоже ловим)
         - "инд. тотал ..."
         - "итб", "итм"
         - "ИТ «Спартака» ..." и похожие формы
    */
    const hasIndTotal =
        /индивидуальн[а-яё]*\s+тота[леп]/i.test(lowerText) ||
        /инд\.?\s*тотал/i.test(lowerText) ||
        /инд тотал/i.test(lowerText);

    const hasITB = /итб/i.test(lowerText);
    const hasITM = /итм/i.test(lowerText);
    const hasITWord = /(^|\s)ит(\s|[«"'])/i.test(lowerText); // "ИТ Спартака", " ИТ «Сочи»"

    if (hasIndTotal || hasITB || hasITM || hasITWord) {
        const hasOver = lowerText.includes("больше");
        const hasUnder = lowerText.includes("меньше");

        // Явно ИТБ
        if (hasITB || (hasOver && !hasITM)) {
            return extractTeamTotal(trimmed, homeTeam, awayTeam, "ИТБ");
        }

        // Явно ИТМ
        if (hasITM || (hasUnder && !hasITB)) {
            return extractTeamTotal(trimmed, homeTeam, awayTeam, "ИТМ");
        }

        // Фоллбэк: если непонятно — считаем ИТБ
        return extractTeamTotal(trimmed, homeTeam, awayTeam, "ИТБ");
    }

    // === 2.5. Гол игрока (Player Goal) ===
    if (
        lowerText.includes("забь") ||
        lowerText.includes("забет") ||
        lowerText.includes("гол") ||
        lowerText.includes("отличит") ||
        lowerText.includes("забросит")
    ) {
        const words = trimmed.split(/[\s,–—-]+/).filter(Boolean);

        let player = null;

        for (const w of words) {
            // Игрок = слово с заглавной буквы и не название команды
            if (/^[А-ЯЁ][а-яё]+$/.test(w)) {
                if (
                    !textMentionsTeam(w.toLowerCase(), homeTeam) &&
                    !textMentionsTeam(w.toLowerCase(), awayTeam)
                ) {
                    player = w;
                    break;
                }
            }
        }

        if (player) {
            // Такие прогнозы возвращаем КАК ЕСТЬ
            return trimmed.replace(/[.,]$/, "").trim();
        }
    }

    /* 3. Проход по словарю */
    for (const group of dictionary) {
        for (const pattern of group.patterns) {
            if (!lowerText.includes(pattern.toLowerCase())) continue;

            switch (group.type) {
                case "Win": {
                    // 1) Явные указания "Победа 1" / "Победа 2"
                    const explicit = lowerText.match(/побед[аы]?\s*(1|2)/);
                    if (explicit) {
                        return explicit[1] === "1" ? "П1" : "П2";
                    }

                    // 2) Проверка по упоминанию команды
                    const home = textMentionsTeam(lowerText, homeTeam);
                    const away = textMentionsTeam(lowerText, awayTeam);

                    if (home) return "П1";
                    if (away) return "П2";

                    // 3) FALLBACK
                    return "П1";
                }

                case "WinRT": {
                    // Победа в основное время — та же П1/П2
                    const home = textMentionsTeam(lowerText, homeTeam);
                    const away = textMentionsTeam(lowerText, awayTeam);

                    if (home) return "П1";
                    if (away) return "П2";

                    return "П1";
                }

                case "Draw": {
                    // Ничья только если ясно указано "ничья" или "исход Х"
                    if (
                        lowerText.includes("ничья") ||
                        /(^|\s)[хx]($|\s)/i.test(lowerText) ||
                        lowerText.includes("исход х") ||
                        lowerText.includes("ничья в матче")
                    ) {
                        return "X";
                    }
                    break;
                }

                case "TotalOver":
                    return extractTotal(trimmed, "ТБ");

                case "TotalUnder":
                    return extractTotal(trimmed, "ТМ");

                case "ITOver":
                    return extractTeamTotal(trimmed, homeTeam, awayTeam, "ИТБ");

                case "ITUnder":
                    return extractTeamTotal(trimmed, homeTeam, awayTeam, "ИТМ");

                case "DoubleChance": {
                    const home = textMentionsTeam(lowerText, homeTeam);
                    const away = textMentionsTeam(lowerText, awayTeam);

                    if (home && !away) return "1X";
                    if (away && !home) return "X2";

                    return "1X";
                }

                case "BothTeams": {
                    const bt = normalizeBothTeams(trimmed, lowerText);
                    if (bt) return bt;
                    break;
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
