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
            await _matchEventsService.SendMatchEventsAsync(chatId, matchId);
        }

        /// <summary>
        /// Запускает парсинг событий конкретного матча (по кнопке "📋 События").
        /// </summary>
        public async Task HandleMatchEventsParsing(long chatId, string callback)
        {
            var matchId = callback.Replace("events_parse_", "");

            try
            {
                // Запускаем Node.js-скрипт парсинга конкретного матча
                await _scriptService.RunSingleMatchEventsAsync(matchId);

                // После завершения — сразу выводим обновлённые события
                await _matchEventsService.SendMatchEventsAsync(chatId, matchId);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Ошибка при парсинге событий матча {MatchId}", matchId);
            }
        }
    }
}
