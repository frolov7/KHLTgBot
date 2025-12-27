using Serilog;
using Telegram.Bot.Types;
using TelegramBOT.Presentation.Handlers.Calendar;
using TelegramBOT.Presentation.Handlers.MatchEvents;
using TelegramBOT.Presentation.Handlers.MatchStats;
using TelegramBOT.Presentation.Handlers.Navigation;
using TelegramBOT.Presentation.Handlers.Predictions;
using TelegramBOT.Presentation.Handlers.Results;
using TelegramBOT.Presentation.Handlers.System;
using TelegramBOT.Presentation.Handlers.Teams;

namespace TelegramBOT.Presentation.Handlers
{
    /// <summary>
    /// Главный обработчик входящих сообщений и callback-запросов.
    /// Делегирует работу соответствующим обработчикам.
    /// </summary>
    public class CommandHandler
    {
        // ==========================================================
        // ============           ЗАВИСИМОСТИ            ============
        // ==========================================================

        private readonly CalendarHandler _calendarHandler;
        private readonly ResultsHandler _resultsHandler;
        private readonly MatchStatsHandler _statsHandler;
        private readonly NavigationHandler _navigationHandler;
        private readonly UpdateHandler _updateHandler;
        private readonly PredictionHandler _predictionHandler;
        private readonly StandingsHandler _standingsHandler;
        private readonly MatchEventsHandler _matchEventsHandler;
        private readonly TeamsHandler _teamsHandler;

        private static bool _isUpdating = false;
        // ==========================================================
        // ============           КОНСТРУКТОР            ============
        // ==========================================================

        public CommandHandler(
            CalendarHandler calendarHandler,
            ResultsHandler resultsHandler,
            MatchStatsHandler statsHandler,
            NavigationHandler navigationHandler,
            PredictionHandler predictionHandler,
            UpdateHandler updateHandler,
            MatchEventsHandler matchEventsHandler,
            StandingsHandler standingsHandler,
            TeamsHandler teamsHandler)
        {
            _calendarHandler = calendarHandler;
            _resultsHandler = resultsHandler;
            _statsHandler = statsHandler;
            _navigationHandler = navigationHandler;
            _predictionHandler = predictionHandler;
            _updateHandler = updateHandler;
            _matchEventsHandler = matchEventsHandler;
            _standingsHandler = standingsHandler;
            _teamsHandler = teamsHandler;
        }

        // ==========================================================
        // ============       ОСНОВНОЙ ВХОДНОЙ МЕТОД     ============
        // ==========================================================

        /// <summary>
        /// Определяет тип обновления (callback или текст) и направляет его в соответствующий обработчик.
        /// </summary>
        public async Task HandleAsync(Update update)
        {
            Log.Information("[HandleAsync] Начало работы метода.");

            if (update.CallbackQuery != null)
            {
                Log.Information("[HandleAsync] Обнаружен CallbackQuery от UserId={UserId}",
                    update.CallbackQuery.From.Id);

                await HandleCallbackQueryAsync(update.CallbackQuery);
                return;
            }

            if (update.Message != null && !update.Message.From.IsBot)
            {
                Log.Information("[HandleAsync] Обнаружено сообщение от UserId={UserId}",
                    update.Message.From.Id);

                await HandleMessageAsync(update.Message);
                return;
            }

            Log.Warning("[HandleAsync] Получено обновление неизвестного типа.");
        }

        // ==========================================================
        // ============       ОБРАБОТКА CALLBACK-ОВ       ============
        // ==========================================================

