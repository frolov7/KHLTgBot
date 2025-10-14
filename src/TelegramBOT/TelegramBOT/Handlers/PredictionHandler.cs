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

        public PredictionHandler(PredictionService predictionService, MessageService messageService)
        {
            _predictionService = predictionService;
            _messageService = messageService;
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
        public async Task HandlePredictionSelected(long chatId, string callback)
        {
            var parts = callback.Split('_');
            if (parts.Length < 3)
            {
                await _messageService.SendTextAsync(chatId, "Неверный формат callback-запроса.");
                return;
            }

            var source = parts[1];
            var matchId = parts[2];

            if (source.Equals("общий прогноз", StringComparison.OrdinalIgnoreCase))
                await _predictionService.ShowSummaryAsync(chatId, matchId);
            else
                await _predictionService.ShowPredictionAsync(chatId, source, matchId);
        }
    }
}
