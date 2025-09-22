using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBOT.UI.Menus
{
    public class ResultsMenuBuilder
    {
        public ReplyKeyboardMarkup Build()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("📅 Сегодня"), new KeyboardButton("📅 Вчера") },
                new[] { new KeyboardButton("⬅️ Запад (Результаты)"), new KeyboardButton("➡️ Восток (Результаты)") },
                new[] { new KeyboardButton("🔄 Обновить данные") },
                new[] { new KeyboardButton("🏠 В главное меню") }
            })
            {
                ResizeKeyboard = true
            };
        }
    }
}
