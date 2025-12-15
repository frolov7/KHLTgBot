namespace TelegramBOT.Domain.Teams.TeamCard
{
    /// <summary>
    /// Статистика камбэков команды.
    /// Отражает, как команда играет матчи, в которых по ходу игры
    /// уступала сопернику с разницей в две шайбы.
    /// </summary>
    public class TeamComebackStats
    {
        /// <summary>
        /// Количество матчей, в которых команда по ходу игры
        /// оказывалась в отставании минимум в две шайбы.
        /// </summary>
        public int GamesTrailingBy2 { get; set; }

        /// <summary>
        /// Количество матчей из GamesTrailingBy2,
        /// в которых команда не проиграла (победа или ничья в основное время / ОТ / Б).
        /// </summary>
        public int ComebacksNoLossFrom2 { get; set; }

        /// <summary>
        /// Процент матчей без поражения после отставания в две шайбы.
        /// Рассчитывается автоматически на основе GamesTrailingBy2 и ComebacksNoLossFrom2.
        /// </summary>
        public double Percent =>
            GamesTrailingBy2 == 0 ? 0 : Math.Round((double)ComebacksNoLossFrom2 / GamesTrailingBy2 * 100, 1);
    }
}
