using Telegram.Bot.Types.ReplyMarkups;
using TelegramBOT.Domain.Models;

namespace TelegramBOT.Presentation.UI.Menus.Calendar
{
    /// <summary>
    /// Создаёт inline-меню для конкретного матча.
    /// Используется при отображении информации о матче, его статистике и прогнозах.
    /// </summary>
    public class MatchMenuBuilder
    {
        /// <summary>
        /// Формирует inline-меню действий для выбранного матча.
        /// </summary>
        /// <param name="match">Объект матча, для которого строится меню.</param>
        public InlineKeyboardMarkup Build(Match match)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📊 Игры между собой", $"stats_{match.MatchId}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("⚔️ Прошлые игры", $"history_{match.MatchId}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔮 Прогнозы", $"predict_{match.MatchId}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("⬅️ Назад (Календарь)", $"back_to_calendar_{match.MatchDate:yyyyMMdd}")
                }
            });
        }
    }
}
