using PuppeteerSharp;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Presentation.UI.Menus.Calendar;
using System.Text;
using Serilog;
using TelegramBOT.Presentation.UI.Menus.Results;
using TelegramBOT.Application.Results;
using TelegramBOT.Infrastructure.Scripts;
using TelegramBOT.Presentation.UI;
using TelegramBOT.Application.Telegram;
using TelegramBOT.Domain.Entities.Matches;
using TelegramBOT.Presentation.Rendering.Html.MatchEvents;

namespace TelegramBOT.Application.MatchEvents
{
    public class MatchEventsService
    {
        private readonly IMatchStatsServiceRepository _matchStatsRepository;
        private readonly MappingService _mappingService;
        private readonly MessageService _messageService;
        private readonly ScriptService _scriptService;
        private readonly ResultsService _resultsService;
        private readonly MatchEventsHtmlBuilder _htmlBuilder;
        private readonly MenuService _menuService;


        public MatchEventsService(
            IMatchStatsServiceRepository matchStatsRepository,
            MappingService mappingService,
            ResultsService resultsService,
            MessageService messageService,
            ScriptService scriptService,
            MenuService menuService,
            MatchEventsHtmlBuilder htmlBuilder)
        {
            _matchStatsRepository = matchStatsRepository;
            _mappingService = mappingService;
            _resultsService = resultsService;
            _messageService = messageService;
            _scriptService = scriptService;
            _htmlBuilder = htmlBuilder;
            _menuService = menuService;
        }

        // =====================================================================
        // ============      ОБРАБОТКА И ЗАГРУЗКА СОБЫТИЙ МАТЧА      ============
        // =====================================================================

        /// <summary>
        /// Выполняет загрузку, опциональный парсинг, визуализацию и отправку событий матча.
        /// Объединяет бизнес-логику работы со статистикой, HTML-рендерингом и ответными меню.
        /// </summary>
        /// <param name="source">
        /// Источник вызова: "calendar" или "results".
        /// Определяет, какое меню будет показано пользователю.
        /// </param>
        /// <param name="forceParse">
        /// Если true — выполняется принудительный парсинг матча перед обработкой.
        /// Если false — данные берутся из хранилища.
        /// </param>
        /// <returns>Асинхронная задача выполнения операции.</returns>
        public async Task ProcessMatchEventsAsync(long chatId, string matchId, string source, bool forceParse)
        {
            Log.Information("[ProcessMatchEvents] matchId={MatchId}, source={Source}, forceParse={Force}",
                matchId, source, forceParse);

            try
            {
                // === 1. Загружаем события из БД ===
                var events = (await _matchStatsRepository.GetMatchEventsAsync(matchId)).ToList();

                bool needParse = forceParse || !events.Any();

                if (needParse)
                {
                    Log.Information("[ProcessMatchEvents] Парсинг требуется (forceParse={Force}, eventsInDb={Has})",
                        forceParse, events.Any());

                    await _scriptService.RunSingleMatchEventsAsync(matchId);

                    // загружаем снова после парсинга
                    events = (await _matchStatsRepository.GetMatchEventsAsync(matchId)).ToList();
                }
                else
                {
                    Log.Information("[ProcessMatchEvents] Парсинг НЕ требуется — данные уже есть в БД");
                }

                // 2. Загружаем матч
                var match = await _matchStatsRepository.GetMatchByIdAsync(matchId);

                if (match == null)
                {
                    await _messageService.SendTextAsync(chatId, "Матч не найден.");
                    return;
                }

                if (!events.Any())
                {
                    await _messageService.SendTextAsync(chatId, "События для этого матча пока недоступны.");
                    return;
                }

                // 3. HTML → PNG
                var html = _htmlBuilder.Build(match, events);
                var image = await RenderHtmlToImageAsync(html);

                var (home, away) = _mappingService.MapTeamNames(match);
                await _messageService.SendPhotoAsync(chatId, image, $"{home} vs {away}");

                // 4. Меню
                await SendMenuAsync(chatId, match, source, home, away);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[ProcessMatchEvents] Ошибка обработки матча {MatchId}", matchId);
                await _messageService.SendTextAsync(chatId, "Ошибка при загрузке событий матча.");
            }
        }

        /// <summary>
        /// Отправляет пользователю нужное меню после отображения событий матча.
        /// Тип меню зависит от источника вызова: "calendar" или "results".
        /// </summary>
        /// <param name="source">
        /// Источник вызова: 
        /// "results" — показать меню результатов, любое другое значение — показать меню календаря.
        /// </param>
        /// <param name="home">Красивое название домашней команды.</param>
        /// <param name="away">Красивое название гостевой команды.</param>
        /// <returns>Асинхронная задача отправки меню.</returns>
        private async Task SendMenuAsync(long chatId, Match match, string source, string home, string away)
        {
            // === Inline меню матча ===
            if (source == "results")
            {
                var video = await _resultsService.GetMatchVideoAsync(match.MatchId);
                var resultsMenu = new ResultsMatchMenuBuilder().Build(match, video);

                await _messageService.SendTextWithKeyboardAsync(
                    chatId,
                    $"{home} vs {away}",
                    resultsMenu
                );
            }
            else
            {
                var matchMenu = new MatchMenuBuilder().Build(match);

                await _messageService.SendTextWithKeyboardAsync(
                    chatId,
                    $"{home} vs {away}",
                    matchMenu
                );
            }

            // === Reply меню календаря ===
            var calendarMenu = _menuService.GetCalendarMenu();

            await _messageService.SendKeyboardAsync(
                chatId,
                "📅 Выберите действие:",
                calendarMenu
            );
        }

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
