using TelegramBOT.Models;

namespace TelegramBOT.Services.Teams
{
    /// <summary>
    /// Интерфейс репозитория данных по матчам команд.
    /// </summary>
    public interface ITeamsRepository
    {
        Task<List<Match>> GetRecentMatchesByTeamAsync(string teamName);
    }
}
