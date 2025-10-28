namespace TelegramBOT.Domain.Models
{
    /// <summary>
    /// Модель статистики команды в турнирной таблице.
    /// Используется для внутренней логики расчёта очков и формирования standings.
    /// </summary>
    public class TeamStats
    {
        /// <summary>Количество сыгранных матчей.</summary>
        public int GamesPlayed { get; set; }

        /// <summary>Количество побед.</summary>
        public int Wins { get; set; }

        /// <summary>Количество поражений.</summary>
        public int Losses { get; set; }

        /// <summary>Количество очков.</summary>
        public int Points { get; set; }

        /// <summary>Количество забитых голов.</summary>
        public int GoalsFor { get; set; }

        /// <summary>Количество пропущенных голов.</summary>
        public int GoalsAgainst { get; set; }

        /// <summary>
        /// Разница забитых и пропущенных голов.
        /// </summary>
        public int GoalDifference => GoalsFor - GoalsAgainst;
    }
}
