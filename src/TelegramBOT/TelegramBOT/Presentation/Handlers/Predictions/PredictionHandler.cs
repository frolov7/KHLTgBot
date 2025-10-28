using TelegramBOT.Presentation.UI.Menus.Predictions;
using TelegramBOT.Application.Predictions;
using TelegramBOT.Infrastructure.Telegram;
using TelegramBOT.Presentation.Handlers.Calendar;

namespace TelegramBOT.Presentation.Handlers.Predictions
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
            await _predictionService.HandlePredictionSelectedAsync(
                chatId,
                callback,
                _messageService,
                _calendarHandler,
                messageId
            );
        }
    }
}
