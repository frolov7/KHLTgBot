using TelegramBOT.Services;
using TelegramBOT.UI.Menus;

namespace TelegramBOT.Handlers
{
    /// <summary>
    /// Обработчик прогнозов: меню источников и вывод текста прогноза.
    /// </summary>
    public class PredictionHandler
    {
        private readonly PredictionService _predictionService;
        private readonly MessageService _messageService;
        private readonly MatchService _matchService;
        private readonly CalendarHandler _calendarHandler;

        public PredictionHandler(
            PredictionService predictionService,
            MessageService messageService,
            MatchService matchService,
            CalendarHandler calendarHandler)
        {
            _predictionService = predictionService;
            _messageService = messageService;
            _matchService = matchService;
            _calendarHandler = calendarHandler;
        }

        /// <summary>
        /// Показать меню выбора источника прогноза.
        /// </summary>
        public async Task ShowSourcesMenu(long chatId, string matchId)
        {
            var keyboard = new PredictionsMenuBuilder().Build(matchId);
            await _messageService.SendKeyboardAsync(chatId, "Выберите источник прогноза:", keyboard);
        }

        /// <summary>
        /// Обрабатывает выбор источника прогноза пользователем.
        /// </summary>
        public async Task HandlePredictionSelected(long chatId, string callback, int? messageId = null)
        {
            if (callback.StartsWith("back_to_match_"))
            {
                var matchId = callback.Replace("back_to_match_", "");
                if (messageId.HasValue)
                    await _messageService.DeleteMessageAsync(chatId, messageId.Value); 

                await _calendarHandler.ShowMatchMenu(chatId, matchId);
                return;
            }

            // Обычная логика показа прогнозов
            var parts = callback.Split('_');
            if (parts.Length < 3)
            {
                await _messageService.SendTextAsync(chatId, "Неверный формат callback-запроса.");
                return;
            }

            var source = parts[1];
            var matchId2 = parts[2];

            var match = await _matchService.GetMatchByIdAsync(matchId2);
            if (match == null)
            {
                await _messageService.SendTextAsync(chatId, "Матч не найден.");
                return;
            }

            if (source.Equals("общий прогноз", StringComparison.OrdinalIgnoreCase))
                await _predictionService.ShowSummaryAsync(chatId, matchId2);
            else
                await _predictionService.ShowPredictionAsync(chatId, source, matchId2);

            var menu = new PredictionsMenuBuilder().Build(matchId2);
            await _messageService.SendPredictionsMenuAsync(chatId, match, menu);
        }
    }
}
