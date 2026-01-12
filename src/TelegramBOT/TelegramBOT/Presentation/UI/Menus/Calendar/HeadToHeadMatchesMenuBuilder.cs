using Telegram.Bot.Types.ReplyMarkups;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Entities.Matches;

public class HeadToHeadMatchesMenuBuilder
{
    private readonly MappingService _mappingService;

    public HeadToHeadMatchesMenuBuilder(MappingService mappingService)
    {
        _mappingService = mappingService;
    }

    public InlineKeyboardMarkup Build(IEnumerable<Match> matches, string originMatchId)
    {
        var rows = new List<List<InlineKeyboardButton>>();

        foreach (var m in matches)
        {
            var (home, away) = _mappingService.MapTeamNames(m);

            rows.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{m.MatchDate:dd.MM} | {home} – {away}",
                    $"open_result_h2h_{originMatchId}_{m.MatchId}"
                )
            });
        }

        rows.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(
                "⬅️ Назад (К матчу)",
                $"back_to_match_{originMatchId}"
            )
        });

        return new InlineKeyboardMarkup(rows);
    }
}
