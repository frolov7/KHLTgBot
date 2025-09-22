using TelegramBOT.Services;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBOT.UI;
using TelegramBOT.Utils;

namespace TelegramBOT.Handlers
{
    public class CalendarHandler
    {
        private readonly MessageService _messageService;
        private readonly MatchService _matchService;
        private readonly MenuService _menuService;
        private readonly MappingService _mappingService;

        public CalendarHandler(MessageService messageService, MatchService matchService, MenuService menuService, MappingService mappingService)
        {
            _messageService = messageService;
            _matchService = matchService;
            _menuService = menuService;
            _mappingService = mappingService;
        }

        public async Task ShowCalendar(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Выберите день", _menuService.GetCalendarMenu());
        }

        public async Task ShowToday(long chatId)
        {
            var matches = await _matchService.GetMatchesTodayAsync();
            await _messageService.SendCalendarAsync(chatId, matches, DateTime.Today, withButtons: true);
        }

        public async Task ShowTomorrow(long chatId)
        {
            var matches = await _matchService.GetMatchesTomorrowAsync();
            await _messageService.SendCalendarAsync(chatId, matches, DateTime.Today.AddDays(1), withButtons: true);
        }

        public async Task ShowNextDaysMenu(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Выберите количество следующих дней:", _menuService.GetNextDaysMenu());
        }

        public async Task ShowNextDays(long chatId, int days)
        {
            var matches = await _matchService.GetMatchesNextDaysAsync(days);
            await _messageService.SendCalendarAsync(chatId, matches, DateTime.Today, DateTime.Today.AddDays(days), withButtons: false);
        }

        public async Task BackToCalendar(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Возврат к календарю", _menuService.GetCalendarMenu());
        }

        public async Task HandleMatchSelected(long chatId, string callback)
        {
            var matchId = callback.Replace("match_", "");
            var match = await _matchService.GetMatchByIdAsync(matchId);

            if (match == null)
            {
                await _messageService.SendTextAsync(chatId, "❌ Матч не найден.");
                return;
            }

            var homeName = _mappingService.Map("TeamNames", match.HomeTeamName);
            var awayName = _mappingService.Map("TeamNames", match.AwayTeamName);

            await _messageService.SendKeyboardAsync(
                chatId,
                $"{homeName} vs {awayName}",
                _menuService.GetMatchMenu(match)
            );
        }
    }
}
