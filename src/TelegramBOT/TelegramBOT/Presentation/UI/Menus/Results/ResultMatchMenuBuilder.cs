using Telegram.Bot.Types.ReplyMarkups;
using TelegramBOT.Domain.Entities.Matches;

namespace TelegramBOT.Presentation.UI.Menus.Results
{
    /// <summary>
    /// Формирует inline-меню для завершённого матча
    /// (результаты, видео, события, прогнозы и навигация).
    /// </summary>
    public class ResultsMatchMenuBuilder
    {
        public InlineKeyboardMarkup Build(
            Match match,
            MatchVideo? video,
            bool fromHeadToHead = false,
            string? originMatchId = null)
        {
            var rows = new List<List<InlineKeyboardButton>>();

            // 🎥 Видеообзор
            if (video != null && !string.IsNullOrWhiteSpace(video.Url))
            {
                rows.Add(new()
                {
                    InlineKeyboardButton.WithUrl("🎥 Видеообзор", video.Url)
                });
            }

            // 📋 События
            rows.Add(new()
            {
                InlineKeyboardButton.WithCallbackData(
                    "📋 События",
                    $"events_results_{match.MatchId}")
            });

            // 🔮 Прогнозы (ВАЖНО: с учётом H2H-контекста)
            rows.Add(new()
            {
                InlineKeyboardButton.WithCallbackData(
                    "🔮 Прогнозы",
                    fromHeadToHead && originMatchId != null
                        ? $"results_predictions_h2h_{originMatchId}_{match.MatchId}"
                        : $"results_predictions_{match.MatchId}"
                )
            });

            // ⬅️ Назад
            rows.Add(new()
            {
                fromHeadToHead && originMatchId != null
                    ? InlineKeyboardButton.WithCallbackData(
                        "⬅️ Назад (К матчам между собой)",
                        $"back_to_h2h_{originMatchId}"
                    )
                    : InlineKeyboardButton.WithCallbackData(
                        "⬅️ Назад (Результаты)",
                        $"back_to_results_{match.MatchDate:yyyyMMdd}"
                    )
            });

            return new InlineKeyboardMarkup(rows);
        }
    }
}
