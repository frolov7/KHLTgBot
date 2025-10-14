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
        /// Показать конкретный прогноз по источнику.
        /// </summary>
        public async Task ShowPrediction(long chatId, string callback)
        {
            await _predictionService.ShowPredictionAsync(chatId, callback);
        }
    }
}