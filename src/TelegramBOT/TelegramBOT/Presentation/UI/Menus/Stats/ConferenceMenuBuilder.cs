using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBOT.Presentation.UI.Menus.Stats
{
    /// <summary>
    /// Меню выбора конференции (для турнирной таблицы).
    /// </summary>
    public class ConferenceMenuBuilder
    {
        public ReplyKeyboardMarkup Build()
        {
            var keyboard = new[]
            {
                new[]
                {
                    new KeyboardButton("🔹 Западная конференция"),
                    new KeyboardButton("🔹 Восточная конференция")
                },
                new[]
                {
                    new KeyboardButton("⬅️ Назад (Таблица)")
                }
            };

            return new ReplyKeyboardMarkup(keyboard)
            {
                ResizeKeyboard = true
            };
        }
    }
}
