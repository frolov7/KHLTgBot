using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBOT.Models;
using TelegramBOT.Utils;

namespace TelegramBOT.Services
{
    /// <summary>
    /// Сервис для удобной работы с сообщениями Telegram.
    /// Содержит методы отправки текста, фото, клавиатур и матчей.
    /// </summary>
    public class MessageService
    {
        private readonly ITelegramBotClient _client;
        private readonly MappingService _mappingService;

        /// <summary>
        /// Конструктор получает клиент через TelegramClientService
        /// </summary>
        public MessageService(ITelegramBotClient client, MappingService mappingService)
        {
            _client = client;
            _mappingService = mappingService;
        }

        /// <summary>
        /// Отправить текстовое сообщение
        /// </summary>
        public async Task SendTextAsync(long chatId, string text)
        {
            await _client.SendMessage(
                chatId,
                text,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }

        /// <summary>
        /// Отправить текст и вернуть объект сообщения (для редактирования потом)
        /// </summary>
        public async Task<Message> SendTextWithResponseAsync(long chatId, string text)
        {
            return await _client.SendMessage(
                chatId,
                text,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }

        /// <summary>
        /// Редактировать текст сообщения
        /// </summary>
        public async Task EditMessageAsync(long chatId, int messageId, string newText)
        {
            await _client.EditMessageText(
                chatId,
                messageId,
                newText,
                parseMode: ParseMode.Html,             
                cancellationToken: CancellationToken.None
            );
        }

        /// <summary>
        /// Отправить фото по ссылке с подписью
        /// </summary>
        public async Task SendPhotoAsync(long chatId, string url, string caption = "")
        {
            await _client.SendPhoto(
                chatId,
                url,
                caption: caption,
                cancellationToken: CancellationToken.None
            );
        }

        /// <summary>
        /// Отправить клавиатуру (ReplyKeyboard или InlineKeyboard)
        /// </summary>
        public async Task SendKeyboardAsync(long chatId, string text, ReplyMarkup? keyboard)
        {
            await _client.SendMessage(
                chatId,
                text,
                replyMarkup: keyboard,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }

        /// <summary>
        /// Удалить кастомную клавиатуру и вернуть стандартную
        /// </summary>
        public async Task RemoveKeyboardAsync(long chatId, string text = "Клавиатура убрана")
        {
            await _client.SendMessage(
                chatId,
                text,
                replyMarkup: new ReplyKeyboardRemove(),
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None
            );
        }

        /// <summary>
        /// Отправить календарь матчей одним сообщением
        /// </summary>
        public async Task SendCalendarAsync(long chatId, List<Match> matches, DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (matches == null || matches.Count == 0)
            {
                await SendTextAsync(chatId, "Матчей не найдено.");
                return;
            }

            var sb = new StringBuilder();

            if (fromDate != null && toDate != null && fromDate != toDate)
                sb.AppendLine($"📅 Матчи с {fromDate:dd.MM.yyyy} по {toDate:dd.MM.yyyy}\n");
            else if (fromDate != null)
                sb.AppendLine($"📅 Матчи на {fromDate:dd.MM.yyyy}\n");
            else
                sb.AppendLine("📅 Список матчей:\n");

            foreach (var match in matches)
            {
                var homeName = _mappingService.Map("TeamNames", match.HomeTeamName);
                var awayName = _mappingService.Map("TeamNames", match.AwayTeamName);
                var status = _mappingService.Map("MatchStatuses", match.Status);

                sb.AppendLine($"⏰ {match.MatchDate:HH:mm} (МСК)");
                sb.AppendLine($"{homeName} vs {awayName}");
                sb.AppendLine($"📌 {status}");
                sb.AppendLine(); // пустая строка между матчами
            }

            await SendTextAsync(chatId, sb.ToString());
        }


        /// <summary>
        /// Отправить результаты матчей одним сообщением
        /// </summary>
        public async Task SendResultsAsync(long chatId, List<Match> matches, DateTime? date = null, string teamName = null)
        {
            if (matches == null || matches.Count == 0)
            {
                await SendTextAsync(chatId, "Результатов не найдено.");
                return;
            }

            var sb = new StringBuilder();

            if (date != null)
                sb.AppendLine($"⚡ Результаты матчей за {date:dd.MM.yyyy}\n");
            else
                sb.AppendLine("⚡ Результаты матчей:\n");

            foreach (var match in matches)
            {
                var homeName = _mappingService.Map("TeamNames", match.HomeTeamName);
                var awayName = _mappingService.Map("TeamNames", match.AwayTeamName);

                string statusText;

                if (teamName != null && match.Status != "SCHEDULED" &&
                    !(match.Status.Contains("PERIOD") || match.Status == "OVERTIME" || match.Status == "PENALTIES"))
                {
                    // показываем Победа/Поражение только для завершённых матчей
                    bool isHome = match.HomeTeamName == teamName;
                    int homeScore = match.HomeScore ?? 0;
                    int awayScore = match.AwayScore ?? 0;

                    bool isWin = (isHome && homeScore > awayScore) || (!isHome && awayScore > homeScore);

                    var shortStatus = _mappingService.Map("MatchStatusesShort", match.Status);

                    statusText = isWin
                        ? $"🏆 Победа ({shortStatus})"
                        : $"❌ Поражение ({shortStatus})";
                }
                else
                    statusText = _mappingService.Map("MatchStatuses", match.Status);

                // дата или время
                if (date != null)
                    sb.AppendLine($"⏰ {match.MatchDate:HH:mm} (МСК)");
                else
                    sb.AppendLine($"📅 {match.MatchDate:dd.MM.yyyy}");

                // счёт или анонс
                if (match.Status != "SCHEDULED")
                    sb.AppendLine($"{homeName} <b>{match.HomeScore ?? 0} : {match.AwayScore ?? 0}</b> {awayName}");
                else
                    sb.AppendLine($"{homeName} vs {awayName}");

                sb.AppendLine(statusText);
                sb.AppendLine();
            }

            await SendTextAsync(chatId, sb.ToString());
        }
    }
}
