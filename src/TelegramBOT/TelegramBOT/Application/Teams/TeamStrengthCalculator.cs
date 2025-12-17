using Serilog;
using TelegramBOT.Domain.Teams.TeamCardStats;

namespace TelegramBOT.Application.Teams
{
    /// <summary>
    /// ЧИСТЫЙ МАТЕМАТИЧЕСКИЙ КАЛЬКУЛЯТОР ИНДЕКСА СИЛЫ КОМАНДЫ
    /// Основная идея:
    /// - Каждая метрика нормализуется отдельно (Z-score + Sigmoid)
    /// - QualityOfWins отвечает за "кого именно команда обыгрывала"
    /// - Никакой Strength of Schedule (SoS) — он избыточен при наличии QoW
    /// </summary>
    public class TeamStrengthCalculator
    {
        public TeamStrengthIndex Calculate(
            TeamMetricsSnapshot team,
            List<TeamMetricsSnapshot> league)
        {
            Log.Information("=== Расчёт индекса силы команды {Team} ===", team.TeamName);

            if (league == null || league.Count == 0)
            {
                Log.Warning("Лиговая выборка пуста — индекс будет нейтральным");
                return Neutral();
            }

            // =====================================================
            // 1. WIN RATE
            // =====================================================
            var (wrMean, wrStd) = MeanStd(league.Select(x => x.WinRate), team.WinRate);
            double winRateNorm = Sigmoid(Z(team.WinRate, wrMean, wrStd));
            double winRateWeighted = winRateNorm * 0.15;

            Log.Information(
                "WinRate: raw={Raw}, norm={Norm}, contribution={Contribution}",
                team.WinRate, winRateNorm, winRateWeighted
            );

            // =====================================================
            // 2. QUALITY OF WINS (ключевая метрика)
            // =====================================================
            var (qowMean, qowStd) = MeanStd(league.Select(x => x.QualityOfWins), team.QualityOfWins);
            double qowNorm = Sigmoid(Z(team.QualityOfWins, qowMean, qowStd));
            double qowWeighted = qowNorm * 0.10;

            Log.Information(
                "QualityOfWins: raw={Raw}, norm={Norm}, contribution={Contribution}",
                team.QualityOfWins, qowNorm, qowWeighted
            );

            // =====================================================
            // 3. GOAL DIFFERENCE
            // =====================================================
            var (gdMean, gdStd) = MeanStd(league.Select(x => x.GoalDiff), team.GoalDiff);
            double goalDiffNorm = Sigmoid(Z(team.GoalDiff, gdMean, gdStd));
            double goalDiffWeighted = goalDiffNorm * 0.15;

            Log.Information(
                "GoalDiff: raw={Raw}, norm={Norm}, contribution={Contribution}",
                team.GoalDiff, goalDiffNorm, goalDiffWeighted
            );

            // =====================================================
            // 4. SCORED FIRST
            // =====================================================
            var (sfMean, sfStd) = MeanStd(league.Select(x => x.ScoredFirstRate), team.ScoredFirstRate);
            double scoredFirstNorm = Sigmoid(Z(team.ScoredFirstRate, sfMean, sfStd));
            double scoredFirstWeighted = scoredFirstNorm * 0.05;

            Log.Information(
                "ScoredFirst: raw={Raw}, norm={Norm}, contribution={Contribution}",
                team.ScoredFirstRate, scoredFirstNorm, scoredFirstWeighted
            );

            // =====================================================
            // 5. COMEBACK
            // =====================================================
            var (cbMean, cbStd) = MeanStd(league.Select(x => x.ComebackRate), team.ComebackRate);
            double comebackNorm = Sigmoid(Z(team.ComebackRate, cbMean, cbStd));
            double comebackWeighted = comebackNorm * 0.05;

            Log.Information(
                "Comeback: raw={Raw}, norm={Norm}, contribution={Contribution}",
                team.ComebackRate, comebackNorm, comebackWeighted
            );

            // =====================================================
            // 6. PERIOD DOMINANCE
            // =====================================================
            var (pdMean, pdStd) = MeanStd(league.Select(x => x.PeriodDominance), team.PeriodDominance);
            double periodNorm = Sigmoid(Z(team.PeriodDominance, pdMean, pdStd));
            double periodWeighted = periodNorm * 0.10;

            Log.Information(
                "Periods: raw={Raw}, norm={Norm}, contribution={Contribution}",
                team.PeriodDominance, periodNorm, periodWeighted
            );

            // =====================================================
            // 7. OVERTIME PERFORMANCE
            // =====================================================
            var (otMean, otStd) = MeanStd(league.Select(x => x.OvertimeWinRate), team.OvertimeWinRate);
            double otNorm = Sigmoid(Z(team.OvertimeWinRate, otMean, otStd));
            double otWeighted = otNorm * 0.05;

            Log.Information(
                "Overtime: raw={Raw}, norm={Norm}, contribution={Contribution}",
                team.OvertimeWinRate, otNorm, otWeighted
            );

            // =====================================================
            // 8. MOMENTUM
            // =====================================================
            var (moMean, moStd) = MeanStd(league.Select(x => (double)x.Momentum), team.Momentum);
            double momentumNorm = Sigmoid(Z(team.Momentum, moMean, moStd));
            double momentumWeighted = momentumNorm * 0.05;

            Log.Information(
                "Momentum: raw={Raw}, norm={Norm}, contribution={Contribution}",
                team.Momentum, momentumNorm, momentumWeighted
            );

            // =====================================================
            // FINAL INDEX
            // =====================================================
            double index =
                winRateWeighted +
                qowWeighted +
                goalDiffWeighted +
                scoredFirstWeighted +
                comebackWeighted +
                periodWeighted +
                otWeighted +
                momentumWeighted;

            index = Math.Round(index * 100, 1);

            Log.Information(
                "ИТОГОВЫЙ ИНДЕКС СИЛЫ {Team} = {Index}",
                team.TeamName, index
            );

            return new TeamStrengthIndex
            {
                Value = index,
                Components = new Dictionary<string, double>
                {
                    ["WinRate"] = winRateNorm,
                    ["QualityOfWins"] = qowNorm,
                    ["GoalDiff"] = goalDiffNorm,
                    ["ScoredFirst"] = scoredFirstNorm,
                    ["Comeback"] = comebackNorm,
                    ["Periods"] = periodNorm,
                    ["OT"] = otNorm,
                    ["Momentum"] = momentumNorm
                }
            };
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private static double Z(double value, double mean, double std)
            => std == 0 ? 0 : (value - mean) / std;

        private static double Sigmoid(double x)
            => 1.0 / (1.0 + Math.Exp(-x));

        private static double StdDev(List<double> values)
        {
            if (values.Count <= 1)
                return 1;

            double avg = values.Average();
            return Math.Sqrt(values.Average(v => Math.Pow(v - avg, 2)));
        }

        private static (double mean, double std) MeanStd(
            IEnumerable<double> leagueValues,
            double teamValue)
        {
            var sample = leagueValues
                .Append(teamValue)
                .ToList();

            double mean = sample.Average();
            double std = StdDev(sample);

            if (std == 0)
                std = 1;

            return (mean, std);
        }

        private static TeamStrengthIndex Neutral()
            => new()
            {
                Value = 50,
                Components = new()
            };
    }
}
