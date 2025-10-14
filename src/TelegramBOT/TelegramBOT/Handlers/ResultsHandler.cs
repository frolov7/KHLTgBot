using TelegramBOT.Services;
using TelegramBOT.UI;

namespace TelegramBOT.Handlers
{
    /// <summary>
    /// Обработчик команд, связанных с результатами матчей.
    /// Управляет показом результатов по дням, обновлением и выбором команд.
    /// </summary>
    public class ResultsHandler
    {
        private readonly MessageService _messageService;
        private readonly MatchService _matchService;
        private readonly MenuService _menuService;
        private readonly ScriptService _scriptService;

        private bool _isUpdatingResults = false;

        public ResultsHandler(
            MessageService messageService,
            MatchService matchService,
            MenuService menuService,
            ScriptService scriptService)
        {
            _messageService = messageService;
            _matchService = matchService;
            _menuService = menuService;
            _scriptService = scriptService;
        }

        /// <summary>
        /// Показывает меню результатов (сегодня, вчера, по конференциям и обновление).
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        public async Task ShowResultsMenu(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Выберите день", _menuService.GetResultsMenu());
        }

        /// <summary>
        /// Запускает обновление данных о результатах матчей.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        public async Task UpdateResults(long chatId)
        {
            if (_isUpdatingResults)
            {
                await _messageService.SendTextAsync(chatId, "⏳ Уже идёт обновление, подождите...");
                return;
            }

            _isUpdatingResults = true;
            await _messageService.RemoveKeyboardAsync(chatId, "⏳ Обновляем результаты, подождите...");

            try
            {
                await _scriptService.RunScraperUpdateAsync();
                await _messageService.SendKeyboardAsync(chatId, "✅ Результаты обновлены!", _menuService.GetMainMenu());
            }
            catch (Exception ex)
            {
                await _messageService.SendTextAsync(chatId, $"❌ Ошибка при обновлении: {ex.Message}");
            }
            finally
            {
                _isUpdatingResults = false;
            }
        }

        /// <summary>
        /// Загружает и показывает результаты матчей за сегодня
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        public async Task ShowTodayResults(long chatId)
        {
            var results = await _matchService.GetResultsTodayAsync();
            await _messageService.SendResultsAsync(chatId, results, DateTime.Today);
        }

        /// <summary>
        /// Загружает и показывает результаты матчей за вчера
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        public async Task ShowYesterdayResults(long chatId)
        {
            var results = await _matchService.GetResultsYesterdayAsync();
            await _messageService.SendResultsAsync(chatId, results, DateTime.Today.AddDays(-1));
        }

        /// <summary>
        /// Показывает меню выбора западных команд для просмотра результатов
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        public async Task ShowWesternTeams(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Выберите команду (Запад)", _menuService.GetWesternTeamsMenu());
        }

        /// <summary>
        /// Показывает меню выбора восточных команд для просмотра результатов
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        public async Task ShowEasternTeams(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Выберите команду (Восток)", _menuService.GetEasternTeamsMenu());
        }

        /// <summary>
        /// Возвращает пользователя в меню результатов.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        public async Task BackToResults(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Возврат к результатам", _menuService.GetResultsMenu());
        }

        /// <summary>
        /// Обрабатывает нажатие на кнопку результата конкретного матча.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="callback">Callback-данные с идентификатором матча.</param>
        public async Task HandleResult(long chatId, string callback)
        {
            var matchId = callback.Replace("result_", "");
            var result = await _matchService.GetMatchResultAsync(matchId);
            await _messageService.SendTextAsync(chatId, $"🏆 Результат:\n{result}");
        }
    }
}
