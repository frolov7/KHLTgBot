using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBOT.Presentation.UI.Menus.Results
{
    /// <summary>
    /// Создаёт клавиатуры для выбора команд в разделе "Результаты" и "Команды".
    /// Содержит меню Западной и Восточной конференций КХЛ.
    /// </summary>
    public class TeamsMenuBuilder
    {
        // ==========================================================
        // ============         ЗАПАДНАЯ КОНФЕРЕНЦИЯ      ===========
        // ==========================================================

        /// <summary>
        /// Формирует клавиатуру для Западной конференции.
        /// </summary>
        public ReplyKeyboardMarkup BuildWestern()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "🦌 Торпедо", "🐉 Шанхай Дрэгонс" },
                new KeyboardButton[] { "🐃 Динамо Минск", "⚒️ Северсталь" },
                new KeyboardButton[] { "★ ЦСКА", "🐆 ХК Сочи" },
                new KeyboardButton[] { "🚂 Локомотив", "⭐ СКА" },
                new KeyboardButton[] { "🔵 Динамо Москва", "🚗 Лада" },
                new KeyboardButton[] { "♦️ Спартак" },
                new KeyboardButton[] { "⬅️ Назад (Результаты)", "🏠 В главное меню" }
            })
            {
                ResizeKeyboard = true
            };
        }

        // ==========================================================
        // ============         ВОСТОЧНАЯ КОНФЕРЕНЦИЯ     ===========
        // ==========================================================

        /// <summary>
        /// Формирует клавиатуру для Восточной конференции.
        /// </summary>
        public ReplyKeyboardMarkup BuildEastern()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "🚘 Автомобилист", "🦅 Авангард" },
                new KeyboardButton[] { "🚜 Трактор", "🐆 Барыс" },
                new KeyboardButton[] { "⛏️ Металлург", "🐅 Амур" },
                new KeyboardButton[] { "🐯 Ак Барс", "⚓ Адмирал" },
                new KeyboardButton[] { "🐺 Нефтехимик", "🕌 Салават Юлаев" },
                new KeyboardButton[] { "❄️ Сибирь" },
                new KeyboardButton[] { "⬅️ Назад (Результаты)", "🏠 В главное меню" }
            })
            {
                ResizeKeyboard = true
            };
        }
    }
}
