using TelegramBOT.Services;
using TelegramBOT.UI;
using TelegramBOT.UI.Menus;
using Microsoft.EntityFrameworkCore;
using TelegramBOT.Data;
using Telegram.Bot.Types.ReplyMarkups;

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

        private readonly AppDbContext _db;

        public ResultsHandler(
            MessageService messageService,
            MatchService matchService,
            MenuService menuService,
            ScriptService scriptService,
            AppDbContext db)
        {
            _messageService = messageService;
            _matchService = matchService;
            _menuService = menuService;
            _scriptService = scriptService;
            _db = db;
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
        public async Task UpdateData(long chatId)
        {
            if (_isUpdatingResults)
            {
                await _messageService.SendTextAsync(chatId, "⏳ Уже идёт обновление, подождите...");
                return;
            }

            _isUpdatingResults = true;
            await _messageService.RemoveKeyboardAsync(chatId, "⏳ Обновляем данные, подождите...");

            try
            {
                var updateResultsTask = _scriptService.RunScraperResultsAsync();
                var updatePredictionsTask = _scriptService.RunScraperPredictionsAsync();
                var updateVideoTask = _scriptService.RunScraperVideoAsync();

                await Task.WhenAll(updateResultsTask, updatePredictionsTask, updateVideoTask);

                await _messageService.SendKeyboardAsync(chatId, "✅ Данные обновлены!", _menuService.GetMainMenu());
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
            await _messageService.SendResultsAsync(chatId, results, DateTime.Today, null, true);
        }

        /// <summary>
        /// Загружает и показывает результаты матчей за вчера
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        public async Task ShowYesterdayResults(long chatId)
        {
            var results = await _matchService.GetResultsYesterdayAsync();
            await _messageService.SendResultsAsync(chatId, results, DateTime.Today.AddDays(-1), null, true);
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
        public async Task HandleResult(long chatId, string callback)
        {
            var matchId = callback.Replace("result_", "");
            var match = await _matchService.GetMatchByIdAsync(matchId);

            if (match == null)
            {
                await _messageService.SendTextAsync(chatId, "❌ Матч не найден.");
                return;
            }

            // Проверяем статус
            bool isFinished = match.Status is "FINISHED" or "AFTER OVERTIME" or "AFTER PENALTIES";
            if (!isFinished)
            {
                await _messageService.SendTextAsync(chatId, "Матч ещё не завершён 🕒");
                return;
            }

            // Ищем видео
            var video = await _db.MatchVideos.FirstOrDefaultAsync(v => v.MatchId == matchId);

            // Получаем клавиатуру через MenuService (аналог ShowSourcesMenu)
            var keyboard = _menuService.GetResultMatchMenu(matchId, video?.Url);

            // Отправляем сообщение с кнопками
            await _messageService.SendKeyboardAsync(
                chatId,
                $"🏒 <b>{match.HomeTeamName}</b> vs <b>{match.AwayTeamName}</b>\n" +
                $"📊 Счёт: <b>{match.HomeScore}:{match.AwayScore}</b>\n" +
                $"📅 {match.MatchDate:dd.MM.yyyy HH:mm} (МСК)" +
                (video == null ? "\n\n🎥 Видеообзор пока недоступен." : ""),
                keyboard
            );
        }


        /// <summary>
        /// Отображает ссылку на видеообзор матча.
        /// </summary>
        public async Task HandleVideoOverview(long chatId, string callback)
        {
            var matchId = callback.Replace("video_", "");
            var video = await _db.MatchVideos.FirstOrDefaultAsync(v => v.MatchId == matchId);

            if (video == null)
            {
                await _messageService.SendTextAsync(chatId, "🎥 Видеообзор для этого матча пока недоступен.");
                return;
            }

            var message =
                $"🎥 <b>{video.Title}</b>\n\n" +
                $"👉 <a href=\"{video.Url}\">Смотреть на YouTube</a>";

            await _messageService.SendTextAsync(chatId, message);
        }
    }
}
