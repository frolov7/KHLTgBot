using TelegramBOT.Domain.Entities.Matches;

namespace TelegramBOT.Domain.Interfaces
{
    /// <summary>
    /// Интерфейс для получения турнирной таблицы КХЛ на основе матчей из БД.
    /// </summary>
    public interface IStandingsRepository
    {
        /// <summary>
        /// Возвращает все завершённые матчи для расчёта турнирной таблицы.
        /// </summary>
        public Task<IEnumerable<Match>> GetAllMatchesAsync();
    }
}
