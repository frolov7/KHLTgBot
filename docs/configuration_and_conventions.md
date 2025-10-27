# 1. Конфигурация проекта (appsettings.json)

Вся конфигурация TelegramBOT хранится централизованно в файле appsettings.json.
Этот файл определяет:

- параметры логирования,
- подключение к базе данных,
- настройки Telegram API,
- пути к скриптам,
- и справочники (словари) для отображения данных на русском языке.

## 1.1 Основные принципы

1. Все параметры конфигурации (токены, пути, строки подключения, словари и т.д.) находятся только в appsettings.json.
   Код не содержит "жёстко прописанных" значений (hardcoded).
2. Telegram Token и другие чувствительные данные не должны храниться в публичных репозиториях.
   Для продакшена — токен загружается из переменных окружения.
3. Конфигурация читается через стандартный механизм .NET IConfiguration
   (автоматически в Program.cs при инициализации DI).
4. Все сервисы получают параметры через внедрение зависимостей — ни один сервис не открывает appsettings.json напрямую.

---

## 1.2 Секции конфигурации

1. Logging

Настройки уровня логирования через Serilog и стандартный ILogger.

- Default — общий уровень логирования.
- Microsoft.AspNetCore, Microsoft.Hosting.Lifetime — для системных событий.
- Логи пишутся в /Log/log-\*.txt.

2. Telegram
   Токен для Telegram API.

```json
{ "Telegram": { "Token": "..." } }
```

Используется в TelegramClientService при инициализации TelegramBotClient.

В продакшене токен должен подставляться из переменной окружения:

```
export Telegram__Token="secret"
```

3. ConnectionStrings

Раздел ConnectionStrings содержит строку подключения к базе данных, но не используется напрямую в классе AppDbContext.
Она внедряется через Dependency Injection при конфигурации сервисов в Program.cs.

```
"ConnectionStrings": {
  "DefaultConnection": "Data Source=...;Initial Catalog=TelegramBOT;Integrated Security=true;"
}
```

В Program.cs строка подключения извлекается из конфигурации (IConfiguration) и передаётся при регистрации AppDbContext:

```services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
```

4. Script

Путь к проекту с JavaScript-скриптами для сбора данных (парсинг Flashscore и т.п.).

```
"Script": {
"WorkingDirectory": "C:\\...\\FlashscoreScraping"
}
```

Используется сервисом ScriptService (в папке Infrastructure/Scripts/) для выполнения JS-скриптов и генерации данных, которые затем обрабатываются ботом.

5. TeamNames

Словарь соответствий названий команд из БД (на английском) к читаемым русским названиям с эмодзи.

```
"TeamNames": {
"Avangard Omsk": "🦅 Авангард",
"CSKA Moscow": "★ ЦСКА"
}
```

Применяется в ResultsService, MatchStatsService и UI/MenuService при отображении команд.
Таким образом, английские идентификаторы из базы преобразуются в локализованные, человекопонятные названия.

6. MatchStatuses

Аналогично TeamNames, хранит отображаемые статусы матчей на русском с форматированием (HTML + emoji).

```
"MatchStatuses": {
"FINISHED": "✅ Завершён (<u>Основное время</u>)",
"SCHEDULED": "📌 Не начался"
}
```

---

## 1.3 Логирование

- Используется Serilog (Infrastructure/Logging/Logger.cs).
- Логи сохраняются в /Log/log-\*.txt.
- Ошибки фиксируются через Log.Error(), информационные события — Log.Information().

---

## 1.4 Исключения и обработка ошибок

- Исключения из Infrastructure пробрасываются вверх, не подавляются.
- Обработка ошибок выполняется на уровне Application или Presentation.
- Пример:
  ```
  try
  {
     await _predictionService.GetPredictionAsync(...);
  }
  catch (Exception ex)
  {
     Log.Error(ex, "Ошибка при получении прогноза");
     await _messageService.SendTextAsync(chatId, "Произошла ошибка. Попробуйте позже.");
  }
  ```

---

## 1.5 Принципы и соглашения

### 1.5.1 Именование

- Классы — существительные (ResultsService), методы — глаголы (BuildMatchListAsync()).
- Интерфейсы начинаются с I: IResultsRepository, IMatchStatsRepository.

### 1.5.2 Комментарии

- XML-комментарии обязательны для всех публичных методов.
- Бизнес-методы описываются кратко: назначение, параметры, возвращаемое значение.

### 1.5.3 Асинхронность

- Все операции ввода/вывода выполняются через async/await.
- Репозитории, сервисы и Telegram API — асинхронные.

### 1.5.4 Форматирование и UI

- HTML и emoji форматирование:
  - в MessageService — для сообщений,
  - в MenuService и TextFormatter — для UI.
- Строки сообщений собираются через StringBuilder.

---

## 1.6 Принципы изоляции и стабильности

- Каждый слой изолирован от изменений других.
- Изменение логики в Infrastructure (например, замена JSON на PostgreSQL) не должно влиять на Application и Presentation.
- Domain никогда не должен знать, как данные сохраняются или отображаются.
- Presentation не должен знать, как данные получены — только как их показать.

---

## 1.7 Тестирование

- Unit-тесты — для Application (проверка логики).
- Mocks — для интерфейсов Domain (IResultsRepository и т.п.).
- Integration-тесты — для Infrastructure (Telegram API, JSON).
- Handlers тестируются с использованием Telegram.Bot.MockClient.
