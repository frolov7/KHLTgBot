using TelegramBOT.Domain.Teams.TeamCardStats;

namespace TelegramBOT.Domain.Teams.TeamCard
{
    /// <summary>
    /// Агрегированная модель статистики команды для карточки Team Card.
    /// Служит единым контейнером всех рассчитанных показателей команды,
    /// используемых для генерации HTML-постера и отображения в Telegram-боте.
    /// Не является сущностью базы данных — формируется на уровне Application.
    /// </summary>
    public class TeamCardStats
    {
        /// <summary>
        /// Общее количество матчей, использованных при расчёте статистики.
        /// </summary>
        public int TotalGames { get; set; }

        /// <summary>
        /// Сводная средняя статистика команды и соперников
        /// (средние тоталы, индивидуальные тоталы и т.п.).
        /// </summary>
        public TeamSummaryStats Summary { get; set; } = new();

        /// <summary>
        /// Статистика побед и поражений с разбивкой
        /// по основному времени и ОТ/буллитам.
        /// </summary>
        public TeamResultsStats Results { get; set; } = new();

        /// <summary>
        /// Статистика тоталов (средние значения, тоталы за последние матчи).
        /// </summary>
        public TeamTotalsStats Totals { get; set; } = new();

        /// <summary>
        /// Статистика результативности по периодам
        /// (индивидуальные и общие тоталы по 1–3 периодам).
        /// </summary>
        public TeamPeriodsStats Periods { get; set; } = new();

        /// <summary>
        /// Статистика первого гола
        /// (сколько раз команда забивала и пропускала первой).
        /// </summary>
        public TeamFirstGoalStats FirstGoal { get; set; } = new();

        /// <summary>
        /// Статистика камбэков команды
        /// (матчи с отставанием в 2 шайбы и исходы таких матчей).
        /// </summary>
        public TeamComebackStats Comebacks { get; set; } = new();

        /// <summary>
        /// Визуальная статистика для отображения в карточке команды
        /// (квадраты результатов последних матчей и тоталов).
        /// </summary>
        public TeamVisualStats Visual { get; set; } = new();

        /// <summary>
        /// Индекс силы команды — агрегированный показатель текущей формы
        /// и уровня команды на основе набора статистических факторов.
        /// Используется для сравнений и визуального отображения в карточке.
        /// </summary>
        public TeamStrengthIndex Strength { get; set; } = new();

        /// <summary>
        /// Статистика побед команды дома и в гостях.
        /// Используется для отображения процента побед за сезон.
        /// </summary>
        public TeamHomeAwayStats HomeAway { get; set; } = new();
    }
}
