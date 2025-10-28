using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Domain.Models;
using TelegramBOT.Infrastructure.Telegram;
using TelegramBOT.Presentation.Handlers.Calendar;
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

        public PredictionService(IPredictionRepository repository, MappingService mappingService)
        {
            _repository = repository;
            _mappingService = mappingService;
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
            => await _repository.GetPredictionAsync(matchId, source);

        /// <summary>
        /// Получает все прогнозы по указанному матчу из разных источников.
        /// </summary>
        /// <param name="matchId">Идентификатор матча.</param>
        /// <returns>Список прогнозов по данному матчу.</returns>
        public async Task<List<Prediction>> GetPredictionsForMatchAsync(string matchId)
            => await _repository.GetPredictionsForMatchAsync(matchId);

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
            // Проверяем возврат к матчу
            if (callback.StartsWith("back_to_match_"))
            {
                var matchId = callback.Replace("back_to_match_", "");
                if (messageId.HasValue)
                    await messageService.DeleteMessageAsync(chatId, messageId.Value);

                await calendarHandler.ShowMatchMenu(chatId, matchId);
                return;
            }

            // Разбираем callback-строку (ожидается: "predict_source_matchId")
            var parts = callback.Split('_');
            if (parts.Length < 3)
            {
                await messageService.SendTextAsync(chatId, "Неверный формат callback-запроса.");
                return;
            }

            var source = parts[1];
            var matchIdParsed = parts[2];

            if (source.Equals("общий прогноз", StringComparison.OrdinalIgnoreCase))
            {
                var predictions = await GetPredictionsForMatchAsync(matchIdParsed);
                var text = BuildSummaryMessage(predictions);
                await messageService.SendTextAsync(chatId, text);
            }
            else
            {
                var prediction = await GetPredictionAsync(matchIdParsed, source);
                if (prediction == null)
                {
                    await messageService.SendTextAsync(chatId, $"Прогноз от {source} не найден.");
                    return;
                }

                var text = BuildPredictionMessage(prediction);
                await messageService.SendTextAsync(chatId, text);
            }

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
        public string BuildSummaryMessage(IEnumerable<Prediction> predictions)
        {
            var allSources = new[]
            {
                "vseprosport", "vprognoze", "stavkatv", "betzona",
                "legalbet", "metaratings", "livesport"
            };

            var msg = new System.Text.StringBuilder();
            msg.AppendLine("📊 <b>Общий прогноз</b>");

            // Проверяем наличие матчей (на случай пустой коллекции)
            Match? match = predictions.FirstOrDefault()?.Match;

            string home = "Хозяева";
            string away = "Гости";

            if (match != null)
                (home, away) = _mappingService.MapTeamNames(match);

            msg.AppendLine($"<b>{home}</b> vs <b>{away}</b>\n");

            // Если вообще нет прогнозов — выводим только список источников с "-"
            if (!predictions.Any())
            {
                foreach (var src in allSources)
                    msg.AppendLine($"<b>{src}</b>: -");
                return msg.ToString();
            }

            // Для каждого источника проверяем наличие прогноза
            foreach (var src in allSources)
            {
                var p = predictions.FirstOrDefault(x =>
                    x.Source.Equals(src, StringComparison.OrdinalIgnoreCase));

                if (p == null)
                {
                    msg.AppendLine($"<b>{src}</b>: -");
                    continue;
                }

                // Берем основной и альтернативный прогнозы
                var main = !string.IsNullOrWhiteSpace(p.MainPrediction) ? p.MainPrediction.Trim() : "-";
                var alt = !string.IsNullOrWhiteSpace(p.AltPrediction) ? $", {p.AltPrediction.Trim()}" : "";

                msg.AppendLine($"<b>{src}</b>: {main}{alt}");
            }

            return msg.ToString();
        }
    }
}
