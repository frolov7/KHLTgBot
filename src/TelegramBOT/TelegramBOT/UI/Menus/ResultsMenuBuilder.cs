using Telegram.Bot.Types.ReplyMarkups;
using TelegramBOT.Models;

namespace TelegramBOT.UI.Menus
{
    /// <summary>
    /// Конструктор inline-меню для блока "Результаты".
    /// </summary>
    public class ResultsMenuBuilder
    {
        /// <summary>
        /// Главное меню "Результаты".
        /// </summary>
        public ReplyKeyboardMarkup Build()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("📅 Сегодня"), new KeyboardButton("📅 Вчера") },
                new[] { new KeyboardButton("⬅️ Запад (Результаты)"), new KeyboardButton("➡️ Восток (Результаты)") },
                new[] { new KeyboardButton("🏠 В главное меню") }
            })
            {
                ResizeKeyboard = true
            };
        }

        /// <summary>
        /// Inline-меню для выбранного матча из раздела "Результаты".
        /// </summary>
        public InlineKeyboardMarkup BuildMatchResultMenu(string matchId, string? videoUrl)
        {
            var buttons = new List<List<InlineKeyboardButton>>();

            if (!string.IsNullOrEmpty(videoUrl))
            {
                buttons.Add(new()
                {
                    InlineKeyboardButton.WithUrl("🎥 Видеообзор", videoUrl)
                });
            }
            else
            {
                buttons.Add(new()
                {
                    InlineKeyboardButton.WithCallbackData("🎥 Видеообзор недоступен", "no_video")
                });
            }

            buttons.Add(new()
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Назад", "back_to_results")
            });

            return new InlineKeyboardMarkup(buttons);
        }
    }
}
