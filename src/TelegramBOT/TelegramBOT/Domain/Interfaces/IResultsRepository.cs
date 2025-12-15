using TelegramBOT.Domain.Entities.Matches;

namespace TelegramBOT.Domain.Interfaces
{
    /// <summary>
    /// Интерфейс репозитория для доступа к данным о результатах матчей.
    /// </summary>
    public interface IResultsRepository
    {
        Task<IEnumerable<Match>> GetResultsByDateAsync(DateTime date);
        Task<Match?> GetResultByIdAsync(string matchId);
        Task<IEnumerable<Match>> GetResultsByTeamAsync(string teamName);
        Task<MatchVideo?> GetMatchVideoByMatchIdAsync(string matchId);
    }
}
