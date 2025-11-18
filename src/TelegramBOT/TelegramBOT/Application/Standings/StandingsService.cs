using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Domain.Models;
using Microsoft.Extensions.Configuration;
using TelegramBOT.Application.Utils;
using PuppeteerSharp;
using TelegramBOT.Infrastructure.Telegram;
using TelegramBOT.Presentation.UI;
using Serilog;

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
            Log.Information("[SendStandingsAsync] Старт. chatId={ChatId}, conference={Conference}",
                chatId, conference);

            try
            {
                // 1. Получаем данные
                Log.Information("[SendStandingsAsync] Загрузка данных конференции...");
                var standings = await GetStandingsDataAsync(conference);

                // 2. Формируем HTML
                Log.Information("[SendStandingsAsync] Формирование HTML...");
                var title = conference == "east" ? "Восточная конференция" : "Западная конференция";
                var html = StandingsHtmlBuilder.Build(standings, title, _mapper);

                // 3. Преобразуем в PNG
                Log.Information("[SendStandingsAsync] Рендеринг изображения...");
                var imageStream = await RenderStandingsImageAsync(html);

                // 4. Отправляем
                Log.Information("[SendStandingsAsync] Отправка изображения пользователю...");
                await _messageService.SendPhotoAsync(
                    chatId,
                    imageStream,
                    conference == "east" ? "🔹 Восточная конференция" : "🔸 Западная конференция"
                );

                // 5. Меню
                await _messageService.SendKeyboardAsync(
                    chatId,
                    "Выберите действие:",
                    _menuService.GetConferenceSelectionMenu()
                );

                Log.Information("[SendStandingsAsync] Завершено успешно.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[SendStandingsAsync] Ошибка формирования таблицы");
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
            Log.Information("[GetStandingsDataAsync] Старт обработки. Conference={Conference}", conference);

            // 1. Загружаем все матчи
            var matches = await _repo.GetAllMatchesAsync();
            Log.Information("[GetStandingsDataAsync] Загружено матчей: {Count}", matches.Count());

            // 2. Загружаем команды конференции
            var conferenceTeams = _config
                .GetSection($"Conferences:{conference}")
                .Get<string[]>() ?? Array.Empty<string>();

            if (conferenceTeams.Length == 0)
            {
                Log.Warning("[GetStandingsDataAsync] Команды конференции не найдены: {Conference}", conference);
                throw new InvalidOperationException($"Не найдены команды конференции: {conference}");
            }

            Log.Information("[GetStandingsDataAsync] Команд в конференции: {Count}", conferenceTeams.Length);

            var standings = new Dictionary<string, TeamStats>();

            // 3. Обработка матчей
            foreach (var match in matches.OrderBy(m => m.MatchDate))
            {
                if (match.HomeScore == null || match.AwayScore == null)
                    continue;

                if (match.Status is "LIVE" or "SCHEDULED")
                    continue;

                var homeTeam = match.HomeTeamName;
                var awayTeam = match.AwayTeamName;

                bool homeInConf = conferenceTeams.Contains(homeTeam);
                bool awayInConf = conferenceTeams.Contains(awayTeam);

                // Матч не влияет на конференцию
                if (!homeInConf && !awayInConf)
                    continue;

                bool homeWin = match.HomeScore > match.AwayScore;
                bool awayWin = match.AwayScore > match.HomeScore;

                bool isOvertime = match.Status == "AFTER OVERTIME";
                bool isShootout = match.Status == "AFTER PENALTIES";

                // Инициализация статистики для команд
                if (homeInConf && !standings.ContainsKey(homeTeam))
                    standings[homeTeam] = new TeamStats();
                if (awayInConf && !standings.ContainsKey(awayTeam))
                    standings[awayTeam] = new TeamStats();

                // Обновление статистики
                if (homeWin)
                {
                    if (homeInConf)
                        standings[homeTeam] = UpdateStats(
                            homeTeam, standings[homeTeam],
                            match.HomeScore.Value, match.AwayScore.Value,
                            isOvertime, isShootout, true, true, awayTeam
                        );

                    if (awayInConf)
                        standings[awayTeam] = UpdateStats(
                            awayTeam, standings[awayTeam],
                            match.AwayScore.Value, match.HomeScore.Value,
                            isOvertime, isShootout, false, false, homeTeam
                        );
                }
                else if (awayWin)
                {
                    if (awayInConf)
                        standings[awayTeam] = UpdateStats(
                            awayTeam, standings[awayTeam],
                            match.AwayScore.Value, match.HomeScore.Value,
                            isOvertime, isShootout, true, false, homeTeam
                        );

                    if (homeInConf)
                        standings[homeTeam] = UpdateStats(
                            homeTeam, standings[homeTeam],
                            match.HomeScore.Value, match.AwayScore.Value,
                            isOvertime, isShootout, false, true, awayTeam
                        );
                }
                else
                {
                    if (homeInConf)
                        standings[homeTeam].GamesPlayed++;
                    if (awayInConf)
                        standings[awayTeam].GamesPlayed++;
                }
            }

            // 5. Общий лог статистики
            Log.Information("=== STANDINGS SNAPSHOT ===");
            foreach (var t in standings)
            {
                var s = t.Value;
                Log.Information(
                    "{Team}: O={PTS}, W={W}, WO={WO}, LO={LO}, L={L}, GP={GP}, GF={GF}:{GA}",
                    t.Key, s.Points, s.Wins, s.OvertimeWins, s.OvertimeLosses,
                    s.Losses, s.GamesPlayed, s.GoalsFor, s.GoalsAgainst
                );
            }
            Log.Information("==========================");

            // 6. Финальная сортировка
            var sorted = standings
                .OrderByDescending(s => s.Value.Points)
                .ThenByDescending(s => s.Value.Wins)
                .ThenByDescending(s => s.Value.GoalsFor - s.Value.GoalsAgainst)
                .ThenByDescending(s => s.Value.GoalsFor)
                .ThenByDescending(s => s.Value.OvertimeWins)
                .ThenByDescending(s => s.Value.ShootoutWins)
                .ThenBy(s => s.Key)
                .ToList();

            Log.Information("[GetStandingsDataAsync] Сортировка завершена. Команд={Count}", sorted.Count);

            // 7. Лог финального рейтинга
            Log.Information("=== SORTED STANDINGS (FINAL) ===");
            int place = 1;
            foreach (var t in sorted)
            {
                var s = t.Value;
                Log.Information(
                    "{Place,2}. {Team,-20} O={PTS}, W={W}, WO={WO}, SO={SO}, Δ={Diff}, GF={GF}:{GA}",
                    place, t.Key,
                    s.Points, s.Wins, s.OvertimeWins, s.ShootoutWins,
                    s.GoalsFor - s.GoalsAgainst,
                    s.GoalsFor, s.GoalsAgainst
                );
                place++;
            }
            Log.Information("=================================");

            Log.Information("[GetStandingsDataAsync] Завершено успешно.");

            return sorted;
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
        private TeamStats UpdateStats(
             string teamName,
             TeamStats stats,
             int scored,
             int conceded,
             bool isOvertime,
             bool isShootout,
             bool isWin,
             bool isHome,
             string opponent)
        {
            // Логируем только ключевое, без отладочного спама
            Log.Information(
                "[UpdateStats] Команда={Team}, Win={Win}, Score={Scored}:{Conceded}, Opp={Opponent}",
                teamName, isWin, scored, conceded, opponent
            );

            int pointsBefore = stats.Points;
            int earnedPoints = 0;

            stats.GamesPlayed++;
            stats.GoalsFor += scored;
            stats.GoalsAgainst += conceded;

            if (isWin)
            {
                if (isOvertime)
                    stats.OvertimeWins++;
                else if (isShootout)
                    stats.ShootoutWins++;
                else
                    stats.Wins++;

                earnedPoints = 2;
                stats.Points += 2;
                stats.RecentForm.Enqueue("🟩");
            }
            else
            {
                if (isOvertime)
                {
                    stats.OvertimeLosses++;
                    earnedPoints = 1;
                    stats.Points += 1;
                }
                else if (isShootout)
                {
                    stats.ShootoutLosses++;
                    earnedPoints = 1;
                    stats.Points += 1;
                }
                else
                {
                    stats.Losses++;
                }

                stats.RecentForm.Enqueue("🟥");
            }

            while (stats.RecentForm.Count > 5)
                stats.RecentForm.Dequeue();

            Log.Information(
                "[UpdateStats] Итог: Team={Team}, Points {Before}->{After}, Form={Form}",
                teamName, pointsBefore, stats.Points, string.Join("", stats.RecentForm)
            );

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
            Log.Information("[RenderStandingsImageAsync] Старт рендеринга HTML.");

            var fetcher = new BrowserFetcher();
            await fetcher.DownloadAsync();

            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox" }
            });

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

            Log.Information("[RenderStandingsImageAsync] Рендеринг завершён.");
            return output;
        }
    }
}
