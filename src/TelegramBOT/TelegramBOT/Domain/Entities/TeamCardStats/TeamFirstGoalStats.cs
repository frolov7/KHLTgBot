namespace TelegramBOT.Domain.Teams.TeamCard
{
    /// <summary>
    /// Статистика первого гола команды.
    /// Показывает, как часто команда открывает счёт в матчах
    /// и как часто пропускает первой.
    /// </summary>
    public class TeamFirstGoalStats
    {
        /// <summary>
        /// Количество матчей, в которых команда забила первый гол.
        /// </summary>
        public int ScoredFirst { get; set; }

        /// <summary>
        /// Количество матчей, в которых команда пропустила первый гол.
        /// </summary>
        public int ConcededFirst { get; set; }
    }
}
