using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBOT.Presentation.UI.Menus.Calendar
{
    /// <summary>
    /// Создаёт клавиатуры для раздела "Календарь".
    /// </summary>
    public class CalendarMenuBuilder
    {
        /// <summary>
        /// Создаёт основное меню календаря (сегодня, завтра, следующие дни).
        /// </summary>
        public ReplyKeyboardMarkup Build()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "📅 Сегодня" },
                new KeyboardButton[] { "📆 Завтра" },
                new KeyboardButton[] { "🏠 В главное меню" }
            })
            {
                ResizeKeyboard = true
            };
        }

        /// <summary>
        /// Создаёт подменю выбора количества следующих дней.
        /// </summary>
        public ReplyKeyboardMarkup BuildNextDays()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "2 дня", "3 дня" },
                new KeyboardButton[] { "4 дня", "5 дней" },
                new KeyboardButton[] { "⬅️ Назад (Календарь)", "🏠 В главное меню" }
            })
            {
                ResizeKeyboard = true
            };
        }
    }
}