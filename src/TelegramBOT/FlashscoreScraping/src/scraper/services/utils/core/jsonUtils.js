import fs from "fs";
import { createLogger } from "./logger.js";

const logger = createLogger("jsonUtils");
/// <summary>
/// Безопасно загружает JSON-массив из файла.
/// </summary>
export function loadJsonArray(filePath) {
    if (!fs.existsSync(filePath)) return [];
    try {
        const text = fs.readFileSync(filePath, "utf-8").trim();
        if (!text) return [];
        return JSON.parse(text);
    } catch (err) {
        logger.error(`Ошибка чтения JSON (${filePath}): ${err.message}`);
        return [];
    }
}

/// <summary>
/// Добавляет новые элементы в JSON без дубликатов по ключу.
/// Если текстовые поля были пустыми — обновляет их при наличии новых данных.
/// </summary>
export function appendUniqueJson(filePath, newItems, keyFn) {
    const existing = loadJsonArray(filePath);
    const map = new Map(existing.map((item) => [keyFn(item), item]));
    let added = 0;

    for (const item of newItems) {
        const key = keyFn(item);
        if (!key) {
            logger.warn(`⚠️ Пропущен элемент без ключа:`, item);
            continue;
        }

        const old = map.get(key);

        if (!old) {
            map.set(key, item);
            added++;
        } else {
            const shouldUpdate =
                (!old.prediction?.main && item.prediction?.main) ||
                (!old.prediction?.text && item.prediction?.text) ||
                (!old.teams?.home?.text && item.teams?.home?.text) ||
                (!old.teams?.away?.text && item.teams?.away?.text);

            if (shouldUpdate) {
                map.set(key, { ...old, ...item });
                logger.info(`Обновлён прогноз для ${item.match}`);
            }
        }
    }

    const merged = Array.from(map.values());

    try {
        fs.writeFileSync(filePath, JSON.stringify(merged, null, 2), "utf-8");
    } catch (err) {
        logger.error(`Ошибка записи JSON (${filePath}): ${err.message}`);
    }

    return { merged, added };
}
