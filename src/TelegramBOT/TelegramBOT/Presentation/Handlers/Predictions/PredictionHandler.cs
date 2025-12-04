using TelegramBOT.Presentation.UI.Menus.Predictions;
using TelegramBOT.Application.Predictions;
using TelegramBOT.Presentation.Handlers.Calendar;
using Serilog;
using TelegramBOT.Application.Telegram;

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
            Log.Information("[HandlePredictionSelected] Начало работы метода. chatId={ChatId}, callback={Callback}, messageId={MessageId}", chatId, callback, messageId);

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
