using TelegramBOT.Infrastructure.Telegram;
using TelegramBOT.Presentation.UI;

namespace TelegramBOT.Presentation.Handlers.Navigation
{
    /// <summary>
    /// Обработчик навигации по боту.
    /// Отвечает за показ главного меню и базовые навигационные действия.
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

        // ==========================================================
        // ============          ГЛАВНОЕ МЕНЮ            ============
        // ==========================================================

        /// <summary>
        /// Отображает главное меню с приветственным текстом.
        /// </summary>
        /// <param name="chatId">Идентификатор Telegram-чата.</param>
        public async Task ShowMainMenu(long chatId)
        {
            await _messageService.SendKeyboardAsync(
                chatId,
                "Выберите действие.",
                _menuService.GetMainMenu()
            );
        }

        // ==========================================================
        // ============          ВОЗВРАТ НАЗАД            ============
        // ==========================================================

        /// <summary>
        /// Возвращает пользователя в главное меню (используется из других разделов).
        /// </summary>
        /// <param name="chatId">Идентификатор Telegram-чата.</param>
        public async Task BackToMainMenu(long chatId)
        {
            await _messageService.SendKeyboardAsync(
                chatId,
                "🔙 Возврат в главное меню.",
                _menuService.GetMainMenu()
            );
        }

        // ==========================================================
        // ============      ВРЕМЕННОЕ УВЕДОМЛЕНИЕ        ============
        // ==========================================================

        /// <summary>
        /// Отправляет короткое информационное сообщение без изменения клавиатуры.
        /// </summary>
        /// <param name="chatId">Идентификатор Telegram-чата.</param>
        /// <param name="text">Текст уведомления.</param>
        public async Task SendTemporaryNotice(long chatId, string text)
        {
            await _messageService.SendTextAsync(chatId, text);
        }
    }
}
