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
    /// Сервис для работы с сообщениями Telegram.
    /// Содержит методы отправки текста, фото, клавиатур и вывода матчей.
    /// </summary>
    public class MessageService
    {
        private readonly ITelegramBotClient _client;
        private readonly MappingService _mappingService;

        public MessageService(ITelegramBotClient client, MappingService mappingService)
        {
            _client = client;
            _mappingService = mappingService;
        }

        // ================================
        // Базовые методы
        // ================================

        /// <summary>
        /// Отправить текстовое сообщение.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="text">Текст сообщения.</param>
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
        /// Отправить текстовое сообщение и вернуть объект сообщения для дальнейшего редактирования.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="text">Текст сообщения.</param>
        /// <returns>Объект <see cref="Message"/>.</returns>
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
        /// Отредактировать существующее сообщение.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="messageId">ID редактируемого сообщения.</param>
        /// <param name="newText">Новый текст.</param>
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
        /// Отправить фото по ссылке.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="url">Ссылка на изображение.</param>
        /// <param name="caption">Подпись (необязательно).</param>
        public async Task SendPhotoAsync(long chatId, string url, string caption = "")
        {
            await _client.SendPhoto(
                chatId,
                url,
                caption: caption,
                cancellationToken: CancellationToken.None
            );
        }

        // ================================
        // Методы для клавиатур
        // ================================

        /// <summary>
        /// Отправить сообщение с клавиатурой (ReplyKeyboard или InlineKeyboard).
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="text">Текст сообщения.</param>
        /// <param name="keyboard">Клавиатура.</param>
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
        /// Удалить кастомную клавиатуру и вернуть стандартную.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="text">Текст (по умолчанию "Клавиатура убрана").</param>
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
        /// Отправить сообщение с inline-клавиатурой.
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

        // ================================
        // Методы для матчей
        // ================================

        /// <summary>
        /// Отправить календарь матчей одним сообщением.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="matches">Список матчей.</param>
        /// <param name="fromDate">Начальная дата (необязательно).</param>
        /// <param name="toDate">Конечная дата (необязательно).</param>
        /// <param name="withButtons">Показать ли кнопки для выбора матчей.</param>
        public async Task SendCalendarAsync(long chatId, List<Match> matches, DateTime? fromDate = null, DateTime? toDate = null, bool withButtons = false)
        {
            if (matches == null || matches.Count == 0)
            {
                await SendTextAsync(chatId, "Матчей не найдено.");
                return;
            }

            var sb = new StringBuilder();

            if (fromDate != null && toDate != null && fromDate != toDate)
                sb.AppendLine($"🗓 Матчи с {fromDate:dd.MM.yyyy} по {toDate:dd.MM.yyyy}");
            else
                sb.AppendLine($"🗓 Матчи на {fromDate ?? DateTime.Today:dd.MM.yyyy}");

            sb.AppendLine("----------------------");

            var buttons = new List<InlineKeyboardButton[]>();

            foreach (var match in matches.OrderBy(m => m.MatchDate))
            {
                var homeName = _mappingService.Map("TeamNames", match.HomeTeamName);
                var awayName = _mappingService.Map("TeamNames", match.AwayTeamName);
                var status = _mappingService.Map("MatchStatuses", match.Status);

                sb.AppendLine($"⏰ {match.MatchDate:HH:mm} (МСК)");
                sb.AppendLine($"{homeName} vs {awayName}");
                sb.AppendLine(status);
                sb.AppendLine();

                if (withButtons)
                {
                    buttons.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            $"{homeName} vs {awayName}",
                            $"match_{match.MatchId}"
                        )
                    });
                }
            }

            if (withButtons)
            {
                var keyboard = new InlineKeyboardMarkup(buttons);
                await _client.SendMessage(
                    chatId: chatId,
                    text: sb.ToString(),
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard,
                    cancellationToken: CancellationToken.None
                );
            }
            else
            {
                await _client.SendMessage(
                    chatId: chatId,
                    text: sb.ToString(),
                    parseMode: ParseMode.Html,
                    cancellationToken: CancellationToken.None
                );
            }
        }

        /// <summary>
        /// Отправить результаты матчей одним сообщением.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="matches">Список матчей.</param>
        /// <param name="date">Дата (необязательно).</param>
        /// <param name="teamName">Название команды (если нужны персонализированные результаты).</param>
        public async Task SendResultsAsync(long chatId, List<Match> matches, DateTime? date = null, string? teamName = null)
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
                    // Победа/поражение только для завершённых матчей
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
                {
                    statusText = _mappingService.Map("MatchStatuses", match.Status);
                }

                // Дата или время
                if (date != null)
                    sb.AppendLine($"⏰ {match.MatchDate:HH:mm} (МСК)");
                else
                    sb.AppendLine($"📅 {match.MatchDate:dd.MM.yyyy}");

                // Счет или анонс
                if (match.Status != "SCHEDULED")
                    sb.AppendLine($"{homeName} <b>{match.HomeScore ?? 0} : {match.AwayScore ?? 0}</b> {awayName}");
                else
                    sb.AppendLine($"{homeName} vs {awayName}");

                sb.AppendLine(statusText);
                sb.AppendLine();
            }

            await SendTextAsync(chatId, sb.ToString());
        }

        /// <summary>
        /// Отправить историю последних матчей для двух команд.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="match">Матч (объект Match).</param>
        /// <param name="homeResults">Список последних игр домашней команды.</param>
        /// <param name="awayResults">Список последних игр гостевой команды.</param>
        public async Task SendHistoryAsync(long chatId, Match match, List<Match> homeResults, List<Match> awayResults)
        {
            string GetMatchSuffix(string status)
            {
                if (string.IsNullOrEmpty(status)) return "";
                if (status.Contains("OVERTIME")) return " (ОТ)";
                if (status.Contains("PENALTIES")) return " (Б)";
                return "";
            }

            string GetResultEmoji(Match m, string teamName)
            {
                if (m.HomeScore == null || m.AwayScore == null) return "";

                bool isHome = m.HomeTeamName == teamName;
                int homeScore = m.HomeScore.Value;
                int awayScore = m.AwayScore.Value;

                bool isWin = (isHome && homeScore > awayScore) || (!isHome && awayScore > homeScore);
                return isWin ? "🏆" : "❌";
            }

            var homeName = _mappingService.Map("TeamNames", match.HomeTeamName);
            var awayName = _mappingService.Map("TeamNames", match.AwayTeamName);

            var sb = new StringBuilder();
            sb.AppendLine("⚔️ Прошлые игры:\n");

            // Домашняя команда
            sb.AppendLine($"{homeName} (последние 10):");
            foreach (var m in homeResults)
            {
                sb.AppendLine(
                    $"{GetResultEmoji(m, match.HomeTeamName)} " +
                    $"({m.MatchDate:dd.MM}) " +
                    $"{_mappingService.Map("TeamNames", m.HomeTeamName)} " +
                    $"{m.HomeScore}:{m.AwayScore}{GetMatchSuffix(m.Status)} " +
                    $"{_mappingService.Map("TeamNames", m.AwayTeamName)}"
                );
            }

            // Гостевая команда
            sb.AppendLine($"\n{awayName} (последние 10):");
            foreach (var m in awayResults)
            {
                sb.AppendLine(
                    $"{GetResultEmoji(m, match.AwayTeamName)} " +
                    $"({m.MatchDate:dd.MM}) " +
                    $"{_mappingService.Map("TeamNames", m.HomeTeamName)} " +
                    $"{m.HomeScore}:{m.AwayScore}{GetMatchSuffix(m.Status)} " +
                    $"{_mappingService.Map("TeamNames", m.AwayTeamName)}"
                );
            }

            await SendTextAsync(chatId, sb.ToString());
        }

        /// <summary>
        /// Отправить историю очных встреч двух команд.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="homeTeam">Название домашней команды (EN).</param>
        /// <param name="awayTeam">Название гостевой команды (EN).</param>
        /// <param name="matches">Список очных матчей.</param>
        public async Task SendHeadToHeadAsync(long chatId, string homeTeam, string awayTeam, List<Match> matches)
        {
            if (matches == null || matches.Count == 0)
            {
                await SendTextAsync(chatId, "Эти команды ещё не встречались.");
                return;
            }

            string GetMatchSuffix(string status)
            {
                if (string.IsNullOrEmpty(status)) return "";
                if (status.Contains("OVERTIME")) return " (ОТ)";
                if (status.Contains("PENALTIES")) return " (Б)";
                return "";
            }

            var homeName = _mappingService.Map("TeamNames", homeTeam);
            var awayName = _mappingService.Map("TeamNames", awayTeam);

            var sb = new StringBuilder();
            sb.AppendLine($"Очные встречи команд (последние {matches.Count}):\n");

            foreach (var m in matches)
            {
                sb.AppendLine(
                    $"📅 {m.MatchDate:dd.MM.yyyy} " +
                    $"{_mappingService.Map("TeamNames", m.HomeTeamName)} " +
                    $"{m.HomeScore}:{m.AwayScore}{GetMatchSuffix(m.Status)} " +
                    $"{_mappingService.Map("TeamNames", m.AwayTeamName)}"
                );
            }

            await SendTextAsync(chatId, sb.ToString());
        }

        /// <summary>
        /// Отправить список прогнозов на матч.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="match">Матч.</param>
        /// <param name="predictions">Список прогнозов.</param>
        public async Task SendPredictionsAsync(long chatId, Match match, List<Prediction> predictions)
        {
            if (predictions == null || predictions.Count == 0)
            {
                await SendTextAsync(chatId, "❌ Прогнозов на этот матч пока нет.");
                return;
            }

            var homeName = _mappingService.Map("TeamNames", match.HomeTeamName);
            var awayName = _mappingService.Map("TeamNames", match.AwayTeamName);

            var sb = new StringBuilder();
            sb.AppendLine($"🔮 Прогнозы на матч:");
            sb.AppendLine($"<b>{homeName} vs {awayName}</b>");
            sb.AppendLine();

            foreach (var p in predictions)
            {
                string resultEmoji = p.Result switch
                {
                    "WIN" => "✅",
                    "LOSE" => "❌",
                    "DRAW" => "➖",
                    _ => "❓"
                };

                // Собираем строку прогноза
                sb.AppendLine($"<b>{p.Source}</b>: {p.MainPrediction ?? p.GeneralText}");
            }

            // ⚡ Разбиваем длинное сообщение на куски до 4096 символов
            var text = sb.ToString();
            const int maxLength = 4096;

            for (int i = 0; i < text.Length; i += maxLength)
            {
                var chunk = text.Substring(i, Math.Min(maxLength, text.Length - i));
                await SendTextAsync(chatId, chunk);
            }
        }


    }
}
