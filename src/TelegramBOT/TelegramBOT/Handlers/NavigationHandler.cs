using TelegramBOT.Services;
using TelegramBOT.UI;

namespace TelegramBOT.Handlers
{
    /// <summary>
    /// Обработчик навигации по боту.
    /// Отвечает за переходы в главное меню и отображение базовых навигационных экранов.
    /// </summary>
    public class NavigationHandler
    {
        private readonly MessageService _messageService;
        private readonly MenuService _menuService;

        public NavigationHandler(MessageService messageService, MenuService menuService)
        {
            _messageService = messageService;
            _menuService = menuService;
        }

        /// <summary>
        /// Отображает главное меню с приветственным сообщением.
        /// </summary>
        /// <param name="chatId">ID чата, куда будет отправлено меню.</param>
        public async Task ShowMainMenu(long chatId)
        {
            await _messageService.SendKeyboardAsync(
                chatId,
                "Выберите действие.",
                _menuService.GetMainMenu()
            );
        }
    }
}
