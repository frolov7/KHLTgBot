using TelegramBOT.Services;
using TelegramBOT.UI;

namespace TelegramBOT.Handlers
{
    public class NavigationHandler
    {
        private readonly MessageService _messageService;
        private readonly MenuService _menuService;

        public NavigationHandler(MessageService messageService, MenuService menuService)
        {
            _messageService = messageService;
            _menuService = menuService;
        }

        public async Task ShowMainMenu(long chatId)
        {
            await _messageService.SendKeyboardAsync(chatId, "Добро пожаловать! Выберите действие.", _menuService.GetMainMenu());
        }
    }
}
