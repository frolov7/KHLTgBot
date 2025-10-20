using TelegramBOT.Models;

namespace TelegramBOT.Services.Predictions
{
    /// <summary>
    /// Интерфейс для абстракции доступа к прогнозам
    /// </summary>
    public interface IPredictionRepository
    {
        Task<Prediction?> GetPredictionAsync(string matchId, string source);
        Task<List<Prediction>> GetPredictionsForMatchAsync(string matchId);
    }
}
