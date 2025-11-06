using TelegramBOT.Domain.Models;

namespace TelegramBOT.Domain.Interfaces
{
    /// <summary>
    /// Интерфейс репозитория для получения статистических данных матчей.
    /// </summary>
    public interface IMatchStatsServiceRepository
    {
        Task<IEnumerable<Match>> GetHeadToHeadMatchesAsync(string home, string away);
        Task<IEnumerable<Prediction>> GetPredictionsByMatchIdAsync(string matchId);
        Task<Match?> GetMatchByIdAsync(string matchId);
        Task<IEnumerable<MatchEvent>> GetMatchEventsAsync(string matchId);
    }
}
