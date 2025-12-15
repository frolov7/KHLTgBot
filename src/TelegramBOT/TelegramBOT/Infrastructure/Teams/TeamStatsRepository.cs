using Microsoft.EntityFrameworkCore;
using Serilog;
using TelegramBOT.Domain.Entities.Matches;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Domain.Teams.TeamCard;
using TelegramBOT.Infrastructure.Data;

namespace TelegramBOT.Infrastructure.Teams
{
    /// <summary>
    /// Репозиторий для получения данных статистики команды (EF Core).
    /// Содержит только доступ к базе данных — без вычислений и логики.
    /// </summary>
    public class TeamStatsRepository : ITeamStatsRepository
    {
        private readonly AppDbContext _db;

        public TeamStatsRepository(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Возвращает последние N матчей команды (домашние и выездные).
        /// </summary>
        public async Task<List<Match>> GetLastMatchesAsync(string englishTeamName, int limit)
        {
            return await _db.Matches
                .Include(m => m.Events)
                    .ThenInclude(e => e.EventType)
                .Where(m =>
                    (m.HomeTeamName == englishTeamName || m.AwayTeamName == englishTeamName) &&
                    m.Status != "SCHEDULED")
                .OrderByDescending(m => m.MatchDate)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Возвращает все матчи команды (без ограничения).
        /// Полезно для вычисления расширенной статистики.
        /// </summary>
        public async Task<List<Match>> GetAllMatchesAsync(string englishTeamName)
        {
            return await _db.Matches
                .Where(m => m.HomeTeamName == englishTeamName ||
                            m.AwayTeamName == englishTeamName)
                .OrderByDescending(m => m.MatchDate)
                .ToListAsync();
        }
    }
}
