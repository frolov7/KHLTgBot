using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBOT.Presentation.UI.Menus.Stats
{
    /// <summary>
    /// Меню для раздела "Таблицы".
    /// </summary>
    public class TablesMenuBuilder
    {
        public ReplyKeyboardMarkup Build()
        {
            var keyboard = new[]
            {
                new[] { new KeyboardButton("🏆 Турнирная таблица") },
                new[] { new KeyboardButton("📊 Рейтинг прогнозов") },
                new[] { new KeyboardButton("⬅️ Назад (Главное меню)") }
            };

            return new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true
            };
        }
    }
}
