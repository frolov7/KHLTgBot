/**
 * Массовая нормализация прогнозов в JSON-файлах
 */

import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";
import { normalizePrediction } from "./normalizePrediction.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const PREDICTIONS_DIR = path.join(__dirname, "../../../../data/predictions");

console.log("=== START GLOBAL NORMALIZATION ===");


function logProblem(type, raw, norm, ctx = {}) {
    const file = ctx.fileName || "unknown-file";
    const match = ctx.matchId || "unknown-match";
    const src = ctx.source || "unknown-source";
    const field = ctx.field || "unknown-field";

    console.log(`\n❗ ПРОБЛЕМА: ${type}`);
    console.log(`   FILE: ${file}`);
    console.log(`   MATCH: ${match}`);
    console.log(`   SRC: ${src}`);
    console.log(`   FIELD: ${field}`);
    console.log(`   RAW: "${raw}"`);
    console.log(`   NORMALIZED: "${norm}"`);
}

function isGarbagePart(text) {
    return /^(\d+(\.\d+)?)\s*шайб/i.test(text);
}

/* НЕДОПУСТИМЫЕ ALT */

function isGarbageAlt(text) {
    if (!text) return true;
    const t = text.toLowerCase().trim();

    // 0. прогнзы на тайм/период/половину — НЕ мусор
    if (/(тайм|период|половин|четверт|квартал|сет|раунд)/i.test(t)) {
        return false;
    }

    // 1. короткие ставки типа 1X, X2, П1, П2 — НЕ мусор
    if (/^(п1|п2|1x|x2|12)$/i.test(t)) {
        return false;
    }

    // 2. чисто "5 шайб."
    if (/^\d+\s*шайб[аы]?\.*$/.test(t)) return true;

    // 3. "5 шайб – Да/Нет."
    if (/^\d+\s*шайб[аы]?\s*[–-]\s*(да|нет)\.*$/.test(t)) return true;

    // 4. начинается с числа и НЕТ ключевых слов → мусор
    if (/^\d+/.test(t) && !(
        t.includes("тотал") ||
        t.includes("индивиду") ||
        t.includes("с форой") ||
        t.includes("побед") ||
        t.includes("обе") ||
        t.includes("каждая команда") ||
        t.includes("итб") || t.includes("итм") ||
        t.includes("тб") || t.includes("тм") ||
        t.includes("ф1") || t.includes("ф2")
    )) return true;

    return false;
}

/* РАЗБИЕНИЕ ALT  */

function splitMultiplePredictions(text) {
    if (!text) return [];

    const result = [];
    let buf = "";
    let depth = 0;

    for (let i = 0; i < text.length; i++) {
        const ch = text[i];

        if (ch === "(") depth++;
        if (ch === ")") depth--;

        // пропускаем запятую внутри чисел "5,5"
        if (/\d/.test(text[i - 1]) && ch === "," && /\d/.test(text[i + 1])) {
            buf += ch;
            continue;
        }

        // разделители: ".,", ";", ".;"
        if ((ch === "," || ch === ";") && depth === 0) {
            if (buf.trim()) result.push(buf.trim());
            buf = "";
            continue;
        }

        buf += ch;
    }

    if (buf.trim()) result.push(buf.trim());
    return result;
}

/* ------------------ ЧТЕНИЕ ФАЙЛОВ ------------------ */

const files = fs.readdirSync(PREDICTIONS_DIR).filter(f => f.endsWith(".json"));

for (const file of files) {
    const fullPath = path.join(PREDICTIONS_DIR, file);
    console.log(`\n--- Обрабатываем файл: ${file} ---`);

    let data;
    try {
        data = JSON.parse(fs.readFileSync(fullPath, "utf-8"));
    } catch (e) {
        console.error("Ошибка чтения:", file);
        continue;
    }

    let updated = 0;

    for (const match of data) {
        if (!match.prediction) continue;

        const home = match.teams?.home?.name || "";
        const away = match.teams?.away?.name || "";

        /* ---------- MAIN ---------- */
        if (match.prediction.main) {
            const ctx = { fileName: file, matchId: match.id, source: match.source, field: "main" };
            const newNorm = normalizePrediction(match.prediction.main, home, away, ctx);

            if (!newNorm) {
                logProblem("MAIN NOTHING MATCHED", match.prediction.main, ctx);
            } else if (newNorm !== match.prediction.main) {
                console.log(`   MAIN FIXED: "${match.prediction.main}" → "${newNorm}"`);
                match.prediction.main = newNorm;
                updated++;
            }
        }

        // ---------------- ALT ----------------
        const alt = match.prediction.alt;

        if (alt) {
            const parts = splitMultiplePredictions(alt);
            const normalizedParts = [];

            for (const p of parts) {
                const clean = p.trim();
                const ctx = {
                    fileName: file,
                    matchId: match.id,
                    source: match.source,
                    field: "alt"
                };

                // Пропускаем мусорные ALT ("5 шайб.", "5 шайб – Да.")
                if (isGarbageAlt(clean)) {
                    logProblem("SKIPPED GARBAGE ALT", clean, null, ctx);
                    continue;
                }

                const norm = normalizePrediction(clean, home, away, ctx);

                if (!norm) {
                    logProblem("NOTHING MATCHED (ALT PART)", clean, norm, ctx);
                    continue;
                }

                normalizedParts.push(norm);
            }

            const newAlt = normalizedParts.join(", ");

            if (newAlt !== alt) {
                console.log(`   ALT FIXED: "${alt}" → "${newAlt}"`);
                match.prediction.alt = newAlt;
                updated++;
            }
        }


    }

    fs.writeFileSync(fullPath, JSON.stringify(data, null, 2), "utf-8");
    console.log(`✔ Сохранён: ${file} | обновлено: ${updated}`);
}

console.log("\n=== GLOBAL NORMALIZATION FINISHED ===");
