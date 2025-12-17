using Serilog;
using TelegramBOT.Domain.Entities.Matches;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Domain.Teams.TeamCard;
using TelegramBOT.Domain.Teams.TeamCardStats;

namespace TelegramBOT.Application.Teams
{
    public class TeamStatsCalculator
    {
        private readonly ITeamStatsRepository _teamStatsRepository;
        private readonly TeamStrengthCalculator _teamStrengthCalculator;

        public TeamStatsCalculator(
            ITeamStatsRepository teamStatsRepository,
            TeamStrengthCalculator teamStrengthCalculator)
        {
            _teamStatsRepository = teamStatsRepository;
            _teamStrengthCalculator = teamStrengthCalculator;
        }

        // ============================================================
        // PUBLIC ENTRY POINT
        // ============================================================

        public async Task<TeamCardStats> CalculateAsync(string teamName, List<Match> matches10)
        {
            Log.Information("=== Расчёт статистики для команды {Team} ===", teamName);

            // ------------------------------------------------------------
            // ВАЛИДАЦИЯ ВХОДНЫХ ДАННЫХ
            // ------------------------------------------------------------
            if (matches10 == null || matches10.Count == 0)
            {
                Log.Warning(
                    "Матчи для команды {Team} отсутствуют — возвращаем пустую статистику",
                    teamName
                );

                return new TeamCardStats();
            }

            Log.Information(
                "Получено матчей для расчёта: {Count}",
                matches10.Count
            );

            var seasonMatches = await _teamStatsRepository.GetSeasonMatchesAsync(teamName);

            // ------------------------------------------------------------
            // 1. БАЗОВАЯ СТАТИСТИКА КОМАНДЫ
            // (всё, кроме индекса силы)
            // ------------------------------------------------------------
            Log.Information(
                "→ Расчёт базовой статистики команды {Team}",
                teamName
            );

            var stats = CalculateBaseStats(
                teamName,
                matches10,
                seasonMatches
            );

            Log.Information(
                "Базовая статистика рассчитана: Games={Games}, WinReg={WinReg}, WinOT={WinOT}",
                stats.TotalGames,
                stats.Results.WinReg,
                stats.Results.WinOT
            );

            // ------------------------------------------------------------
            // 2. ИНДЕКС СИЛЫ КОМАНДЫ (SNAPSHOT-МОДЕЛЬ)
            // ------------------------------------------------------------
            Log.Information(
                "→ Переход к расчёту индекса силы команды {Team}",
                teamName
            );

            await CalculateTeamStrengthIndexAsync(
                teamName,
                matches10,
                stats
            );

            // ------------------------------------------------------------
            // ФИНАЛЬНОЕ ЛОГИРОВАНИЕ
            // ------------------------------------------------------------
            if (stats.Strength != null)
            {
                Log.Information(
                    "Индекс силы команды {Team} успешно рассчитан: {Value}",
                    teamName,
                    stats.Strength.Value
                );
            }
            else
            {
                Log.Warning(
                    "Индекс силы команды {Team} не был рассчитан",
                    teamName
                );
            }

            Log.Information("=== Статистика команды {Team} успешно рассчитана ===", teamName);

            return stats;
        }

        // ============================================================
        // BASE STATS (без силы)
        // ============================================================

