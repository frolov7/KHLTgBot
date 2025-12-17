using TelegramBOT.Domain.Entities.Matches;
using TelegramBOT.Domain.Teams.TeamCard;

public interface ITeamStatsRepository
{
    Task<List<Match>> GetLastMatchesAsync(string englishTeamName, int limit);

    Task<List<Match>> GetAllMatchesAsync(string englishTeamName);

    Task<List<Match>> GetSeasonMatchesAsync(string englishTeamName);
}
