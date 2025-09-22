using TelegramBOT.Services;
using TelegramBOT.UI;

namespace TelegramBOT.Handlers
{
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

        public async Task ShowStatsMenu(long chatId)
        {
            // Заглушка
        }

        public async Task HandleStats(long chatId, string callback)
        {
            // Заглушка
        }

        public async Task HandleHistory(long chatId, string callback)
        {
            // Заглушка
        }
    }
}
