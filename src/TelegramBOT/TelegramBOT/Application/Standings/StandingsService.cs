using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Domain.Models;
using Microsoft.Extensions.Configuration;
using TelegramBOT.Application.Utils;
using PuppeteerSharp;
using TelegramBOT.Infrastructure.Telegram;
using TelegramBOT.Presentation.UI;

namespace TelegramBOT.Application.Standings
{
    /// <summary>
    /// Сервис бизнес-логики отображения и отправки турнирной таблицы.
    /// Отвечает за загрузку матчей, расчёт статистики и отправку результата пользователю.
    /// </summary>
    public class StandingsService
    {
        private readonly IStandingsRepository _repo;
        private readonly IConfiguration _config;
        private readonly MappingService _mapper;
        private readonly MessageService _messageService;
        private readonly MenuService _menuService;

        public StandingsService(
            IStandingsRepository repo,
            IConfiguration config,
            MappingService mapper,
            MessageService messageService,
            MenuService menuService)
        {
            _repo = repo;
            _config = config;
            _mapper = mapper;
            _messageService = messageService;
            _menuService = menuService;
        }

        // ==========================================================
        // ============      ПУБЛИЧНЫЙ МЕТОД ОТПРАВКИ     ==========
        // ==========================================================

        /// <summary>
        /// Формирует и отправляет турнирную таблицу выбранной конференции пользователю.
        /// </summary>
        /// <param name="chatId">Идентификатор Telegram-чата, которому будет отправлено сообщение.</param>
        /// <param name="conference">Идентификатор конференции ("east" — Восточная, "west" — Западная).</param>
        public async Task SendStandingsAsync(long chatId, string conference)
        {
            try
            {
                // 1. Получаем данные
                var standings = await GetStandingsDataAsync(conference);

                // 2. Формируем HTML
                var title = conference == "east" ? "Восточная конференция" : "Западная конференция";
                var html = StandingsHtmlBuilder.Build(standings, title, _mapper);

                // 3. Преобразуем в картинку
                var imageStream = await RenderStandingsImageAsync(html);

                // 4. Отправляем пользователю
                await _messageService.SendPhotoAsync(
                    chatId,
                    imageStream,
                    conference == "east" ? "🔹 Восточная конференция" : "🔸 Западная конференция"
                );

                // 5. Добавляем меню
                await _messageService.SendKeyboardAsync(
                    chatId,
                    "Выберите действие:",
                    _menuService.GetConferenceSelectionMenu()
                );
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Ошибка при формировании турнирной таблицы");
                await _messageService.SendTextAsync(chatId, "⚠️ Не удалось загрузить таблицу. Попробуйте позже.");
            }
        }

        // ==========================================================
        // ============      ОСНОВНАЯ БИЗНЕС-ЛОГИКА        ==========
        // ==========================================================

        /// <summary>
        /// Загружает список всех матчей, фильтрует их по выбранной конференции
        /// и рассчитывает статистику команд (очки, победы, поражения и т.д.).
        /// </summary>
        /// <param name="conference">Идентификатор конференции ("east" или "west").</param>
        /// <returns>
        /// Отсортированный список команд и их статистики, где ключ — название команды,
        /// а значение — объект <see cref="TeamStats"/>.
        /// </returns>
        private async Task<List<KeyValuePair<string, TeamStats>>> GetStandingsDataAsync(string conference)
        {
            var matches = await _repo.GetAllMatchesAsync();
            var teamsInConference = _config
                .GetSection($"Conferences:{conference}")
                .Get<string[]>();

            if (teamsInConference == null || teamsInConference.Length == 0)
                throw new InvalidOperationException($"⚠️ Не найдены команды конференции: {conference}");

            var standings = new Dictionary<string, TeamStats>();

            foreach (var match in matches)
            {
                if (match.HomeScore == null || match.AwayScore == null)
                    continue;

                var homeTeam = match.HomeTeamName;
                var awayTeam = match.AwayTeamName;

                bool homeInConf = teamsInConference.Contains(homeTeam);
                bool awayInConf = teamsInConference.Contains(awayTeam);

                if (!homeInConf && !awayInConf)
                    continue;

                // Инициализация записей
                if (homeInConf && !standings.ContainsKey(homeTeam))
                    standings[homeTeam] = new TeamStats();

                if (awayInConf && !standings.ContainsKey(awayTeam))
                    standings[awayTeam] = new TeamStats();

                // Расчёт очков и статистики
                if (homeInConf)
                    standings[homeTeam] = UpdateStats(standings[homeTeam], match.HomeScore.Value, match.AwayScore.Value, isHome: true);

                if (awayInConf)
                    standings[awayTeam] = UpdateStats(standings[awayTeam], match.AwayScore.Value, match.HomeScore.Value, isHome: false);
            }

            // Сортировка по очкам, затем по разнице голов
            return standings
                .OrderByDescending(s => s.Value.Points)
                .ThenByDescending(s => s.Value.GoalsFor - s.Value.GoalsAgainst)
                .ToList();
        }

        // ==========================================================
        // ============      ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ        ==========
        // ==========================================================

        /// <summary>
        /// Выполняет обновление статистики конкретной команды после одного сыгранного матча.
        /// </summary>
        /// <param name="stats">Текущая статистика команды, которая будет обновлена.</param>
        /// <param name="scored">Количество голов, забитых данной командой в матче.</param>
        /// <param name="conceded">Количество голов, пропущенных данной командой в матче.</param>
        /// <param name="isHome">
        /// Флаг, указывающий, является ли команда хозяином площадки.
        /// True — домашний матч, False — выездной.
        /// </param>
        /// <returns>Обновлённая статистика команды после учёта результата матча.</returns>
        private TeamStats UpdateStats(TeamStats stats, int scored, int conceded, bool isHome)
        {
            stats.GamesPlayed++;
            stats.GoalsFor += scored;
            stats.GoalsAgainst += conceded;

            if (scored > conceded)
            {
                stats.Wins++;
                stats.Points += 2;
            }
            else if (scored < conceded)
            {
                stats.Losses++;
            }

            return stats;
        }

        /// <summary>
        /// Преобразует сгенерированный HTML-код турнирной таблицы в изображение PNG
        /// с помощью библиотеки PuppeteerSharp для последующей отправки пользователю.
        /// </summary>
        /// <param name="html">HTML-код таблицы, который необходимо отрендерить.</param>
        /// <returns>Поток <see cref="Stream"/> с изображением таблицы в формате PNG.</returns>
        private async Task<Stream> RenderStandingsImageAsync(string html)
        {
            var fetcher = new BrowserFetcher();
            await fetcher.DownloadAsync();

            var options = new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox" }
            };

            using var browser = await Puppeteer.LaunchAsync(options);
            using var page = await browser.NewPageAsync();

            await page.SetContentAsync(html);
            await page.EvaluateExpressionAsync("document.body.style.background = '#1e1e1e'");

            var screenshot = await page.ScreenshotStreamAsync(new ScreenshotOptions
            {
                Type = ScreenshotType.Png,
                FullPage = true
            });

            var output = new MemoryStream();
            await screenshot.CopyToAsync(output);
            output.Seek(0, SeekOrigin.Begin);
            return output;
        }
    }
}
