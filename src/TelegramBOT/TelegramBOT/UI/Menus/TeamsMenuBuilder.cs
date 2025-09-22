using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBOT.UI.Menus
{
    public class TeamsMenuBuilder
    {
        public ReplyKeyboardMarkup BuildWestern()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("🦌 Торпедо Нижний Новгород"), new KeyboardButton("🐉 Куньлунь Ред Стар") },
                new[] { new KeyboardButton("🐃 Динамо Минск"), new KeyboardButton("⚒️ Северсталь Череповец") },
                new[] { new KeyboardButton("★ ЦСКА Москва"), new KeyboardButton("🐆 ХК Сочи") },
                new[] { new KeyboardButton("🚂 Локомотив Ярославль"), new KeyboardButton("⭐ СКА Санкт-Петербург") },
                new[] { new KeyboardButton("🔵 Динамо Москва"), new KeyboardButton("🚗 Лада Тольятти") },
                new[] { new KeyboardButton("♦️ Спартак Москва") },
                new[] { new KeyboardButton("⬅️ Назад (Результаты)"), new KeyboardButton("🏠 В главное меню") }
            })
            {
                ResizeKeyboard = true
            };
        }

        public ReplyKeyboardMarkup BuildEastern()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("🚘 Автомобилист Екатеринбург"), new KeyboardButton("🦅 Авангард Омск") },
                new[] { new KeyboardButton("🚜 Трактор Челябинск"), new KeyboardButton("🐆 Барыс Астана") },
                new[] { new KeyboardButton("⛏️ Металлург Магнитогорск"), new KeyboardButton("🐅 Амур Хабаровск") },
                new[] { new KeyboardButton("🐯 Ак Барс Казань"), new KeyboardButton("⚓ Адмирал Владивосток") },
                new[] { new KeyboardButton("🐺 Нефтехимик Нижнекамск"), new KeyboardButton("🕌 Салават Юлаев Уфа") },
                new[] { new KeyboardButton("❄️ Сибирь Новосибирск") },
                new[] { new KeyboardButton("⬅️ Назад (Результаты)"), new KeyboardButton("🏠 В главное меню") }
            })
            {
                ResizeKeyboard = true
            };
        }
    }
}
