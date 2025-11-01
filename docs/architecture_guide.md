# 1. Общие принципы архитектуры проекта TelegramBOT

## 1.1 Основная идея

Архитектура проекта основана на принципах **Clean Architecture** и **S.O.L.I.D.**, что обеспечивает гибкость, расширяемость и лёгкое сопровождение кода.

**Главная цель:** каждый слой отвечает только за свою зону ответственности и **не знает о внутренних деталях других слоёв**.  
Зависимости всегда направлены **вглубь — от внешних слоёв к внутренним**.  
Это гарантирует, что **изменения в нижнем уровне (например, Infrastructure)** не повлияют на работу **верхнего уровня (Presentation или Application)**.

## 1.2 Принципы чистой архитектуры

- Каждый слой решает **только свои задачи**.
- **Верхние слои** зависят только от **абстракций (интерфейсов)**, а не от конкретных реализаций.
- **Нижние слои** могут изменяться без влияния на верхние (например, смена БД или Telegram-клиента не ломает бизнес-логику).
- **Связи направлены к центру** — внешние уровни знают о внутренних, но не наоборот.
- Каждый слой подчиняется принципам **S.O.L.I.D.**:
  - **S (Single Responsibility)** — один класс = одна ответственность;
  - **O (Open/Closed)** — система расширяема без изменения существующего кода;
  - **L (Liskov Substitution)** — интерфейсы можно подменять реализациями без побочных эффектов;
  - **I (Interface Segregation)** — интерфейсы не перегружены ненужными методами;
  - **D (Dependency Inversion)** — зависимости строятся на абстракциях, а не реализациях.

---

# 2. Слои проекта и их назначение

- **Domain (Core)** — бизнес-сущности, интерфейсы и правила, независимые от реализации.
- **Application (Services)** — бизнес-логика и правила работы с доменными сущностями. Содержит чистые сервисы (CalendarService, PredictionService и т.д.), не зависящие от Telegram API или внешних источников.
- **Infrastructure** — реализация интерфейсов: работа с Telegram API, внешними источниками данных, файлами, логами и базой данных. Также содержит фоновые процессы (BotBackgroundService) и интеграционные сервисы.
- **Presentation (UI)** — Telegram-интерфейс: обработчики команд, меню, сообщения и визуализация.

---

# 3. C# — TelegramBOT (основной проект)

## 3.1 Структура слоёв

```
TelegramBOT/
├── Domain/                                   ← Бизнес-сущности и контракты (ядро, без зависимостей)
│   ├── Entities/
│   │   ├── Match.cs
│   │   ├── MatchVideo.cs
│   │   ├── Prediction.cs
│   │   ├── TeamStats.cs
│   │   ├── Team.cs
│   │   └── Users.cs
│   │
│   ├── Interfaces/
│   │   ├── ICalendarRepository.cs
│   │   ├── IPredictionRepository.cs
│   │   ├── IResultsRepository.cs
│   │   ├── IStandingsRepository.cs
│   │   ├── ITeamsRepository.cs
│   │   ├── IMatchStatsServiceRepository.cs
│   │   └── IMessageService.cs
│   │
│   └── Enums/
│       └── (вынести при появлении перечислений: MatchStatus, LeagueType и т.п.)
│
├── Application/
│   ├── Calendar/
│   │   └── CalendarService.cs
│   ├── Predictions/
│   │   └── PredictionService.cs
│   ├── Results/
│   │   └── ResultsService.cs
│   ├── Teams/
│   │   └── TeamsService.cs
│   ├── Standings/
│   │   ├── StandingsHtmlBuilder.cs
│   │   └── StandingsService.cs
│   ├── MatchStats/
│   │   └── MatchStatsService.cs
│   └── Utils/
│       └── MappingService.cs
│
├── Infrastructure/
│   ├── Data/
│   │   └── AppDbContext.cs
│   │
│   ├── Calendar/
│   │   └── CalendarRepository.cs
│   ├── Predictions/
│   │   └── PredictionRepository.cs
│   ├── Results/
│   │   └── ResultsRepository.cs
│   ├── Teams/
│   │   └── TeamsRepository.cs
│   ├── MatchStats/
│   │   └── MatchStatsRepository.cs
│   ├── Standings/
│   │   └── StandingsRepository.cs
│   │
│   ├── Telegram/
│   │   ├── MessageService.cs
│   │   ├── TelegramClientService.cs
│   │   └── BotBackgroundService.cs
│   │
│   ├── Scripts/
│   │   └── ScriptService.cs
│   │
│   └── Logging/
│       └── Logger.cs
│
├── Presentation/                             ← Telegram UI и обработчики
│   ├── Handlers/
│   │   ├── Calendar/
│   │   │   └── CalendarHandler.cs
│   │   ├── MatchStats/
│   │   │   └── MatchStatsHandler.cs
│   │   ├── Navigation/
│   │   │   └── NavigationHandler.cs
│   │   ├── Predictions/
│   │   │   └── PredictionHandler.cs
│   │   ├── Results/
│   │   │   └── ResultsHandler.cs
│   │   ├── System/
│   │   │   └── UpdateHandler.cs
│   │   ├── Teams/
│   │   │   └── TeamsHandler.cs
│   │   └── CommandHandler.cs
│   │
│   └── UI/
│       ├── Menus/
│       │   ├── Calendar/
│       │   │   ├── CalendarMenuBuilder.cs
│       │   │   └── MatchMenuBuilder.cs
│       │   ├── Main/
│       │   │   └── MainMenuBuilder.cs
│       │   ├── Predictions/
│       │   │   └── PredictionsMenuBuilder.cs
│       │   ├── Stats/
│       │   │   ├── TablesMenuBuilder.cs
│       │   │   └── ConferenceMenuBuilder.cs
│       │   └── Results/
│       │       ├── ResultsMenuBuilder.cs
│       │       └── TeamsMenuBuilder.cs
│       │
│       └── MenuService.cs
│
├── appsettings.json
├── deploy.ps1
├── Dockerfile
└── Program.cs
```