        private TeamCardStats CalculateBaseStats(
            string teamName,
            List<Match> matches10,
            List<Match> seasonMatches)
        {
            var stats = new TeamCardStats
            {
                TotalGames = matches10.Count
            };

            // ===============================
            // FIRST GOAL STATISTICS
            // Кто забивал первым / пропускал первым
            // ===============================
            var (scoredFirst, concededFirst) =
                CalculateFirstGoalStats(teamName, matches10);
            stats.FirstGoal.ScoredFirst = scoredFirst;
            stats.FirstGoal.ConcededFirst = concededFirst;

            // ===============================
            // SUMMARY TOTALS
            // Средние тоталы и индивидуальные тоталы
            // ===============================
            stats.Summary.AvgTotal = CalculateAverageTotal(matches10);
            (stats.Summary.TeamTotal, stats.Summary.OppTotal) =
                CalculateTeamTotals(teamName, matches10);

            // ===============================
            // MATCH RESULTS (W / L)
            // Победы и поражения (осн. время / ОТ / Б)
            // ===============================
            (stats.Results.WinReg,
             stats.Results.WinOT,
             stats.Results.LoseReg,
             stats.Results.LoseOT) =
                CalculateWinsAndLosses(teamName, matches10);

            // ===============================
            // VISUAL RESULTS (LAST 10 MATCHES)
            // Визуальный ряд последних матчей
            // ===============================
            stats.Visual.Last10 = CalculateLast10Results(teamName, matches10);

            // ===============================
            // VISUAL TOTALS (4.5 / 5.5)
            // Пробитие тоталов для визуализации
            // ===============================
            stats.Visual.Totals45 = CalculateTotals(teamName, matches10, 4.5);
            stats.Visual.Totals55 = CalculateTotals(teamName, matches10, 5.5);

            // ===============================
            // PERIOD STATISTICS
            // Статистика по периодам (1 / 2 / 3)
            // ===============================
            CalculatePeriods(teamName, matches10, stats.Periods);

            // ===============================
            // AVERAGE TOTAL (LAST 10)
            // Средний тотал за последние матчи
            // ===============================
            stats.Totals.AvgTotal10 =
                matches10.Average(m => (m.HomeScore ?? 0) + (m.AwayScore ?? 0));

            // ===============================
            // COMEBACK STATISTICS
            // Камбэки с -2 и не проиграли
            // ===============================
            CalculateComebacksNoLoss(teamName, matches10, stats.Comebacks);

            // ===============================
            // HOME / AWAY WIN STATS
            // Победы дома и в гостях (за всё время)
            // ===============================
            stats.HomeAway = CalculateHomeAwayWinStats(teamName, seasonMatches);

            Log.Information(
                "Home/Away stats: Home {HomeWins}/{HomeGames} ({HomePct}%), Away {AwayWins}/{AwayGames} ({AwayPct}%)",
                stats.HomeAway.HomeWins,
                stats.HomeAway.HomeGames,
                stats.HomeAway.HomeWinPercent,
                stats.HomeAway.AwayWins,
                stats.HomeAway.AwayGames,
                stats.HomeAway.AwayWinPercent
            );

            Log.Information("=== Статистика успешно рассчитана ===");

            return stats;
        }

        private async Task<List<TeamMetricsSnapshot>> BuildOpponentSnapshotsAsync(string teamName, List<Match> matches)
        {
            Log.Information("→ Построение snapshot’ов соперников для команды {Team}", teamName);

            var opponents = matches
                .Select(m =>
                    m.HomeTeamName == teamName
                        ? m.AwayTeamName
                        : m.HomeTeamName)
                .Distinct()
                .ToList();

            var snapshots = new List<TeamMetricsSnapshot>();

            foreach (var opponent in opponents)
            {
                Log.Information("→ Соперник {Opponent}", opponent);

                var oppMatches =
                    await _teamStatsRepository.GetLastMatchesAsync(opponent, 10);

                if (oppMatches == null || oppMatches.Count == 0)
                {
                    Log.Warning("Нет матчей для соперника {Opponent}", opponent);
                    continue;
                }

                var snapshot =
                    TeamMetricsSnapshotBuilder.Build(opponent, oppMatches);

                snapshots.Add(snapshot);

                Log.Information(
                    "Snapshot соперника {Opponent}: WinRate={WinRate}, GoalDiff={GoalDiff}",
                    opponent,
                    snapshot.WinRate,
                    snapshot.GoalDiff
                );
            }

            return snapshots;
        }

        // ------------------------------------------------------------------
        // helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Рассчитывает средний общий тотал шайб за список матчей
        /// (сумма голов обеих команд в среднем за матч).
        /// </summary>

        private static double CalculateAverageTotal(List<Match> matches) => matches.Average(m => (m.HomeScore ?? 0) + (m.AwayScore ?? 0));

        /// <summary>
        /// Рассчитывает средние индивидуальные тоталы:
        /// сколько шайб в среднем забивает команда и её соперники за матч.
        /// </summary>
        private static (double team, double opp) CalculateTeamTotals(string teamName, List<Match> matches)
        {
            double team = 0, opp = 0;

            foreach (var m in matches)
            {
                bool home = m.HomeTeamName == teamName;
                team += home ? (m.HomeScore ?? 0) : (m.AwayScore ?? 0);
                opp += home ? (m.AwayScore ?? 0) : (m.HomeScore ?? 0);
            }

            return (
                Math.Round(team / matches.Count, 2),
                Math.Round(opp / matches.Count, 2)
            );
        }

        /// <summary>
        /// Подсчитывает количество побед и поражений команды:
        /// отдельно в основное время и в овертайме/буллитах.
        /// </summary>
        private static (int win, int winOT, int lose, int loseOT) CalculateWinsAndLosses(string teamName, List<Match> matches)
        {
            int win = 0, winOT = 0, lose = 0, loseOT = 0;

            foreach (var m in matches)
            {
                bool home = m.HomeTeamName == teamName;

                int gf = home ? (m.HomeScore ?? 0) : (m.AwayScore ?? 0);
                int ga = home ? (m.AwayScore ?? 0) : (m.HomeScore ?? 0);

                bool ot =
                    m.Status == "AFTER OVERTIME" ||
                    m.Status == "AFTER PENALTIES";

                if (gf > ga)
                    if (ot) winOT++; else win++;
                else if (gf < ga)
                    if (ot) loseOT++; else lose++;
            }

            return (win, winOT, lose, loseOT);
        }

