using Serilog;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Domain.Models;
using TelegramBOT.Infrastructure.Telegram;
using TelegramBOT.Presentation.Handlers.Calendar;
using TelegramBOT.Presentation.Rendering.Html;
using TelegramBOT.Presentation.UI.Menus.Predictions;

namespace TelegramBOT.Application.Predictions
{
    /// <summary>
    /// Сервис бизнес-логики прогнозов: получение, подготовка и форматирование данных.
    /// Не содержит логики Telegram — только бизнес-правила.
    /// </summary>
    public class PredictionService
    {
        private readonly IPredictionRepository _repository;
        private readonly MappingService _mappingService;
        private readonly IConfiguration _config;
        private readonly MessageService _messageService;

        public PredictionService(
            IPredictionRepository repository,
            MappingService mappingService,
            IConfiguration config,
            MessageService messageService)
        {
            _repository = repository;
            _mappingService = mappingService;
            _config = config;
            _messageService = messageService;
        }

        // ==========================================================
        // ===============      БЛОК ЗАГРУЗКИ ДАННЫХ     =============
        // ==========================================================

        /// <summary>
        /// Получает прогноз по конкретному матчу и источнику.
        /// </summary>
        /// <param name="matchId">Идентификатор матча.</param>
        /// <param name="source">Название источника прогноза (например, "legalbet").</param>
        /// <returns>Объект <see cref="Prediction"/> или <c>null</c>, если прогноз не найден.</returns>

        public async Task<Prediction?> GetPredictionAsync(string matchId, string source)
        {
            Log.Information("[GetPredictionAsync] matchId={MatchId}, source={Source}", matchId, source);
            return await _repository.GetPredictionAsync(matchId, source);
        }

        /// <summary>
        /// Получает все прогнозы по указанному матчу из разных источников.
        /// </summary>
        /// <param name="matchId">Идентификатор матча.</param>
        /// <returns>Список прогнозов по данному матчу.</returns>
        public async Task<List<Prediction>> GetPredictionsForMatchAsync(string matchId)
        {
            Log.Information("[GetPredictionsForMatchAsync] matchId={MatchId}", matchId);
            return await _repository.GetPredictionsForMatchAsync(matchId);
        }

        // ==========================================================
        // ===============      БЛОК ОБРАБОТКИ CALLBACK      =========
        // ==========================================================

        /// <summary>
        /// Обрабатывает выбор источника прогноза или возврат к меню матча.
        /// Выполняет получение прогноза(ов) и формирует сообщение для Telegram.
        /// </summary>
        /// <param name="chatId">Идентификатор Telegram-чата.</param>
        /// <param name="callback">Callback-строка, содержащая источник и ID матча.</param>
        /// <param name="messageService">Сервис для отправки и удаления сообщений.</param>
        /// <param name="calendarHandler">Хендлер календаря для возврата к меню матча.</param>
        /// <param name="messageId">Необязательный идентификатор сообщения для удаления.</param>
        public async Task HandlePredictionSelectedAsync(
             long chatId,
             string callback,
             MessageService messageService,
             CalendarHandler calendarHandler,
             int? messageId = null)
        {
            Log.Information("[HandlePredictionSelectedAsync] Старт. chatId={ChatId}, callback={Callback}", chatId, callback);

            // Возврат к матчу
            if (callback.StartsWith("back_to_match_"))
            {
                var matchId = callback.Replace("back_to_match_", "");

                Log.Information("[HandlePredictionSelectedAsync] Возврат к меню матча. matchId={MatchId}", matchId);

                if (messageId.HasValue)
                    await messageService.DeleteMessageAsync(chatId, messageId.Value);

                await calendarHandler.ShowMatchMenu(chatId, matchId);
                return;
            }

            // Разбор callback
            var parts = callback.Split('_');
            if (parts.Length < 3)
            {
                Log.Warning("[HandlePredictionSelectedAsync] Неверный формат callback: {Callback}", callback);
                await messageService.SendTextAsync(chatId, "Неверный формат callback-запроса.");
                return;
            }

            var source = parts[1];
            var matchIdParsed = parts[2];

            Log.Information("[HandlePredictionSelectedAsync] Выбран источник: {Source}, matchId={MatchId}", source, matchIdParsed);

            // Общий суммарный прогноз
            if (source.Equals("общий прогноз", StringComparison.OrdinalIgnoreCase))
            {
                var predictions = await GetPredictionsForMatchAsync(matchIdParsed);
                Log.Information("[HandlePredictionSelectedAsync] Получено {Count} прогнозов для общего анализа", predictions.Count);

                await SendSummaryPredictionAsync(chatId, predictions);
            }
            else
            {
                // Прогноз конкретного источника
                var prediction = await GetPredictionAsync(matchIdParsed, source);
                if (prediction == null)
                {
                    Log.Information("[HandlePredictionSelectedAsync] Прогноз отсутствует. source={Source}, matchId={MatchId}", source, matchIdParsed);
                    await messageService.SendTextAsync(chatId, $"Прогноз от {source} не найден.");
                    return;
                }

                Log.Information("[HandlePredictionSelectedAsync] Прогноз найден. source={Source}", source);

                var text = BuildPredictionMessage(prediction);
                await messageService.SendTextAsync(chatId, text);
            }

            Log.Information("[HandlePredictionSelectedAsync] Отправка меню выбора нового источника");
            var menu = new PredictionsMenuBuilder().Build(matchIdParsed);
            await messageService.SendKeyboardAsync(chatId, "Выберите другой источник:", menu);
        }

