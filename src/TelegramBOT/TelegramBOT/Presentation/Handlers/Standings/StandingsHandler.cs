using Telegram.Bot;
using TelegramBOT.Application.Standings;
using TelegramBOT.Infrastructure.Telegram;
using TelegramBOT.Presentation.UI;

/// <summary>
/// Обработчик, отвечающий за отображение турнирных таблиц и меню выбора конференции.
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

    /// <summary>
    /// Отображает пользователю меню выбора конференции (Восточная / Западная).
    /// </summary>
    /// <param name="chatId">Идентификатор чата Telegram, в который будет отправлено меню.</param>
    public async Task ShowConferenceSelection(long chatId)
    {
        var keyboard = _menuService.GetConferenceSelectionMenu();

        await _messageService.SendKeyboardAsync(
            chatId,
            "Выберите конференцию:",
            keyboard
        );
    }

    /// <summary>
    /// Загружает и отправляет пользователю актуальную турнирную таблицу выбранной конференции.
    /// </summary>
    /// <param name="chatId">Идентификатор чата Telegram, в который будет отправлено сообщение.</param>
    /// <param name="conference">Идентификатор конференции ("east" — Восточная, "west" — Западная).</param>
    public async Task ShowStandings(long chatId, string conference)
    {
        await _service.SendStandingsAsync(chatId, conference);
    }
}
