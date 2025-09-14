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
        public ReplyKeyboardMarkup GetCalendarMenu()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Сегодня" },
                new KeyboardButton[] { "Завтра" },
                new KeyboardButton[] { "Следующие 5 дней" },
                new KeyboardButton[] { "⬅️ Назад" }
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
                new KeyboardButton[] { "⬅️ Назад" }
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
                new[] { new KeyboardButton("🔍 Результаты команды") },
                new[] { new KeyboardButton("🔄 Обновить") },
                new[] { new KeyboardButton("⬅️ Назад") }
            })
            {
                ResizeKeyboard = true
            };
        }

        // Меню выбора конференции
        public ReplyKeyboardMarkup GetTeamsConferenceMenu()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("Запад"), new KeyboardButton("Восток") },
                new[] { new KeyboardButton("⬅️ Назад") }
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
                new[] { new KeyboardButton("⭐ СКА Санкт-Петербург"), new KeyboardButton("★ ЦСКА Москва") },
                new[] { new KeyboardButton("🔵 Динамо Москва"), new KeyboardButton("♦️ Спартак Москва") },
                new[] { new KeyboardButton("🚂 Локомотив Ярославль"), new KeyboardButton("🦌 Торпедо Нижний Новгород") },
                new[] { new KeyboardButton("⚒️ Северсталь Череповец"), new KeyboardButton("🐆 ХК Сочи") },
                new[] { new KeyboardButton("🐃 Динамо Минск") },
                new[] { new KeyboardButton("⬅️ Назад") }
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
                new[] { new KeyboardButton("🦅 Авангард Омск"), new KeyboardButton("🐯 Ак Барс Казань") },
                new[] { new KeyboardButton("⛏️ Металлург Магнитогорск"), new KeyboardButton("🕌 Салават Юлаев Уфа") },
                new[] { new KeyboardButton("🚘 Автомобилист Екатеринбург"), new KeyboardButton("🚜 Трактор Челябинск") },
                new[] { new KeyboardButton("⚓ Адмирал Владивосток"), new KeyboardButton("❄️ Сибирь Новосибирск") },
                new[] { new KeyboardButton("🐺 Нефтехимик Нижнекамск"), new KeyboardButton("🐅 Амур Хабаровск") },
                new[] { new KeyboardButton("🚗 Лада Тольятти"), new KeyboardButton("🐉 Куньлунь Ред Стар") },
                new[] { new KeyboardButton("⬅️ Назад") }
            })
            {
                ResizeKeyboard = true
            };
        }


    }
}
