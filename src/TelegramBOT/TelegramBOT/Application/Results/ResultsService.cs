using Serilog;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Domain.Models;
using TelegramBOT.Infrastructure.Scripts;
using TelegramBOT.Infrastructure.Telegram;
using TelegramBOT.Presentation.UI;

namespace TelegramBOT.Application.Results
{
    /// <summary>
    /// Сервис бизнес-логики для работы с результатами матчей.
    /// Отвечает за получение, обновление и форматирование данных о результатах.
    /// </summary>
    public class ResultsService
    {
        private readonly IResultsRepository _resultRepository;
        private readonly ScriptService _scriptService;
        private readonly MappingService _mappingService;
        private readonly MessageService _messageService;

        public ResultsService(
            IResultsRepository resultRepository,
            ScriptService scriptService,
            MappingService mappingService,
            MessageService messageService)
        {
            _resultRepository = resultRepository;
            _scriptService = scriptService;
            _mappingService = mappingService;
            _messageService = messageService;
        }

        // ==========================================================
        // ===============      БЛОК ЗАГРУЗКИ ДАННЫХ     =============
        // ==========================================================

        /// <summary>
        /// Загружает и отправляет результаты матчей за указанную дату с inline-кнопками.
        /// </summary>
        public async Task SendResultsAsync(long chatId, DateTime date)
        {
            var matches = await _resultRepository.GetResultsByDateAsync(date);

            if (!matches.Any())
            {
                await _messageService.SendTextAsync(chatId, "Результатов не найдено");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"⚡ Результаты матчей за {date:dd.MM.yyyy}\n");

            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var match in matches)
            {
                var (home, away) = _mappingService.MapTeamNames(match);
                var status = _mappingService.Map("MatchStatuses", match.Status);

                sb.AppendLine($"⏰ {match.MatchDate:HH:mm} (МСК)");
                sb.AppendLine($"{home} <b>{match.HomeScore} : {match.AwayScore}</b> {away}");
                sb.AppendLine(status);
                sb.AppendLine();

                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData($"{home} vs {away}", $"result_{match.MatchId}")
                });
            }

            var keyboard = new InlineKeyboardMarkup(buttons);
            await _messageService.SendTextWithKeyboardAsync(chatId, sb.ToString(), keyboard);
        }

        /// <summary>
        /// Возвращает результаты всех матчей определённой команды.
        /// </summary>
        /// <param name="teamName">Название команды.</param>
        /// <returns>Список матчей с участием указанной команды.</returns>
        public async Task<IEnumerable<Match>> GetResultsByTeamAsync(string teamName)
        {
            return await _resultRepository.GetResultsByTeamAsync(teamName);
        }

        /// <summary>
        /// Возвращает результат конкретного матча по его идентификатору.
        /// </summary>
        /// <param name="matchId">Идентификатор матча.</param>
        /// <returns>Объект <see cref="Match"/> или <c>null</c>, если матч не найден.</returns>
        public async Task<Match?> GetResultByIdAsync(string matchId)
        {
            return await _resultRepository.GetResultByIdAsync(matchId);
        }

        /// <summary>
        /// Возвращает видеообзор для указанного матча, если он есть в БД.
        /// </summary>
        /// <param name="matchId"> Идентификатор матча для поиска в БД (значение <see cref="Match.MatchId"/>),
        /// </param>
        /// <returns> Экземпляр <see cref="MatchVideo"/> при наличии записи; 
        /// иначе <see langword="null"/>.
        /// </returns>
        public async Task<MatchVideo?> GetMatchVideoAsync(string matchId)
        {
            return await _resultRepository.GetMatchVideoByMatchIdAsync(matchId);
        }

        // ==========================================================
        // ===============      ОБНОВЛЕНИЕ ДАННЫХ       =============
        // ==========================================================

        /// <summary>
        /// Запускает обновление данных о результатах и прогнозах.
        /// </summary>
        /// <returns><c>true</c>, если обновление завершилось успешно; иначе <c>false</c>.</returns>
        public async Task<bool> UpdateResultsAsync()
        {
            try
            {
                await _scriptService.RunScrapersAsync();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка при обновлении данных (результаты/прогнозы)");
                return false;
            }
        }

        // ==========================================================
        // ===============      ФОРМАТИРОВАНИЕ ДАННЫХ   =============
        // ==========================================================

        /// <summary>
        /// Формирует красивое текстовое сообщение с результатами матчей.
        /// </summary>
        /// <param name="matches">Список матчей.</param>
        /// <param name="date">Дата (опционально).</param>
        /// <param name="teamName">Название команды (если нужно показать результаты конкретной команды).</param>
        /// <returns>Готовый текст сообщения для Telegram.</returns>
        public string BuildResultsMessage(IEnumerable<Match> matches, DateTime? date = null, string? teamName = null)
        {
            if (matches == null || !matches.Any())
                return "Результатов не найдено";

            var sb = new StringBuilder();

            // Заголовок
            if (date != null)
                sb.AppendLine($"⚡ Результаты матчей за {date:dd.MM.yyyy}\n");
            else
                sb.AppendLine("⚡ Результаты матчей:\n");

            foreach (var match in matches)
            {
                var (homeName, awayName) = _mappingService.MapTeamNames(match);
                string statusText;

                // Победа / поражение (если указана команда)
                if (teamName != null && match.Status != "SCHEDULED" &&
                    !(match.Status.Contains("PERIOD") || match.Status == "OVERTIME" || match.Status == "PENALTIES"))
                {
                    bool isHome = match.HomeTeamName == teamName;
                    int homeScore = match.HomeScore ?? 0;
                    int awayScore = match.AwayScore ?? 0;
                    bool isWin = isHome && homeScore > awayScore || !isHome && awayScore > homeScore;

                    var shortStatus = _mappingService.Map("MatchStatusesShort", match.Status);
                    statusText = isWin
                        ? $"🏆 Победа ({shortStatus})"
                        : $"❌ Поражение ({shortStatus})";
                }
                else
                {
                    statusText = _mappingService.Map("MatchStatuses", match.Status);
                }

                // Время или дата
                if (date != null)
                    sb.AppendLine($"⏰ {match.MatchDate:HH:mm} (МСК)");
                else
                    sb.AppendLine($"📅 {match.MatchDate:dd.MM.yyyy}");

                // Счёт или анонс
                if (match.Status != "SCHEDULED")
                    sb.AppendLine($"{homeName} <b>{match.HomeScore ?? 0} : {match.AwayScore ?? 0}</b> {awayName}");
                else
                    sb.AppendLine($"{homeName} vs {awayName}");

                sb.AppendLine(statusText);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Загружает данные матча, локализует названия команд, подтягивает видеообзор
        /// и отправляет пользователю inline-меню для раздела «Результаты».
        /// </summary>
        /// <param name="chatId">ID Telegram-чата.</param>
        /// <param name="matchId">Уникальный идентификатор матча.</param>
        /// <param name="menuService">Фасад построения меню.</param>
        public async Task SendResultMatchMenuAsync(long chatId, string matchId, MenuService menuService)
        {
            var match = await _resultRepository.GetResultByIdAsync(matchId);
            if (match == null)
            {
                await _messageService.SendTextAsync(chatId, "Матч не найден.");
                return;
            }

            var (home, away) = _mappingService.MapTeamNames(match);

            // Получаем видеообзор (может быть null)
            var video = await GetMatchVideoAsync(matchId);

            var title = $"⚡ <b>{home}</b> vs <b>{away}</b>";
            await _messageService.SendKeyboardAsync(chatId, title, menuService.GetResultMatchMenu(match, video));
        }

        /// <summary>
        /// Загружает и отправляет пользователю результаты конкретной команды.
        /// Обрабатывает callback, извлекает название команды, выполняет обратное
        /// отображение в английское имя (для поиска в БД) и отправляет локализованное сообщение.
        /// </summary>
        /// <param name="chatId">ID Telegram-чата.</param>
        /// <param name="callback">Строка callback (например, "team_SKA St. Petersburg").</param>
        public async Task SendTeamResultsAsync(long chatId, string callback)
        {
            // Извлекаем локализованное имя из callback
            var localizedName = callback.Replace("team_", "");

            // Преобразуем в английское имя для работы с базой данных
            var englishName = _mappingService.ReverseMap("TeamNames", localizedName);

            // Загружаем результаты команды
            var results = await _resultRepository.GetResultsByTeamAsync(englishName);

            if (results == null || !results.Any())
            {
                await _messageService.SendTextAsync(chatId, $"Результаты команды <b>{localizedName}</b> не найдены.");
                return;
            }

            // Формируем текстовое сообщение и отправляем
            var message = BuildResultsMessage(results, null, englishName);
            await _messageService.SendTextAsync(chatId, message);
        }
    }
}
