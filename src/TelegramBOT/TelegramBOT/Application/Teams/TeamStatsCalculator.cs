using Microsoft.Extensions.FileSystemGlobbing;
using Serilog;
using TelegramBOT.Domain.Models;

namespace TelegramBOT.Application.Teams
{
    public static class TeamStatsCalculator
    {
        public static TeamCardStats Calculate(
            string teamName,
            List<Match> matches15,
            List<Match> matches7,
            int scoredFirst,
            int concededFirst)
        {
            Log.Information("=== Расчёт статистики для команды {Team} ===", teamName);
            Log.Information("Получено матчей: {Count}", matches15.Count);

            foreach (var m in matches15)
            {
                Log.Information(
                    "Матч {Id}: {Home} {HS}:{AS} {Away} | Статус={Status}",
                    m.MatchId,
                    m.HomeTeamName, m.HomeScore,
                    m.AwayTeamName, m.AwayScore,
                    m.Status
                );
            }

            var stats = new TeamCardStats();

            if (matches15.Count == 0 || matches7.Count == 0)
            {
                Log.Warning("Матчей нет — возвращаем пустую статистику.");
                return stats;
            }

            stats.TotalGames = matches15.Count;

            stats.AvgTotal = CalculateAverageTotal(matches15);
            Log.Information("Средний тотал матча = {AvgTotal}", stats.AvgTotal);

            (stats.TeamTotal, stats.OppTotal) = CalculateTeamTotals(teamName, matches15);
            Log.Information("ИТ команды = {TeamIT}, ИТ соперника = {OppIT}", stats.TeamTotal, stats.OppTotal);

            (stats.WinReg, stats.WinOT, stats.LoseReg, stats.LoseOT) =
                CalculateWinsAndLosses(teamName, matches15);
            Log.Information(
                "Побед осн={WinReg}, Побед ОТ/Б={WinOT}, Поражение осн={LoseReg}, Поражение ОТ/Б={LoseOT}",
                stats.WinReg, stats.WinOT, stats.LoseReg, stats.LoseOT
            );

            stats.ScoredFirst = scoredFirst;
            stats.ConcededFirst = concededFirst;
            Log.Information("Забили первыми = {Scored}, Пропустили первыми = {Conceded}",
            scoredFirst, concededFirst);

            stats.Totals45 = CalculateTotals(matches7, 4.5);
            stats.Totals55 = CalculateTotals(matches7, 5.5);
            Log.Information("Сформированы Totals45 и Totals55");

            CalculatePeriods(teamName, matches15, stats);
            Log.Information(
                "Периоды: 1п {P1IT}/{P1T}, 2п {P2IT}/{P2T}, 3п {P3IT}/{P3T}",
                stats.Period1IT_Avg, stats.Period1Total_Avg,
                stats.Period2IT_Avg, stats.Period2Total_Avg,
                stats.Period3IT_Avg, stats.Period3Total_Avg
            );

            Log.Information("=== Статистика успешно рассчитана ===");

            return stats;
        }

        private static double CalculateAverageTotal(List<Match> matches)
        {
            double total = matches.Sum(m =>
            {
                int sum = (m.HomeScore ?? 0) + (m.AwayScore ?? 0);
                Log.Information("Тотал матча: {HS} + {AS} = {Total}", m.HomeScore, m.AwayScore, sum);
                return sum;
            });

            return total / matches.Count;
        }

        private static (double team, double opp) CalculateTeamTotals(string teamName, List<Match> matches)
        {
            double sumTeam = 0;
            double sumOpp = 0;

            foreach (var m in matches)
            {
                bool isHome = m.HomeTeamName == teamName;

                int gf = isHome ? (m.HomeScore ?? 0) : (m.AwayScore ?? 0);
                int ga = isHome ? (m.AwayScore ?? 0) : (m.HomeScore ?? 0);

                Log.Information("Матч IT: {Team} GF={GF}, GA={GA}", teamName, gf, ga);

                sumTeam += gf;
                sumOpp += ga;
            }

            return (sumTeam / matches.Count, sumOpp / matches.Count);
        }