        /// <summary>
        /// Обрабатывает нажатия inline-кнопок (callback-запросы Telegram).
        /// </summary>
        private async Task HandleCallbackQueryAsync(CallbackQuery query)
        {
            var callback = query.Data ?? "";
            var chatId = query.Message.Chat.Id;
            var messageId = query.Message.MessageId;

            Log.Information("[HandleCallbackQueryAsync] Начало работы метода. Параметры: chatId={ChatId}, messageId={MessageId}, callback={Callback}", chatId, messageId, callback);

            switch (callback)
            {
                // ---------- Прогнозы ----------
                case var _ when callback.StartsWith("predict_"):
                    Log.Information("[HandleCallbackQueryAsync] Обработчик: HandlePredictions");
                    await _statsHandler.HandlePredictions(chatId, callback);
                    break;

                case var _ when callback.StartsWith("prediction_") || callback.StartsWith("back_to_match_"):
                    Log.Information("[HandleCallbackQueryAsync] Обработчик: HandlePredictionSelected");
                    await _predictionHandler.HandlePredictionSelected(chatId, callback, messageId);
                    break;

                // ---------- События ----------
                case var _ when callback.StartsWith("events_results_"):
                    Log.Information("[HandleCallbackQueryAsync] Обработчик: HandleMatchEvents (results)");
                    await _matchEventsHandler.HandleMatchEvents(chatId, callback, "results");
                    break;

                case var _ when callback.StartsWith("events_calendar_"):
                    Log.Information("[HandleCallbackQueryAsync] Обработчик: HandleMatchEvents (calendar)");
                    await _matchEventsHandler.HandleMatchEvents(chatId, callback, "calendar");
                    break;

                case var _ when callback.StartsWith("events_parse_"):
                    Log.Information("[HandleCallbackQueryAsync] Обработчик: HandleMatchEventsParsing");
                    await _matchEventsHandler.HandleMatchEventsParsing(chatId, callback);
                    break;

                // ---------- Статистика ----------
                case var _ when callback.StartsWith("stats_"):
                    Log.Information("[HandleCallbackQueryAsync] Обработчик: HandleHeadToHead");
                    await _statsHandler.HandleHeadToHead(chatId, callback);
                    break;

                case var _ when callback.StartsWith("history_"):
                    Log.Information("[HandleCallbackQueryAsync] Обработчик: HandleHistory");
                    await _statsHandler.HandleHistory(chatId, callback);
                    break;

                // ---------- Результаты ----------
                case var _ when callback.StartsWith("result_"):
                    Log.Information("[HandleCallbackQueryAsync] Обработчик: HandleResult");
                    await _resultsHandler.HandleResult(chatId, callback);
                    break;

                case var _ when callback.StartsWith("back_to_results_"):
                    Log.Information("[HandleCallbackQueryAsync] Обработчик: HandleBackToResults");
                    await _resultsHandler.HandleBackToResults(chatId, callback);
                    break;

                // ---------- Прогнозы (раздел Результаты) ----------
                case var _ when callback.StartsWith("results_predictions_"):
                    Log.Information("[HandleCallbackQueryAsync] Обработчик: HandleMatchPredictions");
                    await _resultsHandler.HandleMatchPredictions(chatId, messageId, callback);
                    break;

                // ---------- Календарь ----------
                case var _ when callback.StartsWith("match_"):
                    Log.Information("[HandleCallbackQueryAsync] Обработчик: HandleMatchSelected");
                    await _calendarHandler.HandleMatchSelected(chatId, callback);
                    break;

                case "back_to_today":
                    Log.Information("[HandleCallbackQueryAsync] Обработчик: ShowToday");
                    await _calendarHandler.ShowToday(chatId);
                    break;

                case var _ when callback.StartsWith("back_to_calendar_"):
                    Log.Information("[HandleCallbackQueryAsync] Обработчик: HandleBackToCalendar");
                    await _calendarHandler.HandleBackToCalendar(chatId, messageId, callback);
                    break;

                // ---------- Команды ----------
                case var _ when callback.StartsWith("teams_conf_"):
                    await _teamsHandler.ShowTeamsByConference(chatId, callback.Replace("teams_conf_", ""));
                    break;

                case var _ when callback.StartsWith("team_"):
                    var teamCode = callback.Replace("team_", "");
                    await _teamsHandler.HandleTeamSelected(chatId, teamCode);
                    break;

                case "teams_back_to_conf":
                    await _teamsHandler.ShowTeamsMenu(chatId);
                    break;

                case "back_to_main":
                    Log.Information("[HandleCallbackQueryAsync] Обработчик: ShowMainMenu");
                    await _navigationHandler.ShowMainMenu(chatId);
                    break;

                default:
                    Log.Warning("[HandleCallbackQueryAsync] Неизвестный callback: {Callback}", callback);
                    break;
            }
        }

        // ==========================================================
        // ============        ОБРАБОТКА СООБЩЕНИЙ        ============
        // ==========================================================

