import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const dataDir = path.join(__dirname, "../../../data");

// список файлов, которые нужно валидировать
const excludeFiles = ["full calendar.json", "russia_khl_all.json"];
const files = fs.readdirSync(dataDir)
    .filter(f => f.endsWith(".json") && !excludeFiles.includes(f));

for (const file of files) {
    const filePath = path.join(dataDir, file);

    try {
        const raw = fs.readFileSync(filePath, "utf-8");
        const json = JSON.parse(raw);

        const predictions = Array.isArray(json) ? json : Object.values(json);

        console.log(`\n🔍 Проверка файла: ${file} (записей: ${predictions.length})`);

        const seenIds = new Set();
        let duplicates = 0;
        let emptyTexts = 0;
        let emptyAnalysis = 0;

        for (const pred of predictions) {
            const id = pred.id || pred.match || null;

            // дубликаты внутри файла
            if (id) {
                if (seenIds.has(id)) {
                    console.error(`🚨 Дубликат matchId "${id}" в файле ${file}`);
                    duplicates++;
                } else {
                    seenIds.add(id);
                }
            }

            // проверка text
            if (pred.prediction && (!pred.prediction.text || pred.prediction.text.trim() === "")) {
                console.warn(`⚠️ Пустой text в файле ${file}, id: "${pred.id}"`);
                emptyTexts++;
            }

            // проверка analysis
            if (pred.teams) {
                if (pred.teams.home && "analysis" in pred.teams.home && !pred.teams.home.analysis?.trim()) {
                    console.warn(`⚠️ Пустой home.analysis в файле ${file}, id: "${pred.id}"`);
                    emptyAnalysis++;
                }
                if (pred.teams.away && "analysis" in pred.teams.away && !pred.teams.away.analysis?.trim()) {
                    console.warn(`⚠️ Пустой away.analysis в файле ${file}, id: "${pred.id}"`);
                    emptyAnalysis++;
                }
            }
        }

        console.log(`📊 Итоги для ${file}:`);
        console.log(`   🔁 Дубликатов: ${duplicates}`);
        console.log(`   📝 Пустых text: ${emptyTexts}`);
        console.log(`   📉 Пустых analysis: ${emptyAnalysis}`);

    } catch (err) {
        console.error(`\n❌ Ошибка в файле ${file}: ${err.message}`);

        const raw = fs.readFileSync(filePath, "utf-8");
        const match = err.message.match(/position (\d+)/);

        if (match) {
            const pos = parseInt(match[1], 10);
            const snippet = raw.substring(Math.max(0, pos - 80), pos + 80);

            console.error("--- Подозрительное место ---");
            console.error(snippet);
            console.error(" ".repeat(80) + "↑");
            console.error(`(символ №${pos})`);

            // доп. диагностика
            if (/double-quoted property/.test(err.message)) {
                console.error("💡 Вероятно, у свойства нет двойных кавычек.");
            } else if (/Unexpected string/.test(err.message)) {
                console.error("💡 Похоже, пропущена запятая между свойствами.");
            } else if (/Unexpected token }|]/.test(err.message)) {
                console.error("💡 Лишняя запятая перед закрывающей скобкой.");
            } else if (/Unexpected end/.test(err.message)) {
                console.error("💡 JSON обрезан: не хватает закрывающей скобки или кавычек.");
            }
        }
    }
}
