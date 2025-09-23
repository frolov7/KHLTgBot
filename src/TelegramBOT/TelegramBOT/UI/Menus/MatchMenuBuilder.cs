using Telegram.Bot.Types.ReplyMarkups;
using TelegramBOT.Models;

namespace TelegramBOT.UI.Menus
{
    /// <summary>
    /// Строитель меню для выбранного матча.
    /// </summary>
    public class MatchMenuBuilder
    {
        /// <summary>
        /// Формирует inline-меню для матча.
        /// </summary>
        /// <param name="match">Матч</param>
        public InlineKeyboardMarkup Build(Match match)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📊 Игры между собой", $"stats_{match.MatchId}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("⚔️ Прошлые игры", $"history_{match.MatchId}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔮 Прогнозы", $"predict_{match.MatchId}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("⬅️ Назад (Календарь)", "back_to_today")
                }
            });
        }
    }
}
