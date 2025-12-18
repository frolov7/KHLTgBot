using PuppeteerSharp;
using Serilog;
using System.Text;
using TelegramBOT.Application.MatchEvents;
using TelegramBOT.Application.Telegram;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Entities.Matches;
using TelegramBOT.Domain.Entities.MatchEvents;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Presentation.Rendering.Html;
using TelegramBOT.Presentation.Rendering.Html.Calendar;
using TelegramBOT.Presentation.UI.Menus.Calendar;
using TelegramBOT.Presentation.UI.Menus.Predictions;

namespace TelegramBOT.Application.MatchStats
{
    public class MatchStatsService
    {
        private readonly IMatchStatsServiceRepository _matchStatsRepository;
        private readonly IResultsRepository _resultsRepository;
        private readonly MappingService _mappingService;
        private readonly MessageService _messageService;
        private readonly IConfiguration _config;

        public MatchStatsService(
            IMatchStatsServiceRepository matchStatsRepository,
            IResultsRepository resultsRepository,
            MappingService mappingService,
            MessageService messageService,
            IConfiguration config)
        {
            _matchStatsRepository = matchStatsRepository;
            _resultsRepository = resultsRepository;
            _mappingService = mappingService;
            _messageService = messageService;
            _config = config;
        }

        // ==========================================================
        // ============      ОЧНЫЕ ВСТРЕЧИ КОМАНД       ============
        // ==========================================================

        /// <summary>
        /// Загружает очные встречи и отправляет пользователю результаты сыгранных матчей.
        /// </summary>
        public async Task SendHeadToHeadAsync(long chatId, string matchId)
        {
            Log.Information(
                "[SendHeadToHeadAsync] Старт. chatId={ChatId}, matchId={MatchId}",
                chatId, matchId);

            // 1. Загружаем основной матч
            var match = await _matchStatsRepository.GetMatchByIdAsync(matchId);
            if (match == null)
            {
                Log.Warning("[SendHeadToHeadAsync] Матч не найден. matchId={MatchId}", matchId);

                await _messageService.SendTextAsync(chatId, "Матч не найден.");
                return;
            }

            // 2. Загружаем очные встречи команд
            var h2hMatches = (await _matchStatsRepository.GetHeadToHeadMatchesAsync(match.HomeTeamName, match.AwayTeamName)).ToList();

            if (!h2hMatches.Any())
            {
                Log.Information("[SendHeadToHeadAsync] Очных встреч не найдено. {Home} vs {Away}", match.HomeTeamName, match.AwayTeamName);

                await _messageService.SendTextAsync(chatId, "Эти команды ещё не встречались.");
                return;
            }

            Log.Information("[SendHeadToHeadAsync] Найдено очных встреч: {Count}. {Home} vs {Away}", h2hMatches.Count, match.HomeTeamName, match.AwayTeamName);

            // 3. Загружаем голы по периодам для всех матчей
            var goalsByMatch = new Dictionary<string, List<PeriodGoals>>();

            foreach (var m in h2hMatches)
            {
                var goals = await _matchStatsRepository.GetGoalsByPeriodsAsync(m.MatchId);
                goalsByMatch[m.MatchId] = goals;
            }

            // 4. Генерация HTML
            var builder = new HeadToHeadPosterHtmlBuilder(_config, _mappingService);

            string html = builder.Build(
                h2hMatches,
                goalsByMatch);

            Log.Debug("[SendHeadToHeadAsync] HTML успешно сгенерирован");

            // 5. Рендер HTML → PNG
            var renderer = new HtmlToImageRenderer();
            byte[] png = await renderer.RenderAsync(html, 1024, 810);

            using var ms = new MemoryStream(png);

            // 6. Формирование меню матча
            var menu = new MatchMenuBuilder().Build(match);

            // 7. Отправка изображения в Telegram
            await _messageService.SendPhotoWithKeyboardAsync(chatId, ms, menu);

            Log.Information("[SendHeadToHeadAsync] Завершено успешно. chatId={ChatId}, matchId={MatchId}", chatId, matchId);
        }

        // ==========================================================
        // ============      ИСТОРИЯ ПОСЛЕДНИХ ИГР      ============
        // ==========================================================

        /// <summary>
        /// Загружает историю последних сыгранных матчей обеих команд и отправляет пользователю.
        /// Отображает исход каждого матча (🏆 победа / ❌ поражение).
        /// </summary>
        public async Task SendTeamsHistoryAsync(long chatId, string matchId)
        {
            Log.Information("[SendTeamsHistoryAsync] Старт. chatId={ChatId}, matchId={MatchId}", chatId, matchId);

            var match = await _matchStatsRepository.GetMatchByIdAsync(matchId);
            if (match == null)
            {
                await _messageService.SendTextAsync(chatId, "Матч не найден.");
                return;
            }

            var homeResults = (await _resultsRepository.GetResultsByTeamAsync(match.HomeTeamName)).ToList();
            var awayResults = (await _resultsRepository.GetResultsByTeamAsync(match.AwayTeamName)).ToList();

            if (!homeResults.Any() && !awayResults.Any())
            {
                await _messageService.SendTextAsync(chatId, "Нет данных по прошлым играм.");
                return;
            }

            // ====== собираем матчи для истории ======
            var matchesForHistory = homeResults
                .Concat(awayResults)
                .DistinctBy(m => m.MatchId)
                .Take(14)
                .ToList();

            // ====== грузим голы по периодам ======
            var goalsByMatch = new Dictionary<string, List<PeriodGoals>>();

            foreach (var m in matchesForHistory)
            {
                goalsByMatch[m.MatchId] =
                    await _matchStatsRepository.GetGoalsByPeriodsAsync(m.MatchId);
            }

            // ====== генерация HTML ======
            var builder = new HistoryPosterHtmlBuilder(_config);

            string html = builder.Build(
                match.HomeTeamName,
                match.AwayTeamName,
                homeResults.Take(7),
                awayResults.Take(7),
                goalsByMatch);

            // ====== рендер ======
            var renderer = new HtmlToImageRenderer();
            byte[] png = await renderer.RenderAsync(html, 1024, 900);

            using var ms = new MemoryStream(png);

            var menu = new MatchMenuBuilder().Build(match);

            await _messageService.SendPhotoWithKeyboardAsync(chatId, ms, menu);
        }

