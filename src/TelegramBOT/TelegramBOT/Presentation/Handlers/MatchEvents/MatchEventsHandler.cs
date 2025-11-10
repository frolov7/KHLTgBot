using TelegramBOT.Application.MatchEvents;
using TelegramBOT.Infrastructure.Scripts;
using TelegramBOT.Infrastructure.Telegram;

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
        /// Обрабатывает callback-запрос для отображения событий матча.
        /// Например: "events_12345"
        /// </summary>
        public async Task HandleMatchEvents(long chatId, string callback)
        {
            var matchId = callback.Replace("events_", "");
            await _matchEventsService.SendMatchEventsAsync(chatId, matchId, "calendar");
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

            try
            {
                // Выводим события матча
                await _matchEventsService.SendMatchEventsAsync(chatId, matchId, source);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Ошибка при отображении событий матча {MatchId}", matchId);
                await _matchEventsService.SendMatchEventsAsync(chatId, matchId, source);
            }
        }

        public async Task HandleMatchEventsParsing(long chatId, string callback)
        {
            var matchId = callback.Replace("events_parse_", "");

            try
            {
                // 1️ Запускаем парсинг через Node.js
                await _scriptService.RunSingleMatchEventsAsync(matchId);

                // 2️ После обновления данных — показываем события (по умолчанию вернёмся в календарь)
                await _matchEventsService.SendMatchEventsAsync(chatId, matchId, "calendar");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Ошибка при парсинге событий матча {MatchId}", matchId);
                await _messageService.SendTextAsync(chatId, "Ошибка при обновлении событий.");
            }
        }
    }
}
