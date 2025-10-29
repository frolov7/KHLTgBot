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

            return new InlineKeyboardMarkup(rows);
        }
    }
}
