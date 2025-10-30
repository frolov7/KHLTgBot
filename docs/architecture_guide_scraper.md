# 1. Общие принципы архитектуры проекта FlashscoreScraping
## 1.1 Основная идея

Проект FlashscoreScraping реализует систему автоматизированного парсинга спортивных данных (матчи, прогнозы, видеообзоры) с различных источников (в первую очередь Flashscore и сторонние сайты прогнозов).
Его основная цель — предоставить актуальные данные для проекта TelegramBOT, выполняющего визуализацию и доставку информации пользователям.

## 1.2 Принципы модульной архитектуры

Архитектура построена по принципам чистой модульной структуры и частично повторяет логику Clean Architecture:

* каждый модуль выполняет одну зону ответственности;
* все зависимости направлены вниз по иерархии — от orchestration (scraperRunner.js) к конкретным парсерам;
* между слоями используется явное взаимодействие через интерфейсы и JSON-файлы, без перекрёстных импортов;
* код полностью асинхронный, использует async/await и промисы.

## 1.3 Ключевые принципы

* Изоляция ответственности — каждый модуль делает только своё: парсер прогнозов не знает о логике импорта, а модуль импорта не парсит HTML.
* Минимум жёстких связей — все пути, URL и файлы объявлены централизованно в constants/constants.js.
* Прозрачный запуск — всё управление выполняется через аргументы CLI (--updateResults, --predictions, --resultvideos, --import и т.д.).
* Расширяемость — добавление нового источника данных не требует изменения основной логики: достаточно добавить новый парсер в services/predictions/.
* Централизованное логирование — все операции записываются через logger.js, чтобы логи можно было анализировать отдельно.

# 2. Слои проекта и их назначение
| Уровень          | Назначение                                                                | Примеры модулей                                                    |
| ---------------- | ------------------------------------------------------------------------- | ------------------------------------------------------------------ |
| **Scraper Core** | Главный управляющий код, orchestrator                                     | `scraperRunner.js`                                                 |
| **Services**     | Управление задачами обновления, запуск парсеров, обработка данных         | `updateMatches.js`, `betzonaParse.js`, `vprognozeParse.js`         |
| **Utils/Core**   | Общие утилиты — логирование, работа с датами, JSON, Puppeteer             | `logger.js`, `pageUtils.js`, `dateUtils.js`                        |
| **DB/Import**    | Загрузка данных в основную базу TelegramBOT через внешние Node.js-скрипты | `importMatches.js`, `importPredictions.js`, `importMatchVideos.js` |
| **Data Layer**   | Локальное хранилище данных (JSON-файлы) между этапами обработки           | `/data/matches/russia_khl_all.json`, `/data/predictions/*.json`    |
| **Constants**    | Централизованные пути и настройки                                         | `constants.js`                                                     |


# 3. JS — FlashscoreScraping (основной проект)
## 3.1 Структура каталогов
```
FlashscoreScraping/
├── src/
│   ├── constants/
│   │   └── constants.js
│   │
│   ├── data/
│   │   ├── calendar/
│   │   │   └── full_calendar.json
│   │   ├── matches/
│   │   │   └── russia_khl_all.json
│   │   ├── predictions/
│   │   │   ├── betzona.json
│   │   │   ├── legabet.json
│   │   │   ├── livesport.json
│   │   │   ├── metaratings.json
│   │   │   ├── stavkatv.json
│   │   │   ├── vprognoze.json
│   │   │   └── vesprosport.json
│   │   └── videos/
│   │       └── resultVideos.json
│   │
│   ├── db/
│   │   ├── import/
│   │   │   ├── importMatches.js
│   │   │   ├── importMatchVideos.js
│   │   │   ├── importPredictions.js
│   │   │   └── updatePredictionResults.js
│   │
│   ├── scraper/
│   │   ├── parsers/
│   │   │   └── khlYoutubeParser.js
│   │   │
│   │   └── services/
│   │       ├── matches/
│   │       │   └── updateMatches.js
│   │       ├── predictions/
│   │       │   ├── betzonaParse.js
│   │       │   ├── legabetParse.js
│   │       │   ├── livesportParse.js
│   │       │   ├── metaRatingsParse.js
│   │       │   ├── stavkatvParse.js
│   │       │   ├── vprognozeParse.js
│   │       │   └── vesprosportParse.js
│   │       │
│   │       └── utils/
│   │           ├── core/
│   │           │   ├── dateUtils.js
│   │           │   ├── jsonUtils.js
│   │           │   ├── logger.js
│   │           │   └── pageUtils.js
│   │           ├── matches/
│   │           │   ├── teamMapUtils.js
│   │           │   └── validateKhlCalendar.js
│   │           └── predictions/
│   │               ├── predictionParser.js
│   │               └── validatePredictionsData.js
│   │
│   └── scraperRunner.js
│
├── app.js
├── package.json
├── package-lock.json
└── .eslintrc.json
```

