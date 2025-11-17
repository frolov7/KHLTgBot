using PuppeteerSharp;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Domain.Models;
using TelegramBOT.Infrastructure.Telegram;
using TelegramBOT.Presentation.UI.Menus.Calendar;
using System.Text;
using Serilog;
using TelegramBOT.Presentation.Rendering.Html;
using TelegramBOT.Presentation.UI.Menus.Results;
using TelegramBOT.Application.Results;

namespace TelegramBOT.Application.MatchEvents
{
    public class MatchEventsService
    {
        private readonly IMatchStatsServiceRepository _matchStatsRepository;
        private readonly MappingService _mappingService;
        private readonly MessageService _messageService;
        private readonly ResultsService _resultsService;
        private readonly MatchEventsHtmlBuilder _htmlBuilder;

        public MatchEventsService(
            IMatchStatsServiceRepository matchStatsRepository,
            MappingService mappingService,
            ResultsService resultsService,
            MessageService messageService,
            MatchEventsHtmlBuilder htmlBuilder)
        {
            _matchStatsRepository = matchStatsRepository;
            _mappingService = mappingService;
            _resultsService = resultsService;
            _messageService = messageService;
            _htmlBuilder = htmlBuilder;
        }

        // ==========================================================
        // ============           СОБЫТИЯ МАТЧА          ============
        // ==========================================================

        /// <summary>
        /// Загружает события матча (голы, удаления, замены вратаря и т.д.)
        /// и отправляет пользователю картинку с визуализацией.
        /// </summary>
        public async Task SendMatchEventsAsync(long chatId, string matchId, string source)
        {
            Log.Information($"[MatchEventsService] Получен запрос на события матча: {matchId} (Источник: {source})");

            var match = await _matchStatsRepository.GetMatchByIdAsync(matchId);
            if (match == null)
            {
                await _messageService.SendTextAsync(chatId, "Матч не найден.");
                return;
            }

            var events = (await _matchStatsRepository.GetMatchEventsAsync(matchId)).ToList();
            if (!events.Any())
            {
                await _messageService.SendTextAsync(chatId, "События для этого матча пока недоступны");
                return;
            }

            // Генерируем HTML
            var html = _htmlBuilder.Build(match, events);

            // Рендерим HTML → PNG
            var image = await RenderHtmlToImageAsync(html);

            // Отправляем пользователю изображение
            var (home, away) = _mappingService.MapTeamNames(match);
            await _messageService.SendPhotoAsync(chatId, image, $"{home} vs {away}");

            // Показываем соответствующее меню
            if (source == "results")
            {
                Log.Information("[MatchEventsService] Отображаем меню результатов для {MatchId}", matchId);
                var video = await _resultsService.GetMatchVideoAsync(match.MatchId);

                var menu = new ResultsMatchMenuBuilder().Build(match, video);
                await _messageService.SendTextWithKeyboardAsync(chatId, $"{home} vs {away}", menu);
            }
            else
            {
                Log.Information("[MatchEventsService] Отображаем меню календаря для {MatchId}", matchId);
                var menu = new MatchMenuBuilder().Build(match);
                await _messageService.SendTextWithKeyboardAsync(chatId, $"{home} vs {away}", menu);
            }
        }

        /// <summary>
        /// Преобразует HTML в PNG через PuppeteerSharp.
        /// </summary>
        private async Task<Stream> RenderHtmlToImageAsync(string html)
        {
            var fetcher = new BrowserFetcher();
            await fetcher.DownloadAsync();

            var options = new LaunchOptions { Headless = true, Args = new[] { "--no-sandbox" } };
            using var browser = await Puppeteer.LaunchAsync(options);
            using var page = await browser.NewPageAsync();

            await page.SetViewportAsync(new ViewPortOptions { Width = 1200, Height = 10 });
            await page.SetContentAsync(html, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle0 } });
            await page.EvaluateExpressionAsync("document.body.style.background = '#121212'");

            var screenshot = await page.ScreenshotStreamAsync(new ScreenshotOptions { Type = ScreenshotType.Png, FullPage = true });
            var ms = new MemoryStream();
            await screenshot.CopyToAsync(ms);
            ms.Position = 0;
            return ms;
        }
    }
}
