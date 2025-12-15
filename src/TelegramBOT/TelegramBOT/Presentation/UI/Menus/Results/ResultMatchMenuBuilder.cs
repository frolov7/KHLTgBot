using Telegram.Bot.Types.ReplyMarkups;
using TelegramBOT.Domain.Entities.Matches;

namespace TelegramBOT.Presentation.UI.Menus.Results
{
    /// <summary>
    /// Формирует inline-меню для завершённого матча (результаты, видео, события и т.д.).
    /// </summary>
    public class ResultsMatchMenuBuilder
    {
        public InlineKeyboardMarkup Build(Match match, MatchVideo? video)
        {
            var rows = new List<List<InlineKeyboardButton>>();

            if (video != null && !string.IsNullOrWhiteSpace(video.Url))
            {
                rows.Add(new List<InlineKeyboardButton> {
                    InlineKeyboardButton.WithUrl("🎥 Видеообзор", video.Url)
                });
            }

            rows.Add(new List<InlineKeyboardButton> {
                InlineKeyboardButton.WithCallbackData("📋 События", $"events_results_{match.MatchId}")
            });

            rows.Add(new List<InlineKeyboardButton> {
                InlineKeyboardButton.WithCallbackData("🔮 Прогнозы", $"results_predictions_{match.MatchId}")
            });

            rows.Add(new List<InlineKeyboardButton> {
                InlineKeyboardButton.WithCallbackData("⬅️ Назад (Результаты)", $"back_to_results_{match.MatchDate:yyyyMMdd}")
            });

            return new InlineKeyboardMarkup(rows);
        }
    }
}
