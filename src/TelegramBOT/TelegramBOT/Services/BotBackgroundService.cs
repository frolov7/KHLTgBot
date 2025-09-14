using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBOT.Handlers;

namespace TelegramBOT.Services
{
    /// <summary>
    /// Фоновый сервис, управляющий жизненным циклом Telegram-бота.
    /// Использует TelegramClientService для получения апдейтов и вызывает CommandHandler.
    /// </summary>
    public class BotBackgroundService : BackgroundService
    {
        private readonly ITelegramBotClient _client;
        private readonly IServiceScopeFactory _scopeFactory;

        /// <summary>
        /// Конструктор фонового сервиса.
        /// </summary>
        /// <param name="telegramClient">Сервис Telegram-клиента.</param>
        /// <param name="scopeFactory">Фабрика для создания DI-скоупов.</param>
        public BotBackgroundService(ITelegramBotClient client, IServiceScopeFactory scopeFactory)
        {
            _client = client;
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// Основной метод фонового сервиса. Запускает обработку апдейтов.
        /// </summary>
        /// <param name="stoppingToken">Токен отмены.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Information("Запущен метод {Method}", nameof(ExecuteAsync));

            _client.StartReceiving(
                async (client, update, ct) =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<CommandHandler>();
                    await handler.HandleAsync(update);
                },
                (client, exception, ct) =>
                {
                    Log.Error(exception, "Ошибка при обработке обновления.");
                    return Task.CompletedTask;
                },
                cancellationToken: stoppingToken
            );

            Log.Information("Бот запущен. Ожидает сообщения...");

            await Task.Delay(-1, stoppingToken);
        }
    }
}
