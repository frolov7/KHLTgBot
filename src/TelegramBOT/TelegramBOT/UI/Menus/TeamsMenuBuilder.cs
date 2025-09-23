using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBOT.UI.Menus
{
    public class TeamsMenuBuilder
    {
        public ReplyKeyboardMarkup BuildWestern()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("🦌 Торпедо"), new KeyboardButton("🐉 Шанхай Дрэгонс") },
                new[] { new KeyboardButton("🐃 Динамо Минск"), new KeyboardButton("⚒️ Северсталь") },
                new[] { new KeyboardButton("★ ЦСКА"), new KeyboardButton("🐆 ХК Сочи") },
                new[] { new KeyboardButton("🚂 Локомотив"), new KeyboardButton("⭐ СКА") },
                new[] { new KeyboardButton("🔵 Динамо Москва"), new KeyboardButton("🚗 Лада") },
                new[] { new KeyboardButton("♦️ Спартак") },
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
                new[] { new KeyboardButton("🚘 Автомобилист"), new KeyboardButton("🦅 Авангард") },
                new[] { new KeyboardButton("🚜 Трактор"), new KeyboardButton("🐆 Барыс") },
                new[] { new KeyboardButton("⛏️ Металлург"), new KeyboardButton("🐅 Амур") },
                new[] { new KeyboardButton("🐯 Ак Барс"), new KeyboardButton("⚓ Адмирал") },
                new[] { new KeyboardButton("🐺 Нефтехимик"), new KeyboardButton("🕌 Салават Юлаев") },
                new[] { new KeyboardButton("❄️ Сибирь") },
                new[] { new KeyboardButton("⬅️ Назад (Результаты)"), new KeyboardButton("🏠 В главное меню") }
            })
            {
                ResizeKeyboard = true
            };
        }
    }
}
