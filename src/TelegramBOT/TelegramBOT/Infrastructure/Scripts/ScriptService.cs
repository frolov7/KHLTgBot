using System.Diagnostics;
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
            var updateResultsTask = RunScraperAsync(ScraperMode.Results);
            var updatePredictionsTask = RunScraperAsync(ScraperMode.Predictions);
            var updateMatchVideoTask = RunScraperAsync(ScraperMode.MatchVideos);
            var updateMatchEventTask = RunScraperAsync(ScraperMode.MatchEvents);

            await Task.WhenAll(updateResultsTask, updatePredictionsTask, updateMatchVideoTask, updateMatchEventTask);

            Log.Information("✅ Обновление результатов и прогнозов завершено успешно.");
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
                    CreateNoWindow = true
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
