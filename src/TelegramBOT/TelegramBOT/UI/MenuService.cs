using Telegram.Bot.Types.ReplyMarkups;
using TelegramBOT.Models;
using TelegramBOT.UI.Menus;

namespace TelegramBOT.UI
{
    /// <summary>
    /// Фасад для доступа к меню.
    /// </summary>
    public class MenuService
    {
        private readonly MainMenuBuilder _main;
        private readonly CalendarMenuBuilder _calendar;
        private readonly StatsMenuBuilder _stats;
        private readonly ResultsMenuBuilder _results;
        private readonly TeamsMenuBuilder _teams;
        private readonly MatchMenuBuilder _match;

        public MenuService()
        {
            _main = new MainMenuBuilder();
            _calendar = new CalendarMenuBuilder();
            _stats = new StatsMenuBuilder();
            _results = new ResultsMenuBuilder();
            _teams = new TeamsMenuBuilder();
            _match = new MatchMenuBuilder();
        }

        public ReplyKeyboardMarkup GetMainMenu() => _main.Build();
        public ReplyKeyboardMarkup GetCalendarMenu() => _calendar.Build();
        public ReplyKeyboardMarkup GetNextDaysMenu() => _calendar.BuildNextDaysMenu();
        public ReplyKeyboardMarkup GetStatsMenu() => _stats.Build();
        public ReplyKeyboardMarkup GetResultsMenu() => _results.Build();
        public ReplyKeyboardMarkup GetWesternTeamsMenu() => _teams.BuildWestern();
        public ReplyKeyboardMarkup GetEasternTeamsMenu() => _teams.BuildEastern();
        public ReplyKeyboardMarkup GetMatchMenu(Match match) => _match.Build(match);
    }
}
