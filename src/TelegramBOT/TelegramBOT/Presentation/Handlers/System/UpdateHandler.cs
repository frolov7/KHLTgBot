using Serilog;
using TelegramBOT.Application.Results;
using TelegramBOT.Infrastructure.Telegram;
using TelegramBOT.Presentation.UI;

namespace TelegramBOT.Presentation.Handlers.System
{
    /// <summary>
    /// Обработчик глобального обновления данных:
    /// результаты, прогнозы, статистика и другие разделы.
    /// </summary>
    public class UpdateHandler
    {
        private readonly MessageService _messageService;
        private readonly ResultsService _resultsService;
        private readonly MenuService _menuService;

        public UpdateHandler(
            MessageService messageService,
            ResultsService resultsService,
            MenuService menuService)
        {
            _messageService = messageService;
            _resultsService = resultsService;
            _menuService = menuService;
        }

        // ==========================================================
        // ============        ГЛОБАЛЬНОЕ ОБНОВЛЕНИЕ       ==========
        // ==========================================================

        /// <summary>
        /// Запускает процесс глобального обновления данных (результаты, прогнозы, статистика и т.д.).
        /// </summary>
        /// <param name="chatId">ID Telegram-чата, куда отправляется уведомление о статусе.</param>
        public async Task RunGlobalUpdate(long chatId)
        {
            Log.Information("[RunGlobalUpdate] Начало работы метода. chatId={ChatId}", chatId);

            var updatingMsg = await _messageService.RemoveKeyboardAsync(chatId, "Выполняется обновление всех данных...");

            bool success = true;

            try
            {
                Log.Information("[RunGlobalUpdate] Запуск обновления данных.");
                success = await _resultsService.UpdateResultsAsync();
                Log.Information("[RunGlobalUpdate] Завершено обновление данных. success={Success}", success);
            }
            catch (Exception ex)
            {
                success = false;
                Log.Error(ex, "[RunGlobalUpdate] Ошибка при выполнении обновления данных.");
            }

            // Удаление временного сообщения
            try
            {
                await _messageService.DeleteMessageAsync(chatId, updatingMsg.MessageId);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[RunGlobalUpdate] Не удалось удалить промежуточное сообщение.");
            }

            var message = success
                ? "Все данные успешно обновлены."
                : "При обновлении данных произошла ошибка. Попробуйте позже.";

            await _messageService.SendKeyboardAsync(chatId, message, _menuService.GetMainMenu());
        }
    }
}
