/**
 * Скрипт проходит по всем JSON-файлам с прогнозами (например, legalbet.json, stavkatv.json,
 * vseprosport.json, betzona.json и др.), которые хранят предсказания парсеров.
 *
 * Для каждого матча в каждом файле:
 *   • извлекает поле prediction.main и prediction.alt (если есть);
 *   • повторно прогоняет их через функцию normalizePrediction, чтобы обновить
 *     формат записи прогнозов по актуальным правилам нормализации;
 *   • корректирует старые или устаревшие форматы до нового стандарта (Ф1 (-1.5), ТБ (5.5), ИТБ1 (2.5), 1X и т.д.);
 *   • сохраняет обновлённые прогнозы обратно в тот же JSON-файл.
 *
 * Скрипт используется для массовой переработки уже сохранённых прогнозов,
 * включая те матчи, которые уже завершены, чтобы привести всю историческую базу
 * к единообразному виду и устранить старые ошибки нормализации.
 */

import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";
import { normalizePrediction } from "./utils/predictions/normalizePrediction.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const PREDICTIONS_DIR = path.join(__dirname, "../../data/predictions");

console.log("=== START GLOBAL NORMALIZATION ===");

// получить список всех json в директории
const files = fs.readdirSync(PREDICTIONS_DIR).filter(f => f.endsWith(".json"));

for (const file of files) {
    const fullPath = path.join(PREDICTIONS_DIR, file);

    console.log(`\n--- Обрабатываем файл: ${file} ---`);

    let data;
    try {
        const raw = fs.readFileSync(fullPath, "utf-8");
        data = JSON.parse(raw);
    } catch (err) {
        console.error(`Ошибка чтения ${file}:`, err);
        continue;
    }

    let updated = 0;

    for (const match of data) {
        if (!match.prediction) continue;

        const main = match.prediction.main;
        const home = match.teams?.home?.name || "";
        const away = match.teams?.away?.name || "";

        if (main) {
            const newNorm = normalizePrediction(main, home, away);

            if (newNorm && newNorm !== main) {
                match.prediction.main = newNorm;
                updated++;
            }
        }

        // обработка alt, если есть
        const alt = match.prediction.alt;
        if (alt) {
            const newAlt = normalizePrediction(alt, home, away);
            if (newAlt && newAlt !== alt) {
                match.prediction.alt = newAlt;
                updated++;
            }
        }
    }

    // сохраняем файл обратно
    try {
        fs.writeFileSync(fullPath, JSON.stringify(data, null, 2), "utf-8");
        console.log(`✔ Сохранён: ${file} (обновлено прогнозов: ${updated})`);
    } catch (err) {
        console.error(`Ошибка записи ${file}:`, err);
    }
}

console.log("\n=== GLOBAL NORMALIZATION FINISHED ===");
