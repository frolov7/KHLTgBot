using TelegramBOT.Services;
using TelegramBOT.UI;

namespace TelegramBOT.Handlers
{
    public class ResultsHandler
    {
        private readonly MessageService _messageService;
        private readonly MatchService _matchService;
        private readonly MenuService _menuService;
        private readonly ScriptService _scriptService;

        private bool _isUpdatingResults = false;

        public ResultsHandler(MessageService messageService, MatchService matchService, MenuService menuService, ScriptService scriptService)
        {
            _messageService = messageService;
            _matchService = matchService;
            _menuService = menuService;
            _scriptService = scriptService;
        }

        public async Task ShowResultsMenu(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Выберите день", _menuService.GetResultsMenu());
        }

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
                await _messageService.SendKeyboardAsync(chatId, "✅ Результаты обновлены!", _menuService.GetResultsMenu());
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

        public async Task ShowTodayResults(long chatId)
        {
            var results = await _matchService.GetResultsTodayAsync();
            await _messageService.SendResultsAsync(chatId, results, DateTime.Today);
        }

        public async Task ShowYesterdayResults(long chatId)
        {
            var results = await _matchService.GetResultsYesterdayAsync();
            await _messageService.SendResultsAsync(chatId, results, DateTime.Today.AddDays(-1));
        }

        public async Task ShowWesternTeams(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Выберите команду (Запад)", _menuService.GetWesternTeamsMenu());
        }

        public async Task ShowEasternTeams(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Выберите команду (Восток)", _menuService.GetEasternTeamsMenu());
        }

        public async Task BackToResults(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Возврат к результатам", _menuService.GetResultsMenu());
        }

        public async Task HandleResult(long chatId, string callback)
        {
            var matchId = callback.Replace("result_", "");
            var result = await _matchService.GetMatchResultAsync(matchId);
            await _messageService.SendTextAsync(chatId, $"🏆 Результат:\n{result}");
        }
    }
}