        private static (int win, int winOT, int lose, int loseOT)
            CalculateWinsAndLosses(string teamName, List<Match> matches)
        {
            int win = 0, winOT = 0, lose = 0, loseOT = 0;

            foreach (var m in matches)
            {
                bool isHome = m.HomeTeamName == teamName;

                int gf = isHome ? (m.HomeScore ?? 0) : (m.AwayScore ?? 0);
                int ga = isHome ? (m.AwayScore ?? 0) : (m.HomeScore ?? 0);

                bool isOT = m.Status == "AFTER OVERTIME" || m.Status == "AFTER PENALTIES";

                Log.Information("Матч Win/Lose: GF={GF}, GA={GA}, isOT={OT}", gf, ga, isOT);

                if (gf > ga)
                    if (isOT) winOT++; else win++;
                else if (gf < ga)
                    if (isOT) loseOT++; else lose++;
            }

            return (win, winOT, lose, loseOT);
        }

        private static List<bool> CalculateTotals(List<Match> matches, double line)
        {
            var result = new List<bool>();

            foreach (var m in matches)
            {
                int hs = m.HomeScore ?? 0;
                int ascore = m.AwayScore ?? 0;
                int total = hs + ascore;

                // Если матч FINISHED — стандартно
                if (m.Status == "FINISHED")
                {
                    bool over = total > line;
                    result.Add(over);
                    continue;
                }

                // Если матч завершён в ОТ или Б
                if (m.Status == "AFTER OVERTIME" || m.Status == "AFTER PENALTIES")
                {
                    int diff = Math.Abs(hs - ascore);

                    // Разница = 1 → победная забита в ОТ
                    if (diff == 1)
                    {
                        // Победная шайба забита в ОТ → тотал не считаем
                        result.Add(false);
                        continue;
                    }

                    // Разница > 1 → шайба ОТ не влияет на тотал → можно считать
                    bool over = total > line;
                    result.Add(over);
                    continue;
                }

                // Остальные статусы (LIVE, SCHEDULED) — считаем как НЕ пробит
                result.Add(false);
            }

            return result;
        }

        private static int ParsePeriod(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return 0;

            raw = raw.ToLower();

            // Игнорируем OT и SO — они не считаются в периодную статистику
            if (raw.StartsWith("ot") || raw.StartsWith("so"))
                return 0;

            // Ищем цифры: "1st period" → 1
            var digits = new string(raw.Where(char.IsDigit).ToArray());

            if (int.TryParse(digits, out int result))
                return result;

            return 0;
        }

        private static void CalculatePeriods(
    string teamName,
    List<Match> matches,
    TeamCardStats stats)
        {
            int p1_team = 0, p1_total = 0;
            int p2_team = 0, p2_total = 0;
            int p3_team = 0, p3_total = 0;

            foreach (var match in matches)
            {
                bool isHome = match.HomeTeamName == teamName;

                foreach (var e in match.Events.Where(e => e.EventType.Name == "Goal")) // гол
                {
                    int period = ParsePeriod(e.Period);

                    bool isTeamGoal =
                        (isHome && e.TeamId == match.HomeTeamId) ||
                        (!isHome && e.TeamId == match.AwayTeamId);

                    switch (period)
                    {
                        case 1:
                            p1_total++;
                            if (isTeamGoal) p1_team++;
                            break;

                        case 2:
                            p2_total++;
                            if (isTeamGoal) p2_team++;
                            break;

                        case 3:
                            p3_total++;
                            if (isTeamGoal) p3_team++;
                            break;
                    }
                }
            }

            int gamesCount = matches.Count;

            stats.Period1IT_Avg = Math.Round((double)p1_team / gamesCount, 2);
            stats.Period1Total_Avg = Math.Round((double)p1_total / gamesCount, 2);

            stats.Period2IT_Avg = Math.Round((double)p2_team / gamesCount, 2);
            stats.Period2Total_Avg = Math.Round((double)p2_total / gamesCount, 2);

            stats.Period3IT_Avg = Math.Round((double)p3_team / gamesCount, 2);
            stats.Period3Total_Avg = Math.Round((double)p3_total / gamesCount, 2);
        }

    }
}
