using TelegramBOT.Domain.Entities.Matches;
using TelegramBOT.Domain.Teams.TeamCardStats;

namespace TelegramBOT.Application.Teams
{
    public static class TeamMetricsSnapshotBuilder
    {
        public static TeamMetricsSnapshot Build(
            string teamName,
            List<Match> matches)
        {
            int games = matches.Count;
            double qualityOfWinsSum = 0;

            if (games == 0)
                return new TeamMetricsSnapshot { TeamName = teamName };

            int wins = 0;
            int scoredFirst = 0;

            int comebackGames = 0;
            int comebackSuccess = 0;

            int otWins = 0;
            int otGames = 0;

            double gf = 0;
            double ga = 0;

            // -----------------------------
            // Period dominance
            // -----------------------------
            double periodScore = 0;
            int periodCount = 0;

            foreach (var m in matches)
            {
                bool home = m.HomeTeamName == teamName;

                int teamGoals = home ? m.HomeScore ?? 0 : m.AwayScore ?? 0;
                int oppGoals = home ? m.AwayScore ?? 0 : m.HomeScore ?? 0;

                gf += teamGoals;
                ga += oppGoals;

                if (teamGoals > oppGoals)
                    wins++;

                // -----------------------------
                // Overtime
                // -----------------------------
                if (m.Status is "AFTER OVERTIME" or "AFTER PENALTIES")
                {
                    otGames++;
                    if (teamGoals > oppGoals)
                        otWins++;
                }

                // -----------------------------
                // First goal
                // -----------------------------
                var firstGoal = m.Events
                    .Where(e => e.EventType.Name == "Goal")
                    .OrderBy(e => ParsePeriod(e.Period))
                    .ThenBy(e => e.Time)
                    .FirstOrDefault();

                if (firstGoal != null)
                {
                    bool teamScoredFirst =
                        (home && firstGoal.TeamId == m.HomeTeamId) ||
                        (!home && firstGoal.TeamId == m.AwayTeamId);

                    if (teamScoredFirst)
                        scoredFirst++;
                }

                // -----------------------------
                // Comeback (-2 or worse → not lost)
                // -----------------------------
                int diff = 0;
                bool wasMinus2 = false;

                var goalsOrdered = m.Events
                    .Where(e => e.EventType.Name == "Goal")
                    .OrderBy(e => ParsePeriod(e.Period))
                    .ThenBy(e => e.Time);

                foreach (var g in goalsOrdered)
                {
                    bool teamGoal =
                        (home && g.TeamId == m.HomeTeamId) ||
                        (!home && g.TeamId == m.AwayTeamId);

                    diff += teamGoal ? 1 : -1;

                    if (diff <= -2)
                        wasMinus2 = true;
                }

                if (wasMinus2)
                {
                    comebackGames++;
                    if (teamGoals >= oppGoals)
                        comebackSuccess++;
                }

                // -----------------------------
                // Quality of Wins (proxy)
                // -----------------------------
                double opponentPower;

                int totalGoals = teamGoals + oppGoals;
                if (totalGoals > 0)
                    opponentPower = (double)oppGoals / totalGoals; // 0..1
                else
                    opponentPower = 0.5;

                double resultWeight;

                if (teamGoals > oppGoals)
                {
                    resultWeight = m.Status is "AFTER OVERTIME" or "AFTER PENALTIES"
                        ? 0.8
                        : 1.0;
                }
                else if (teamGoals < oppGoals)
                {
                    resultWeight = m.Status is "AFTER OVERTIME" or "AFTER PENALTIES"
                        ? -0.2
                        : -1.0;
                }
                else
                {
                    resultWeight = 0;
                }

                qualityOfWinsSum += opponentPower * resultWeight;

                // -----------------------------
                // Period dominance
                // -----------------------------
                var goalsByPeriod = m.Events
                    .Where(e => e.EventType.Name == "Goal")
                    .GroupBy(e => ParsePeriod(e.Period));

                foreach (var p in goalsByPeriod)
                {
                    if (p.Key < 1 || p.Key > 3)
                        continue;

                    int teamPeriodGoals = p.Count(g =>
                        (home && g.TeamId == m.HomeTeamId) ||
                        (!home && g.TeamId == m.AwayTeamId));

                    int oppPeriodGoals = p.Count() - teamPeriodGoals;

                    if (teamPeriodGoals > oppPeriodGoals)
                        periodScore += 1;
                    else if (teamPeriodGoals < oppPeriodGoals)
                        periodScore -= 1;

                    periodCount++;
                }
            }

            double periodDominance =
                periodCount == 0 ? 0 : periodScore / periodCount;

            return new TeamMetricsSnapshot
            {
                TeamName = teamName,
                WinRate = (double)wins / games,
                GoalDiff = (gf - ga) / games,
                ScoredFirstRate = (double)scoredFirst / games,
                ComebackRate =
                    comebackGames == 0 ? 0 : (double)comebackSuccess / comebackGames,
                PeriodDominance = periodDominance,
                OvertimeWinRate =
                    otGames == 0 ? 0 : (double)otWins / otGames,
                Momentum = CalculateMomentum(matches, teamName),
                QualityOfWins = games == 0 ? 0 : qualityOfWinsSum / games
            };
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private static int ParsePeriod(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return 0;

            raw = raw.ToLowerInvariant();

            if (raw.StartsWith("ot") || raw.StartsWith("so"))
                return 0;

            var digits = new string(raw.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out int p) ? p : 0;
        }

        private static int CalculateMomentum(List<Match> matches, string teamName)
        {
            int streak = 0;

            foreach (var m in matches.OrderByDescending(m => m.MatchDate))
            {
                bool home = m.HomeTeamName == teamName;

                int gf = home ? m.HomeScore ?? 0 : m.AwayScore ?? 0;
                int ga = home ? m.AwayScore ?? 0 : m.HomeScore ?? 0;

                if (gf > ga)
                    streak++;
                else
                    break;
            }

            return streak;
        }
    }
}
