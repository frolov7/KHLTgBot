using TelegramBOT.Models;

namespace TelegramBOT.Services.Stats
{
    /// <summary>
    /// Интерфейс репозитория для получения статистических данных матчей.
    /// </summary>
    public interface IMatchStatsServiceRepository
    {
        Task<IEnumerable<Match>> GetHeadToHeadMatchesAsync(string home, string away);
        Task<IEnumerable<Prediction>> GetPredictionsByMatchIdAsync(string matchId);
        Task<Match?> GetMatchByIdAsync(string matchId);
    }
}
