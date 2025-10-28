using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBOT.Infrastructure.Telegram
{
    public class MessageService
    {
        private readonly ITelegramBotClient _client;

        public MessageService(ITelegramBotClient client)
        {
            _client = client;
        }

        // ================================
        // Отправка сообщений
        // ================================

        /// <summary>
        /// Отправляет простое текстовое сообщение.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="text">Текст сообщения.</param>
        public async Task<Message> SendTextAsync(long chatId, string text, bool removeKeyboard = false)
        {
            var markup = removeKeyboard ? new ReplyKeyboardRemove() : null;

            return await _client.SendMessage(
                chatId: chatId,
                text: text,
                parseMode: ParseMode.Html,
                replyMarkup: markup,
                cancellationToken: CancellationToken.None
            );
        }

        /// <summary>
        /// Отправляет сообщение и возвращает объект сообщения для дальнейшей обработки.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="text">Текст сообщения.</param>
        /// <returns>Объект <see cref="Message"/>.</returns>
        public async Task<Message> SendTextWithResponseAsync(long chatId, string text)
        {
            return await _client.SendMessage(
                chatId: chatId,
                text: text,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }

        /// <summary>
        /// Отправляет фото с подписью.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="url">URL изображения.</param>
        /// <param name="caption">Подпись к изображению (необязательно).</param>
        public async Task SendPhotoAsync(long chatId, string url, string? caption = null)
        {
            await _client.SendPhoto(
                chatId: chatId,
                photo: url,
                caption: caption,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }

        // ================================
        // Редактирование сообщений
        // ================================

        /// <summary>
        /// Редактирует существующее сообщение.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="messageId">ID редактируемого сообщения.</param>
        /// <param name="newText">Новый текст сообщения.</param>
        public async Task EditMessageAsync(long chatId, int messageId, string newText)
        {
            await _client.EditMessageText(
                chatId: chatId,
                messageId: messageId,
                text: newText,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }

        // ================================
        // Клавиатуры
        // ================================

        /// <summary>
        /// Отправляет сообщение с клавиатурой (ReplyKeyboardMarkup или InlineKeyboardMarkup).
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="text">Текст сообщения.</param>
        /// <param name="keyboard">Клавиатура (Reply или Inline).</param>
        public async Task SendKeyboardAsync(long chatId, string text, ReplyMarkup? keyboard)
        {
            await _client.SendMessage(
                chatId: chatId,
                text: text,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: CancellationToken.None
            );
        }

        /// <summary>
        /// Отправляет inline-клавиатуру с сообщением.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="text">Текст сообщения.</param>
        /// <param name="keyboard">Inline-клавиатура.</param>
        public async Task SendTextWithKeyboardAsync(long chatId, string text, InlineKeyboardMarkup keyboard)
        {
            await _client.SendMessage(
                chatId: chatId,
                text: text,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: CancellationToken.None
            );
        }

        /// <summary>
        /// Убирает пользовательскую клавиатуру (возвращает стандартную).
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="text">Текст уведомления (по умолчанию "Клавиатура убрана").</param>
        public async Task<Message> RemoveKeyboardAsync(long chatId, string text)
        {
            return await _client.SendMessage(
                chatId: chatId,
                text: text,
                parseMode: ParseMode.Html,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: CancellationToken.None
            );
        }

        // ================================
        // Удаление сообщений
        // ================================

        /// <summary>
        /// Удаляет сообщение по ID.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="messageId">ID удаляемого сообщения.</param>
        public async Task DeleteMessageAsync(long chatId, int messageId)
        {
            await _client.DeleteMessage(
                chatId: chatId,
                messageId: messageId,
                cancellationToken: CancellationToken.None
            );
        }

        // ================================
        // Редактирование сообщений
        // ================================

        /// <summary>
        /// Редактирует сообщение по ID.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="messageId">ID редактируемого сообщения.</param>
        /// <param name="newText">измененное сообщения.</param>
        public async Task EditMessageTextAsync(long chatId, int messageId, string newText)
        {
            await _client.EditMessageText(
                chatId: chatId,
                messageId: messageId,
                text: newText,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }
    }
}
