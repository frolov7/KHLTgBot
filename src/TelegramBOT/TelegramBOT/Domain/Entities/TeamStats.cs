namespace TelegramBOT.Domain.Models
{
    /// <summary>
    /// Модель статистики команды для расчёта турнирной таблицы.
    /// </summary>
    public class TeamStats
    {
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }                 // Победы в основное время
        public int OvertimeWins { get; set; }         // Победы в овертайме
        public int ShootoutWins { get; set; }         // Победы по буллитам
        public int OvertimeLosses { get; set; }       // Поражения в овертайме
        public int ShootoutLosses { get; set; }       // Поражения по буллитам
        public int Losses { get; set; }               // Поражения в основное время
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int Points { get; set; }
        public Queue<string> RecentForm { get; set; } = new();
    }

}
