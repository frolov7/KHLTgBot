using TelegramBOT.Domain.Models;

namespace TelegramBOT.Domain.Interfaces
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
