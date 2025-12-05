using Microsoft.EntityFrameworkCore;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Domain.Models;
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

        public async Task<(int scoredFirst, int concededFirst)> GetFirstGoalStatsAsync(string teamName, List<Match> matches)
        {
            int scored = 0;
            int conceded = 0;

            foreach (var match in matches)
            {
                var firstGoal = await _db.MatchEvents
                    .Include(e => e.Team) 
                    .Include(e => e.EventType)
                    .Where(e => e.MatchId == match.MatchId && e.EventType.Name == "Goal")
                    .OrderBy(e => e.Period)
                    .ThenBy(e => e.Time)
                    .FirstOrDefaultAsync();

                if (firstGoal == null)
                    continue;

                string scoringTeam = firstGoal.Team?.Name;

                bool isHomeTeam = match.HomeTeamName == teamName;

                if (scoringTeam == match.HomeTeamName)
                {
                    if (isHomeTeam) scored++;
                    else conceded++;
                }
                else
                {
                    if (isHomeTeam) conceded++;
                    else scored++;
                }
            }

            return (scored, conceded);
        }
    }
}
