namespace TelegramBOT.Domain.Teams.TeamCard
{
    /// <summary>
    /// Статистика команды по периодам.
    /// Показывает среднюю результативность команды и общий тотал шайб
    /// в каждом периоде матча.
    /// </summary>
    public class TeamPeriodsStats
    {
        /// <summary>
        /// Средний индивидуальный тотал команды в 1 периоде.
        /// </summary>
        public double Period1IT_Avg { get; set; }

        /// <summary>
        /// Средний общий тотал шайб в 1 периоде (обе команды).
        /// </summary>
        public double Period1Total_Avg { get; set; }

        /// <summary>
        /// Средний индивидуальный тотал команды во 2 периоде.
        /// </summary>
        public double Period2IT_Avg { get; set; }

        /// <summary>
        /// Средний общий тотал шайб во 2 периоде (обе команды).
        /// </summary>
        public double Period2Total_Avg { get; set; }

        /// <summary>
        /// Средний индивидуальный тотал команды в 3 периоде.
        /// </summary>
        public double Period3IT_Avg { get; set; }

        /// <summary>
        /// Средний общий тотал шайб в 3 периоде (обе команды).
        /// </summary>
        public double Period3Total_Avg { get; set; }
    }
}
