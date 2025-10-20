using TelegramBOT.Models;

namespace TelegramBOT.Data.Repositories
{
    /// <summary>
    /// Интерфейс репозитория календаря матчей.
    /// Определяет контракты для работы с данными без конкретной реализации.
    /// </summary>
    public interface ICalendarRepository
    {
        Task<List<Match>> GetMatchesByDateRangeAsync(DateTime from, DateTime to);
        Task<Match?> GetMatchAsync(string matchId);
    }
}
