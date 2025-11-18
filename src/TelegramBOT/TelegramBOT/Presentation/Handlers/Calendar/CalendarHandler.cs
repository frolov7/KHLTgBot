using Serilog;
using TelegramBOT.Application.Calendar;
using TelegramBOT.Infrastructure.Telegram;
using TelegramBOT.Presentation.UI;

namespace TelegramBOT.Presentation.Handlers.Calendar
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
            Log.Information("[ShowCalendarMenu] Начало работы метода. Параметры: chatId={ChatId}", chatId);

            await _messageService.SendKeyboardAsync(
                chatId,
                "Выберите день",
                _menuService.GetCalendarMenu()
            );
        }

        /// <summary>
        /// Показывает меню выбора количества следующих дней.
        /// </summary>
        public async Task ShowNextDaysMenu(long chatId)
        {
            Log.Information("[ShowNextDaysMenu] Начало работы метода. Параметры: chatId={ChatId}", chatId);

            await _messageService.SendKeyboardAsync(
                chatId,
                "Выберите количество следующих дней",
                _menuService.GetNextDaysMenu()
            );
        }

        // ============================
        // Отображение матчей
        // ============================

        /// <summary>
        /// Загружает и отображает матчи на сегодня.
        /// </summary>
        public async Task ShowToday(long chatId)
        {
            Log.Information("[ShowToday] Начало работы метода. Параметры: chatId={ChatId}", chatId);

            await _calendarService.SendMatchesAsync(
                chatId,
                DateTime.Today,
                DateTime.Today
            );
        }

        /// <summary>
        /// Загружает и отображает матчи на завтра.
        /// </summary>
        public async Task ShowTomorrow(long chatId)
        {
            var tomorrow = DateTime.Today.AddDays(1);

            Log.Information("[ShowTomorrow] Начало работы метода. Параметры: chatId={ChatId}, date={Date}", chatId, tomorrow);

            await _calendarService.SendMatchesAsync(chatId, tomorrow, tomorrow);
        }

        // ============================
        // Работа с матчами
        // ============================

        /// <summary>
        /// Показывает меню конкретного матча.
        /// </summary>
        public async Task ShowMatchMenu(long chatId, string matchId)
        {
            Log.Information("[ShowMatchMenu] Начало работы метода. Параметры: chatId={ChatId}, matchId={MatchId}", chatId, matchId);

            await _calendarService.SendMatchMenuAsync(chatId, matchId, _menuService);
        }

        /// <summary>
        /// Обрабатывает выбор матча через callback.
        /// </summary>
        public async Task HandleMatchSelected(long chatId, string callback)
        {
            Log.Information("[HandleMatchSelected] Начало работы метода. Параметры: chatId={ChatId}, callback={Callback}", chatId, callback);

            var matchId = callback.Replace("match_", "");

            Log.Information("[HandleMatchSelected] Извлечён matchId={MatchId}", matchId);

            await _calendarService.SendMatchMenuAsync(chatId, matchId, _menuService);
        }

        /// <summary>
        /// Возврат к календарю на конкретную дату.
        /// </summary>
        public async Task HandleBackToCalendar(long chatId, int? messageId, string callback)
        {
            Log.Information("[HandleBackToCalendar] Начало работы метода. Параметры: chatId={ChatId}, messageId={MessageId}, callback={Callback}", chatId, messageId, callback);

            if (messageId.HasValue)
            {
                Log.Information("[HandleBackToCalendar] Удаление сообщения messageId={MessageId}", messageId);
                await _messageService.DeleteMessageAsync(chatId, messageId.Value);
            }

            if (!_calendarService.TryParseCallbackDate(callback, out var date))
            {
                Log.Warning("[HandleBackToCalendar] Не удалось получить дату из callback. Используется текущая дата.");
                date = DateTime.Today;
            }

            Log.Information("[HandleBackToCalendar] Отображение матчей за дату {Date}", date);

            await _calendarService.SendMatchesAsync(chatId, date, date);
        }

        /// <summary>
        /// Возвращает пользователя в меню календаря.
        /// </summary>
        public async Task BackToCalendar(long chatId)
        {
            Log.Information("[BackToCalendar] Начало работы метода. Параметры: chatId={ChatId}", chatId);
            await ShowCalendarMenu(chatId);
        }
    }
}
