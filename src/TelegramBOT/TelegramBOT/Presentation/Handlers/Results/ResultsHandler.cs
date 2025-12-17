using TelegramBOT.Application.Results;
using TelegramBOT.Application.Utils;
using TelegramBOT.Presentation.UI;
using System.Globalization;
using Serilog;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBOT.Application.Telegram;
using TelegramBOT.Presentation.Rendering.Html.Calendar;
using TelegramBOT.Presentation.Rendering.Html;
using TelegramBOT.Domain.Interfaces;
using static TelegramBOT.Presentation.Rendering.Html.Calendar.MatchPredictionPosterHtmlBuilder;

namespace TelegramBOT.Presentation.Handlers.Results
{
    public class ResultsHandler     
    {
        private readonly MessageService _messageService;
        private readonly ResultsService _resultsService;
        private readonly MenuService _menuService;
        private readonly MappingService _mappingService;
        private readonly IMatchStatsServiceRepository _matchStatsRepository;
        private readonly IConfiguration _config;

        public ResultsHandler(
            IMatchStatsServiceRepository matchStatsRepository,
            IConfiguration config,
            MessageService messageService,
            ResultsService resultsService,
            MenuService menuService,
            MappingService mappingService)
        {
            _matchStatsRepository = matchStatsRepository;
            _messageService = messageService;
            _resultsService = resultsService;
            _menuService = menuService;
            _mappingService = mappingService;
            _config = config;
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
            Log.Information("[ShowResultsMenu] Начало работы метода. chatId={ChatId}", chatId);
            await _messageService.SendKeyboardAsync(chatId, "Выберите действие", _menuService.GetResultsMenu());
        }

        /// <summary>
        /// Возврат к результатам матчей за указанную дату.
        /// Пример callback: back_to_results_20251105
        /// </summary>
        public async Task HandleBackToResults(long chatId, string callback)
        {
            Log.Information("[HandleBackToResults] Начало работы метода. chatId={ChatId}, callback={Callback}", chatId, callback);

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
            Log.Information("[BackToResults] Начало работы метода. chatId={ChatId}", chatId);
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
            Log.Information("[ShowTodayResults] Начало работы метода. chatId={ChatId}", chatId);
            await _resultsService.SendResultsAsync(chatId, DateTime.Today);
        }

        /// <summary>
        /// Загружает и отображает результаты матчей за вчерашний день.
        /// </summary>
        /// <param name="chatId">Идентификатор Telegram-чата.</param>
        public async Task ShowYesterdayResults(long chatId)
        {
            Log.Information("[ShowYesterdayResults] Начало работы метода. chatId={ChatId}", chatId);
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
            Log.Information("[HandleResult] Начало работы метода. chatId={ChatId}, callback={Callback}", chatId, callback);

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
            Log.Information("[HandleTeamSelection] Начало работы метода. chatId={ChatId}, callback={Callback}", chatId, callback);
            await _resultsService.SendTeamResultsAsync(chatId, callback);
        }

        // ==========================================================
        // ============      БЛОК МЕНЮ КОМАНД          =============
        // ==========================================================
        /*
        /// <summary>
        /// Отображает меню выбора команд западной конференции
        /// для просмотра их последних результатов.
        /// </summary>
        /// <param name="chatId">ID чата Telegram.</param>
        public async Task ShowWesternTeams(long chatId)
        {
            Log.Information("[ShowWesternTeams] Начало работы метода. chatId={ChatId}", chatId);
            await _messageService.SendKeyboardAsync(chatId, "Выберите команду (Запад)", _menuService.GetWesternTeamsMenu());
        }

        /// <summary>
        /// Отображает меню выбора команд восточной конференции
        /// для просмотра их последних результатов.
        /// </summary>
        /// <param name="chatId">ID чата Telegram.</param>
        public async Task ShowEasternTeams(long chatId)
        {
            Log.Information("[ShowEasternTeams] Начало работы метода. chatId={ChatId}", chatId);
            await _messageService.SendKeyboardAsync(chatId, "Выберите команду (Восток)", _menuService.GetEasternTeamsMenu());
        }
        */
        // ==========================================================
        // ============      БЛОК ПРОГНОЗОВ (РЕЗУЛЬТАТЫ)   ==========
        // ==========================================================

        /// <summary>
        /// Обрабатывает кнопку «Прогнозы» в разделе результатов.
        /// Callback: results_predictions_{matchId}.
        /// Вызывает бизнес-логику в ResultsService и отображает данные.
        /// </summary>
        /// <param name="chatId">ID Telegram-чата.</param>
        /// <param name="messageId">Редактируемое сообщение.</param>
        /// <param name="callback">Callback-строка с matchId.</param>
        public async Task HandleMatchPredictions(long chatId, int messageId, string callback)
        {
            Log.Information("[HandleMatchPredictions] chatId={ChatId}, callback={Callback}", chatId, callback);

            // ==== 1. matchId ====
            var matchId = callback.Replace("results_predictions_", "");

            // ==== 2. Матч ====
            var match = await _resultsService.GetResultByIdAsync(matchId);
            if (match == null)
            {
                await _messageService.SendTextAsync(chatId, "Матч не найден.");
                return;
            }

            // ==== 3. Прогнозы (С РЕЗУЛЬТАТАМИ: WIN / LOSE / DRAW) ====
            var predictions = await _matchStatsRepository.GetPredictionsByMatchIdAsync(matchId);
            // ↑ должен возвращать Prediction.Result

            if (predictions == null || !predictions.Any())
            {
                await _messageService.SendTextAsync(chatId, "Прогнозы отсутствуют.");
                return;
            }

            Log.Information(
                "[HandleMatchPredictions] Найдено {Count} прогнозов. matchId={MatchId}",
                predictions.Count(),
                matchId
            );

            // ==== 4. HTML (РЕЖИМ РЕЗУЛЬТАТОВ) ====
            var builder = new MatchPredictionPosterHtmlBuilder(_config);

            string html = builder.Build(
                predictions,
                match.HomeTeamName,
                match.AwayTeamName,
                PredictionPosterMode.Result   // 🔥 КЛЮЧЕВОЕ ОТЛИЧИЕ
            );

            // ==== 5. PNG ====
            var renderer = new HtmlToImageRenderer();
            byte[] png = await renderer.RenderAsync(html, 1100, 900);

            await using var ms = new MemoryStream(png);

            // ==== 6. Клавиатура "Назад к результатам" ====
            var menu = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "⬅️ Назад (К матчу)",
                    $"result_{matchId}"
                )
            });

            // ==== 7. Фото + клавиатура одним сообщением ====
            await _messageService.SendPhotoWithKeyboardAsync(chatId, ms, menu);

            Log.Information("[HandleMatchPredictions] Картинка прогнозов с результатами отправлена успешно");
        }
    }
}
