using Serilog;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBOT.Application.Telegram
{
    /// <summary>
    /// Сервис для работы с Telegram API.
    /// Отвечает за создание клиента и запуск получения апдейтов.
    /// </summary>
    public class TelegramClientService
    {
        public ITelegramBotClient Client { get; }

        /// <summary>
        /// Конструктор. Инициализирует Telegram Bot Client из конфигурации.
        /// </summary>
        /// <param name="config">Конфигурация приложения (appsettings.json).</param>
        /// <exception cref="ArgumentNullException">Если токен не найден.</exception>
        public TelegramClientService(IConfiguration config)
        {
            var token = config["Telegram:Token"];
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token), "Telegram bot token is not configured");

            Client = new TelegramBotClient(token);

            Log.Information("TelegramClientService инициализирован.");
        }

        /// <summary>
        /// Запускает приём апдейтов от Telegram.
        /// </summary>
        /// <param name="updateHandler">Делегат для обработки входящих апдейтов.</param>
        /// <param name="errorHandler">Делегат для обработки ошибок.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        public void StartReceiving(
            Func<ITelegramBotClient, Update, CancellationToken, Task> updateHandler,
            Func<ITelegramBotClient, Exception, CancellationToken, Task> errorHandler,
            CancellationToken cancellationToken)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() // слушаем все типы апдейтов
            };

            Client.StartReceiving(
                updateHandler,
                errorHandler,
                receiverOptions,
                cancellationToken
            );

            Log.Information("Запущен процесс получения апдейтов от Telegram.");
        }
    }
}
