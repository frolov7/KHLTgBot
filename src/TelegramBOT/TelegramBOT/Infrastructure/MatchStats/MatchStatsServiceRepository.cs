using Microsoft.EntityFrameworkCore;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Domain.Models;
using TelegramBOT.Infrastructure.Data;

namespace TelegramBOT.Infrastructure.MatchStats
{
    /// <summary>
    /// Репозиторий для работы с данными статистики матчей:
    /// очные встречи, история игр и прогнозы.
    /// </summary>
    public class MatchStatsServiceRepository : IMatchStatsServiceRepository
    {
        private readonly AppDbContext _db;

        public MatchStatsServiceRepository(AppDbContext db)
        {
            _db = db;
        }

        // ==========================================================
        // ============      ОЧНЫЕ ВСТРЕЧИ КОМАНД       =============
        // ==========================================================

        /// <summary>
        /// Возвращает список уже сыгранных очных встреч между двумя командами.
        /// Только завершённые матчи (с заполненным счётом).
        /// </summary>
        /// <param name="home">Название домашней команды.</param>
        /// <param name="away">Название гостевой команды.</param>
        /// <param name="limit">Максимальное количество матчей (по умолчанию — 10).</param>
        public async Task<IEnumerable<Match>> GetHeadToHeadMatchesAsync(string home, string away)
        {
            return await _db.Matches
                .Where(m =>
                    (
                        m.HomeTeamName == home && m.AwayTeamName == away ||
                        m.HomeTeamName == away && m.AwayTeamName == home
                    )
                    && m.MatchDate < DateTime.Today
                    && m.HomeScore != null
                    && m.AwayScore != null)
                .OrderBy(m => m.MatchDate)
                .ToListAsync();
        }

        // ==========================================================
        // ============      ПРОГНОЗЫ НА МАТЧИ           ============
        // ==========================================================

        /// <summary>
        /// Возвращает список прогнозов для указанного матча.
        /// </summary>
        public async Task<IEnumerable<Prediction>> GetPredictionsByMatchIdAsync(string matchId)
        {
            return await _db.Predictions
                .Where(p => p.MatchId == matchId)
                .ToListAsync();
        }

        // ==========================================================
        // ============      ПОЛУЧЕНИЕ МАТЧА             ============
        // ==========================================================

        /// <summary>
        /// Возвращает матч по идентификатору.
        /// </summary>
        public async Task<Match?> GetMatchByIdAsync(string matchId)
        {
            return await _db.Matches.FirstOrDefaultAsync(m => m.MatchId == matchId);
        }
    }
}