        /// <summary>
        /// Преобразует строковое представление периода матча
        /// (например: "1st period", "OT") в номер периода.
        /// Используется для расчёта периодной статистики.
        /// </summary>
        private static int ParsePeriod(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return 0;

            raw = raw.ToLower();

            if (raw.StartsWith("ot") || raw.StartsWith("so"))
                return 0;

            var digits = new string(raw.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out int p) ? p : 0;
        }

        /// <summary>
        /// Рассчитывает статистику по периодам:
        /// средний индивидуальный тотал команды и общий тотал
        /// для 1, 2 и 3 периодов.
        /// </summary>
        private static void CalculatePeriods(string teamName, List<Match> matches, TeamPeriodsStats stats)
        {
            int p1t = 0, p1 = 0, p2t = 0, p2 = 0, p3t = 0, p3 = 0;

            foreach (var m in matches)
            {
                bool home = m.HomeTeamName == teamName;

                foreach (var e in m.Events.Where(e => e.EventType.Name == "Goal"))
                {
                    int p = ParsePeriod(e.Period);
                    bool teamGoal =
                        (home && e.TeamId == m.HomeTeamId) ||
                        (!home && e.TeamId == m.AwayTeamId);

                    switch (p)
                    {
                        case 1: p1t++; if (teamGoal) p1++; break;
                        case 2: p2t++; if (teamGoal) p2++; break;
                        case 3: p3t++; if (teamGoal) p3++; break;
                    }
                }
            }

            int games = matches.Count;

            stats.Period1IT_Avg = Math.Round((double)p1 / games, 2);
            stats.Period1Total_Avg = Math.Round((double)p1t / games, 2);
            stats.Period2IT_Avg = Math.Round((double)p2 / games, 2);
            stats.Period2Total_Avg = Math.Round((double)p2t / games, 2);
            stats.Period3IT_Avg = Math.Round((double)p3 / games, 2);
            stats.Period3Total_Avg = Math.Round((double)p3t / games, 2);
        }

        /// <summary>
        /// Рассчитывает статистику камбэков:
        /// сколько матчей команда проигрывала минимум в 2 шайбы
        /// и сколько из них завершила без поражения.
        /// </summary>
        private static void CalculateComebacksNoLoss(string teamName, List<Match> matches, TeamComebackStats stats)
        {
            foreach (var m in matches)
            {
                bool home = m.HomeTeamName == teamName;
                int diff = 0;
                bool wasMinus2 = false;

                var goals = m.Events
                    .Where(e => e.EventType.Name == "Goal")
                    .OrderBy(e => e.Period)
                    .ThenBy(e => e.Time);

                foreach (var g in goals)
                {
                    bool teamGoal =
                        (home && g.TeamId == m.HomeTeamId) ||
                        (!home && g.TeamId == m.AwayTeamId);

                    diff += teamGoal ? 1 : -1;
                    if (diff <= -2) wasMinus2 = true;
                }

                if (!wasMinus2) continue;

                stats.GamesTrailingBy2++;

                int gf = home ? (m.HomeScore ?? 0) : (m.AwayScore ?? 0);
                int ga = home ? (m.AwayScore ?? 0) : (m.HomeScore ?? 0);

                if (m.Status != "FINISHED" || gf >= ga)
                    stats.ComebacksNoLossFrom2++;
            }
        }

        /// <summary>
        /// Определяет, сколько раз команда забивала первой
        /// и сколько раз пропускала первой в заданных матчах.
        /// </summary>
        private static (int scoredFirst, int concededFirst) CalculateFirstGoalStats(string teamName, List<Match> matches)
        {
            int scored = 0, conceded = 0;

            foreach (var match in matches)
            {
                var firstGoal = match.Events
                    .Where(e => e.EventType.Name == "Goal")
                    .OrderBy(e => e.Period)
                    .ThenBy(e => e.Time)
                    .FirstOrDefault();

                if (firstGoal == null)
                    continue;

                bool isHome = match.HomeTeamName == teamName;
                bool teamScored =
                    (isHome && firstGoal.TeamId == match.HomeTeamId) ||
                    (!isHome && firstGoal.TeamId == match.AwayTeamId);

                if (teamScored) scored++;
                else conceded++;
            }

            return (scored, conceded);
        }