---

# 4. Принципы ответственности слоёв

| Слой               | Отвечает за                                                     | Не должен                         |
| ------------------ | --------------------------------------------------------------- | --------------------------------- |
| **Domain**         | Сущности и интерфейсы (`IResultsRepository`, `IMessageService`) | Содержать код Telegram, JSON, SQL |
| **Application**    | Бизнес-логика (`ResultsService`, `MappingService`)              | Работать напрямую с Telegram API  |
| **Infrastructure** | Реализация интерфейсов — Telegram, JSON, логирование, файлы     | Содержать бизнес-логику           |
| **Presentation**   | Telegram UI, обработчики команд, меню                           | Читать или писать данные напрямую |

---

# 5. Архитектурный поток

```
Telegram Update
↓
CommandHandler (Presentation)
↓
MatchStatsService / ResultsService (Application)
↓
IResultsRepository / IMatchStatsRepository (Domain)
↓
ResultsRepository / MatchStatsRepository (Infrastructure)
↓
JSON / External API / Database
```

---

# 6. Dependency Injection (DI)

- Все зависимости регистрируются в `Program.cs` через `IServiceCollection`.
- Infrastructure и Application не создают объекты напрямую через `new`.
- Для масштабируемости можно использовать расширения:
  - `AddDomainServices()`
  - `AddApplicationServices()`
  - `AddInfrastructureServices()`
  - `AddPresentationHandlers()`

---

---

# 7. Структура базы данных

### Основные таблицы:

| Таблица         | Назначение                                                                               |
| --------------- | ---------------------------------------------------------------------------------------- |
| **Users**       | Хранит данные пользователей Telegram (имя, ник, телефон, даты регистрации и обновления). |
| **Teams**       | Список команд, участвующих в матчах (уникальные названия).                               |
| **Matches**     | Информация о матчах — дата, статус, участники, счёт.                                     |
| **Predictions** | Прогнозы на матчи (основной, альтернативный, текст, результат).                          |
| **MatchVideos** | Видеообзоры и записи матчей с привязкой к YouTube.                                       |

---

## 7.1 SQL-структура таблиц

### Таблица `Users`

```sql
CREATE TABLE Users (
    userId BIGINT PRIMARY KEY,
    firstName NVARCHAR(64) NOT NULL,
    secondName NVARCHAR(64) NULL,
    username NVARCHAR(64) NULL,
    phoneNumber NVARCHAR(12) NULL,
    createdAt DATETIME NOT NULL DEFAULT GETDATE(),
    updatedAt DATETIME NOT NULL DEFAULT GETDATE()
);
```

### Таблица `Matches`

```sql
CREATE TABLE Matches (
    match_id VARCHAR(50) PRIMARY KEY,
    match_date DATETIME NOT NULL,
    status VARCHAR(50) NOT NULL, -- SCHEDULED, AFTER PENALTIES, AFTER OVERTIME, FINISHED

    home_team_name VARCHAR(255) NOT NULL,
    home_team_id INT NOT NULL,
    home_score INT NULL,

    away_team_name VARCHAR(255) NOT NULL,
    away_team_id INT NOT NULL,
    away_score INT NULL,

    FOREIGN KEY (home_team_id) REFERENCES Teams(team_id),
    FOREIGN KEY (away_team_id) REFERENCES Teams(team_id)
);

```

### Таблица `Teams`

```sql
CREATE TABLE Teams (
    team_id INT IDENTITY(1,1) PRIMARY KEY,
    name VARCHAR(255) UNIQUE NOT NULL
);
```

### Таблица `Predictions`

```sql
CREATE TABLE Predictions (
    prediction_id INT IDENTITY(1,1) PRIMARY KEY,
    match_id VARCHAR(50) NOT NULL FOREIGN KEY REFERENCES Matches(match_id),

    source NVARCHAR(255),
    url NVARCHAR(MAX),

    main_prediction NVARCHAR(MAX),
    alt_prediction NVARCHAR(MAX),
    score NVARCHAR(50),
    general_text NVARCHAR(MAX),
    result NVARCHAR(255),

    home_team_text NVARCHAR(MAX),
    away_team_text NVARCHAR(MAX)
);
```

### Таблица `MatchVideos`

```sql
CREATE TABLE MatchVideos (
    video_id INT IDENTITY(1,1) PRIMARY KEY,
    match_id VARCHAR(50) NOT NULL FOREIGN KEY REFERENCES Matches(match_id),
    title NVARCHAR(255) NOT NULL,
    url NVARCHAR(500) NOT NULL
);
```

## 7.2 Взаимосвязи

- Users — независимая сущность (Telegram пользователи).
- Teams связаны с Matches через home_team_id и away_team_id.
- Predictions ссылаются на Matches по match_id.
- MatchVideos также привязаны к Matches через match_id.

Эта структура обеспечивает целостность данных и позволяет строить запросы для отображения матчей, прогнозов и видеообзоров в Telegram UI.
