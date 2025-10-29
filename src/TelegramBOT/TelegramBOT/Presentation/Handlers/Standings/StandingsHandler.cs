using Telegram.Bot;
using TelegramBOT.Application.Standings;
using TelegramBOT.Infrastructure.Telegram;
using TelegramBOT.Presentation.UI;

namespace TelegramBOT.Presentation.Handlers
{
    /// <summary>
    /// Обработчик, отвечающий за отображение меню "Таблицы" и турнирных таблиц.
    /// </summary>
    public class StandingsHandler
    {

        private readonly ITelegramBotClient _bot;
        private readonly MenuService _menuService;
        private readonly StandingsService _service;
        private readonly MessageService _messageService;

        public StandingsHandler(
            ITelegramBotClient bot,
            MenuService menuService,
            StandingsService service,
            MessageService messageService)
        {
            _bot = bot;
            _menuService = menuService;
            _service = service;
            _messageService = messageService;
        }

        // ==========================================================
        // ============       МЕНЮ "ТАБЛИЦЫ"             ============
        // ==========================================================

        /// <summary>
        /// Отображает пользователю меню выбора типа таблицы (например, турнирная таблица).
        /// </summary>
        /// <param name="chatId">ID чата Telegram, в который будет отправлено меню.</param>
        public async Task ShowTablesMenu(long chatId)
        {
            var keyboard = _menuService.GetTablesMenu();

            await _messageService.SendKeyboardAsync(
                chatId,
                "Выберите таблицу:",
                keyboard
            );
        }

        // ==========================================================
        // ============       МЕНЮ ВЫБОРА КОНФЕРЕНЦИИ    ============
        // ==========================================================

        /// <summary>
        /// Отображает пользователю меню выбора конференции (Восточная / Западная).
        /// </summary>
        /// <param name="chatId">ID чата Telegram, в который будет отправлено меню.</param>
        public async Task ShowConferenceSelection(long chatId)
        {
            var keyboard = _menuService.GetConferenceSelectionMenu();

            await _messageService.SendKeyboardAsync(
                chatId,
                "Выберите конференцию:",
                keyboard
            );
        }

        // ==========================================================
        // ============       ОТОБРАЖЕНИЕ ТАБЛИЦЫ        ============
        // ==========================================================

        /// <summary>
        /// Загружает и отправляет пользователю актуальную турнирную таблицу
        /// для выбранной конференции.
        /// </summary>
        /// <param name="chatId">ID чата Telegram, в который будет отправлено сообщение.</param>
        /// <param name="conference">Идентификатор конференции ("east" — Восточная, "west" — Западная).</param>
        public async Task ShowStandings(long chatId, string conference)
        {
            await _service.SendStandingsAsync(chatId, conference);
        }

        // ==========================================================
        // ============       ОБРАБОТКА КОМАНД            ============
        // ==========================================================

        /// <summary>
        /// Обрабатывает входящие команды, относящиеся к меню "Таблицы" и турнирной таблице.
        /// </summary>
        /// <param name="chatId">ID чата Telegram.</param>
        /// <param name="text">Текст команды (нажатой кнопки).</param>
        /// <returns>True, если команда была обработана; иначе false.</returns>
        public async Task<bool> HandleStandingsCommands(long chatId, string text)
        {
            switch (text)
            {
                // ---------- Конференции ----------
                case "🔸 Западная конференция":
                    await ShowStandings(chatId, "west");
                    return true;

                case "🔹 Восточная конференция":
                    await ShowStandings(chatId, "east");
                    return true;

                // ---------- Навигация ----------
                case "⬅️ Назад (Главное меню)":
                    await _messageService.SendKeyboardAsync(chatId, "Возврат в главное меню.", _menuService.GetMainMenu());
                    return true;

                case "⬅️ Назад (Таблица)":
                    await ShowTablesMenu(chatId);
                    return true;

                default:
                    return false;
            }
        }
    }
}
