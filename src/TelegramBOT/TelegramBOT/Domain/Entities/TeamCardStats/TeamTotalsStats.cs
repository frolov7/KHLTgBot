namespace TelegramBOT.Domain.Teams.TeamCard
{
    using System.Collections.Generic;

    /// <summary>
    /// Статистика тоталов команды.
    /// Используется для анализа результативности матчей
    /// </summary>
    public class TeamTotalsStats
    {
        /// <summary>
        /// Средний общий тотал шайб за последние 10 матчей.
        /// Считается как среднее значение суммы шайб обеих команд.
        /// </summary>
        public double AvgTotal10 { get; set; }

        /// <summary>
        /// История пробития тотала 4.5 за последние матчи.
        /// true  — тотал больше 4.5 пробит,
        /// false — тотал не пробит.
        /// Порядок соответствует хронологии матчей.
        /// </summary>
        public List<bool> Totals45 { get; set; } = new();

        /// <summary>
        /// История пробития тотала 5.5 за последние матчи.
        /// true  — тотал больше 5.5 пробит,
        /// false — тотал не пробит.
        /// Порядок соответствует хронологии матчей.
        /// </summary>
        public List<bool> Totals55 { get; set; } = new();
    }
}