        // ==========================================================
        // ===============      БЛОК ФОРМИРОВАНИЯ ТЕКСТА     =========
        // ==========================================================

        /// <summary>
        /// Формирует детализированный текст прогноза от одного источника.
        /// </summary>
        /// <param name="prediction">Объект прогноза, содержащий текстовые поля анализа и предсказания.</param>
        /// <returns>Отформатированная строка для отправки пользователю в Telegram.</returns>
        public string BuildPredictionMessage(Prediction prediction)
        {
            Log.Information("[BuildPredictionMessage] Построение текста для источника {Source}", prediction.Source);

            string home, away;

            if (prediction.Match != null)
                (home, away) = _mappingService.MapTeamNames(prediction.Match);
            else
            {
                home = "Хозяева";
                away = "Гости";
            }

            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"<b>{home} vs {away}</b>\n");

            if (!string.IsNullOrWhiteSpace(prediction.HomeTeamText))
                sb.AppendLine($"📌 <b>Анализ {home}:</b>\n{prediction.HomeTeamText.Trim()}\n");

            if (!string.IsNullOrWhiteSpace(prediction.AwayTeamText))
                sb.AppendLine($"📌 <b>Анализ {away}:</b>\n{prediction.AwayTeamText.Trim()}\n");

            if (!string.IsNullOrWhiteSpace(prediction.GeneralText))
                sb.AppendLine($"📝 {prediction.GeneralText.Trim()}\n");

            sb.AppendLine($"🔮 <b>Основной прогноз:</b> {prediction.MainPrediction ?? "-"}");

            if (!string.IsNullOrWhiteSpace(prediction.AltPrediction))
                sb.AppendLine($"\n💡 <b>Альтернативный прогноз:</b> {prediction.AltPrediction}");

            if (!string.IsNullOrWhiteSpace(prediction.Score))
                sb.AppendLine($"\n📊 <b>Примерный счёт:</b> {prediction.Score}");

            if (!string.IsNullOrWhiteSpace(prediction.Url))
                sb.AppendLine($"\n🔗 <b>Источник:</b> <a href=\"{prediction.Url}\">{prediction.Source}</a>");

            return sb.ToString();
        }

        /// <summary>
        /// Формирует сводное сообщение по всем доступным источникам прогнозов.
        /// </summary>
        /// <param name="predictions">Коллекция прогнозов по одному матчу.</param>
        /// <returns>Форматированный текст со сводными прогнозами всех источников.</returns>
        public async Task SendSummaryPredictionAsync(long chatId, IEnumerable<Prediction> predictions)
        {
            Log.Information("[SendSummaryPredictionAsync] Сборка общего прогноза. SourcesCount={Count}", predictions.Count());

            var match = predictions.FirstOrDefault()?.Match;

            string home = "Хозяева";
            string away = "Гости";

            if (match != null)
                (home, away) = _mappingService.MapTeamNames(match);

            // === HTML Генерация ===
            var builder = new MatchPredictionPosterHtmlBuilder(_config);
            string html = builder.Build(predictions, home, away);

            // === Рендер ===
            var renderer = new HtmlToImageRenderer();
            byte[] png = await renderer.RenderAsync(html, 1100, 900);

            using var ms = new MemoryStream(png);

            // === Отправка изображения ===
            await _messageService.SendPhotoAsync(chatId, ms);

            Log.Information("[SendSummaryPredictionAsync] Картинка общего прогноза отправлена");
        }
    }
}