# 4. Принципы ответственности слоёв
| Слой / Категория     | Отвечает за                                                         | Не должен                                    |
| -------------------- | ------------------------------------------------------------------- | -------------------------------------------- |
| **scraper/parsers**  | Извлечение HTML-данных и их разбор                                  | Работать с базой данных или JSON напрямую    |
| **scraper/services** | Управление процессом сбора данных, агрегация и координация парсеров | Вмешиваться в детали импорта или хранения    |
| **db/import**        | Импорт данных в БД TelegramBOT (через SQL или API)                  | Выполнять сетевые запросы к сайтам           |
| **data/**            | Хранение исходных или промежуточных данных                          | Изменять структуру данных                    |
| **utils/**           | Проверка, логирование, преобразование данных                        | Выполнять бизнес-логику или сетевые операции |

# 5. Основные процессы и потоки данных
## 5.1 Поток обновления матчей
```
scraperRunner.js (--updateResults)
↓
updateMatches.js → Flashscore KHL pages
↓
pageUtils.openPageAndNavigate() → Puppeteer
↓
JSON обновляется → /data/matches/russia_khl_all.json
↓
exec(importMatches.js) → импорт в БД TelegramBOT
↓
exec(importPredictions.js) → проверка прогнозов

```
## 5.2 Поток парсинга прогнозов
```
scraperRunner.js (--predictions)
↓
Запускаются парсеры:
  betzonaParse.js
  legalbetParse.js
  livesportParse.js
  metaratingsParse.js
  stavkatvParse.js
  vprognozeParse.js
  vseprosportParse.js
↓
cheerio → HTML → структурированные объекты
↓
appendUniqueJson() → JSON-файлы в /data/predictions/
↓
exec(importPredictions.js) → запись в БД

```

## 5.3 Хранение данных в JSON
Проект FlashscoreScraping использует файловое хранилище в формате JSON для передачи данных между этапами парсинга и последующей интеграции с проектом TelegramBOT.
Каждый модуль (матчи, прогнозы, видео) сохраняет результаты в отдельные файлы, но все они связаны единым идентификатором матча — match_id, который берётся из файла /data/matches/russia_khl_all.json.

### 5.3.1 Общая концепция хранения

* Все данные разделены по категориям: matches, predictions, videos.
* Ключевым полем, связывающим эти категории, является id (или match_id).
Этот идентификатор уникален и совпадает во всех JSON-файлах, чтобы TelegramBOT мог связывать:
    * матч → его прогнозы → видеообзор;
* Файлы обновляются по мере выполнения соответствующих задач (--updateResults, --predictions, --resultvideos);
* Формат данных стандартизирован, чтобы их можно было без проблем импортировать в БД бота.

### 5.3.2 Матчи — /data/matches/russia_khl_all.json
Файл представляет собой объект, где ключом выступает идентификатор матча (match_id с Flashscore), а значением — подробная структура данных о матче.
```
{
  "boRXxDHG": {
    "id": "boRXxDHG",
    "date": "06.09.2025 14:30",
    "status": "AFTER PENALTIES",
    "home": { "name": "Magnitogorsk" },
    "away": { "name": "Bars Kazan" },
    "result": { "home": "4", "away": "3" }
  },
  "6RfsR4nA": {
    "id": "6RfsR4nA",
    "date": "06.09.2025 16:00",
    "status": "FINISHED",
    "home": { "name": "CSKA Moscow" },
    "away": { "name": "Dynamo Moscow" },
    "result": { "home": "6", "away": "2" }
  }
}
```
Назначение полей:

* id — уникальный идентификатор матча;
* date — время начала (формат DD.MM.YYYY HH:mm);
* status — текущее состояние (например SCHEDULED, FINISHED, AFTER PENALTIES, AFTER OVERTIME);
* home и away — объекты с именами команд;
* result — счёт по итогу матча.

Эти данные напрямую импортируются в БД TelegramBOT при вызове importMatches.js.

### 5.3.3 Прогнозы — /data/predictions/*.json
Каждый файл соответствует одному источнику (например, betzona.json, vprognoze.json).
Структура массива едина для всех, однако некоторые поля (teams.home.text, alt, score) могут отсутствовать в зависимости от источника.
```
[
  {
    "source": "betzona",
    "url": "https://betzona.ru/traktor-avtomobilist-prognoz-1761571094.html",
    "match": "Трактор – Автомобилист",
    "teams": {
      "home": { "name": "Трактор", "text": "..." },
      "away": { "name": "Автомобилист", "text": "..." }
    },
    "prediction": { "main": "Ф1(0)", "text": "", "result": null },
    "id": "QNmvHdmT"
  },
  {
    "source": "betzona",
    "url": "https://betzona.ru/severstal-amur-prognoz-1761599901.html",
    "match": "Северсталь – Амур",
    "teams": {
      "home": { "name": "Северсталь", "text": "..." },
      "away": { "name": "Амур", "text": "..." }
    },
    "prediction": { "main": "Победа 1", "text": "", "result": null },
    "id": "p8UABzBj"
  }
]
```
Описание ключей:
* source — название сайта-источника (betzona, vprognoze, и т. д.);
* url — оригинальная ссылка на прогноз;
* match — текстовое представление пары команд;
* teams.home / teams.away — названия и аналитические тексты;
* prediction.main — основной исход (например Ф1(0), П2, ТМ 4.5);
* prediction.result — результат прогноза после завершения матча (true, false или null);
* id — уникальный идентификатор прогноза.

Файлы создаются или обновляются при запуске scraperRunner.js --predictions,
а позже импортируются в TelegramBOT через importPredictions.js.

### 5.3.4 Видеообзоры — /data/videos/resultVideos.json
Хранит массив видеоматериалов с официального YouTube-канала КХЛ.
Каждая запись содержит минимум идентификатор и ссылку на видео.
```
[
  {
    "title": "ШАНХАЙСКИЕ ДРАКОНЫ – ДИНАМО МОСКВА | Обзор матча Фонбет КХЛ сезон 2025/2026 | 14.10.2025",
    "url": "https://www.youtube.com/watch?v=rSODs0Y1TAs",
    "id": "MZpjnOy2"
  },
  {
    "title": "ДИНАМО МИНСК – ТОРПЕДО | Обзор матча Фонбет КХЛ сезон 2025/2026 | 14.10.2025",
    "url": "https://www.youtube.com/watch?v=KPjYvPBEwMg",
    "id": "QRNgAzOi"
  }
]
```
Описание ключей:

* title — полное название видео;
* url — прямая ссылка на YouTube;
* id — уникальный идентификатор ролика (короткий UUID или ID в БД).

Файл обновляется при вызове scraperRunner.js --resultvideos и служит источником для TelegramBOT при публикации обзоров.

# 6. Основной orchestrator — scraperRunner.js

Файл scraperRunner.js является главной точкой входа в систему.
Он управляет выполнением всех сценариев:
* принимает аргументы CLI (--updateResults, --predictions, --resultvideos, --import, --validate);
* создаёт экземпляр браузера Puppeteer (headless: "new");
* запускает соответствующие сервисы;
* выполняет пост-обработку (импорт в БД);
* ведёт централизованный лог через createLogger().

Каждый этап сопровождается временными метками и статусами выполнения.

# 7. Принципы кода и соглашения

* Асинхронность: все операции I/O (парсинг, запись, HTTP-запросы, Puppeteer) реализованы через async/await.
* Логирование:
    * logger.js создаёт отдельный логгер для каждого модуля (createLogger("updateMatches")).
    * Формат лога: [INFO], [ERROR], [WARN], [DEBUG].
* Отладка: все ошибки логируются, но не блокируют выполнение следующих источников.
* CLI-интерфейс: скрипт можно вызвать напрямую:
    ```
    node src/scraper/scraperRunner.js --predictions
    ```
* Комментарии: используются XML-style /// <summary> — единообразно с C# TelegramBOT.
* Валидация: отдельные модули (validateKhlCalendar.js, validatePredictionsData.js) проверяют корректность JSON до импорта.

# 8. Используемые технологии и библиотеки
| Библиотека                 | Назначение                                                     |
| -------------------------- | -------------------------------------------------------------- |
| **Puppeteer**              | Автоматизация браузера Chrome/Chromium для парсинга Flashscore |
| **Cheerio**                | Парсинг HTML на стороне Node.js (аналог jQuery)                |
| **Day.js**                 | Работа с датами, конвертация форматов                          |
| **Iconv-lite**             | Конвертация stdout из `child_process` в UTF-8                  |
| **Child_process.exec**     | Запуск вспомогательных Node.js скриптов (импорта)              |
| **FS / Path / URL**        | Работа с файловой системой и путями в ESM                      |
| **Fetch API (встроенный)** | Загрузка страниц для сайтов прогнозов                          |

# 9. Расширение архитектуры

Чтобы добавить новый источник прогнозов:
1. Создать файл src/scraper/services/predictions/newsourceParse.js.
2. Реализовать функцию:
    ```
    export async function scrapePredictions({ logger }) { ... }
    ```
3. Подключить её в scraperRunner.js в массив scrapers.
4. Определить путь сохранения в constants.js.
5. Добавить поддержку импорта при необходимости.

# 10. Пример команд запуска
```
# Обновление результатов матчей КХЛ
node src/scraper/scraperRunner.js --updateResults

# Парсинг всех прогнозов
node src/scraper/scraperRunner.js --predictions

# Парсинг видеообзоров
node src/scraper/scraperRunner.js --resultvideos

# Импорт данных вручную
node src/scraper/scraperRunner.js --import

```