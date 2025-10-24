import fs from "fs";

/// <summary>
/// Унифицированный логгер для парсеров.
/// Каждый модуль создаёт собственный экземпляр через createLogger(site),
/// чтобы разграничить логи между источниками.
/// </summary>
export function createLogger(site = "parser") {
    const prefix = `[${site}]`;

    return {
        /// <summary>Начало парсинга источника.</summary>
        start() {
            console.log(`\n--- Начало парсинга: ${site} ---`);
        },

        /// <summary>Завершение парсинга источника.</summary>
        end() {
            console.log(`--- Завершён парсинг: ${site} ---\n`);
        },

        /// <summary>Информационное сообщение.</summary>
        info(message) {
            console.log(`${prefix} ${message}`);
        },

        /// <summary>Предупреждение.</summary>
        warn(message) {
            console.log(`${prefix} ⚠️ ${message}`);
        },

        /// <summary>Ошибка.</summary>
        error(message, error) {
            console.log(`${prefix} ❌ ${message}${error ? ` (${error.message})` : ""}`);
        },

        /// <summary>Вывод краткого итога после парсинга.</summary>
        summary(total, newCount) {
            console.log(`${prefix} Итог: добавлено новых прогнозов ${newCount}/${total}`);
        },

        /// <summary>Сохраняет JSON в файл.</summary>
        saveJson(filePath, data) {
            fs.writeFileSync(filePath, JSON.stringify(data, null, 2), "utf-8");
            console.log(`${prefix} 💾 Сохранено в ${filePath}`);
        },
    };
}
