using TelegramBOT.Services.Core;
using TelegramBOT.Services.Results;
using TelegramBOT.UI;

namespace TelegramBOT.Handlers.Results
{
    public class ResultsHandler
    {
        private readonly MessageService _messageService;
        private readonly ResultsService _resultsService;
        private readonly MenuService _menuService;

        public ResultsHandler(
            MessageService messageService,
            ResultsService resultsService,
            MenuService menuService)
        {
            _messageService = messageService;
            _resultsService = resultsService;
            _menuService = menuService;
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
            await _messageService.SendKeyboardAsync(chatId, "📊 Выберите действие:", _menuService.GetResultsMenu());
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
            var results = await _resultsService.GetResultsByDateAsync(DateTime.Today);
            var message = _resultsService.BuildResultsMessage(results, DateTime.Today);
            await _messageService.SendTextAsync(chatId, message);
        }

        /// <summary>
        /// Загружает и отображает результаты матчей за вчерашний день.
        /// </summary>
        /// <param name="chatId">Идентификатор Telegram-чата.</param>
        public async Task ShowYesterdayResults(long chatId)
        {
            var results = await _resultsService.GetResultsByDateAsync(DateTime.Today.AddDays(-1));
            var message = _resultsService.BuildResultsMessage(results, DateTime.Today.AddDays(-1));
            await _messageService.SendTextAsync(chatId, message);
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
            var result = await _resultsService.GetResultByIdAsync(matchId);

            if (result == null)
            {
                await _messageService.SendTextAsync(chatId, "❌ Матч не найден.");
                return;
            }

            // Формируем сообщение с результатом одного матча через общий метод форматирования
            var message = _resultsService.BuildResultsMessage(new[] { result });
            await _messageService.SendTextAsync(chatId, message);
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