        /// <summary>
        /// Формирует текстовый список матчей для отображения в сообщении Telegram.
        /// Универсальный метод для истории последних игр и очных встреч.
        /// </summary>
        /// <param name="sb">StringBuilder, в который добавляется форматированный список матчей.</param>
        /// <param name="matches">Коллекция матчей, которые необходимо вывести.</param>
        /// <param name="teamName">Системное имя команды (из базы данных), относительно которой строится список.</param>
        /// <param name="mappedName">Отображаемое (человекочитаемое) имя команды с эмодзи.</param>
        /// <param name="includeOutcome">Если true — добавляются эмодзи исходов (🏆 / ❌). Используется для последних матчей команд.</param>
        private void BuildMatchList(StringBuilder sb, IEnumerable<Match> matches, string teamName, string mappedName, bool includeOutcome = true)
        {
            foreach (var m in matches)
            {
                var emoji = includeOutcome ? GetMatchOutcomeEmoji(m, teamName) : "📅";
                var (home, away) = _mappingService.MapTeamNames(m);

                bool isHome = m.HomeTeamName == teamName;
                var opponent = isHome ? away : home;

                string extraStatus = m.Status switch
                {
                    "AFTER OVERTIME" => " (ОТ)",
                    "AFTER PENALTIES" => " (Бул)",
                    _ => string.Empty
                };

                var line = isHome
                    ? $"{emoji} {m.MatchDate:dd.MM} — {mappedName} <b>{m.HomeScore}:{m.AwayScore}</b>{extraStatus} {opponent}"
                    : $"{emoji} {m.MatchDate:dd.MM} — {opponent} <b>{m.HomeScore}:{m.AwayScore}</b>{extraStatus} {mappedName}";

                sb.AppendLine(line);
            }
        }

        /// <summary>
        /// Возвращает эмодзи исхода матча для указанной команды.
        /// </summary>
        private string GetMatchOutcomeEmoji(Match match, string teamName)
        {
            if (match.HomeScore == null || match.AwayScore == null)
                return "📅";

            bool isHome = match.HomeTeamName == teamName;
            bool isWin = (isHome && match.HomeScore > match.AwayScore) || (!isHome && match.AwayScore > match.HomeScore);

            return isWin ? "🏆" : "❌";
        }

        // ==========================================================
        // ============      ПРОГНОЗЫ НА МАТЧ            ============
        // ==========================================================

        /// <summary>
        /// Загружает прогнозы по матчу и отправляет меню с выбором источников.
        /// </summary>
        public async Task SendPredictionsAsync(long chatId, string matchId)
        {
            Log.Information("[SendPredictionsAsync] Старт. chatId={ChatId}, matchId={MatchId}", chatId, matchId);

            var match = await _matchStatsRepository.GetMatchByIdAsync(matchId);
            if (match == null)
            {
                Log.Information("[SendPredictionsAsync] Матч не найден. matchId={MatchId}", matchId);
                await _messageService.SendTextAsync(chatId, "Матч не найден.");
                return;
            }

            var predictions = await _matchStatsRepository.GetPredictionsByMatchIdAsync(matchId);
            if (!predictions.Any())
            {
                Log.Information("[SendPredictionsAsync] Прогнозы отсутствуют. matchId={MatchId}", matchId);
                await _messageService.SendTextAsync(chatId, "Прогнозов пока нет.");
                return;
            }

            Log.Information("[SendPredictionsAsync] Найдено {Count} прогнозов. matchId={MatchId}", predictions.Count(), matchId);

            // ==== 1. Команды ====
            var (home, away) = _mappingService.MapTeamNames(match);

            // ==== 2. HTML ====
            var builder = new MatchPredictionPosterHtmlBuilder(_config);
            string html = builder.Build(
                predictions,
                match.HomeTeamName,  // имя для файла
                match.AwayTeamName   // имя для файла
            );

            // ==== 3. PNG ====
            var renderer = new HtmlToImageRenderer();
            byte[] png = await renderer.RenderAsync(html, 1100, 900);

            using var ms = new MemoryStream(png);

            // ==== 4. Клавиатура ====
            var menu = new PredictionsMenuBuilder().Build(matchId);

            // ==== 5. Фото + клавиатура одним сообщением ====
            await _messageService.SendPhotoWithKeyboardAsync(chatId, ms, menu);

            Log.Information("[SendPredictionsAsync] Картинка прогнозов отправлена успешно.");
        }
    }
}
