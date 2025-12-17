namespace TelegramBOT.Domain.Teams.TeamCardStats
{
    /// <summary>
    /// Индекс силы команды — агрегированный показатель текущей формы
    /// и общего уровня команды.
    /// Используется для сравнений команд и визуального отображения в карточке.
    /// </summary>
    public class TeamStrengthIndex
    {
        /// <summary>
        /// Итоговое значение индекса силы команды.
        /// Диапазон: 0–100 (чем выше, тем сильнее команда).
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Вклад отдельных компонентов индекса силы (0–1).
        /// Используется для отладки, аналитики и UI.
        /// Ключ — название метрики (WinRate, GoalDiff, Momentum и т.д.).
        /// </summary>
        public Dictionary<string, double> Components { get; set; } = new();
    }
}
