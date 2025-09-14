// app.js
import('./src/scraper/scraperRunner.js')
    .then((main) => {
        // Аргументы из командной строки
        let args = process.argv.slice(2);

        // Если аргументы не переданы, по умолчанию ставим --results
        if (args.length === 0) {
            args = ['--results'];
        }

        console.log("Запущено с аргументами:", args);

        // Запускаем основную функцию из index.js
        if (typeof main.default === 'function') {
            main.default(args);
        }
    })
    .catch((err) => {
        console.error(err);
        process.exit(1);
    });