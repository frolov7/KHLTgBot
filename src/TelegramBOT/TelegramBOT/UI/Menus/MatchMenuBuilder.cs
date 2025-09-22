using Telegram.Bot.Types.ReplyMarkups;
using TelegramBOT.Models;

namespace TelegramBOT.UI.Menus
{
    public class MatchMenuBuilder
    {
        public ReplyKeyboardMarkup Build(Match match)
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("📊 Игры между собой") },
                new[] { new KeyboardButton("⚔️ Прошлые игры") },
                new[] { new KeyboardButton("🔮 Прогнозы") },
                new[] { new KeyboardButton("⬅️ Назад (Календарь)") }
            })
            {
                ResizeKeyboard = true
            };
        }
    }
}
