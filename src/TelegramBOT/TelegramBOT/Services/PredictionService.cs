using Microsoft.EntityFrameworkCore;
using System.Text;
using TelegramBOT.Data;
using TelegramBOT.Models;
using TelegramBOT.Utils;

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
        /// Показать прогноз пользователю.
        /// </summary>
        public async Task ShowPredictionAsync(long chatId, string callback)
        {
            var parts = callback.Split('_');
            if (parts.Length < 3)
            {
                await _messageService.SendTextAsync(chatId, "❌ Неверный формат запроса прогноза.");
                return;
            }

            var source = parts[1];
            var matchId = parts[2];

            var prediction = await _db.Predictions
                .Include(p => p.Match)   // грузим связанный матч
                .FirstOrDefaultAsync(p => p.MatchId == matchId && p.Source == source);

            if (prediction == null)
            {
                await _messageService.SendTextAsync(chatId, $"❌ Прогноз от {source} не найден.");
                return;
            }

            // Мапим названия команд через MappingService
            var homeName = prediction.Match != null
                ? _mappingService.Map("TeamNames", prediction.Match.HomeTeamName)
                : "Хозяева";

            var awayName = prediction.Match != null
                ? _mappingService.Map("TeamNames", prediction.Match.AwayTeamName)
                : "Гости";

            var msg = new StringBuilder();

            // Название матча
            msg.AppendLine($"<b>{homeName} vs {awayName}</b>");
            msg.AppendLine();

            // Анализ хозяев (если есть)
            if (!string.IsNullOrWhiteSpace(prediction.HomeTeamText))
            {
                msg.AppendLine($"📌 <b>Анализ команды {homeName}:</b>");
                msg.AppendLine(prediction.HomeTeamText.Trim());
                msg.AppendLine();
            }

            // Анализ гостей (если есть)
            if (!string.IsNullOrWhiteSpace(prediction.AwayTeamText))
            {
                msg.AppendLine($"📌 <b>Анализ команды {awayName}:</b>");
                msg.AppendLine(prediction.AwayTeamText.Trim());
                msg.AppendLine();
            }

            // Общий прогнозный текст (если есть)
            if (!string.IsNullOrWhiteSpace(prediction.GeneralText))
            {
                msg.AppendLine($"📝 {prediction.GeneralText.Trim()}");
                msg.AppendLine();
            }

            // Основной прогноз
            msg.AppendLine($"🔮 <b>Основной прогноз:</b> {prediction.MainPrediction ?? "-"}");

            // Альтернативный прогноз
            if (!string.IsNullOrWhiteSpace(prediction.AltPrediction))
                msg.AppendLine($"💡 <b>Альтернативный прогноз:</b> {prediction.AltPrediction}");

            // Примерный счёт
            if (!string.IsNullOrWhiteSpace(prediction.Score))
                msg.AppendLine($"📊 <b>Примерный счёт:</b> {prediction.Score}");
            
            msg.AppendLine();
            msg.AppendLine($"<b>Ссылка:</b> <a href=\"{prediction.Url}\">{prediction.Source}</a>");
            var text = msg.ToString();

            // Разбивка на куски (ограничение Telegram — 4096 символов)
            const int maxLength = 4000;
            for (int i = 0; i < text.Length; i += maxLength)
            {
                var chunk = text.Substring(i, Math.Min(maxLength, text.Length - i));
                await _messageService.SendTextAsync(chatId, chunk);
            }
        }
    }
}
