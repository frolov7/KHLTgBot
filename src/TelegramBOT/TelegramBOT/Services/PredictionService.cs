using Microsoft.EntityFrameworkCore;
using System.Text;
using TelegramBOT.Data;
using TelegramBOT.Models;

namespace TelegramBOT.Services
{
    /// <summary>
    /// Сервис для работы с прогнозами: загрузка из БД и предоставление данных по матчам.
    /// </summary>
    public class PredictionService
    {
        private readonly AppDbContext _db;
        private readonly MessageService _messageService;

        public PredictionService(AppDbContext db, MessageService messageService)
        {
            _db = db;
            _messageService = messageService;
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

            var source = parts[1];   // legalbet / metaratings и т.д.
            var matchId = parts[2];

            var prediction = await GetPredictionAsync(matchId, source);

            if (prediction == null)
            {
                await _messageService.SendTextAsync(chatId, $"❌ Прогноз от {source} не найден.");
                return;
            }

            var msg = new StringBuilder()
                .AppendLine($"📌 <b>{prediction.Match?.HomeTeamName} vs {prediction.Match?.AwayTeamName}</b>")
                .AppendLine()
                .AppendLine($"🏠 Анализ домашней: {prediction.HomeTeamText}")
                .AppendLine()
                .AppendLine($"🚌 Анализ гостевой: {prediction.AwayTeamText}")
                .AppendLine()
                .AppendLine($"🔮 Основной прогноз: {prediction.MainPrediction}")
                .AppendLine($"💡 Альтернатива: {prediction.AltPrediction}")
                .AppendLine($"📊 Примерный счёт: {prediction.Score}")
                .AppendLine($"📝 {prediction.GeneralText}")
                .ToString();

            await _messageService.SendTextAsync(chatId, msg);
        }
    }
}
