namespace TelegramBOT.Domain.Models
{
    /// <summary>
    /// Агрегированная статистика команды для карточки Team Card Poster.
    /// Не является сущностью базы данных — используется только для вычислений и отображения.
    /// </summary>
    public class TeamCardStats
    {
        /// <summary>
        /// Общее количество матчей, использованных в расчётах.
        /// </summary>
        public int TotalGames { get; set; }

        /// <summary>
        /// Средний тотал матча (забитые + пропущенные).
        /// </summary>
        public double AvgTotal { get; set; }

        /// <summary>
        /// Средний индивидуальный тотал команды.
        /// </summary>
        public double TeamTotal { get; set; }

        /// <summary>
        /// Средний индивидуальный тотал соперников.
        /// </summary>
        public double OppTotal { get; set; }

        /// <summary>
        /// Победы в основное время.
        /// </summary>
        public int WinReg { get; set; }

        /// <summary>
        /// Победы в ОТ или серии буллитов.
        /// </summary>
        public int WinOT { get; set; }

        /// <summary>
        /// Поражения в основное время.
        /// </summary>
        public int LoseReg { get; set; }

        /// <summary>
        /// Поражения в ОТ или серии буллитов.
        /// </summary>
        public int LoseOT { get; set; }

        /// <summary>
        /// Количество матчей, в которых команда забила первой.
        /// </summary>
        public int ScoredFirst { get; set; }

        /// <summary>
        /// Количество матчей, в которых команда пропустила первой.
        /// </summary>
        public int ConcededFirst { get; set; }

        /// <summary>
        /// Список соответствий тотала 4.5 (true — пробили, false — не пробили).
        /// Длина списка = TotalGames.
        /// </summary>
        public List<bool> Totals45 { get; set; } = new();

        /// <summary>
        /// Список соответствий тотала 5.5 (true — пробили, false — не пробили).
        /// Длина списка = TotalGames.
        /// </summary>
        public List<bool> Totals55 { get; set; } = new();

        /// <summary> 
        /// ИТ за 1 период. 
        /// </summary>
        public double Period1IT_Avg { get; set; }

        /// <summary> 
        /// Общий тотал 1 периода. 
        /// </summary>
        public double Period1Total_Avg { get; set; }

        /// <summary> 
        /// ИТ за 2 период. 
        /// </summary>
        public double Period2IT_Avg { get; set; }

        /// <summary> 
        /// Общий тотал 2 периода. 
        /// </summary>
        public double Period2Total_Avg { get; set; }

        /// <summary> 
        /// ИТ за 3 период. 
        /// </summary>
        public double Period3IT_Avg { get; set; }

        /// <summary> 
        /// Общий тотал 3 периода.
        /// </summary>
        public double Period3Total_Avg { get; set; }
    }
}
