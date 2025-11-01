using PuppeteerSharp;
using System.Text;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Domain.Models;
using TelegramBOT.Infrastructure.Telegram;
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

        public MatchStatsService(
            IMatchStatsServiceRepository matchStatsRepository,
            IResultsRepository resultsRepository,
            MappingService mappingService,
            MessageService messageService)
        {
            _matchStatsRepository = matchStatsRepository;
            _resultsRepository = resultsRepository;
            _mappingService = mappingService;
            _messageService = messageService;
        }

        // ==========================================================
        // ============      ОЧНЫЕ ВСТРЕЧИ КОМАНД       ============
        // ==========================================================

        /// <summary>
        /// Загружает очные встречи и отправляет пользователю результаты сыгранных матчей.
        /// </summary>
        public async Task SendHeadToHeadAsync(long chatId, string matchId)
        {
            var match = await _matchStatsRepository.GetMatchByIdAsync(matchId);
            if (match == null)
            {
                await _messageService.SendTextAsync(chatId, "Матч не найден.");
                return;
            }

            // Получаем очные встречи через репозиторий
            var matches = await _matchStatsRepository.GetHeadToHeadMatchesAsync(match.HomeTeamName, match.AwayTeamName);
            if (!matches.Any())
            {
                await _messageService.SendTextAsync(chatId, "Эти команды ещё не встречались.");
                return;
            }

            var (home, away) = _mappingService.MapTeamNames(match);
            var sb = new StringBuilder();
            sb.AppendLine($"<b>Игры между собой {home} и {away}:</b>\n");

            BuildMatchList(sb, matches, match.HomeTeamName, home, includeOutcome: false);

            await _messageService.SendTextAsync(chatId, sb.ToString());

            // После списка — вернуть inline-меню матча
            var menu = new MatchMenuBuilder().Build(match);
            await _messageService.SendTextWithKeyboardAsync(chatId, $"{home} vs {away}", menu);
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

            var (home, away) = _mappingService.MapTeamNames(match);
            var sb = new StringBuilder();
            sb.AppendLine("<b>Последние матчи команд:</b>\n");

            // ------------------ Домашняя команда ------------------
            sb.AppendLine($"<b>{home}</b> (последние {homeResults.Count}):");
            BuildMatchList(sb, homeResults, match.HomeTeamName, home, includeOutcome: true);
            sb.AppendLine();

            // ------------------ Гостевая команда ------------------
            sb.AppendLine($"<b>{away}</b> (последние {awayResults.Count}):");
            BuildMatchList(sb, awayResults, match.AwayTeamName, away, includeOutcome: true);

            await _messageService.SendTextAsync(chatId, sb.ToString());

            // Меню матча (возврат)
            var menu = new MatchMenuBuilder().Build(match);
            await _messageService.SendTextWithKeyboardAsync(chatId, $"{home} vs {away}", menu);
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
        private void BuildMatchList(
            StringBuilder sb,
            IEnumerable<Match> matches,
            string teamName,
            string mappedName,
            bool includeOutcome = true
            )
        {
            foreach (var m in matches)
            {
                // 🏆 или ❌
                var emoji = includeOutcome ? GetMatchOutcomeEmoji(m, teamName) : "📅";
                var (home, away) = _mappingService.MapTeamNames(m);

                // Определяем соперника
                bool isHome = m.HomeTeamName == teamName;
                var opponent = isHome ? away : home;

                // Добавляем уточнение статуса (ОТ / Бул)
                string extraStatus = m.Status switch
                {
                    "AFTER OVERTIME" => " (ОТ)",
                    "AFTER PENALTIES" => " (Бул)",
                    _ => string.Empty
                };

                // Формируем строку результата
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
            int homeScore = match.HomeScore ?? 0;
            int awayScore = match.AwayScore ?? 0;

            bool isWin = isHome && homeScore > awayScore || !isHome && awayScore > homeScore;

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
            var match = await _matchStatsRepository.GetMatchByIdAsync(matchId);
            if (match == null)
            {
                await _messageService.SendTextAsync(chatId, "Матч не найден.");
                return;
            }

            var predictions = await _matchStatsRepository.GetPredictionsByMatchIdAsync(matchId);
            if (!predictions.Any())
            {
                await _messageService.SendTextAsync(chatId, "Прогнозов пока нет.");
                return;
            }

            var (home, away) = _mappingService.MapTeamNames(match);
            var text = $"🔮 Прогнозы на матч <b>{home}</b> vs <b>{away}</b>";

            var menu = new PredictionsMenuBuilder().Build(matchId);
            await _messageService.SendTextWithKeyboardAsync(chatId, text, menu);
        }

        // ==========================================================
        // ============      СОБЫТИЯ МАТЧА               ============
        // ==========================================================

        /// <summary>
        /// Загружает события матча (голы, удаления и т.д.) и отправляет пользователю.
        /// </summary>
        public async Task SendMatchEventsAsync(long chatId, string matchId)
        {
            var match = await _matchStatsRepository.GetMatchByIdAsync(matchId);
            if (match == null)
            {
                await _messageService.SendTextAsync(chatId, "Матч не найден.");
                return;
            }

            var events = (await _matchStatsRepository.GetMatchEventsAsync(matchId)).ToList();
            if (!events.Any())
            {
                await _messageService.SendTextAsync(chatId, "❌ События для этого матча пока недоступны.");
                return;
            }

            // 1) HTML
            var html = MatchEventsHtmlBuilder.Build(match, events, _mappingService);

            // 2) PNG
            var image = await RenderHtmlToImageAsync(html);

            // 3) Отправка
            var (home, away) = _mappingService.MapTeamNames(match);
            await _messageService.SendPhotoAsync(chatId, image, $"{home} vs {away}");

            // 4) Вернуть меню матча
            var menu = new MatchMenuBuilder().Build(match);
            await _messageService.SendTextWithKeyboardAsync(chatId, $"{home} vs {away}", menu);
        }

        /// <summary>Рендерит произвольный HTML в PNG через PuppeteerSharp.</summary>
        private async Task<Stream> RenderHtmlToImageAsync(string html)
        {
            var fetcher = new BrowserFetcher();
            await fetcher.DownloadAsync();

            var options = new LaunchOptions { Headless = true, Args = new[] { "--no-sandbox" } };
            using var browser = await Puppeteer.LaunchAsync(options);
            using var page = await browser.NewPageAsync();

            await page.SetViewportAsync(new ViewPortOptions { Width = 1200, Height = 10 }); // ширина под макет
            await page.SetContentAsync(html, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle0 } });

            // фон
            await page.EvaluateExpressionAsync("document.body.style.background = '#121212'");

            var ss = await page.ScreenshotStreamAsync(new ScreenshotOptions { Type = ScreenshotType.Png, FullPage = true });
            var ms = new MemoryStream();
            await ss.CopyToAsync(ms);
            ms.Position = 0;
            return ms;
        }
    }
}
