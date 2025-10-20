using Microsoft.EntityFrameworkCore;
using TelegramBOT.Data;
using TelegramBOT.Models;

namespace TelegramBOT.Data.Repositories
{
    /// <summary>
    /// Репозиторий для доступа к матчам (работа с БД).
    /// Содержит только CRUD и запросы без бизнес-логики.
    /// </summary>
    public class CalendarRepository : ICalendarRepository
    {
        private readonly AppDbContext _db;

        public CalendarRepository(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Возвращает список матчей в заданном диапазоне дат.
        /// </summary>
        public async Task<List<Match>> GetMatchesByDateRangeAsync(DateTime from, DateTime to)
        {
            return await _db.Matches
                .Where(m => m.MatchDate.Date >= from.Date && m.MatchDate.Date <= to.Date)
                .OrderBy(m => m.MatchDate)
                .ToListAsync();
        }

        /// <summary>
        /// Возвращает матч по его ID.
        /// </summary>
        public async Task<Match?> GetMatchAsync(string matchId)
        {
            return await _db.Matches.FirstOrDefaultAsync(m => m.MatchId == matchId);
        }
    }
}
