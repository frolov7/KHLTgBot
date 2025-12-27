using TelegramBOT.Domain.Entities.Predictions;

namespace TelegramBOT.Domain.Interfaces
{
    /// <summary>
    /// Интерфейс для абстракции доступа к прогнозам
    /// </summary>
    public interface IPredictionRepository
    {
        Task<Prediction?> GetPredictionAsync(string matchId, string source);
        Task<List<Prediction>> GetPredictionsForMatchAsync(string matchId);

        /// <summary>
        /// Возвращает все прогнозы с привязанными матчами.
        /// Используется для аналитики и расчёта статистики.
        /// </summary>
        Task<List<Prediction>> GetAllAsync();
    }
}
