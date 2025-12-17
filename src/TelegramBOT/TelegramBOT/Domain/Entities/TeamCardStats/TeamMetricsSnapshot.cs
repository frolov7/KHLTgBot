namespace TelegramBOT.Domain.Teams.TeamCardStats
{
    /// <summary>
    /// Снимок метрик команды за выбранный период (обычно last 10)
    /// ЧИСТЫЕ ЧИСЛА — без матчей, без логики
    /// </summary>
    public class TeamMetricsSnapshot
    {
        public string TeamName { get; set; } = string.Empty;

        /// <summary>Победы / матчи</summary>
        public double WinRate { get; set; }

        /// <summary>Средняя разница шайб (GF - GA)</summary>
        public double GoalDiff { get; set; }

        /// <summary>Доля матчей, где забили первыми</summary>
        public double ScoredFirstRate { get; set; }

        /// <summary>Камбэки без поражения при -2</summary>
        public double ComebackRate { get; set; }

        /// <summary>Доминирование по периодам</summary>
        public double PeriodDominance { get; set; }

        /// <summary>Процент побед в ОТ / Б</summary>
        public double OvertimeWinRate { get; set; }

        /// <summary>Серия побед (0–N)</summary>
        public double Momentum { get; set; }

        /// <summary>
        /// Качество побед команды.
        /// Учитывает силу соперников и тип результата (основа / ОТ / Б).
        /// Используется как дополнительная метрика стабильности. Может быть отрицательной при плохих результатах.
        /// </summary>
        public double QualityOfWins { get; set; }
    }
}
