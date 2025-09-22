using TelegramBOT.Services;
using TelegramBOT.UI;

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

        public StatsHandler(MessageService messageService, MatchService matchService, MenuService menuService)
        {
            _messageService = messageService;
            _matchService = matchService;
            _menuService = menuService;
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
            // Заглушка: показать статистику игр между командами
            await _messageService.SendTextAsync(chatId, $"📊 Игры между собой (заглушка) для матча {matchId}");
        }

        /// <summary>
        /// Обрабатывает кнопку «Прошлые игры».
        /// Отображает список последних матчей выбранных команд.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="callback">Callback-данные с matchId.</param>
        public async Task HandleHistory(long chatId, string callback)
        {
            var matchId = callback.Replace("history_", "");
            // Заглушка: показать прошлые игры
            await _messageService.SendTextAsync(chatId, $"⚔️ Прошлые игры (заглушка) для матча {matchId}");
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