        /// <summary>
        /// Обрабатывает входящие текстовые сообщения пользователя.
        /// Делегирует выполнение соответствующему разделу.
        /// </summary>
        private async Task HandleMessageAsync(Message message)
        {
            var chatId = message.Chat.Id;
            var text = message.Text ?? "";

            Log.Information("[HandleMessageAsync] Начало работы метода. Входные параметры: chatId={ChatId}, text={Text}", chatId, text);

            // Если обновление уже идёт — игнорируем любые сообщения
            if (_isUpdating)
            {
                Log.Information("[HandleMessageAsync] Обновление данных активно. Сообщение проигнорировано.");
                await _navigationHandler.SendTemporaryNotice(chatId, "⏳ Обновление данных уже выполняется...");
                return;
            }

            Log.Information("[HandleMessageAsync] Сообщение от UserId={UserId}: {Text}", message.From?.Id, text);

            // ---------- Главное меню ----------
            if (text == "/start" || text == "🏠 В главное меню")
            {
                Log.Information("[HandleMessageAsync] Команда главного меню: {Text}", text);
                await _navigationHandler.ShowMainMenu(chatId);
                return;
            }

            // ---------- Основные команды ----------
            if (await HandleMainMenuCommands(chatId, text)) { Log.Information("[HandleMessageAsync] Команда обработана в HandleMainMenuCommands"); return; }

            // ---------- Календарь ----------
            if (await HandleCalendarCommands(chatId, text)) { Log.Information("[HandleMessageAsync] Команда обработана в HandleCalendarCommands"); return; }

            // ---------- Результаты ----------
            if (await HandleResultsCommands(chatId, text)) { Log.Information("[HandleMessageAsync] Команда обработана в HandleResultsCommands"); return; }

            // ---------- Таблицы ----------
            if (await HandleTablesCommands(chatId, text)) { Log.Information("[HandleMessageAsync] Команда обработана в HandleTablesCommands"); return; }

            // ---------- Турнирная таблица ----------
            if (await _standingsHandler.HandleStandingsCommands(chatId, text)) { Log.Information("[HandleMessageAsync] Команда обработана в HandleStandingsCommands"); return; }

            Log.Warning("[HandleMessageAsync] Неизвестная команда: {Text}", text);
        }

        // ==========================================================
        // ============       ПОДБЛОК — Главное меню         ============
        // ==========================================================
        private async Task<bool> HandleMainMenuCommands(long chatId, string text)
        {
            Log.Information("[HandleMainMenuCommands] Входные параметры: chatId={ChatId}, text={Text}", chatId, text);

            switch (text)
            {
                case "📅 Календарь":
                    Log.Information("[HandleMainMenuCommands] Обработчик: ShowCalendarMenu");
                    await _calendarHandler.ShowCalendarMenu(chatId);
                    return true;

                case "⚡ Результаты":
                    Log.Information("[HandleMainMenuCommands] Обработчик: ShowResultsMenu");
                    await _resultsHandler.ShowResultsMenu(chatId);
                    return true;

                case "📊 Таблицы":
                    Log.Information("[HandleMainMenuCommands] Обработчик: ShowTablesMenu");
                    await _standingsHandler.ShowTablesMenu(chatId);
                    return true;

                case "🏒 Команды":
                    Log.Information("[HandleMainMenuCommands] Обработчик: ShowTeamsMenu");
                    await _teamsHandler.ShowTeamsMenu(chatId);
                    return true;

                case "🔄 Обновить данные":
                    Log.Information("[HandleMainMenuCommands] Обработчик: RunGlobalUpdate");
                    _isUpdating = true;
                    try
                    {
                        await _updateHandler.RunGlobalUpdate(chatId);
                    }
                    finally
                    {
                        _isUpdating = false;
                    }
                    return true;

                default:
                    return false;
            }
        }

