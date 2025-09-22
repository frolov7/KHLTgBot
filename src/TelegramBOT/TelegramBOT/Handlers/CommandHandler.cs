using Serilog;
using Telegram.Bot.Types;
using TelegramBOT.Services;

namespace TelegramBOT.Handlers
{
    public class CommandHandler
    {
        private readonly CalendarHandler _calendarHandler;
        private readonly ResultsHandler _resultsHandler;
        private readonly StatsHandler _statsHandler;
        private readonly NavigationHandler _navigationHandler;
        private readonly TeamsHandler _teamsHandler;

        public CommandHandler(
            CalendarHandler calendarHandler,
            ResultsHandler resultsHandler,
            StatsHandler statsHandler,
            NavigationHandler navigationHandler,
            TeamsHandler teamsHandler
        )
        {
            _calendarHandler = calendarHandler;
            _resultsHandler = resultsHandler;
            _statsHandler = statsHandler;
            _navigationHandler = navigationHandler;
            _teamsHandler = teamsHandler;
        }

        public async Task HandleAsync(Update update)
        {
            if (update.CallbackQuery != null)
            {
                await HandleCallbackQueryAsync(update.CallbackQuery);
                return;
            }

            if (update.Message != null && !update.Message.From.IsBot)
            {
                await HandleMessageAsync(update.Message);
            }
        }

        private async Task HandleCallbackQueryAsync(CallbackQuery query)
        {
            var callback = query.Data ?? "";
            var chatId = query.Message.Chat.Id;

            if (callback.StartsWith("match_"))
                await _calendarHandler.HandleMatchSelected(chatId, callback);
            else if (callback.StartsWith("stats_"))
                await _statsHandler.HandleStats(chatId, callback);
            else if (callback.StartsWith("history_"))
                await _statsHandler.HandleHistory(chatId, callback);
            else if (callback.StartsWith("result_"))
                await _resultsHandler.HandleResult(chatId, callback);
            else if (callback == "back_to_today")
                await _calendarHandler.ShowToday(chatId);
        }

        private async Task HandleMessageAsync(Message message)
        {
            var chatId = message.Chat.Id;
            var text = message.Text ?? "";

            Log.Information("Пользователь ({@User}) написал: {Text}",
                new
                {
                    Name = $"{message.From?.FirstName} {message.From?.LastName}".Trim(),
                    Username = message.From?.Username ?? "null",
                    UserId = message.From?.Id
                },
                text
            );

            switch (text)
            {
                case "/start":
                    await _navigationHandler.ShowMainMenu(chatId);
                    break;

                case "📅 Календарь":
                    await _calendarHandler.ShowCalendar(chatId);
                    break;

                case "Сегодня":
                    await _calendarHandler.ShowToday(chatId);
                    break;

                case "Завтра":
                    await _calendarHandler.ShowTomorrow(chatId);
                    break;

                case "Следующие N дней":
                    await _calendarHandler.ShowNextDaysMenu(chatId);
                    break;

                case "2 дня":
                    await _calendarHandler.ShowNextDays(chatId, 2);
                    break;

                case "3 дня":
                    await _calendarHandler.ShowNextDays(chatId, 3);
                    break;

                case "4 дня":
                    await _calendarHandler.ShowNextDays(chatId, 4);
                    break;

                case "5 дней":
                    await _calendarHandler.ShowNextDays(chatId, 5);
                    break;

                case "⬅️ Назад (Календарь)":
                    await _calendarHandler.BackToCalendar(chatId);
                    break;

                case "📊 Статистика":
                    await _statsHandler.ShowStatsMenu(chatId);
                    break;

                case "⚡ Результаты":
                    await _resultsHandler.ShowResultsMenu(chatId);
                    break;

                case "🔄 Обновить данные":
                    await _resultsHandler.UpdateResults(chatId);
                    break;

                case "📅 Сегодня":
                    await _resultsHandler.ShowTodayResults(chatId);
                    break;

                case "📅 Вчера":
                    await _resultsHandler.ShowYesterdayResults(chatId);
                    break;

                case "⬅️ Запад (Результаты)":
                    await _resultsHandler.ShowWesternTeams(chatId);
                    break;

                case "➡️ Восток (Результаты)":
                    await _resultsHandler.ShowEasternTeams(chatId);
                    break;

                case "⬅️ Назад (Результаты)":
                    await _resultsHandler.BackToResults(chatId);
                    break;

                case "🏠 В главное меню":
                    await _navigationHandler.ShowMainMenu(chatId);
                    break;

                default:
                    await _teamsHandler.HandleTeamCommand(chatId, text);
                    break;
            }

            Log.Information("Метод {Method} завершил работу.", nameof(HandleAsync));
        }
    }
}
