using TelegramBOT.Application.Results;
using TelegramBOT.Application.Utils;
using TelegramBOT.Infrastructure.Telegram;
using TelegramBOT.Presentation.UI;
using System.Globalization;

namespace TelegramBOT.Presentation.Handlers.Results
{
    public class ResultsHandler
    {
        private readonly MessageService _messageService;
        private readonly ResultsService _resultsService;
        private readonly MenuService _menuService;
        private readonly MappingService _mappingService;

        public ResultsHandler(
            MessageService messageService,
            ResultsService resultsService,
            MenuService menuService,
            MappingService mappingService)
        {
            _messageService = messageService;
            _resultsService = resultsService;
            _menuService = menuService;
            _mappingService = mappingService;
        }

        // ==========================================================
        // ============      БЛОК ОСНОВНОГО МЕНЮ       =============
        // ==========================================================

        /// <summary>
        /// Отображает основное меню раздела «Результаты».
        /// </summary>
        /// <param name="chatId">Идентификатор Telegram-чата.</param>
        public async Task ShowResultsMenu(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Выберите действие", _menuService.GetResultsMenu());
        }

        /// <summary>
        /// Возврат к результатам матчей за указанную дату.
        /// Пример callback: back_to_results_20251105
        /// </summary>
        public async Task HandleBackToResults(long chatId, string callback)
        {
            if (!_resultsService.TryParseCallbackDate(callback, out var date))
                date = DateTime.Today;

            await _resultsService.SendResultsAsync(chatId, date);
        }

        /// <summary>
        /// Возвращает пользователя в основное меню результатов.
        /// </summary>
        /// <param name="chatId">Идентификатор Telegram-чата.</param>
        public async Task BackToResults(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Возврат к результатам", _menuService.GetResultsMenu());
        }

        // ==========================================================
        // ============      БЛОК ОТОБРАЖЕНИЯ ПО ДНЯМ      =============
        // ==========================================================

        /// <summary>
        /// Загружает и отображает результаты матчей за сегодняшний день.
        /// </summary>
        /// <param name="chatId">Идентификатор Telegram-чата.</param>
        public async Task ShowTodayResults(long chatId)
        {
            await _resultsService.SendResultsAsync(chatId, DateTime.Today);
        }

        /// <summary>
        /// Загружает и отображает результаты матчей за вчерашний день.
        /// </summary>
        /// <param name="chatId">Идентификатор Telegram-чата.</param>
        public async Task ShowYesterdayResults(long chatId)
        {
            await _resultsService.SendResultsAsync(chatId, DateTime.Today.AddDays(-1));
        }

        // ==========================================================
        // ============      БЛОК ОТОБРАЖЕНИЯ МАТЧЕЙ    =============
        // ==========================================================

        /// <summary>
        /// Обрабатывает нажатие на конкретный матч и отображает его результат.
        /// </summary>
        /// <param name="chatId">Идентификатор Telegram-чата.</param>
        /// <param name="callback">Callback-строка, содержащая идентификатор матча (формат: result_{matchId}).</param>
        public async Task HandleResult(long chatId, string callback)
        {
            var matchId = callback.Replace("result_", "");
            await _resultsService.SendResultMatchMenuAsync(chatId, matchId, _menuService);
        }

        /// <summary>
        /// Обрабатывает выбор команды и отображает её последние результаты.
        /// </summary>
        /// <param name="chatId">ID Telegram-чата.</param>
        /// <param name="callback">Callback-строка (например: "team_Северсталь").</param>
        public async Task HandleTeamSelection(long chatId, string callback)
        {
            await _resultsService.SendTeamResultsAsync(chatId, callback);
        }

        // ==========================================================
        // ============      БЛОК МЕНЮ КОМАНД          =============
        // ==========================================================

        /// <summary>
        /// Отображает меню выбора команд западной конференции
        /// для просмотра их последних результатов.
        /// </summary>
        /// <param name="chatId">ID чата Telegram.</param>
        public async Task ShowWesternTeams(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Выберите команду (Запад)", _menuService.GetWesternTeamsMenu());
        }

        /// <summary>
        /// Отображает меню выбора команд восточной конференции
        /// для просмотра их последних результатов.
        /// </summary>
        /// <param name="chatId">ID чата Telegram.</param>
        public async Task ShowEasternTeams(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Выберите команду (Восток)", _menuService.GetEasternTeamsMenu());
        }
    }
}
