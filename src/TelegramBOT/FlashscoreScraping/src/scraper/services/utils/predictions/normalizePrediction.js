// src/scraper/services/predictions/utils/normalizePrediction.js

import fs from "fs";
import path from "path";

/**
 * Загружает словарь нормализации из JSON.
 * Расположен в /src/data/normalizationDictionary.json
 */
function loadDictionary() {
    const filePath = path.resolve("src/data/normalizationDictionary.json");

    if (!fs.existsSync(filePath)) {
        console.warn("⚠ Файл normalizationDictionary.json не найден!");
        return [];
    }

    const json = JSON.parse(fs.readFileSync(filePath, "utf-8"));

    // Преобразуем в массив "синоним → канон"
    return json.map(item => ({
        canonical: item.canonical,
        synonyms: item.synonyms || []
    }));
}

/**
 * Основная функция нормализации текста прогноза.
 * Заменяет синонимы на каноническую форму из словаря.
 */
export function normalizePredictionText(text) {
    if (!text) return text;

    const dict = loadDictionary();

    let result = text.trim();

    for (const entry of dict) {
        const canonical = entry.canonical;

        for (const variant of entry.synonyms) {
            const escaped = variant.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
            const regex = new RegExp(`\\b${escaped}\\b`, "gi");

            result = result.replace(regex, canonical);
        }
    }

    return result;
}

/**
 * Нормализация объекта прогнозов
 * { main, alt, text }
 */
export function normalizePrediction(prediction) {
    if (!prediction) return prediction;

    return {
        ...prediction,
        main: normalizePredictionText(prediction.main),
        alt: normalizePredictionText(prediction.alt),
        text: normalizePredictionText(prediction.text)
    };
}
