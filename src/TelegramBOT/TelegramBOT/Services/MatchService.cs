using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using TelegramBOT.Data;
using TelegramBOT.Models;

namespace TelegramBOT.Services
{
    /// <summary>
    /// Сервис для получения матчей из базы
    /// </summary>
    public class MatchService
    {
        private readonly AppDbContext _db;

        public MatchService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Получить матчи на сегодня
        /// </summary>
        public async Task<List<Match>> GetMatchesTodayAsync()
        {
            var today = DateTime.Today;
            return await _db.Matches
                .Where(m => m.MatchDate.Date == today)
                .OrderBy(m => m.MatchDate)
                .ToListAsync();
        }
        /// <summary>
        /// Получить матчи на завтра
        /// </summary>
        public async Task<List<Match>> GetMatchesTomorrowAsync()
        {
            var tomorrow = DateTime.Today.AddDays(1);
            return await _db.Matches
                .Where(m => m.MatchDate.Date == tomorrow && m.Status == "SCHEDULED")
                .OrderBy(m => m.MatchDate)
                .ToListAsync();
        }

        /// <summary>
        /// Получить матчи на N дней вперед
        /// </summary>
        public async Task<List<Match>> GetMatchesNextDaysAsync(int days)
        {
            var fromDate = DateTime.Today.AddDays(1);
            var toDate = DateTime.Today.AddDays(days);

            return await _db.Matches
                .Where(m => m.MatchDate.Date >= fromDate
                            && m.MatchDate.Date <= toDate
                            && m.Status == "SCHEDULED")
                .OrderBy(m => m.MatchDate)
                .ToListAsync();
        }
        // ================================
        // Методы для результатов
        // ================================

        /// <summary>
        /// Результаты матчей за сегодня
        /// </summary>
        public async Task<List<Match>> GetResultsTodayAsync()
        {
            var today = DateTime.Today;

            return await _db.Matches
                .Where(m => m.MatchDate.Date == today && m.Status != "SCHEDULED")
                .OrderBy(m => m.MatchDate)
                .ToListAsync();
        }

        /// <summary>
        /// Результаты матчей за вчера
        /// </summary>
        public async Task<List<Match>> GetResultsYesterdayAsync()
        {
            var yesterday = DateTime.Today.AddDays(-1);

            return await _db.Matches
                .Where(m => m.MatchDate.Date == yesterday && m.Status != "SCHEDULED")
                .OrderBy(m => m.MatchDate)
                .ToListAsync();
        }

        /// <summary>
        /// Результаты последних 10 матчей команды
        /// </summary>
        public async Task<List<Match>> GetAllResultsByTeamAsync(string teamName)
        {
            return await _db.Matches
                .Where(m => (m.HomeTeamName.Contains(teamName) || m.AwayTeamName.Contains(teamName))
                            && m.Status != "SCHEDULED")
                .OrderBy(m => m.MatchDate)
                .ToListAsync();
        }

        public async Task<Match> GetMatchByIdAsync(string matchId)
        {
            return await _db.Matches
                .FirstOrDefaultAsync(m => m.MatchId == matchId);
        }

        public async Task<string> GetMatchStatsAsync(string matchId)
        {
            return await Task.FromResult("Заглушка");
        }

        public async Task<string> GetMatchHistoryAsync(string matchId)
        {
            return await Task.FromResult("Заглушка");
        }

        public async Task<string> GetMatchResultAsync(string matchId)
        {
            return await Task.FromResult("Заглушка");
        }

    }
}