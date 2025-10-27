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
│   │   ├── Team.cs
│   │   └── Users.cs
│   │
│   ├── Interfaces/
│   │   ├── ICalendarRepository.cs
│   │   ├── IPredictionRepository.cs
│   │   ├── IResultsRepository.cs
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
