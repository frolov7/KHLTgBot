using TelegramBOT.Domain.Models;

public interface ITeamStatsRepository
{
    Task<List<Match>> GetLastMatchesAsync(string englishTeamName, int limit = 15);

    Task<List<Match>> GetAllMatchesAsync(string englishTeamName);

    Task<(int scoredFirst, int concededFirst)> GetFirstGoalStatsAsync(
        string englishTeamName,
        List<Match> matches);
}
