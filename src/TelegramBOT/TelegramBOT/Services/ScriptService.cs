using System.Diagnostics;

namespace TelegramBOT.Services
{
    /// <summary>
    /// Сервис для запуска внешних Node.js-скриптов.
    /// Используется для обновления и загрузки данных через парсер.
    /// </summary>
    public class ScriptService
    {
        private readonly IConfiguration _config;

        public ScriptService(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Запускает скрипт парсера для обновления последних результатов.
        /// </summary>
        public async Task RunScraperResultsAsync()
        {
            await RunNodeScriptAsync("src/scraper/scraperRunner.js --updateResults");
        }

        /// <summary>
        /// Запускает скрипт парсера для обновления последних прогнозов.
        /// </summary>
        public async Task RunScraperPredictionsAsync()
        {
            await RunNodeScriptAsync("src/scraper/scraperRunner.js --predictions");
        }

        /// <summary>
        /// Запускает скрипт парсера ссылок на видео с обзором матчей.
        /// </summary>
        public async Task RunScraperVideoAsync()
        {
            await RunNodeScriptAsync("src/scraper/scraperRunner.js --all");
        }

        /// <summary>
        /// Вспомогательный метод для запуска Node.js скрипта.
        /// </summary>
        /// <param name="arguments">Аргументы командной строки для скрипта.</param>
        private async Task RunNodeScriptAsync(string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = arguments,
                    WorkingDirectory = _config["Script:WorkingDirectory"],
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (!string.IsNullOrWhiteSpace(output))
                    Console.WriteLine("SCRAPER OUT: " + output);

                if (!string.IsNullOrWhiteSpace(error))
                    Console.WriteLine("SCRAPER ERR: " + error);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Ошибка при запуске скрипта: " + ex.Message);
            }
        }
    }
}
