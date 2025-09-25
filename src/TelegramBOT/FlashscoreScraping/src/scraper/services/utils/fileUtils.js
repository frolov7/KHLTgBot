import fs from "fs";

/**
 * Безопасно загружает JSON-массив из файла.
 * @param {string} filePath Путь к JSON-файлу
 * @returns {Array} Массив данных или []
 */
export function loadJsonArray(filePath) {
    if (!fs.existsSync(filePath)) return [];
    try {
        const text = fs.readFileSync(filePath, "utf-8").trim();
        if (!text) return [];
        return JSON.parse(text);
    } catch (err) {
        console.error(`❌ Ошибка чтения JSON ${filePath}: ${err.message}`);
        return [];
    }
}

/**
 * Добавляет новые элементы в JSON без дубликатов.
 * @param {string} filePath Путь к файлу
 * @param {Array} newItems Новые элементы
 * @param {(item: any) => string} keyFn Функция генерации уникального ключа
 * @returns {{ merged: Array, added: number }} Итоговый массив и количество новых элементов
 */
export function appendUniqueJson(filePath, newItems, keyFn) {
    const oldItems = loadJsonArray(filePath);
    const seen = new Set(oldItems.map(keyFn));
    let added = 0;

    for (const item of newItems) {
        const key = keyFn(item);
        if (!seen.has(key)) {
            oldItems.push(item);
            seen.add(key);
            added++;
        }
    }

    fs.writeFileSync(filePath, JSON.stringify(oldItems, null, 2), "utf-8");
    return { merged: oldItems, added };
}