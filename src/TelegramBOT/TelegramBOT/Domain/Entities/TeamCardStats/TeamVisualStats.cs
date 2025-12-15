namespace TelegramBOT.Domain.Teams.TeamCard
{
    using System.Collections.Generic;
    using TelegramBOT.Domain.Entities.Teams;

    /// <summary>
    /// Визуальная статистика команды для отображения в карточке.
    /// Содержит данные, которые напрямую используются для отрисовки
    /// иконок, квадратов и графических элементов (без вычислений).
    /// </summary>
    public class TeamVisualStats
    {
        /// <summary>
        /// Результаты последних 10 матчей команды.
        /// Используется для визуального ряда побед/поражений
        /// с учётом ОТ и буллитов.
        /// </summary>
        public List<MatchResultInfo> Last10 { get; set; } = new();

        /// <summary>
        /// Визуальные результаты пробития тотала 4.5.
        /// Каждый элемент соответствует одному матчу и используется
        /// для отрисовки цветного квадрата в карточке команды.
        /// </summary>
        public List<MatchResultInfo> Totals45 { get; set; } = new();

        /// <summary>
        /// Визуальные результаты пробития тотала 5.5.
        /// Каждый элемент соответствует одному матчу и используется
        /// для отрисовки цветного квадрата в карточке команды.
        /// </summary>
        public List<MatchResultInfo> Totals55 { get; set; } = new();
    }
}
