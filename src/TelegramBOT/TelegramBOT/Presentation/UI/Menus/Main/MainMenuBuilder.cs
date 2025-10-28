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
            var keyboard = new[]
            {
                new[]
                {
                    new KeyboardButton("📅 Календарь"),
                    new KeyboardButton("⚡ Результаты"),
                },
                new[]
                {
                    new KeyboardButton("📊 Статистика"),
                    new KeyboardButton("🏒 Команды"),
                },
                new[]
                {
                    new KeyboardButton("🔄 Обновить данные")
                }
            };

            return new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true
            };
        }

    }
}
