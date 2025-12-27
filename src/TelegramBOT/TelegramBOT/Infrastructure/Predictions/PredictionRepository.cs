using Microsoft.EntityFrameworkCore;
using TelegramBOT.Domain.Entities.Predictions;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Infrastructure.Data;

namespace TelegramBOT.Infrastructure.Predictions
{
    /// <summary>
    /// Репозиторий для работы с прогнозами (через Entity Framework Core).
    /// Отвечает за доступ к данным в БД и загрузку связанных матчей.
    /// </summary>
    public class PredictionRepository : IPredictionRepository
    {
        private readonly AppDbContext _db;

        public PredictionRepository(AppDbContext db)
        {
            _db = db;
        }

        // ==========================================================
        // ===============      БЛОК ЗАГРУЗКИ ПРОГНОЗОВ     =========
        // ==========================================================

        /// <summary>
        /// Возвращает прогноз по указанному матчу и источнику.
        /// </summary>
        /// <param name="matchId">Идентификатор матча.</param>
        /// <param name="source">Источник прогноза (например, "legalbet").</param>
        /// <returns>
        /// Объект <see cref="Prediction"/>, если найден; иначе <c>null</c>.
        /// </returns>
        public async Task<Prediction?> GetPredictionAsync(string matchId, string source)
        {
            return await _db.Predictions
                .Include(p => p.Match)
                .FirstOrDefaultAsync(p => p.MatchId == matchId && p.Source == source);
        }

        /// <summary>
        /// Возвращает список всех прогнозов для конкретного матча.
        /// </summary>
        /// <param name="matchId">Идентификатор матча.</param>
        /// <returns>
        /// Коллекция объектов <see cref="Prediction"/> с привязанными данными матча.
        /// </returns>
        public async Task<List<Prediction>> GetPredictionsForMatchAsync(string matchId)
        {
            return await _db.Predictions
                .Include(p => p.Match)
                .Where(p => p.MatchId == matchId)
                .ToListAsync();
        }

        /// <summary>
        /// Возвращает все прогнозы с привязанными матчами.
        /// Используется для аналитики и расчёта статистики.
        /// </summary>
        public async Task<List<Prediction>> GetAllAsync()
        {
            return await _db.Predictions
                .Include(p => p.Match)
                .ToListAsync();
        }
    }
}
