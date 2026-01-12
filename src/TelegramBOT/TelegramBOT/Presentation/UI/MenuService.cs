using Telegram.Bot.Types.ReplyMarkups;
using TelegramBOT.Domain.Entities.Matches;
using TelegramBOT.Presentation.UI.Menus.Calendar;
using TelegramBOT.Presentation.UI.Menus.Main;
using TelegramBOT.Presentation.UI.Menus.Results;
using TelegramBOT.Presentation.UI.Menus.Stats;
using TelegramBOT.Presentation.UI.Menus.Teams;

namespace TelegramBOT.Presentation.UI
{
    /// <summary>
    /// Фасад для получения различных меню Telegram-бота.
    /// </summary>
    public class MenuService
    {
        private readonly MainMenuBuilder _main;
        private readonly CalendarMenuBuilder _calendar;
        private readonly ResultsMatchMenuBuilder _resultsMatch;
        private readonly ResultsMenuBuilder _results;
        private readonly MatchMenuBuilder _match;
        private readonly ConferenceMenuBuilder _conference;
        private readonly TablesMenuBuilder _tables;

        public MenuService()
        {
            _main = new MainMenuBuilder();
            _calendar = new CalendarMenuBuilder();
            _resultsMatch = new ResultsMatchMenuBuilder();
            _results = new ResultsMenuBuilder();
            _match = new MatchMenuBuilder();
            _conference = new ConferenceMenuBuilder();
            _tables = new TablesMenuBuilder();
        }

        // ---------- Основное меню ----------
        public ReplyKeyboardMarkup GetMainMenu() => _main.Build();

        // ---------- Календарь ----------
        public ReplyKeyboardMarkup GetCalendarMenu() => _calendar.Build();
        public ReplyKeyboardMarkup GetNextDaysMenu() => _calendar.BuildNextDays();

        // ---------- Таблицы ----------
        public ReplyKeyboardMarkup GetConferenceSelectionMenu() => _conference.Build();
        public ReplyKeyboardMarkup GetTablesMenu() => _tables.Build();

        // ---------- Результаты ----------
        public ReplyKeyboardMarkup GetResultsMenu() => _results.Build();
        public InlineKeyboardMarkup GetResultMatchMenu(Match match, MatchVideo? video, bool fromHeadToHead = false, string? originMatchId = null)
        {
            return _resultsMatch.Build(
                match,
                video,
                fromHeadToHead,
                originMatchId
            );
        }

        // ---------- Команды ----------
        public InlineKeyboardMarkup GetTeamsConferenceMenu()
            => TeamsMenuBuilder.BuildConferenceMenu();

        public InlineKeyboardMarkup GetTeamsByConferenceMenu(string conference)
            => TeamsMenuBuilder.BuildTeamsMenu(conference);

        // ---------- Матчи ----------
        public InlineKeyboardMarkup GetMatchMenu(Match match) => _match.Build(match);
    }
}