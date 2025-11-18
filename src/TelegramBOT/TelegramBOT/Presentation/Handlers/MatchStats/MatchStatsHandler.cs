using Serilog;
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
            Log.Information("[HandleHeadToHead] Начало работы метода. chatId={ChatId}, callback={Callback}", chatId, callback);

            var matchId = callback.Replace("stats_", "");
            Log.Information("[HandleHeadToHead] Извлечён matchId={MatchId}", matchId);

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
            Log.Information("[HandleHistory] Начало работы метода. chatId={ChatId}, callback={Callback}", chatId, callback);

            var matchId = callback.Replace("history_", "");
            Log.Information("[HandleHistory] Извлечён matchId={MatchId}", matchId);

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
            Log.Information("[HandlePredictions] Начало работы метода. chatId={ChatId}, callback={Callback}", chatId, callback);

            var matchId = callback.Replace("predict_", "");
            Log.Information("[HandlePredictions] Извлечён matchId={MatchId}", matchId);

            await _statsService.SendPredictionsAsync(chatId, matchId);
        }
    }
}
