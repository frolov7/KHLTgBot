using Telegram.Bot.Types.ReplyMarkups;
using TelegramBOT.Models;
using TelegramBOT.UI.Menus.Calendar;
using TelegramBOT.UI.Menus.Main;
using TelegramBOT.UI.Menus.Results;

namespace TelegramBOT.UI
{
    /// <summary>
    /// Фасад для получения различных меню Telegram-бота.
    /// </summary>
    public class MenuService
    {
        private readonly MainMenuBuilder _main;
        private readonly CalendarMenuBuilder _calendar;
        //private readonly MatchDetailsMenuBuilder _stats;
        private readonly ResultsMenuBuilder _results;
        private readonly TeamsMenuBuilder _teams;
        private readonly MatchMenuBuilder _match;

        public MenuService()
        {
            _main = new MainMenuBuilder();
            _calendar = new CalendarMenuBuilder();
            //_stats = new MatchDetailsMenuBuilder();
            _results = new ResultsMenuBuilder();
            _teams = new TeamsMenuBuilder();
            _match = new MatchMenuBuilder();
        }

        // ---------- Основное меню ----------
        public ReplyKeyboardMarkup GetMainMenu() => _main.Build();

        // ---------- Календарь ----------
        public ReplyKeyboardMarkup GetCalendarMenu() => _calendar.Build();
        public ReplyKeyboardMarkup GetNextDaysMenu() => _calendar.BuildNextDays();

        // ---------- Статистика ----------
        //public ReplyKeyboardMarkup GetStatsMenu() => _stats.Build();

        // ---------- Результаты ----------
        public ReplyKeyboardMarkup GetResultsMenu() => _results.Build();

        // ---------- Команды ----------
        public ReplyKeyboardMarkup GetWesternTeamsMenu() => _teams.BuildWestern();
        public ReplyKeyboardMarkup GetEasternTeamsMenu() => _teams.BuildEastern();

        // ---------- Матчи ----------
        public InlineKeyboardMarkup GetMatchMenu(Match match) => _match.Build(match);
    }
}