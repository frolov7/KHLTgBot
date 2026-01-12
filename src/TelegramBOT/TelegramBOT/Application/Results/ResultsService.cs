using Serilog;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBOT.Application.Predictions;
using TelegramBOT.Application.Telegram;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Entities.Matches;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Infrastructure.Scripts;
using TelegramBOT.Presentation.Rendering.Html;
using TelegramBOT.Presentation.Rendering.Html.Results;
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
        private readonly IMatchStatsServiceRepository _matchStatsRepository;
        private readonly ScriptService _scriptService;
        private readonly MappingService _mappingService;
        private readonly MessageService _messageService;
        private readonly PredictionService _predictionService;
        private readonly IConfiguration _config;

        public ResultsService(
            IResultsRepository resultRepository,
            IMatchStatsServiceRepository matchStatsRepository,
            ScriptService scriptService,
            MappingService mappingService,
            MessageService messageService,
            PredictionService predictionService,
            IConfiguration config)   // <-- добавили
        {
            _resultRepository = resultRepository;
            _matchStatsRepository = matchStatsRepository;
            _scriptService = scriptService;
            _mappingService = mappingService;
            _messageService = messageService;
            _predictionService = predictionService;
            _config = config;
        }

        // ==========================================================
        // ============             УТИЛИТЫ              ============
        // ==========================================================
        public bool TryParseCallbackDate(string callback, out DateTime date)
        {
            Log.Information("[TryParseCallbackDate] Входные данные: {Callback}", callback);

            var dateStr = callback.Replace("back_to_results_", "");
            var result = DateTime.TryParseExact(
                dateStr, "yyyyMMdd", null,
                System.Globalization.DateTimeStyles.None, out date
            );

            Log.Information("[TryParseCallbackDate] Распарсено: {Success}, Дата={Date}", result, date);
            return result;
        }

        // ==========================================================
        // ===============      БЛОК ЗАГРУЗКИ ДАННЫХ     =============
        // ==========================================================

        /// <summary>
        /// Загружает и отправляет результаты матчей за указанную дату с inline-кнопками.
        /// </summary>
        public async Task SendResultsAsync(long chatId, DateTime date)
        {
            Log.Information("[SendResultsAsync] Старт. chatId={ChatId}, date={Date}", chatId, date);

            var matches = await _resultRepository.GetResultsByDateAsync(date);

            if (!matches.Any())
            {
                await _messageService.SendTextAsync(chatId, "Результатов не найдено");
                return;
            }

            // === HTML Генерация ===
            var builder = new MatchdayResultsPosterHtmlBuilder(_config);
            var goalsByMatch = await _matchStatsRepository.GetGoalsByPeriodsForMatchesAsync(matches.Select(m => m.MatchId));

            string html = builder.Build(matches, date, goalsByMatch);

            // === Рендер ===
            var renderer = new HtmlToImageRenderer();
            byte[] png = await renderer.RenderAsync(html, 1024, 900);

            using var ms = new MemoryStream(png);

            // === Клавиатура (то же самое что было!) ===
            var buttons = matches
                .Select(m =>
                {
                    var (home, away) = _mappingService.MapTeamNames(m);
                    return new List<InlineKeyboardButton>
                    {
                InlineKeyboardButton.WithCallbackData($"{home} vs {away}", $"result_{m.MatchId}")
                    };
                })
                .ToList();

            var keyboard = new InlineKeyboardMarkup(buttons);

            await _messageService.SendPhotoWithKeyboardAsync(chatId, ms, keyboard);

            Log.Information("[SendResultsAsync] Завершено успешно");
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
            Log.Information("[UpdateResultsAsync] Старт обновления результатов");

            try
            {
                await _scriptService.RunScrapersAsync();
                Log.Information("[UpdateResultsAsync] Успешно завершено");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[UpdateResultsAsync] Ошибка обновления");
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
            Log.Information("[BuildResultsMessage] Старт. Matches={Count}", matches.Count());

            if (matches == null || !matches.Any())
            {
                Log.Information("[BuildResultsMessage] Нет данных");
                return "Результатов не найдено";
            }

            var sb = new StringBuilder();

            if (date != null)
                sb.AppendLine($"⚡ Результаты матчей за {date:dd.MM.yyyy}\n");
            else
                sb.AppendLine("⚡ Результаты матчей:\n");

            foreach (var match in matches)
            {
                var (homeName, awayName) = _mappingService.MapTeamNames(match);

                string statusText;

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

                if (date != null)
                    sb.AppendLine($"⏰ {match.MatchDate:HH:mm} (МСК)");
                else
                    sb.AppendLine($"📅 {match.MatchDate:dd.MM.yyyy}");

                if (match.Status != "SCHEDULED")
                    sb.AppendLine($"{homeName} <b>{match.HomeScore ?? 0} : {match.AwayScore ?? 0}</b> {awayName}");
                else
                    sb.AppendLine($"{homeName} vs {awayName}");

                sb.AppendLine(statusText);
                sb.AppendLine();
            }

            Log.Information("[BuildResultsMessage] Завершено успешно");
            return sb.ToString();
        }

        /// <summary>
        /// Загружает данные матча, локализует названия команд, подтягивает видеообзор
        /// и отправляет пользователю inline-меню для раздела «Результаты».
        /// </summary>
        /// <param name="chatId">ID Telegram-чата.</param>
        /// <param name="matchId">Уникальный идентификатор матча.</param>
        /// <param name="menuService">Фасад построения меню.</param>
        public async Task SendResultMatchMenuAsync(long chatId, string matchId, MenuService menuService, bool fromHeadToHead = false, string? originMatchId = null)
        {
            Log.Information("[SendResultMatchMenuAsync] Старт. chatId={ChatId}, matchId={MatchId}", chatId, matchId);

            var match = await _resultRepository.GetResultByIdAsync(matchId);
            if (match == null)
            {
                await _messageService.SendTextAsync(chatId, "Матч не найден.");
                return;
            }

            var video = await GetMatchVideoAsync(matchId);

            // === 1. Генерация HTML ===
            var builder = new SingleMatchResultPosterHtmlBuilder(_config, _mappingService);
            string html = builder.Build(match);

            var renderer = new HtmlToImageRenderer();
            byte[] pngBytes = await renderer.RenderAsync(html, 1024, 1191);

            await using var ms = new MemoryStream(pngBytes);

            var keyboard = menuService.GetResultMatchMenu(
                match,
                video,
                fromHeadToHead,
                originMatchId
            );

            await _messageService.SendPhotoWithKeyboardAsync(chatId, ms, keyboard);


            Log.Information("[SendResultMatchMenuAsync] Завершено успешно");
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
            Log.Information("[SendTeamResultsAsync] Старт. chatId={ChatId}, callback={Callback}", chatId, callback);

            var localizedName = callback.Replace("team_", "");
            var englishName = _mappingService.ReverseMap("TeamNames", localizedName);

            Log.Information("[SendTeamResultsAsync] Локализованное имя={Local}, Английское={English}",
                localizedName, englishName);

            var results = await _resultRepository.GetResultsByTeamAsync(englishName);

            if (results == null || !results.Any())
            {
                Log.Information("[SendTeamResultsAsync] Данных нет");
                await _messageService.SendTextAsync(chatId, $"Результаты команды <b>{localizedName}</b> не найдены.");
                return;
            }

            var message = BuildResultsMessage(results, null, englishName);
            await _messageService.SendTextAsync(chatId, message);

            Log.Information("[SendTeamResultsAsync] Завершено успешно");
        }

        // ==========================================================
        // ===============      БЛОК ПРОГНОЗОВ МАТЧА      =============
        // ==========================================================

        /// <summary>
        /// Получает, агрегирует и форматирует прогнозы для завершённого матча.
        /// Формирование полностью аналогично «Общему прогнозу» в календаре,
        /// но дополнительно добавляется стикер результата (WIN/LOSE/DRAW).
        /// </summary>
        /// <param name="matchId">Идентификатор матча.</param>
        /// <returns>HTML-текст для Telegram, содержащий список прогнозов.</returns>
        public async Task<string> BuildFinishedMatchPredictionsAsync(string matchId)
        {
            Log.Information("[BuildFinishedMatchPredictionsAsync] matchId={MatchId}", matchId);

            var predictions = await _predictionService.GetPredictionsForMatchAsync(matchId);

            if (predictions == null || predictions.Count == 0)
                return "🔮 Прогнозы отсутствуют.";

            var match = predictions.First().Match;
            string home = "Хозяева";
            string away = "Гости";

            if (match != null)
                (home, away) = _mappingService.MapTeamNames(match);

            string[] sources =
            {
                "vseprosport", "vprognoze", "stavkatv",
                "betzona", "legalbet", "metaratings", "livesport"
            };

            var sb = new StringBuilder();
            sb.AppendLine("🔮 <b>Прогнозы</b>");
            sb.AppendLine($"{home} vs {away}\n");

            foreach (var src in sources)
            {
                var p = predictions.FirstOrDefault(x =>
                    x.Source.Equals(src, StringComparison.OrdinalIgnoreCase));

                Log.Information(
                    "[PredictionCheck] matchId={MatchId}, source={Source}, found={Found}, result='{Result}', main='{Main}', alt='{Alt}'",
                    matchId,
                    src,
                    p != null,
                    p?.Result,
                    p?.MainPrediction,
                    p?.AltPrediction
                );
                string emoji = p?.Result switch
                {
                    "WIN" => "🟩",
                    "LOSE" => "🟥",
                    "DRAW" => "🟨",
                    _ => "⬜"
                };

                if (p == null)
                {
                    sb.AppendLine($"{emoji} <b>{src}</b>: -");
                    continue;
                }

                string main = string.IsNullOrWhiteSpace(p.MainPrediction) ? "-" : p.MainPrediction.Trim();
                string alt = string.IsNullOrWhiteSpace(p.AltPrediction) ? "" : $", {p.AltPrediction.Trim()}";

                sb.AppendLine($"{emoji} <b>{src}</b>: {main}{alt}");
            }

            return sb.ToString();
        }
    }
}
