using Microsoft.EntityFrameworkCore;
using TelegramBOT.Domain.Entities.Matches;
using TelegramBOT.Domain.Entities.MatchEvents;
using TelegramBOT.Domain.Entities.Predictions;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Infrastructure.Data;

namespace TelegramBOT.Infrastructure.MatchStats
{
    /// <summary>
    /// Репозиторий для работы с данными статистики матчей:
    /// очные встречи, история игр и прогнозы.
    /// </summary>
    public class MatchStatsServiceRepository : IMatchStatsServiceRepository
    {
        private readonly AppDbContext _db;

        public MatchStatsServiceRepository(AppDbContext db)
        {
            _db = db;
        }

        // ==========================================================
        // ============      ОЧНЫЕ ВСТРЕЧИ КОМАНД       =============
        // ==========================================================

        /// <summary>
        /// Возвращает список уже сыгранных очных встреч между двумя командами.
        /// Только завершённые матчи (с заполненным счётом).
        /// </summary>
        /// <param name="home">Название домашней команды.</param>
        /// <param name="away">Название гостевой команды.</param>
        /// <param name="limit">Максимальное количество матчей (по умолчанию — 10).</param>
        public async Task<IEnumerable<Match>> GetHeadToHeadMatchesAsync(string home, string away)
        {
            return await _db.Matches
                .Where(m =>
                    (
                        m.HomeTeamName == home && m.AwayTeamName == away ||
                        m.HomeTeamName == away && m.AwayTeamName == home
                    )
                    && m.MatchDate < DateTime.Today
                    && m.HomeScore != null
                    && m.AwayScore != null)
                .OrderBy(m => m.MatchDate)
                .ToListAsync();
        }

        // ==========================================================
        // ============      ПРОГНОЗЫ НА МАТЧИ           ============
        // ==========================================================

        /// <summary>
        /// Возвращает список прогнозов для указанного матча.
        /// </summary>
        public async Task<IEnumerable<Prediction>> GetPredictionsByMatchIdAsync(string matchId)
        {
            return await _db.Predictions
                .Where(p => p.MatchId == matchId)
                .ToListAsync();
        }

        // ==========================================================
        // ============      ПОЛУЧЕНИЕ МАТЧА             ============
        // ==========================================================

        /// <summary>
        /// Возвращает матч по идентификатору.
        /// </summary>
        public async Task<Match?> GetMatchByIdAsync(string matchId)
        {
            return await _db.Matches.FirstOrDefaultAsync(m => m.MatchId == matchId);
        }

        // ==========================================================
        // ============      СОБЫТИЯ МАТЧА               ============
        // ==========================================================

        /// <summary>
        /// Загружает список событий матча (голы, удаления и т.д.) из KHL_EVENTS.json.
        /// </summary>
        public async Task<IEnumerable<MatchEvent>> GetMatchEventsAsync(string matchId)
        {
            // Загружаем события матча с подгрузкой всех связанных таблиц
            var events = await _db.MatchEvents
                .Include(e => e.EventType)
                .Include(e => e.GoalDetail)
                .Include(e => e.GoalieChange)
                .Include(e => e.ShootoutDetail)
                .Include(e => e.Penalty)
                .Include(e => e.Team)
                .Where(e => e.MatchId == matchId)
                .AsNoTracking()
                .ToListAsync();

            // Сортировка по периоду и времени (всё в одном месте)
            return events
                .OrderBy(e =>
                    e.Period.StartsWith("1") ? 1 :
                    e.Period.StartsWith("2") ? 2 :
                    e.Period.StartsWith("3") ? 3 :
                    e.Period.StartsWith("OT", StringComparison.OrdinalIgnoreCase) ? 4 :
                    e.Period.StartsWith("SO", StringComparison.OrdinalIgnoreCase) ? 5 : 99)
                .ThenBy(e => e.Time)
                .ToList();
        }

        // ==========================================================
        // ============      СТАТИСТИКА ПО ПЕРИОДАМ        ===========
        // ==========================================================

        /// <summary>
        /// Возвращает количество заброшенных шайб по периодам
        /// для каждой команды в указанном матче.
        /// 
        /// Используются только события типа "GOAL".
        /// </summary>
        /// <param name="matchId">Идентификатор матча.</param>
        /// <returns>
        /// Список, содержащий количество голов команды в каждом периоде.
        /// </returns>
        public async Task<List<PeriodGoals>> GetGoalsByPeriodsAsync(string matchId)
        {
            return await _db.MatchEvents
                .Where(e =>
                    e.MatchId == matchId &&
                    e.EventType.Name == "GOAL" &&
                    e.Period != null &&
                    (e.Period == "1st period" ||
                     e.Period == "2nd period" ||
                     e.Period == "3rd period"))
                .GroupBy(e => new { e.Period, e.TeamId })
                .Select(g => new PeriodGoals
                {
                    Period = g.Key.Period!,
                    TeamId = g.Key.TeamId!.Value,
                    Goals = g.Count()
                })
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Возвращает количество заброшенных шайб по периодам
        /// для каждой команды сразу по нескольким матчам.
        /// 
        /// Ключ словаря — MatchId.
        /// Используются только события типа "GOAL".
        /// </summary>
        /// <param name="matchIds">Список идентификаторов матчей.</param>
        /// <returns>
        /// Словарь:
        ///   MatchId -> список PeriodGoals (период, команда, количество голов)
        /// </returns>
        public async Task<Dictionary<string, List<PeriodGoals>>> GetGoalsByPeriodsForMatchesAsync(IEnumerable<string> matchIds)
        {
            var ids = matchIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            if (!ids.Any())
                return new Dictionary<string, List<PeriodGoals>>();

            var goals = await _db.MatchEvents
                .Where(e =>
                    ids.Contains(e.MatchId) &&
                    e.EventType.Name == "GOAL" &&
                    e.Period != null &&
                    (e.Period == "1st period" ||
                     e.Period == "2nd period" ||
                     e.Period == "3rd period"))
                .GroupBy(e => new
                {
                    e.MatchId,
                    e.Period,
                    e.TeamId
                })
                .Select(g => new
                {
                    g.Key.MatchId,
                    PeriodGoal = new PeriodGoals
                    {
                        Period = g.Key.Period!,
                        TeamId = g.Key.TeamId!.Value,
                        Goals = g.Count()
                    }
                })
                .AsNoTracking()
                .ToListAsync();

            return goals
                .GroupBy(x => x.MatchId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.PeriodGoal).ToList()
                );
        }
    }
}
