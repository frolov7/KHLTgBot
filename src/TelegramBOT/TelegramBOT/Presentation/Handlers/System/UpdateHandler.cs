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
            // 1️⃣ Отправляем сообщение об обновлении и убираем клавиатуру
            var updatingMsg = await _messageService.RemoveKeyboardAsync(chatId, "⏳ Выполняется обновление всех данных...");

            bool success = true;

            try
            {
                Log.Information("🔄 Начато глобальное обновление данных.");

                success = await _resultsService.UpdateResultsAsync();

                Log.Information("✅ Глобальное обновление данных завершено успешно.");
            }
            catch (Exception ex)
            {
                success = false;
                Log.Error(ex, "❌ Ошибка при выполнении глобального обновления данных.");
            }

            // Удаляем сообщение
            try
            {
                await _messageService.DeleteMessageAsync(chatId, updatingMsg.MessageId);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Не удалось удалить сообщение о ходе обновления.");
            }

            // Отправляем финальное уведомление
            var message = success
                ? "✅ Все данные успешно обновлены!"
                : "❌ Произошла ошибка при обновлении данных. Попробуйте позже.";

            await _messageService.SendKeyboardAsync(chatId, message, _menuService.GetMainMenu());
        }

    }
}
