// src/scraper/services/utils/logger.js
import fs from "fs";

/**
 * Унифицированный логгер для парсеров.
 * Каждый парсер создаёт свой экземпляр через createLogger(site),
 * чтобы избежать конфликтов при одновременном запуске.
 */
export function createLogger(site = "parser") {
    const prefix = `[${site}]`;

    return {
        start() {
            console.log(`\n--- Начало парсинга: ${site} ---`);
        },
        end() {
            console.log(`--- Завершён парсинг: ${site} ---\n`);
        },
        info(message) {
            console.log(`${prefix} ${message}`);
        },
        warn(message) {
            console.warn(`${prefix} ⚠️ ${message}`);
        },
        error(message, error) {
            console.error(`${prefix} ❌ ${message}: ${error?.message || error}`);
        },
        summary(total, newCount) {
            console.log(`${prefix} ✅ Итог: добавлено новых прогнозов ${newCount}/${total}`);
        },
        formatDate(date) {
            return date instanceof Date
                ? date.toISOString().split(".")[0] + "Z"
                : String(date);
        },
        saveJson(filePath, data) {
            fs.writeFileSync(filePath, JSON.stringify(data, null, 2), { encoding: "utf8" });
            console.log(`${prefix} Данные сохранены в ${filePath}`);
        },
    };
}
