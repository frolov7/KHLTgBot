using System.Diagnostics;
using System.Text;
using Serilog;

namespace TelegramBOT.Infrastructure.Scripts
{
    /// <summary>
    /// Сервис для запуска внешних Node.js-скриптов (парсеров данных).
    /// Используется для обновления результатов, прогнозов и полной загрузки данных.
    /// </summary>
    public class ScriptService
    {
        private readonly IConfiguration _config;

        public ScriptService(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Тип выполняемого скрипта.
        /// </summary>
        public enum ScraperMode
        {
            Results,
            Predictions,
            MatchVideos,
            MatchEvents,
            All
        }

        // ==========================================================
        // ============      ПУБЛИЧНЫЕ МЕТОДЫ           ============
        // ==========================================================

        /// <summary>
        /// Запускает парсеры параллельно.
        /// </summary>
        public async Task RunScrapersAsync()
        {
            // 1. Обновляем матчи + считаем результаты прогнозов
            await RunScraperAsync(ScraperMode.Results);
            // 2. Парсим прогнозы + импорт
            await RunScraperAsync(ScraperMode.Predictions);
            // 3. Повторно считаем WIN / LOSE (ОБЯЗАТЕЛЬНО)
            await RunNodeScriptAsync(
                "src/db/import/updatePredictionResults.js"
            );

            // 4. Остальное параллельно
            var videosTask = RunScraperAsync(ScraperMode.MatchVideos);
            var eventsTask = RunScraperAsync(ScraperMode.MatchEvents);

            await Task.WhenAll(videosTask, eventsTask);

            Log.Information("✅ Обновление данных завершено корректно");
        }

        /// <summary>
        /// Запускает скрипт в заданном режиме (результаты, прогнозы, все данные).
        /// </summary>
        public async Task RunScraperAsync(ScraperMode mode)
        {
            var args = mode switch
            {
                ScraperMode.Results => "--updateResults",
                ScraperMode.Predictions => "--predictions",
                ScraperMode.MatchVideos => "--resultvideos",
                ScraperMode.MatchEvents => "--events",
                ScraperMode.All => "--all",
                _ => throw new ArgumentOutOfRangeException(nameof(mode))
            };

            var scriptPath = _config["Script:ScraperPath"] ?? "src/scraper/scraperRunner.js";
            await RunNodeScriptAsync($"{scriptPath} {args}");
        }

        /// <summary>
        /// Запускает скрипт парсинга событий для конкретного матча по matchId
        /// </summary>
        public async Task RunSingleMatchEventsAsync(string matchId)
        {
            var scriptPath = _config["Script:ScraperPath"] ?? "src/scraper/scraperRunner.js";
            var workingDir = _config["Script:WorkingDirectory"] ?? Directory.GetCurrentDirectory();

            var args = $"{scriptPath} --events-single {matchId}";
            await RunNodeScriptAsync(args);
        }

        // ==========================================================
        // ============      ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ     ============
        // ==========================================================

        /// <summary>
        /// Запускает внешний Node.js-скрипт с указанными аргументами.
        /// </summary>
        private async Task RunNodeScriptAsync(string arguments)
        {
            try
            {
                var workingDir = _config["Script:WorkingDirectory"] ?? Directory.GetCurrentDirectory();

                var startInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = arguments,
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,

                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using var process = new Process { StartInfo = startInfo };

                Log.Information("🚀 Запуск скрипта: node {Arguments}", arguments);
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (!string.IsNullOrWhiteSpace(output))
                    Log.Information("📤 Скрипт вывел: \n{Output}", output.Trim());

                if (!string.IsNullOrWhiteSpace(error))
                    Log.Warning("⚠️ Ошибка скрипта: {Error}", error.Trim());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка при запуске Node.js-скрипта");
            }
        }
    }
}
