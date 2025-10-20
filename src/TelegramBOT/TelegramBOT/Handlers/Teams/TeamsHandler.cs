using TelegramBOT.Services.Core;
using TelegramBOT.Services.Teams;

namespace TelegramBOT.Handlers.Teams
{
    /// <summary>
    /// Обработчик Telegram-команд, связанных с командами.
    /// Отвечает за выбор команды и отображение её последних матчей.
    /// </summary>
    public class TeamsHandler
    {
        private readonly MessageService _messageService;
        private readonly TeamsService _teamsService;

        public TeamsHandler(MessageService messageService, TeamsService teamsService)
        {
            _messageService = messageService;
            _teamsService = teamsService;
        }

        // ==========================================================
        // ============     ОБРАБОТКА ВЫБОРА КОМАНД     ============
        // ==========================================================

        /// <summary>
        /// Обрабатывает выбор команды пользователем и отправляет её последние результаты.
        /// </summary>
        /// <param name="chatId">ID Telegram-чата.</param>
        /// <param name="text">Отображаемое имя команды.</param>
        public async Task HandleTeamCommand(long chatId, string text)
        {
            var teams = _teamsService.GetTeamsDictionary();

            if (!teams.TryGetValue(text, out var teamName))
            {
                await _messageService.SendTextAsync(chatId, "❌ Команда не найдена.");
                return;
            }

            var matches = await _teamsService.GetResultsByTeamAsync(teamName);
            var message = _teamsService.BuildTeamResultsMessage(matches, teamName);

            await _messageService.SendTextAsync(chatId, message);
        }
    }
}