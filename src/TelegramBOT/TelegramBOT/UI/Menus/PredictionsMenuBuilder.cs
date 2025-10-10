using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBOT.UI.Menus
{
    public class PredictionsMenuBuilder
    {
        private static readonly string[] Sources =
        {
            "vseprosport",
            "vprognoze",
            "stavkatv",
            "betzona",
            "legalbet",
            "metaratings",
            "livesport"
        };

        public InlineKeyboardMarkup Build(string matchId)
        {
            var rows = Sources
                .Select(src => new[]
                {
                    InlineKeyboardButton.WithCallbackData(src, $"prediction_{src.ToLower()}_{matchId}")
                })
                .ToList();

            // Кнопка Назад
            rows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"match_{matchId}")
            });

            return new InlineKeyboardMarkup(rows);
        }
    }
}