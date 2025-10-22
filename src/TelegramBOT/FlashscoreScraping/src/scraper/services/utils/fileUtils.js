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
    let existing = [];
    if (fs.existsSync(filePath)) {
        existing = JSON.parse(fs.readFileSync(filePath, "utf-8"));
    }

    const map = new Map();

    // Загружаем старые записи
    for (const item of existing) {
        map.set(keyFn(item), item);
    }

    let added = 0;
    for (const item of newItems) {
        const key = keyFn(item);
        const old = map.get(key);

        if (!old) {
            map.set(key, item);
            added++;
        } else {
            // 🔥 если раньше текст был пустой, а теперь появился → обновляем
            const shouldUpdate =
                (!old.prediction?.main && item.prediction?.main) ||
                (!old.prediction?.text && item.prediction?.text) ||
                (!old.teams?.home?.text && item.teams?.home?.text) ||
                (!old.teams?.away?.text && item.teams?.away?.text);

            if (shouldUpdate) {
                map.set(key, { ...old, ...item });
                console.log(`Обновили прогноз для ${item.match}`);
            }
        }
    }

    const merged = Array.from(map.values());
    fs.writeFileSync(filePath, JSON.stringify(merged, null, 2), "utf-8");

    return { merged, added };
}