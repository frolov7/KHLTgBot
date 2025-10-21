using Microsoft.EntityFrameworkCore;
using TelegramBOT.Data;
using TelegramBOT.Models;

namespace TelegramBOT.Services.Results
{
    /// <summary>
    /// Репозиторий для доступа к данным о результатах матчей (через EF Core).
    /// Инкапсулирует все запросы к базе данных, связанные с матчами.
    /// </summary>
    public class ResultsRepository : IResultsRepository
    {
        private readonly AppDbContext _db;

        public ResultsRepository(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Возвращает список матчей, сыгранных в указанную дату.
        /// </summary>
        /// <param name="date">Дата, за которую нужно получить результаты.</param>
        /// <returns>Коллекция матчей с завершёнными результатами за указанную дату.</returns>
        public async Task<IEnumerable<Match>> GetResultsByDateAsync(DateTime date)
        {
            return await _db.Matches
                .Where(m => m.MatchDate.Date == date.Date &&
                            m.HomeScore != null &&
                            m.AwayScore != null)
                .OrderBy(m => m.MatchDate)
                .ToListAsync();
        }

        /// <summary>
        /// Возвращает результат конкретного матча по его идентификатору.
        /// </summary>
        /// <param name="matchId">Уникальный идентификатор матча.</param>
        /// <returns>Объект <see cref="Match"/> или <c>null</c>, если не найден.</returns>
        public async Task<Match?> GetResultByIdAsync(string matchId)
        {
            return await _db.Matches.FirstOrDefaultAsync(m => m.MatchId == matchId);
        }

        /// <summary>
        /// Возвращает список матчей, где участвовала указанная команда.
        /// Учитываются как домашние, так и выездные матчи.
        /// </summary>
        /// <param name="teamName">Название команды (например, "SKA St. Petersburg").</param>
        /// <returns>Коллекция матчей с завершёнными результатами.</returns>
        public async Task<IEnumerable<Match>> GetResultsByTeamAsync(string teamName)
        {
            return await _db.Matches
                .Where(m =>
                    (m.HomeTeamName == teamName || m.AwayTeamName == teamName) &&
                    m.HomeScore != null &&
                    m.AwayScore != null)
                .OrderBy(m => m.MatchDate)
                .Take(7)
                .ToListAsync();
        }
    }
}
