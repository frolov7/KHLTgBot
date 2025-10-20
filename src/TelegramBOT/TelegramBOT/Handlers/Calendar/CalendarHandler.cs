using TelegramBOT.Services.Calendar;
using TelegramBOT.UI;
using TelegramBOT.Services.Core;

namespace TelegramBOT.Handlers.Calendar
{
    /// <summary>
    /// Обработчик команд и навигации календаря матчей.
    /// Отвечает за вызов сервисов в ответ на действия пользователя.
    /// </summary>
    public class CalendarHandler
    {
        private readonly CalendarService _calendarService;
        private readonly MenuService _menuService;
        private readonly MessageService _messageService;

        public CalendarHandler(CalendarService calendarService, MenuService menuService, MessageService messageService)
        {
            _calendarService = calendarService;
            _menuService = menuService;
            _messageService = messageService;
        }

        // ============================
        // Отображение меню
        // ============================

        /// <summary>
        /// Показывает основное меню календаря.
        /// </summary>
        public async Task ShowCalendarMenu(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Выберите день", _menuService.GetCalendarMenu());
        }

        /// <summary>
        /// Показывает меню выбора количества следующих дней.
        /// </summary>
        public async Task ShowNextDaysMenu(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Выберите количество следующих дней", _menuService.GetNextDaysMenu());
        }

        // ============================
        // Отображение матчей
        // ============================

        /// <summary>
        /// Загружает и отображает матчи на сегодня.
        /// </summary>
        public async Task ShowToday(long chatId)
        {
            await _calendarService.SendMatchesAsync(chatId, DateTime.Today, DateTime.Today);
        }

        /// <summary>
        /// Загружает и отображает матчи на завтра.
        /// </summary>
        public async Task ShowTomorrow(long chatId)
        {
            var tomorrow = DateTime.Today.AddDays(1);
            await _calendarService.SendMatchesAsync(chatId, tomorrow, tomorrow);
        }

        /// <summary>
        /// Загружает и отображает матчи на N следующих дней.
        /// </summary>
        public async Task ShowNextDays(long chatId, int days)
        {
            await _calendarService.SendMatchesAsync(chatId, DateTime.Today.AddDays(1), DateTime.Today.AddDays(days));
        }

        // ============================
        // Работа с матчами
        // ============================

        /// <summary>
        /// Показывает меню конкретного матча.
        /// </summary>
        public async Task ShowMatchMenu(long chatId, string matchId)
        {
            await _calendarService.SendMatchMenuAsync(chatId, matchId, _menuService);
        }

        /// <summary>
        /// Обрабатывает выбор матча через callback.
        /// </summary>
        public async Task HandleMatchSelected(long chatId, string callback)
        {
            var matchId = callback.Replace("match_", "");
            await _calendarService.SendMatchMenuAsync(chatId, matchId, _menuService);
        }

        /// <summary>
        /// Возврат к календарю на конкретную дату.
        /// </summary>
        public async Task HandleBackToCalendar(long chatId, int? messageId, string callback)
        {
            if (messageId.HasValue)
                await _messageService.DeleteMessageAsync(chatId, messageId.Value);

            if (!_calendarService.TryParseCallbackDate(callback, out var date))
                date = DateTime.Today;

            await _calendarService.SendMatchesAsync(chatId, date, date);
        }

        /// <summary>
        /// Возвращает пользователя в меню календаря.
        /// </summary>
        public async Task BackToCalendar(long chatId)
        {
            await ShowCalendarMenu(chatId);
        }
    }
}
