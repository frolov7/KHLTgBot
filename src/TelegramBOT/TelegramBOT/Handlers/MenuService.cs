using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBOT.Handlers
{
    /// <summary>
    /// Сервис для построения меню и кнопок.
    /// </summary>
    public class MenuService
    {
        /// <summary>
        /// Главное меню (показывается при старте).
        /// </summary>
        public ReplyKeyboardMarkup GetMainMenu()
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

        /// <summary>
        /// Меню календаря матчей.
        /// </summary>
        // Меню календаря матчей
        public ReplyKeyboardMarkup GetCalendarMenu()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Сегодня" },
                new KeyboardButton[] { "Завтра" },
                new KeyboardButton[] { "Следующие N дней" },
                new KeyboardButton[] { "🏠 В главное меню" }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }

        // Подменю для выбора диапазона "следующие N дней"
        public ReplyKeyboardMarkup GetNextDaysMenu()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "2 дня", "3 дня" },
                new KeyboardButton[] { "4 дня", "5 дней" },
                new KeyboardButton[] { "⬅️ Назад", "🏠 В главное меню" }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }

        /// <summary>
        /// Меню статистики.
        /// </summary>
        public ReplyKeyboardMarkup GetStatsMenu()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Статистика команд" },
                new KeyboardButton[] { "Статистика игроков" },
                new KeyboardButton[] { "🏠 В главное меню" }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }

        public ReplyKeyboardMarkup GetResultsKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("📅 Сегодня"), 
                new KeyboardButton("📅 Вчера") },
                new[] { new KeyboardButton("⬅️ Запад"), new KeyboardButton("➡️ Восток") },
                new[] { new KeyboardButton("🔄 Обновить данные") },
                new[] { new KeyboardButton("🏠 В главное меню") }
            })
            {
                ResizeKeyboard = true
            };
        }

        // Западные команды
        public ReplyKeyboardMarkup GetWesternTeamsMenu()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("🦌 Торпедо Нижний Новгород"), new KeyboardButton("🐉 Куньлунь Ред Стар") },
                new[] { new KeyboardButton("🐃 Динамо Минск"), new KeyboardButton("⚒️ Северсталь Череповец") },
                new[] { new KeyboardButton("★ ЦСКА Москва"), new KeyboardButton("🐆 ХК Сочи") },
                new[] { new KeyboardButton("🚂 Локомотив Ярославль"), new KeyboardButton("⭐ СКА Санкт-Петербург") },
                new[] { new KeyboardButton("🔵 Динамо Москва"), new KeyboardButton("🚗 Лада Тольятти") },
                new[] { new KeyboardButton("♦️ Спартак Москва") },
                new[] { new KeyboardButton("🏠 В главное меню") }
            })
            {
                ResizeKeyboard = true
            };
        }

        // Восточные команды
        public ReplyKeyboardMarkup GetEasternTeamsMenu()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("🚘 Автомобилист Екатеринбург"), new KeyboardButton("🦅 Авангард Омск") },
                new[] { new KeyboardButton("🚜 Трактор Челябинск"), new KeyboardButton("🐆 Барыс Астана") },
                new[] { new KeyboardButton("⛏️ Металлург Магнитогорск"), new KeyboardButton("🐅 Амур Хабаровск") },
                new[] { new KeyboardButton("🐯 Ак Барс Казань"), new KeyboardButton("⚓ Адмирал Владивосток") },
                new[] { new KeyboardButton("🐺 Нефтехимик Нижнекамск"), new KeyboardButton("🕌 Салават Юлаев Уфа") },
                new[] { new KeyboardButton("❄️ Сибирь Новосибирск") },
                new[] { new KeyboardButton("🏠 В главное меню") }
            })
            {
                ResizeKeyboard = true
            };
        }

    }
}
