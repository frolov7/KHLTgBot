using Serilog;
using Telegram.Bot;
using TelegramBOT.Presentation.Handlers;

namespace TelegramBOT.Application.Telegram
{
    /// <summary>
    /// Фоновый сервис, управляющий жизненным циклом Telegram-бота.
    /// Запускает получение апдейтов и делегирует их CommandHandler.
    /// </summary>
    public class BotBackgroundService : BackgroundService
    {
        private readonly ITelegramBotClient _client;
        private readonly IServiceScopeFactory _scopeFactory;

        public BotBackgroundService(ITelegramBotClient client, IServiceScopeFactory scopeFactory)
        {
            _client = client;
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// Основной цикл фонового сервиса Telegram-бота.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Information("{Service} запущен и готов к приему апдейтов", nameof(BotBackgroundService));

            try
            {
                _client.StartReceiving(
                    async (client, update, ct) =>
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var handler = scope.ServiceProvider.GetRequiredService<CommandHandler>();

                        try
                        {
                            await handler.HandleAsync(update);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Ошибка при обработке апдейта: {@Update}", update);
                        }
                    },
                    (client, exception, ct) =>
                    {
                        Log.Error(exception, "Ошибка в Telegram-потоке получения апдейтов");
                        return Task.CompletedTask;
                    },
                    cancellationToken: stoppingToken
                );

                Log.Information("Telegram-бот успешно запущен и ожидает сообщения...");

                // Блокируем поток до отмены
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Игнорируем — нормальное завершение
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Критическая ошибка в {Service}", nameof(BotBackgroundService));
            }
            finally
            {
                Log.Information("{Service} остановлен", nameof(BotBackgroundService));
            }
        }
    }
}
