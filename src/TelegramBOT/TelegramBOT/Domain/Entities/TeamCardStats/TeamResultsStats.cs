namespace TelegramBOT.Domain.Teams.TeamCard
{
    /// <summary>
    /// Модель статистики результатов матчей команды.
    /// Используется в карточке команды (Team Card) для отображения распределения
    /// побед и поражений в зависимости от типа завершения матча.
    /// Не является сущностью базы данных — формируется на этапе расчёта статистики.
    /// </summary>
    public class TeamResultsStats
    {
        /// <summary>
        /// Количество побед в основное время.
        /// </summary>
        public int WinReg { get; set; }

        /// <summary>
        /// Количество побед в овертайме или серии буллитов.
        /// </summary>
        public int WinOT { get; set; }

        /// <summary>
        /// Количество поражений в основное время.
        /// </summary>
        public int LoseReg { get; set; }

        /// <summary>
        /// Количество поражений в овертайме или серии буллитов.
        /// </summary>
        public int LoseOT { get; set; }
    }
}
