using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBOT.UI.Menus.Results
{
    /// <summary>
    /// Создаёт клавиатуру для раздела "Результаты".
    /// </summary>
    public class ResultsMenuBuilder
    {
        /// <summary>
        /// Формирует основное меню раздела результатов матчей.
        /// </summary>
        public ReplyKeyboardMarkup Build()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "📆 Сегодня", "📅 Вчера" },
                new KeyboardButton[] { "⬅️ Запад (Результаты)", "➡️ Восток (Результаты)" },
                new KeyboardButton[] { "🏠 В главное меню" }
            })
            {
                ResizeKeyboard = true
            };
        }
    }
}
