using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBOT.Presentation.UI.Menus.Main
{
    /// <summary>
    /// Создаёт клавиатуру основного меню бота.
    /// </summary>
    public class MainMenuBuilder
    {
        /// <summary>
        /// Формирует главное меню бота.
        /// </summary>
        public ReplyKeyboardMarkup Build()
        {
            return new(new[]
            {
                new KeyboardButton[] { "📅 Календарь", "⚡ Результаты" },
                new KeyboardButton[] { "📈 Статистика", "📊 Таблицы" },
                new KeyboardButton[] { "🔄 Обновить данные" }
            })
            {
                ResizeKeyboard = true
            };
        }
    }
}
