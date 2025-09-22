using Microsoft.EntityFrameworkCore;
using TelegramBOT.Data;
using TelegramBOT.Models;

namespace TelegramBOT.Services
{
    /// <summary>
    /// Сервис для работы с матчами: календарь, результаты и данные по конкретным играм.
    /// </summary>
    public class MatchService
    {
        private readonly AppDbContext _db;

        public MatchService(AppDbContext db)
        {
            _db = db;
        }

        // ================================
        // Методы для календаря матчей
        // ================================

        /// <summary>
        /// Получить список матчей на сегодня.
        /// </summary>
        /// <returns>Список матчей за текущий день.</returns>
        public async Task<List<Match>> GetMatchesTodayAsync()
        {
            var today = DateTime.Today;
            return await _db.Matches
                .Where(m => m.MatchDate.Date == today)
                .OrderBy(m => m.MatchDate)
                .ToListAsync();
        }

        /// <summary>
        /// Получить список матчей на завтра.
        /// </summary>
        /// <returns>Список матчей на следующий день.</returns>
        public async Task<List<Match>> GetMatchesTomorrowAsync()
        {
            var tomorrow = DateTime.Today.AddDays(1);
            return await _db.Matches
                .Where(m => m.MatchDate.Date == tomorrow && m.Status == "SCHEDULED")
                .OrderBy(m => m.MatchDate)
                .ToListAsync();
        }

        /// <summary>
        /// Получить список матчей на N дней вперед.
        /// </summary>
        /// <param name="days">Количество дней вперед от текущей даты.</param>
        /// <returns>Список матчей в указанном диапазоне.</returns>
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
        // Методы для результатов матчей
        // ================================

        /// <summary>
        /// Получить результаты матчей за сегодня.
        /// </summary>
        /// <returns>Список матчей с результатами за текущий день.</returns>
        public async Task<List<Match>> GetResultsTodayAsync()
        {
            var today = DateTime.Today;

            return await _db.Matches
                .Where(m => m.MatchDate.Date == today && m.Status != "SCHEDULED")
                .OrderBy(m => m.MatchDate)
                .ToListAsync();
        }

        /// <summary>
        /// Получить результаты матчей за вчера.
        /// </summary>
        /// <returns>Список матчей с результатами за вчерашний день.</returns>
        public async Task<List<Match>> GetResultsYesterdayAsync()
        {
            var yesterday = DateTime.Today.AddDays(-1);

            return await _db.Matches
                .Where(m => m.MatchDate.Date == yesterday && m.Status != "SCHEDULED")
                .OrderBy(m => m.MatchDate)
                .ToListAsync();
        }

        /// <summary>
        /// Получить последние сыгранные матчи конкретной команды.
        /// </summary>
        /// <param name="teamName">Название команды (EN).</param>
        /// <returns>Список последних матчей команды.</returns>
        public async Task<List<Match>> GetAllResultsByTeamAsync(string teamName)
        {
            return await _db.Matches
                .Where(m => (m.HomeTeamName.Contains(teamName) || m.AwayTeamName.Contains(teamName))
                            && m.Status != "SCHEDULED")
                .OrderBy(m => m.MatchDate)
                .ToListAsync();
        }

        // ================================
        // Методы для конкретного матча
        // ================================

        /// <summary>
        /// Найти матч по его идентификатору.
        /// </summary>
        /// <param name="matchId">ID матча.</param>
        /// <returns>Объект <see cref="Match"/> или null, если не найден.</returns>
        public async Task<Match?> GetMatchByIdAsync(string matchId)
        {
            return await _db.Matches
                .FirstOrDefaultAsync(m => m.MatchId == matchId);
        }

        /// <summary>
        /// Получить статистику матча (заглушка).
        /// </summary>
        /// <param name="matchId">ID матча.</param>
        /// <returns>Строка со статистикой.</returns>
        public async Task<string> GetMatchStatsAsync(string matchId)
        {
            return await Task.FromResult("Статистика матча (заглушка)");
        }

        /// <summary>
        /// Получить историю встреч команд (заглушка).
        /// </summary>
        /// <param name="matchId">ID матча.</param>
        /// <returns>Строка с историей встреч.</returns>
        public async Task<string> GetMatchHistoryAsync(string matchId)
        {
            return await Task.FromResult("История встреч (заглушка)");
        }

        /// <summary>
        /// Получить результат матча (заглушка).
        /// </summary>
        /// <param name="matchId">ID матча.</param>
        /// <returns>Строка с результатом.</returns>
        public async Task<string> GetMatchResultAsync(string matchId)
        {
            return await Task.FromResult("Результат матча (заглушка)");
        }
    }
}
