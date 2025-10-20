using TelegramBOT.Models;
using TelegramBOT.Services.Utils;

namespace TelegramBOT.Services.Predictions
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
        // ===============      БЛОК ФОРМИРОВАНИЯ ТЕКСТА     =========
        // ==========================================================

        /// <summary>
        /// Формирует детализированный текст прогноза от одного источника.
        /// </summary>
        /// <param name="prediction">Объект прогноза, содержащий текстовые поля анализа и предсказания.</param>
        /// <returns>Отформатированная строка для отправки пользователю в Telegram.</returns>
        public string BuildPredictionMessage(Prediction prediction)
        {
            var home = prediction.Match != null
                ? _mappingService.Map("TeamNames", prediction.Match.HomeTeamName)
                : "Хозяева";

            var away = prediction.Match != null
                ? _mappingService.Map("TeamNames", prediction.Match.AwayTeamName)
                : "Гости";

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
            var home = match != null ? _mappingService.Map("TeamNames", match.HomeTeamName) : "Хозяева";
            var away = match != null ? _mappingService.Map("TeamNames", match.AwayTeamName) : "Гости";

            msg.AppendLine($"<b>{home} vs {away}</b>\n");

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
