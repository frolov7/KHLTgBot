// UI/Menus/Results/ResultsMatchMenuBuilder.cs
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBOT.Domain.Models;

namespace TelegramBOT.Presentation.UI.Menus.Results
{
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
                InlineKeyboardButton.WithCallbackData("📊 Статистика", $"stats_{match.MatchId}")
            });

            rows.Add(new List<InlineKeyboardButton> {
                InlineKeyboardButton.WithCallbackData("📈 Факты", $"facts_{match.MatchId}")
            });

            rows.Add(new List<InlineKeyboardButton> {
                InlineKeyboardButton.WithCallbackData("⬅️ Назад (Результаты)", $"back_to_results_{match.MatchDate:yyyyMMdd}")
            });

            return new InlineKeyboardMarkup(rows);
        }
    }
}
