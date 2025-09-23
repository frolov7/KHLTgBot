using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBOT.UI.Menus
{
    public class MainMenuBuilder
    {
        public ReplyKeyboardMarkup Build()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "📅 Календарь" },
                new KeyboardButton[] { "📊 Статистика" },
                new KeyboardButton[] { "🏒 Команды" },
                new KeyboardButton[] { "⚡ Результаты" }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }
    }
}
