using TelegramBOT.Domain.Models;

namespace TelegramBOT.Application.Teams
{
    public static class TeamStatsCalculator
    {
        public static TeamStatsResult Calculate(string teamName, List<Match> matches)
        {
            var r = new TeamStatsResult();

            int total = matches.Count;
            if (total == 0) return r;

            r.TotalGames = total;

            int winReg = 0, winOT = 0, loseReg = 0, loseOT = 0;
            int scoredFirst = 0, concededFirst = 0;

            double sumTotal = 0;
            double sumIT = 0;
            double sumOppIT = 0;

            var totals45 = new List<bool>();
            var totals55 = new List<bool>();

            foreach (var m in matches)
            {
                bool isHome = m.HomeTeamName == teamName;

                int goalsFor = isHome ? (m.HomeScore ?? 0) : (m.AwayScore ?? 0);
                int goalsAgainst = isHome ? (m.AwayScore ?? 0) : (m.HomeScore ?? 0);

                int totalGoals = (m.HomeScore ?? 0) + (m.AwayScore ?? 0);

                totals45.Add(totalGoals > 4.5);
                totals55.Add(totalGoals > 5.5);

                sumTotal += totalGoals;
                sumIT += goalsFor;
                sumOppIT += goalsAgainst;

                bool win = goalsFor > goalsAgainst;
                bool lose = goalsFor < goalsAgainst;

                bool isOT = m.Status == "AFTER OVERTIME" || m.Status == "AFTER PENALTIES";

                if (win)
                {
                    if (isOT) winOT++;
                    else winReg++;
                }
                else if (lose)
                {
                    if (isOT) loseOT++;
                    else loseReg++;
                }

                // TODO: через MatchEvents определить первый гол
            }

            r.AvgTotal = sumTotal / total;
            r.TeamIT = sumIT / total;
            r.OppIT = sumOppIT / total;

            r.WinReg = winReg;
            r.WinOT = winOT;
            r.LoseReg = loseReg;
            r.LoseOT = loseOT;

            r.ScoredFirst = scoredFirst;
            r.ConcededFirst = concededFirst;

            r.Totals45 = totals45;
            r.Totals55 = totals55;

            return r;
        }
    }
}

public class TeamStatsResult
{
    public int TotalGames { get; set; }

    public double AvgTotal { get; set; }
    public double TeamIT { get; set; }
    public double OppIT { get; set; }

    public int WinReg { get; set; }
    public int WinOT { get; set; }
    public int LoseReg { get; set; }
    public int LoseOT { get; set; }

    public int ScoredFirst { get; set; }
    public int ConcededFirst { get; set; }

    public List<bool> Totals45 { get; set; } = new();
    public List<bool> Totals55 { get; set; } = new();
}
