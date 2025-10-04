using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBOT.UI.Menus
{
    public class PredictionsMenuBuilder
    {
        public InlineKeyboardMarkup Build(string matchId)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Legalbet", $"prediction_legalbet_{matchId}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Metaratings", $"prediction_metaratings_{matchId}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Vseprosport", $"prediction_vseprosport_{matchId}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("StavkaTV", $"prediction_stavkatv_{matchId}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Vprognoze", $"prediction_vprognoze_{matchId}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"match_{matchId}")
                }
            });
        }
    }
}
