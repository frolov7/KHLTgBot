using Telegram.Bot.Types.ReplyMarkups;
using TelegramBOT.Domain.Models;

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
                InlineKeyboardButton.WithCallbackData("📋 События", $"events_parse_{match.MatchId}")
            });

            rows.Add(new List<InlineKeyboardButton> {
                InlineKeyboardButton.WithCallbackData("⬅️ Назад (Результаты)", "back_to_results")
            });

            return new InlineKeyboardMarkup(rows);
        }
    }
}
