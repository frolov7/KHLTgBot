using TelegramBOT.Services.Core;
using TelegramBOT.Services.Predictions;
using TelegramBOT.UI.Menus;
using TelegramBOT.Handlers.Calendar;
using TelegramBOT.Models;
using TelegramBOT.UI.Menus.Predictions;

namespace TelegramBOT.Handlers.Predictions
{
    public class PredictionHandler
    {
        private readonly PredictionService _predictionService;
        private readonly MessageService _messageService;
        private readonly CalendarHandler _calendarHandler;

        public PredictionHandler(
            PredictionService predictionService,
            MessageService messageService,
            CalendarHandler calendarHandler)
        {
            _predictionService = predictionService;
            _messageService = messageService;
            _calendarHandler = calendarHandler;
        }

        // ==========================================================
        // ============      БЛОК ОТОБРАЖЕНИЯ МЕНЮ      =============
        // ==========================================================

        /// <summary>
        /// Отображает пользователю меню выбора источника прогнозов (например, Legalbet, Metaratings и др.).
        /// </summary>
        /// <param name="chatId">Идентификатор Telegram-чата, куда будет отправлено меню.</param>
        /// <param name="matchId">Идентификатор матча, для которого отображаются прогнозы.</param>
        public async Task ShowSourcesMenu(long chatId, string matchId)
        {
            var keyboard = new PredictionsMenuBuilder().Build(matchId);
            await _messageService.SendKeyboardAsync(chatId, "Выберите источник прогноза:", keyboard);
        }

        // ==========================================================
        // ============      БЛОК ОБРАБОТКИ ВЫБОРА      =============
        // ==========================================================

        /// <summary>
        /// Обрабатывает callback-запрос пользователя при выборе источника прогноза или возвращении к матчу.
        /// </summary>
        /// <param name="chatId">Идентификатор Telegram-чата.</param>
        /// <param name="callback">Callback-строка, содержащая идентификатор источника и матча.</param>
        /// <param name="messageId">
        /// Необязательный параметр — идентификатор сообщения, которое следует удалить
        /// (например, при возврате к меню матча).
        /// </param>
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

            var parts = callback.Split('_');
            if (parts.Length < 3)
            {
                await _messageService.SendTextAsync(chatId, "Неверный формат callback-запроса.");
                return;
            }

            var source = parts[1];
            var matchIdParsed = parts[2];

            if (source.Equals("общий прогноз", StringComparison.OrdinalIgnoreCase))
            {
                var predictions = await _predictionService.GetPredictionsForMatchAsync(matchIdParsed);
                var text = _predictionService.BuildSummaryMessage(predictions);
                await _messageService.SendTextAsync(chatId, text);
            }
            else
            {
                var prediction = await _predictionService.GetPredictionAsync(matchIdParsed, source);
                if (prediction == null)
                {
                    await _messageService.SendTextAsync(chatId, $"❌ Прогноз от {source} не найден.");
                    return;
                }

                var text = _predictionService.BuildPredictionMessage(prediction);
                await _messageService.SendTextAsync(chatId, text);
            }

            var menu = new PredictionsMenuBuilder().Build(matchIdParsed);
            await _messageService.SendKeyboardAsync(chatId, "Выберите другой источник:", menu);
        }
    }
}
