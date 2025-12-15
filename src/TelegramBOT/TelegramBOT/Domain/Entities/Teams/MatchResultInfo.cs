namespace TelegramBOT.Domain.Teams.TeamCard
{
    /// <summary>
    /// Результат одного матча в упрощённом виде для визуализации в карточке команды.
    /// Не содержит UI-специфичных данных (цвета, иконки, base64 и т.п.),
    /// используется как входная модель для построения визуальных элементов.
    /// </summary>
    public class MatchResultInfo
    {
        /// <summary>
        /// Признак победы команды в матче.
        /// true — победа, false — поражение.
        /// </summary>
        public bool IsWin { get; set; }

        /// <summary>
        /// Признак того, что матч был завершён в овертайме.
        /// </summary>
        public bool IsOT { get; set; }

        /// <summary>
        /// Признак того, что матч был завершён в серии буллитов.
        /// </summary>
        public bool IsPEN { get; set; }

        /// <summary>
        /// Английское название команды-соперника.
        /// Используется для получения логотипа и отображения в карточке.
        /// </summary>
        public string OpponentTeamName { get; set; } = string.Empty;
    }
}
