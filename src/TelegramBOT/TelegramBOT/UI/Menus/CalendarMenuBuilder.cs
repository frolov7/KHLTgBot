using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBOT.UI.Menus
{
    public class CalendarMenuBuilder
    {
        public ReplyKeyboardMarkup Build()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Сегодня" },
                new KeyboardButton[] { "Завтра" },
                new KeyboardButton[] { "Следующие N дней" },
                new KeyboardButton[] { "🏠 В главное меню" }
            })
            {
                ResizeKeyboard = true
            };
        }

        public ReplyKeyboardMarkup BuildNextDaysMenu()
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
