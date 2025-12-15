namespace TelegramBOT.Domain.Entities.Teams
{
    /// <summary>
    /// Модель визуального представления результата матча.
    /// Используется на этапе рендеринга карточки команды (HTML/PNG)
    /// и содержит все данные, необходимые для отображения одного
    /// матча в виде цветного квадрата с логотипом соперника.
    /// </summary>
    public class MatchResultSquare
    {
        /// <summary>
        /// Признак победы команды в матче.
        /// true — победа, false — поражение.
        /// </summary>
        public bool IsWin { get; set; }

        /// <summary>
        /// Признак того, что матч завершился в овертайме.
        /// Используется для выбора специального цвета квадрата.
        /// </summary>
        public bool IsOT { get; set; }

        /// <summary>
        /// Признак того, что матч завершился в серии буллитов.
        /// Используется для выбора специального цвета квадрата.
        /// </summary>
        public bool IsPEN { get; set; }

        /// <summary>
        /// Английское название команды-соперника.
        /// Используется для логики и сопоставления с логотипом.
        /// </summary>
        public string OpponentTeamName { get; set; } = string.Empty;

        /// <summary>
        /// Логотип команды-соперника в формате Base64.
        /// Используется напрямую в HTML (img src="data:image/png;base64,...").
        /// </summary>
        public string OpponentLogoBase64 { get; set; } = string.Empty;
    }
}
