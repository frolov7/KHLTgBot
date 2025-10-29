using Microsoft.EntityFrameworkCore;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Domain.Models;
using TelegramBOT.Infrastructure.Data;

namespace TelegramBOT.Infrastructure.Standings
{
    /// <summary>
    /// Репозиторий для получения всех матчей (используется для турнирной таблицы).
    /// </summary>
    public class StandingsRepository : IStandingsRepository
    {
        private readonly AppDbContext _db;

        public StandingsRepository(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Возвращает все завершённые матчи (с известными результатами).
        /// </summary>
        public async Task<IEnumerable<Match>> GetAllMatchesAsync()
        {
            return await _db.Matches
                .Where(m => m.HomeScore != null && m.AwayScore != null)
                .OrderBy(m => m.MatchDate)
                .ToListAsync();
        }
    }
}