        /// <summary>
        /// Формирует визуальную статистику пробития тотала
        /// для заданной линии (например 4.5 или 5.5 шайб).
        /// </summary>
        private static List<MatchResultInfo> CalculateTotals(string teamName, List<Match> matches, double line)
        {
            var result = new List<MatchResultInfo>();

            foreach (var m in matches)
            {
                int total = (m.HomeScore ?? 0) + (m.AwayScore ?? 0);
                if (m.Status != "FINISHED")
                    total = Math.Max(0, total - 1);

                result.Add(new MatchResultInfo
                {
                    OpponentTeamName =
                        m.HomeTeamName == teamName
                            ? m.AwayTeamName
                            : m.HomeTeamName,
                    IsWin = total > line
                });
            }

            return result;
        }

        /// <summary>
        /// Формирует визуальные результаты последних матчей команды:
        /// победа или поражение с учётом овертайма и буллитов.
        /// </summary>
        private static List<MatchResultInfo> CalculateLast10Results(string teamName, List<Match> matches)
        {
            return matches.Select(m =>
            {
                bool home = m.HomeTeamName == teamName;

                int gf = home ? (m.HomeScore ?? 0) : (m.AwayScore ?? 0);
                int ga = home ? (m.AwayScore ?? 0) : (m.HomeScore ?? 0);

                return new MatchResultInfo
                {
                    OpponentTeamName =
                        home ? m.AwayTeamName : m.HomeTeamName,
                    IsWin = gf > ga,
                    IsOT = m.Status == "AFTER OVERTIME",
                    IsPEN = m.Status == "AFTER PENALTIES"
                };
            }).ToList();
        }

        /// <summary>
        /// Строит snapshots для команды и её соперников
        /// и рассчитывает индекс силы команды.
        /// </summary>
        private async Task CalculateTeamStrengthIndexAsync(
            string teamName,
            List<Match> matches10,
            TeamCardStats stats)
        {
            // =====================================================
            // SNAPSHOT ДЛЯ ОСНОВНОЙ КОМАНДЫ
            // =====================================================
            Log.Information("→ Построение snapshot основной команды {Team}", teamName);

            var teamSnapshot =
                TeamMetricsSnapshotBuilder.Build(teamName, matches10);

            // =====================================================
            // SNAPSHOT’Ы ДЛЯ СОПЕРНИКОВ
            // =====================================================
            Log.Information("→ Построение snapshot соперников");

            var opponentSnapshots = new List<TeamMetricsSnapshot>();

            var opponents = matches10
                .Select(m => m.HomeTeamName == teamName
                    ? m.AwayTeamName
                    : m.HomeTeamName)
                .Distinct();

            foreach (var opponent in opponents)
            {
                Log.Information("→ Соперник {Opponent}", opponent);

                var oppMatches =
                    await _teamStatsRepository.GetLastMatchesAsync(opponent, 10);

                if (oppMatches == null || oppMatches.Count == 0)
                {
                    Log.Warning("Нет матчей для соперника {Opponent}", opponent);
                    continue;
                }

                var snapshot =
                    TeamMetricsSnapshotBuilder.Build(opponent, oppMatches);

                opponentSnapshots.Add(snapshot);
            }

            // =====================================================
            // ИНДЕКС СИЛЫ (НА ОСНОВЕ SNAPSHOT’ОВ)
            // =====================================================
            Log.Information("→ Расчёт индекса силы команды {Team}", teamName);

            stats.Strength = _teamStrengthCalculator.Calculate(
                teamSnapshot,
                opponentSnapshots
            );

            Log.Information(
                "Индекс силы команды {Team} рассчитан: {Value}",
                teamName,
                stats.Strength.Value
            );
        }

        private static TeamHomeAwayStats CalculateHomeAwayWinStats(
    string teamName,
    List<Match> matches)
        {
            int homeGames = 0, homeWins = 0;
            int awayGames = 0, awayWins = 0;

            foreach (var m in matches)
            {
                bool isHome = m.HomeTeamName == teamName;
                bool isAway = m.AwayTeamName == teamName;

                if (!isHome && !isAway)
                    continue;

                int teamGoals = isHome ? (m.HomeScore ?? 0) : (m.AwayScore ?? 0);
                int oppGoals = isHome ? (m.AwayScore ?? 0) : (m.HomeScore ?? 0);

                if (isHome)
                {
                    homeGames++;
                    if (teamGoals > oppGoals)
                        homeWins++;
                }
                else
                {
                    awayGames++;
                    if (teamGoals > oppGoals)
                        awayWins++;
                }
            }

            return new TeamHomeAwayStats
            {
                HomeGames = homeGames,
                HomeWins = homeWins,
                HomeWinPercent = homeGames == 0
                    ? 0
                    : Math.Round(homeWins * 100.0 / homeGames, 1),

                AwayGames = awayGames,
                AwayWins = awayWins,
                AwayWinPercent = awayGames == 0
                    ? 0
                    : Math.Round(awayWins * 100.0 / awayGames, 1)
            };
        }

    }
}
