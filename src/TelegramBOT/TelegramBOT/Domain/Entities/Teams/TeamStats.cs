using TelegramBOT.Domain.Teams.TeamCard;

namespace TelegramBOT.Domain.Entities.Teams
{
    /// <summary>
    /// Агрегированная статистика хоккейной команды,
    /// используемая для формирования турнирной таблицы и визуализации формы команды.
    /// </summary>
    public class TeamStats
    {
        /// <summary>
        /// Общее количество сыгранных матчей командой.
        /// </summary>
        public int GamesPlayed { get; set; }

        /// <summary>
        /// Количество побед в основное время (без овертайма и буллитов).
        /// </summary>
        public int Wins { get; set; }

        /// <summary>
        /// Количество побед, одержанных в овертайме.
        /// </summary>
        public int OvertimeWins { get; set; }

        /// <summary>
        /// Количество побед, одержанных в серии буллитов.
        /// </summary>
        public int ShootoutWins { get; set; }

        /// <summary>
        /// Количество поражений в овертайме
        /// (команда проиграла матч после основного времени).
        /// </summary>
        public int OvertimeLosses { get; set; }

        /// <summary>
        /// Количество поражений в серии буллитов.
        /// </summary>
        public int ShootoutLosses { get; set; }

        /// <summary>
        /// Количество поражений в основное время.
        /// </summary>
        public int Losses { get; set; }

        /// <summary>
        /// Общее количество шайб, заброшенных командой во всех матчах.
        /// </summary>
        public int GoalsFor { get; set; }

        /// <summary>
        /// Общее количество шайб, пропущенных командой во всех матчах.
        /// </summary>
        public int GoalsAgainst { get; set; }

        /// <summary>
        /// Общее количество набранных очков в турнирной таблице.
        /// 
        /// Логика начисления:
        /// - 2 очка за победу (в основное время, ОТ или буллиты)
        /// - 1 очко за поражение в ОТ или буллитах
        /// - 0 очков за поражение в основное время
        /// </summary>
        public int Points { get; set; }

        /// <summary>
        /// Очередь результатов последних матчей команды (форма команды).
        /// 
        /// Используется для визуализации "Формы" в таблице:
        /// каждый элемент содержит информацию о результате матча
        /// (победа/поражение, ОТ/буллиты и соперник).
        /// 
        /// Хранит последние 7 матчей.
        /// </summary>
        public Queue<MatchResultInfo> RecentForm { get; set; } = new();
    }
}
