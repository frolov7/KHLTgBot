using TelegramBOT.Services;
using TelegramBOT.UI;
using TelegramBOT.Utils;

namespace TelegramBOT.Handlers
{
    /// <summary>
    /// Обработчик команд, связанных с календарём матчей.
    /// Отвечает за показ матчей на разные дни и переходы между меню.
    /// </summary>
    public class CalendarHandler
    {
        private readonly MessageService _messageService;
        private readonly MatchService _matchService;
        private readonly MenuService _menuService;
        private readonly MappingService _mappingService;

        public CalendarHandler(
            MessageService messageService,
            MatchService matchService,
            MenuService menuService,
            MappingService mappingService)
        {
            _messageService = messageService;
            _matchService = matchService;
            _menuService = menuService;
            _mappingService = mappingService;
        }

        /// <summary>
        /// Показывает меню выбора даты (сегодня, завтра, следующие N дней).
        /// </summary>
        /// <param name="chatId">ID чата, куда отправлять сообщение.</param>
        public async Task ShowCalendar(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Выберите день", _menuService.GetCalendarMenu());
        }

        /// <summary>
        /// Загружает и показывает список матчей на сегодня.
        /// </summary>
        /// <param name="chatId">ID чата, куда отправлять сообщение.</param>
        public async Task ShowToday(long chatId)
        {
            var matches = await _matchService.GetMatchesTodayAsync();
            await _messageService.SendCalendarAsync(chatId, matches, DateTime.Today, withButtons: true);
        }

        /// <summary>
        /// Загружает и показывает список матчей на завтра.
        /// </summary>
        /// <param name="chatId">ID чата, куда отправлять сообщение.</param>
        public async Task ShowTomorrow(long chatId)
        {
            var matches = await _matchService.GetMatchesTomorrowAsync();
            await _messageService.SendCalendarAsync(chatId, matches, DateTime.Today.AddDays(1), withButtons: true);
        }

        /// <summary>
        /// Показывает подменю для выбора диапазона "следующие N дней".
        /// </summary>
        /// <param name="chatId">ID чата, куда отправлять сообщение.</param>
        public async Task ShowNextDaysMenu(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Выберите количество следующих дней:", _menuService.GetNextDaysMenu());
        }

        /// <summary>
        /// Загружает и показывает список матчей на указанное количество следующих дней.
        /// </summary>
        /// <param name="chatId">ID чата, куда отправлять сообщение.</param>
        /// <param name="days">Количество дней вперёд для показа матчей.</param>
        public async Task ShowNextDays(long chatId, int days)
        {
            var matches = await _matchService.GetMatchesNextDaysAsync(days);
            await _messageService.SendCalendarAsync(chatId, matches, DateTime.Today, DateTime.Today.AddDays(days), withButtons: false);
        }

        /// <summary>
        /// Возвращает пользователя в меню календаря.
        /// </summary>
        /// <param name="chatId">ID чата, куда отправлять сообщение.</param>
        public async Task BackToCalendar(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Возврат к календарю", _menuService.GetCalendarMenu());
        }

        /// <summary>
        /// Обрабатывает выбор конкретного матча пользователем и показывает меню матча.
        /// </summary>
        /// <param name="chatId">ID чата, куда отправлять сообщение.</param>
        /// <param name="callback">Данные callback-запроса с ID матча.</param>
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
