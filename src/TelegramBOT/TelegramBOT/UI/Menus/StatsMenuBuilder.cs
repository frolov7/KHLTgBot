using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBOT.UI.Menus
{
    public class StatsMenuBuilder
    {
        public ReplyKeyboardMarkup Build()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Статистика команд" },
                new KeyboardButton[] { "Статистика игроков" },
                new KeyboardButton[] { "🏠 В главное меню" }
            })
            {
                ResizeKeyboard = true
            };
        }
    }
}
