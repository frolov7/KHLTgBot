using Serilog;
using Telegram.Bot.Types;
using TelegramBOT.Presentation.Handlers.Calendar;
using TelegramBOT.Presentation.Handlers.MatchStats;
using TelegramBOT.Presentation.Handlers.Navigation;
using TelegramBOT.Presentation.Handlers.Predictions;
using TelegramBOT.Presentation.Handlers.Results;
using TelegramBOT.Presentation.Handlers.System;

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
            StandingsHandler standingsHandler)
        {
            _calendarHandler = calendarHandler;
            _resultsHandler = resultsHandler;
            _statsHandler = statsHandler;
            _navigationHandler = navigationHandler;
            _predictionHandler = predictionHandler;
            _updateHandler = updateHandler;
            _standingsHandler = standingsHandler;
        }

        // ==========================================================
        // ============       ОСНОВНОЙ ВХОДНОЙ МЕТОД     ============
        // ==========================================================

        /// <summary>
        /// Определяет тип обновления (callback или текст) и направляет его в соответствующий обработчик.
        /// </summary>
        public async Task HandleAsync(Update update)
        {
            if (update.CallbackQuery != null)
                await HandleCallbackQueryAsync(update.CallbackQuery);

            else if (update.Message != null && !update.Message.From.IsBot)
                await HandleMessageAsync(update.Message);
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

            Log.Information("Получен callback: {Callback}", callback);

            switch (callback)
            {
                // ---------- Прогнозы ----------
                case var _ when callback.StartsWith("predict_"):
                    await _statsHandler.HandlePredictions(chatId, callback);
                    break;

                case var _ when callback.StartsWith("prediction_") || callback.StartsWith("back_to_match_"):
                    await _predictionHandler.HandlePredictionSelected(chatId, callback, messageId);
                    break;

                // ---------- Статистика ----------
                case var _ when callback.StartsWith("stats_"):
                    await _statsHandler.HandleHeadToHead(chatId, callback);
                    break;

                case var _ when callback.StartsWith("history_"):
                    await _statsHandler.HandleHistory(chatId, callback);
                    break;

                // ---------- Результаты ----------
                case var _ when callback.StartsWith("result_"):
                    await _resultsHandler.HandleResult(chatId, callback);
                    break;

                // ---------- Календарь ----------
                case var _ when callback.StartsWith("match_"):
                    await _calendarHandler.HandleMatchSelected(chatId, callback);
                    break;

                case "back_to_today":
                    await _calendarHandler.ShowToday(chatId);
                    break;

                case var _ when callback.StartsWith("back_to_calendar_"):
                    await _calendarHandler.HandleBackToCalendar(chatId, messageId, callback);
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

            // Если обновление уже идёт — игнорируем любые сообщения
            if (_isUpdating)
            {
                await _navigationHandler.SendTemporaryNotice(chatId, "⏳ Обновление данных уже выполняется...");
                return;
            }

            Log.Information("Сообщение от пользователя ({UserId}): {Text}", message.From?.Id, text);

            // ---------- Главное меню ----------
            if (text == "/start" || text == "🏠 В главное меню")
            {
                await _navigationHandler.ShowMainMenu(chatId);
                return;
            }

            // ---------- Основные команды ----------
            if (await HandleMainMenuCommands(chatId, text)) return;

            // ---------- Календарь ----------
            if (await HandleCalendarCommands(chatId, text)) return;

            // ---------- Результаты ----------
            if (await HandleResultsCommands(chatId, text)) return;

            // ---------- Таблицы ----------
            if (await HandleTablesCommands(chatId, text)) return;

            // ---------- Турнирная таблица ----------
            if (await _standingsHandler.HandleStandingsCommands(chatId, text)) return;

            Log.Information("Обработка завершена для: {Text}", text);
        }

        // ==========================================================
        // ============       ПОДБЛОК — Главное меню         ============
        // ==========================================================
        private async Task<bool> HandleMainMenuCommands(long chatId, string text)
        {
            switch (text)
            {
                case "📅 Календарь":
                    await _calendarHandler.ShowCalendarMenu(chatId);
                    return true;

                case "⚡ Результаты":
                    await _resultsHandler.ShowResultsMenu(chatId);
                    return true;

                case "📊 Таблицы":
                    await _standingsHandler.ShowTablesMenu(chatId);
                    return true;

                case "🔄 Обновить данные":
                    _isUpdating = true;
                    try
                    {
                        //await _navigationHandler.HideKeyboardAsync(chatId);
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
            switch (text)
            {
                case "📅 Сегодня":
                    await _calendarHandler.ShowToday(chatId);
                    return true;
                case "📆 Завтра":
                    await _calendarHandler.ShowTomorrow(chatId);
                    return true;
                case "Следующие N дней":
                    await _calendarHandler.ShowNextDaysMenu(chatId);
                    return true;
                case "2 дня":
                    await _calendarHandler.ShowNextDays(chatId, 2);
                    return true;
                case "3 дня":
                    await _calendarHandler.ShowNextDays(chatId, 3);
                    return true;
                case "4 дня":
                    await _calendarHandler.ShowNextDays(chatId, 4);
                    return true;
                case "5 дней":
                    await _calendarHandler.ShowNextDays(chatId, 5);
                    return true;
                 
                case "⬅️ Назад (Календарь)":
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
            switch (text)
            {
                case "📆 Сегодня":
                    await _resultsHandler.ShowTodayResults(chatId);
                    return true;

                case "📅 Вчера":
                    await _resultsHandler.ShowYesterdayResults(chatId);
                    return true;

                case "⬅️ Запад (Результаты)":
                    await _resultsHandler.ShowWesternTeams(chatId);
                    return true;

                case "➡️ Восток (Результаты)":
                    await _resultsHandler.ShowEasternTeams(chatId);
                    return true;

                case "⬅️ Назад (Результаты)":
                    await _resultsHandler.BackToResults(chatId);
                    return true;

                default:
                    // Если пользователь нажал на команду из меню
                    var teamNames = new[]
                    {
                        "🦌 Торпедо", "🐉 Шанхай Дрэгонс", "🐃 Динамо Минск", "⚒️ Северсталь",
                        "★ ЦСКА", "🐆 ХК Сочи", "🚂 Локомотив", "⭐ СКА",
                        "🔵 Динамо Москва", "🚗 Лада", "♦️ Спартак",
                        "🚘 Автомобилист", "🦅 Авангард", "🚜 Трактор", "🐆 Барыс",
                        "⛏️ Металлург", "🐅 Амур", "🐯 Ак Барс", "⚓ Адмирал",
                        "🐺 Нефтехимик", "🕌 Салават Юлаев", "❄️ Сибирь"
                    };

                    // Проверяем, есть ли совпадение
                    if (teamNames.Contains(text))
                    {
                        // Убираем эмодзи (чтобы получить чистое имя для поиска в БД)
                        var teamName = text;
                        var firstSpaceIndex = teamName.IndexOf(' ');
                        if (firstSpaceIndex > 0)
                            teamName = teamName.Substring(firstSpaceIndex + 1);

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
            switch (text)
            {
                case "🏆 Турнирная таблица":
                    await _standingsHandler.ShowConferenceSelection(chatId);
                    return true;

                case "⬅️ Назад (Главное меню)":
                    await _navigationHandler.ShowMainMenu(chatId);
                    return true;

                default:
                    return false;
            }
        }

        // ==========================================================
        // ============       ПОДБЛОК — ТУРНИРНАЯ ТАБЛИЦА  ============
        // ==========================================================
        private async Task<bool> HandleStandingsCommands(long chatId, string text)
        {
            switch (text)
            {
                case "🔸 Западная конференция":
                    await _standingsHandler.ShowStandings(chatId, "west");
                    return true;

                case "🔹 Восточная конференция":
                    await _standingsHandler.ShowStandings(chatId, "east");
                    return true;

                case "⬅️ Назад (Таблица)":
                    await _navigationHandler.ShowMainMenu(chatId);
                    return true;

                default:
                    return false;
            }
        }
    }
}
