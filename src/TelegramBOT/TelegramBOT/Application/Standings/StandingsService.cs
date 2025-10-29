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
            // Загружаем все матчи из репозитория
            var matches = await _repo.GetAllMatchesAsync();

            // Берём список команд из конфигурации
            var conferenceTeams = _config
                .GetSection($"Conferences:{conference}")
                .Get<string[]>() ?? Array.Empty<string>();

            if (conferenceTeams.Length == 0)
                throw new InvalidOperationException($"⚠️ Не найдены команды конференции: {conference}");

            var standings = new Dictionary<string, TeamStats>();

            // Перебираем все матчи по дате
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

                // Пропускаем, если обе команды не из конференции
                if (!homeInConf && !awayInConf)
                    continue;

                // Определяем победителя
                bool homeWin = match.HomeScore > match.AwayScore;
                bool awayWin = match.AwayScore > match.HomeScore;

                bool isOvertime = match.Status == "AFTER OVERTIME";
                bool isShootout = match.Status == "AFTER PENALTIES";
                bool isOTOrSO = isOvertime || isShootout;

                // 💬 Отладка только матчей Bars Kazan
                if (homeTeam.Contains("Bars Kazan") || awayTeam.Contains("Bars Kazan"))
                {
                    Serilog.Log.Information(
                        $"[DEBUG] Проверка матча: {match.MatchDate:yyyy-MM-dd} | " +
                        $"{homeTeam} {match.HomeScore}:{match.AwayScore} {awayTeam} | " +
                        $"Status={match.Status}"
                    );
                }

                // Инициализация статистики для команд
                if (homeInConf && !standings.ContainsKey(homeTeam))
                    standings[homeTeam] = new TeamStats();
                if (awayInConf && !standings.ContainsKey(awayTeam))
                    standings[awayTeam] = new TeamStats();

                // === Победа хозяев ===
                if (homeWin)
                {
                    if (homeInConf)
                        standings[homeTeam] = UpdateStats(
                            homeTeam,
                            standings[homeTeam],
                            match.HomeScore.Value,
                            match.AwayScore.Value,
                            isOvertime: isOvertime,
                            isShootout: isShootout,
                            isWin: true,
                            isHome: true,
                            opponent: awayTeam
                        );

                    if (awayInConf)
                        standings[awayTeam] = UpdateStats(
                            awayTeam,
                            standings[awayTeam],
                            match.AwayScore.Value,
                            match.HomeScore.Value,
                            isOvertime: isOvertime,
                            isShootout: isShootout,
                            isWin: false,
                            isHome: false,
                            opponent: homeTeam
                        );
                }

                // === Победа гостей ===
                else if (awayWin)
                {
                    if (awayInConf)
                        standings[awayTeam] = UpdateStats(
                            awayTeam,
                            standings[awayTeam],
                            match.AwayScore.Value,
                            match.HomeScore.Value,
                            isOvertime: isOvertime,
                            isShootout: isShootout,
                            isWin: true,
                            isHome: false,
                            opponent: homeTeam
                        );

                    if (homeInConf)
                        standings[homeTeam] = UpdateStats(
                            homeTeam,
                            standings[homeTeam],
                            match.HomeScore.Value,
                            match.AwayScore.Value,
                            isOvertime: isOvertime,
                            isShootout: isShootout,
                            isWin: false,
                            isHome: true,
                            opponent: awayTeam
                        );
                }



                // === Ничья — на всякий случай (если появится в будущем) ===
                else
                {
                    if (homeInConf)
                        standings[homeTeam].GamesPlayed++;
                    if (awayInConf)
                        standings[awayTeam].GamesPlayed++;
                }
            }
            if (standings.ContainsKey("Bars Kazan"))
            {
                var s = standings["Bars Kazan"];
                Serilog.Log.Information(
                    $"[SUMMARY Bars Kazan] Итого после всех матчей: " +
                    $"Игры={s.GamesPlayed}, Победы={s.Wins}, Победы ОТ/Б={s.OvertimeWins}, " +
                    $"Поражения ОТ/Б={s.OvertimeLosses}, Поражения={s.Losses}, Очки={s.Points}, " +
                    $"Голы {s.GoalsFor}:{s.GoalsAgainst}"
                );
            }

            Serilog.Log.Information("=== STANDINGS DEBUG ===");
            foreach (var team in standings)
            {
                var s = team.Value;
                Serilog.Log.Information(
                    $"{team.Key}: O={s.Points}, В={s.Wins}, ВО={s.OvertimeWins}, ПО={s.OvertimeLosses}, П={s.Losses}, И={s.GamesPlayed}, Г={s.GoalsFor}:{s.GoalsAgainst}"
                );
            }
            Serilog.Log.Information("========================");

            // ✅ Финальная сортировка строго по регламенту КХЛ
            var sorted = standings
                .OrderByDescending(s => s.Value.Points)                                // 1️⃣ Очки
                .ThenByDescending(s => s.Value.Wins)                                   // 2️⃣ Победы в основное время
                .ThenByDescending(s => s.Value.GoalsFor - s.Value.GoalsAgainst)        // 3️⃣ Разница шайб
                .ThenByDescending(s => s.Value.GoalsFor)                               // 4️⃣ Заброшенные шайбы
                .ThenByDescending(s => s.Value.OvertimeWins)                           // 5️⃣ Победы в ОТ
                .ThenByDescending(s => s.Value.ShootoutWins)                           // 6️⃣ Победы по буллитам
                .ThenBy(s => s.Key)                                                    // 7️⃣ Алфавит (на всякий случай)
                .ToList();

            Serilog.Log.Information("=== SORTED STANDINGS (STRICT KHL FINAL) ===");
            int rank = 1;
            foreach (var t in sorted)
            {
                var s = t.Value;
                Serilog.Log.Information(
                    $"{rank,2}. {t.Key,-22} O={s.Points}, В={s.Wins}, ВО={s.OvertimeWins}, ВБ={s.ShootoutWins}, Δ={s.GoalsFor - s.GoalsAgainst}, Г={s.GoalsFor}:{s.GoalsAgainst}"
                );
                rank++;
            }
            Serilog.Log.Information("==========================================");
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

            // 💬 Подробный лог
            if (teamName.Contains("Bars Kazan", StringComparison.OrdinalIgnoreCase))
            {
                string location = isHome ? "Дома" : "В гостях";
                string opponentStr = isHome ? $"vs {opponent}" : $"@ {opponent}";
                string type = isOvertime ? "ОТ" : isShootout ? "Б" : "ОСН";

                Serilog.Log.Information(
                    $"[DEBUG {teamName}] {DateTime.Now:HH:mm:ss} | {location} {opponentStr} | " +
                    $"Результат: {(isWin ? "Победа" : "Поражение")} ({type}) | " +
                    $"Счёт: {scored}:{conceded} | Очков: +{earnedPoints} | Было: {pointsBefore} → Стало: {stats.Points}"
                );
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
