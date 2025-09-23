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
        /// Получить результат матча (заглушка).
        /// </summary>
        /// <param name="matchId">ID матча.</param>
        /// <returns>Строка с результатом.</returns>
        public async Task<string> GetMatchResultAsync(string matchId)
        {
            return await Task.FromResult("Результат матча (заглушка)");
        }

        /// <summary>
        /// Получить очные встречи двух команд.
        /// </summary>
        /// <param name="homeTeam">Название домашней команды.</param>
        /// <param name="awayTeam">Название гостевой команды.</param>
        /// <returns>Список сыгранных матчей между командами.</returns>
        public async Task<List<Match>> GetHeadToHeadMatchesAsync(string homeTeam, string awayTeam)
        {
            return await _db.Matches
                .Where(m =>
                    ((m.HomeTeamName == homeTeam && m.AwayTeamName == awayTeam) ||
                     (m.HomeTeamName == awayTeam && m.AwayTeamName == homeTeam))
                    && m.Status != "SCHEDULED")
                .OrderByDescending(m => m.MatchDate)
                .Take(10) // последние 10 встреч
                .ToListAsync();
        }

        /// <summary>
        /// Получить матч и последние игры обеих команд.
        /// </summary>
        /// <param name="matchId">ID матча.</param>
        /// <returns>
        /// Кортеж:
        /// - Match – сам матч;
        /// - List&lt;Match&gt; – последние игры домашней команды;
        /// - List&lt;Match&gt; – последние игры гостевой команды.
        /// </returns>
        public async Task<(Match? match, List<Match> homeResults, List<Match> awayResults)> GetTeamsHistoryAsync(string matchId)
        {
            var match = await GetMatchByIdAsync(matchId);

            if (match == null)
                return (null, new List<Match>(), new List<Match>());

            var homeResults = await GetAllResultsByTeamAsync(match.HomeTeamName);
            var awayResults = await GetAllResultsByTeamAsync(match.AwayTeamName);

            // Берем последние 10 по дате
            var lastHome = homeResults
                .OrderByDescending(m => m.MatchDate)
                .Take(10)
                .ToList();

            var lastAway = awayResults
                .OrderByDescending(m => m.MatchDate)
                .Take(10)
                .ToList();

            return (match, lastHome, lastAway);
        }
    }
}
