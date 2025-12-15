using TelegramBOT.Domain.Entities.Matches;

namespace TelegramBOT.Domain.Interfaces
{
    /// <summary>
    /// Интерфейс репозитория данных по матчам команд.
    /// </summary>
    public interface ITeamsRepository
    {
        Task<List<Match>> GetRecentMatchesByTeamAsync(string teamName);
    }
}
