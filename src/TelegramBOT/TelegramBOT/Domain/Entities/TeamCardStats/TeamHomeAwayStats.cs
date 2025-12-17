namespace TelegramBOT.Domain.Teams.TeamCardStats
{
    /// <summary>
    /// Статистика побед команды дома и в гостях за сезон (или за весь период).
    /// Используется для анализа разницы в результатах home / away.
    /// </summary>
    public class TeamHomeAwayStats
    {
        /// <summary>
        /// Количество домашних матчей команды.
        /// </summary>
        public int HomeGames { get; set; }

        /// <summary>
        /// Количество побед команды в домашних матчах.
        /// </summary>
        public int HomeWins { get; set; }

        /// <summary>
        /// Процент побед команды дома.
        /// Рассчитывается как (HomeWins / HomeGames) * 100.
        /// </summary>
        public double HomeWinPercent { get; set; }

        /// <summary>
        /// Количество выездных матчей команды.
        /// </summary>
        public int AwayGames { get; set; }

        /// <summary>
        /// Количество побед команды в выездных матчах.
        /// </summary>
        public int AwayWins { get; set; }

        /// <summary>
        /// Процент побед команды в гостях.
        /// Рассчитывается как (AwayWins / AwayGames) * 100.
        /// </summary>
        public double AwayWinPercent { get; set; }
    }
}
