using TelegramBOT.Services;
using TelegramBOT.UI;
using TelegramBOT.Utils;

namespace TelegramBOT.Handlers
{
    /// <summary>
    /// Обработчик статистики и связанных разделов матчей.
    /// Отвечает за показ статистики, истории встреч и прогнозов.
    /// </summary>
    public class StatsHandler
    {
        private readonly MessageService _messageService;
        private readonly MatchService _matchService;
        private readonly MenuService _menuService;
        private readonly MappingService _mappingService;

        public StatsHandler(MessageService messageService, MatchService matchService, MenuService menuService, MappingService mappingService)
        {
            _messageService = messageService;
            _matchService = matchService;
            _menuService = menuService;
            _mappingService = mappingService;
        }

        /// <summary>
        /// Показывает меню статистики для выбранного матча.
        /// (Игры между собой, Прошлые игры, Прогнозы).
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        public async Task ShowStatsMenu(long chatId)
        {
            // Заглушка: вывод меню статистики
            await _messageService.SendTextAsync(chatId, "📊 Меню статистики (заглушка)");
        }

        /// <summary>
        /// Обрабатывает кнопку «Игры между собой».
        /// Отображает историю очных встреч команд.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="callback">Callback-данные с matchId.</param>
        public async Task HandleStats(long chatId, string callback)
        {
            var matchId = callback.Replace("stats_", "");
            var match = await _matchService.GetMatchByIdAsync(matchId);

            if (match == null)
            {
                await _messageService.SendTextAsync(chatId, "❌ Матч не найден.");
                return;
            }

            var matches = await _matchService.GetHeadToHeadMatchesAsync(match.HomeTeamName, match.AwayTeamName);

            await _messageService.SendHeadToHeadAsync(chatId, match.HomeTeamName, match.AwayTeamName, matches);
        }

        /// <summary>
        /// Обрабатывает кнопку «Прошлые игры».
        /// Загружает историю матчей и передает её в MessageService.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="callback">Callback-данные с matchId.</param>
        public async Task HandleHistory(long chatId, string callback)
        {
            var matchId = callback.Replace("history_", "");

            var (match, homeResults, awayResults) = await _matchService.GetTeamsHistoryAsync(matchId);

            if (match == null)
            {
                await _messageService.SendTextAsync(chatId, "❌ Матч не найден.");
                return;
            }

            if (!homeResults.Any() && !awayResults.Any())
            {
                await _messageService.SendTextAsync(chatId, "Нет прошлых игр для этих команд.");
                return;
            }

            await _messageService.SendHistoryAsync(chatId, match, homeResults, awayResults);
        }

        /// <summary>
        /// Обрабатывает кнопку «Прогнозы».
        /// Отображение прогнозов
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="callback">Callback-данные с matchId.</param>
        public async Task HandlePredictions(long chatId, string callback)
        {
            var matchId = callback.Replace("predict_", "");
            // Заглушка: показать прогнозы
            await _messageService.SendTextAsync(chatId, $"🔮 Прогнозы (заглушка) для матча {matchId}");
        }
    }
}
