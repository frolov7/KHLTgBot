using Telegram.Bot.Types.ReplyMarkups;
using TelegramBOT.Models;

namespace TelegramBOT.UI.Menus.Results
{
    /// <summary>
    /// Создаёт inline-меню для конкретного матча в разделе "Результаты".
    /// </summary>
    public class ResultsMatchMenuBuilder
    {
        /// <summary>
        /// Формирует inline-меню действий для выбранного матча.
        /// </summary>
        public InlineKeyboardMarkup Build(Match match)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🎥 Видеообзор", $"video_{match.MatchId}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📊 Статистика", $"stats_{match.MatchId}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📈 Факты", $"facts_{match.MatchId}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("⬅️ Назад (Результаты)", $"back_to_results_{match.MatchDate:yyyyMMdd}")
                }
            });
        }
    }
}
