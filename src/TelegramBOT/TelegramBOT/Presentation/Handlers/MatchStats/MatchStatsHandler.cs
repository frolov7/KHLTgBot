using TelegramBOT.Application.MatchStats;

namespace TelegramBOT.Presentation.Handlers.MatchStats
{
    /// <summary>
    /// Обработчик пользовательских команд, связанных со статистикой матчей.
    /// Делегирует выполнение бизнес-логики в StatsService.
    /// </summary>
    public class MatchStatsHandler
    {
        private readonly MatchStatsService _statsService;

        public MatchStatsHandler(MatchStatsService statsService)
        {
            _statsService = statsService;
        }

        // ==========================================================
        // ============      ОЧНЫЕ ВСТРЕЧИ               ============
        // ==========================================================

        /// <summary>
        /// Обрабатывает callback для отображения очных встреч.
        /// </summary>
        public async Task HandleHeadToHead(long chatId, string callback)
        {
            var matchId = callback.Replace("stats_", "");
            await _statsService.SendHeadToHeadAsync(chatId, matchId);
        }

        // ==========================================================
        // ============      ПРОШЛЫЕ ИГРЫ                ============
        // ==========================================================

        /// <summary>
        /// Обрабатывает callback для отображения последних матчей команд.
        /// </summary>
        public async Task HandleHistory(long chatId, string callback)
        {
            var matchId = callback.Replace("history_", "");
            await _statsService.SendTeamsHistoryAsync(chatId, matchId);
        }

        // ==========================================================
        // ============      ПРОГНОЗЫ                   ============
        // ==========================================================

        /// <summary>
        /// Обрабатывает callback для отображения прогнозов на матч.
        /// </summary>
        public async Task HandlePredictions(long chatId, string callback)
        {
            var matchId = callback.Replace("predict_", "");
            await _statsService.SendPredictionsAsync(chatId, matchId);
        }
    }
}
