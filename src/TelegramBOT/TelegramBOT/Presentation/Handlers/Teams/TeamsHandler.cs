using TelegramBOT.Presentation.UI.Menus.Teams;
using TelegramBOT.Application.Telegram;
using TelegramBOT.Presentation.UI.Menus.Results;
using TelegramBOT.Presentation.UI;
using TelegramBOT.Application.Teams;

namespace TelegramBOT.Presentation.Handlers.Teams
{
    public class TeamsHandler
    {
        private readonly MenuService _menuService;
        private readonly MessageService _messageService;
        private readonly TeamsService _teamsService;

        public TeamsHandler(MenuService menuService, MessageService messageService, TeamsService teamsService)
        {
            _menuService = menuService;
            _messageService = messageService;
            _teamsService = teamsService;
        }

        public async Task ShowTeamsMenu(long chatId)
        {
            var keyboard = _menuService.GetTeamsConferenceMenu();
            await _messageService.SendTextWithKeyboardAsync(chatId, "Выберите конференцию", keyboard);
        }

        public async Task ShowTeamsByConference(long chatId, string conference)
        {
            var keyboard = _menuService.GetTeamsByConferenceMenu(conference);
            await _messageService.SendTextWithKeyboardAsync(chatId, "Выберите конференцию", keyboard);
        }

        public async Task HandleTeamSelected(long chatId, string teamCode)
        {
            await _teamsService.SendTeamCardAsync(chatId, teamCode);
        }
    }
}
