using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBOT.Presentation.UI.Menus.Predictions
{
    /// <summary>
    /// Создаёт inline-меню для раздела "Прогнозы".
    /// Используется при отображении прогнозов на конкретный матч.
    /// </summary>
    public class PredictionsMenuBuilder
    {
        // ----------------------------------------------------------
        // Источники прогнозов
        // ----------------------------------------------------------
        private static readonly string[] Sources =
        {
            //"Общий прогноз",
            "vseprosport",
            "vprognoze",
            "stavkatv",
            "betzona",
            "legalbet",
            "metaratings",
            "livesport"
        };

        /// <summary>
        /// Формирует inline-меню для выбора источника прогноза.
        /// </summary>
        /// <param name="matchId">Идентификатор матча, к которому относится прогноз.</param>
        public InlineKeyboardMarkup Build(string matchId, bool fromHeadToHead = false, string? originMatchId = null)
        {
            var rows = Sources.Select(
                src => new[]{
                    InlineKeyboardButton.WithCallbackData(
                        src,
                        $"prediction_{src.ToLower()}_{matchId}"
                    )
                }
            )
            .ToList();

            rows.Add(new[]{
                fromHeadToHead && originMatchId != null
                    ? InlineKeyboardButton.WithCallbackData(
                        "⬅️ Назад (К матчу)",
                        $"result_h2h_{originMatchId}_{matchId}"
                      )
                    : InlineKeyboardButton.WithCallbackData(
                        "⬅️ Назад (К матчу)",
                        $"back_to_match_{matchId}"
                      )
            });

            return new InlineKeyboardMarkup(rows);
        }
    }
}
