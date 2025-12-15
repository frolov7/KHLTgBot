using Microsoft.EntityFrameworkCore;
using TelegramBOT.Domain.Entities.Matches;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Infrastructure.Data;

namespace TelegramBOT.Infrastructure.Teams
{
    /// <summary>
    /// Репозиторий для доступа к данным о матчах выбранной команды.
    /// </summary>
    public class TeamsRepository : ITeamsRepository
    {
        private readonly AppDbContext _db;

        public TeamsRepository(AppDbContext db)
        {
            _db = db;
        }

        // ==========================================================
        // ============          МАТЧИ КОМАНДЫ            ============
        // ==========================================================

        /// <summary>
        /// Возвращает последние сыгранные матчи команды.
        /// </summary>
        /// <param name="teamName">Название команды в БД (EN).</param>
        /// <param name="limit">Количество последних матчей.</param>
        public async Task<List<Match>> GetRecentMatchesByTeamAsync(string teamName)
        {
            return await _db.Matches
                .Where(m => (m.HomeTeamName == teamName || m.AwayTeamName == teamName)
                            && m.Status != "SCHEDULED")
                .OrderBy(m => m.MatchDate)
                .Take(7)
                .ToListAsync();
        }
    }
}
