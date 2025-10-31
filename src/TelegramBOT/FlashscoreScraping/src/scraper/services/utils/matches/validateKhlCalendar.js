import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";
import { createLogger } from "../core/logger.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const logger = createLogger("validateKhlCalendar");

// правильный путь до data
const filePath = path.join(__dirname, "../../../data/khl_all_matches.json");

try {
    const raw = fs.readFileSync(filePath, "utf-8");

    // пробуем распарсить
    const data = JSON.parse(raw);

    logger.info("JSON валидный!");
    logger.info("Всего матчей:", Object.keys(data).length);

    // фильтруем только матчи, где есть результат
    const withResults = Object.values(data).filter(
        (m) => m.result && m.result.home !== null && m.result.away !== null
    );

    logger.info("Матчей с результатами:", withResults.length);

} catch (err) {
    logger.error("Ошибка в JSON:", err.message);

    // выводим несколько символов вокруг ошибки, чтобы проще искать
    if (err.message.includes("position")) {
        const match = err.message.match(/position (\d+)/);
        if (match) {
            const pos = parseInt(match[1], 10);
            const raw = fs.readFileSync(filePath, "utf-8");
            const snippet = raw.substring(Math.max(0, pos - 50), pos + 50);

            logger.error("\n--- Подозрительное место вокруг ошибки ---");
            logger.error(snippet);
            logger.error("\n(👆 смотри здесь — возможно лишняя запятая или не хватает скобки)");
        }
    }
}
