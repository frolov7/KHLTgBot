using Microsoft.EntityFrameworkCore;
using System.Text;
using TelegramBOT.Data;
using TelegramBOT.Models;
using TelegramBOT.Utils;
using System.Linq;

namespace TelegramBOT.Services
{
    /// <summary>
    /// Сервис для работы с прогнозами: загрузка из БД и предоставление данных по матчам.
    /// </summary>
    public class PredictionService
    {
        private readonly AppDbContext _db;
        private readonly MessageService _messageService;
        private readonly MappingService _mappingService;

        public PredictionService(AppDbContext db, MessageService messageService, MappingService mappingService)
        {
            _db = db;
            _messageService = messageService;
            _mappingService = mappingService;
        }

        /// <summary>
        /// Получить прогноз по матчу и источнику.
        /// </summary>
        public async Task<Prediction?> GetPredictionAsync(string matchId, string source)
        {
            return await _db.Predictions
                .FirstOrDefaultAsync(p => p.MatchId == matchId && p.Source == source);
        }

        /// <summary>
        /// Показать прогноз от конкретного источника.
        /// </summary>
        public async Task ShowPredictionAsync(long chatId, string source, string matchId)
        {
            var prediction = await _db.Predictions
                .Include(p => p.Match)
                .FirstOrDefaultAsync(p => p.MatchId == matchId && p.Source == source);

            if (prediction == null)
            {
                await _messageService.SendTextAsync(chatId, $"❌ Прогноз от {source} не найден.");
                return;
            }

            var homeName = prediction.Match != null
                ? _mappingService.Map("TeamNames", prediction.Match.HomeTeamName)
                : "Хозяева";

            var awayName = prediction.Match != null
                ? _mappingService.Map("TeamNames", prediction.Match.AwayTeamName)
                : "Гости";

            var msg = new StringBuilder();

            msg.AppendLine($"<b>{homeName} vs {awayName}</b>");
            msg.AppendLine();

            if (!string.IsNullOrWhiteSpace(prediction.HomeTeamText))
            {
                msg.AppendLine($"📌 <b>Анализ команды {homeName}:</b>");
                msg.AppendLine(prediction.HomeTeamText.Trim());
                msg.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(prediction.AwayTeamText))
            {
                msg.AppendLine($"📌 <b>Анализ команды {awayName}:</b>");
                msg.AppendLine(prediction.AwayTeamText.Trim());
                msg.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(prediction.GeneralText))
            {
                msg.AppendLine($"📝 {prediction.GeneralText.Trim()}");
                msg.AppendLine();
            }

            msg.AppendLine($"🔮 <b>Основной прогноз:</b> {prediction.MainPrediction ?? "-"}");

            if (!string.IsNullOrWhiteSpace(prediction.AltPrediction))
                msg.AppendLine($"💡 <b>Альтернативный прогноз:</b> {prediction.AltPrediction}");

            if (!string.IsNullOrWhiteSpace(prediction.Score))
                msg.AppendLine($"📊 <b>Примерный счёт:</b> {prediction.Score}");

            msg.AppendLine();
            msg.AppendLine($"<b>Ссылка:</b> <a href=\"{prediction.Url}\">{prediction.Source}</a>");

            await SendInChunksAsync(chatId, msg.ToString());
        }

        /// <summary>
        /// Показать общий прогноз по всем сайтам.
        /// </summary>
        public async Task ShowSummaryAsync(long chatId, string matchId)
        {
            var predictions = await _db.Predictions
                .Include(p => p.Match)
                .Where(p => p.MatchId == matchId)
                .ToListAsync();

            if (predictions.Count == 0)
            {
                await _messageService.SendTextAsync(chatId, "Прогнозы по этому матчу не найдены.");
                return;
            }

            var match = predictions.First().Match;
            var homeName = match != null
                ? _mappingService.Map("TeamNames", match.HomeTeamName)
                : "Хозяева";

            var awayName = match != null
                ? _mappingService.Map("TeamNames", match.AwayTeamName)
                : "Гости";

            var msg = new StringBuilder();
            msg.AppendLine($"📊 <b>Общий прогноз</b>");
            msg.AppendLine($"<b>{homeName} vs {awayName}</b>");
            msg.AppendLine();

            // Сортируем источники по алфавиту — так аккуратнее
            foreach (var p in predictions.OrderBy(p => p.Source))
            {
                var main = string.IsNullOrWhiteSpace(p.MainPrediction) ? "-" : p.MainPrediction.Trim();
                var alt = string.IsNullOrWhiteSpace(p.AltPrediction) ? "" : $", {p.AltPrediction.Trim()}";

                msg.AppendLine($"<b>{p.Source}</b>: {main}{alt}");
            }

            await SendInChunksAsync(chatId, msg.ToString());
        }

        /// <summary>
        /// Вспомогательный метод: отправляет длинный текст частями.
        /// </summary>
        private async Task SendInChunksAsync(long chatId, string text)
        {
            const int maxLength = 4000;
            for (int i = 0; i < text.Length; i += maxLength)
            {
                var chunk = text.Substring(i, Math.Min(maxLength, text.Length - i));
                await _messageService.SendTextAsync(chatId, chunk);
            }
        }
    }
}
