using Serilog;
using Telegram.Bot.Types;
using TelegramBOT.Application.MatchEvents;
using TelegramBOT.Application.Results;
using TelegramBOT.Application.Telegram;
using TelegramBOT.Infrastructure.Scripts;
using TelegramBOT.Presentation.UI.Menus.Calendar;
using TelegramBOT.Presentation.UI.Menus.Results;

namespace TelegramBOT.Presentation.Handlers.MatchEvents
{
    /// <summary>
    /// Обработчик пользовательских команд, связанных с событиями матчей.
    /// Может вызываться из разных частей приложения (календарь, результаты и т.д.).
    /// </summary>
    public class MatchEventsHandler
    {
        private readonly MatchEventsService _matchEventsService;
        private readonly ScriptService _scriptService;
        private readonly MessageService _messageService;

        public MatchEventsHandler(
            MatchEventsService matchEventsService,
            ScriptService scriptService,
            MessageService messageService
        )
        {
            _matchEventsService = matchEventsService;
            _scriptService = scriptService;
            _messageService = messageService;
        }

        /// <summary>
        /// Запускает парсинг событий конкретного матча (по кнопке "📋 События").
        /// </summary>
        public async Task HandleMatchEvents(long chatId, string callback, string source)
        {
            var matchId = callback
                .Replace("events_results_", "")
                .Replace("events_calendar_", "")
                .Replace("events_", "");

            var loading = await _messageService
                .RemoveKeyboardAsync(chatId, "⏳ Загружаем события матча...");

            await _matchEventsService.ProcessMatchEventsAsync(
                chatId,
                matchId,
                source,
                forceParse: source == "calendar" // если календарь → парсим
            );

            try { await _messageService.DeleteMessageAsync(chatId, loading.MessageId); }
            catch { }
        }

        /// <summary>
        /// Обрабатывает запрос на ручное обновление событий матча.
        /// Используется, когда требуется принудительно перепарсить данные 
        /// и отправить пользователю обновлённую визуализацию.
        /// </summary>
        public async Task HandleMatchEventsParsing(long chatId, string callback)
        {
            var matchId = callback.Replace("events_parse_", "");

            var loading = await _messageService
                .SendTextAsync(chatId, "⏳ Обновляем данные...");

            await _matchEventsService.ProcessMatchEventsAsync(
                chatId,
                matchId,
                "calendar",
                forceParse: true
            );

            try { await _messageService.DeleteMessageAsync(chatId, loading.MessageId); }
            catch { }
        }
    }
}
