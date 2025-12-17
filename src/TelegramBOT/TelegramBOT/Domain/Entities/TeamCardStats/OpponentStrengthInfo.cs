namespace TelegramBOT.Domain.Teams
{
    /// <summary>
    /// Информация о силе соперника.
    /// Используется при расчёте силы расписания (Strength of Schedule).
    /// </summary>
    public class OpponentStrengthInfo
    {
        /// <summary>
        /// Название команды-соперника.
        /// </summary>
        public string TeamName { get; set; } = "";

        /// <summary>
        /// Базовый индекс силы соперника.
        /// Диапазон: 0–1 (чем выше, тем сильнее соперник).
        /// </summary>
        public double PowerIndex { get; set; }
    }
}