        // ==========================================================
        // ============       ПОДБЛОК — КАЛЕНДАРЬ         ============
        // ==========================================================
        private async Task<bool> HandleCalendarCommands(long chatId, string text)
        {
            Log.Information("[HandleCalendarCommands] Входные параметры: chatId={ChatId}, text={Text}", chatId, text);

            switch (text)
            {
                case "📅 Сегодня":
                    Log.Information("[HandleCalendarCommands] Обработчик: ShowToday");
                    await _calendarHandler.ShowToday(chatId);
                    return true;

                case "📆 Завтра":
                    Log.Information("[HandleCalendarCommands] Обработчик: ShowTomorrow");
                    await _calendarHandler.ShowTomorrow(chatId);
                    return true;

                case "⬅️ Назад (Календарь)":
                    Log.Information("[HandleCalendarCommands] Обработчик: BackToCalendar");
                    await _calendarHandler.BackToCalendar(chatId);
                    return true;

                default:
                    return false;
            }
        }

        // ==========================================================
        // ============       ПОДБЛОК — РЕЗУЛЬТАТЫ         ============
        // ==========================================================
        private async Task<bool> HandleResultsCommands(long chatId, string text)
        {
            Log.Information("[HandleResultsCommands] Входные параметры: chatId={ChatId}, text={Text}", chatId, text);

            switch (text)
            {
                case "📆 Сегодня":
                    Log.Information("[HandleResultsCommands] Обработчик: ShowTodayResults");
                    await _resultsHandler.ShowTodayResults(chatId);
                    return true;

                case "📅 Вчера":
                    Log.Information("[HandleResultsCommands] Обработчик: ShowYesterdayResults");
                    await _resultsHandler.ShowYesterdayResults(chatId);
                    return true;
                /*
                case "⬅️ Запад (Результаты)":
                    Log.Information("[HandleResultsCommands] Обработчик: ShowWesternTeams");
                    await _resultsHandler.ShowWesternTeams(chatId);
                    return true;

                case "➡️ Восток (Результаты)":
                    Log.Information("[HandleResultsCommands] Обработчик: ShowEasternTeams");
                    await _resultsHandler.ShowEasternTeams(chatId);
                    return true;
                */
                case "⬅️ Назад (Результаты)":
                    Log.Information("[HandleResultsCommands] Обработчик: BackToResults");
                    await _resultsHandler.BackToResults(chatId);
                    return true;

                default:
                    // проверка команд по именам команд
                    var teamNames = new[]
                    {
                        "🦌 Торпедо", "🐉 Шанхай Дрэгонс", "🐃 Динамо Минск", "⚒️ Северсталь",
                        "★ ЦСКА", "🐆 ХК Сочи", "🚂 Локомотив", "⭐ СКА",
                        "🔵 Динамо Москва", "🚗 Лада", "♦️ Спартак",
                        "🚘 Автомобилист", "🦅 Авангард", "🚜 Трактор", "🐆 Барыс",
                        "⛏️ Металлург", "🐅 Амур", "🐯 Ак Барс", "⚓ Адмирал",
                        "🐺 Нефтехимик", "🕌 Салават Юлаев", "❄️ Сибирь"
                    };

                    if (teamNames.Contains(text))
                    {
                        var teamName = text.Substring(text.IndexOf(' ') + 1);

                        Log.Information("[HandleResultsCommands] Обработчик: HandleTeamSelection, team={Team}", teamName);

                        await _resultsHandler.HandleTeamSelection(chatId, $"team_{teamName}");
                        return true;
                    }

                    return false;
            }
        }

        // ==========================================================
        // ============       ПОДБЛОК — ТАБЛИЦЫ           ============
        // ==========================================================
        private async Task<bool> HandleTablesCommands(long chatId, string text)
        {
            Log.Information("[HandleTablesCommands] Входные параметры: chatId={ChatId}, text={Text}", chatId, text);

            switch (text)
            {
                case "🏆 Турнирная таблица":
                    Log.Information("[HandleTablesCommands] Обработчик: ShowConferenceSelection");
                    await _standingsHandler.ShowConferenceSelection(chatId);
                    return true;

                case "📊 Рейтинг прогнозов":
                    Log.Information("[HandleTablesCommands] Обработчик: ShowPredictionsRating");
                    await _standingsHandler.ShowPredictionsRating(chatId);
                    return true;

                case "⬅️ Назад (Главное меню)":
                    Log.Information("[HandleTablesCommands] Обработчик: ShowMainMenu");
                    await _navigationHandler.ShowMainMenu(chatId);
                    return true;

                default:
                    return false;
            }
        }
    }
}
