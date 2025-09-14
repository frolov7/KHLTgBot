using System.Diagnostics;

namespace TelegramBOT.Services
{
    public class ScriptService
    {
        private readonly IConfiguration _config;

        public ScriptService(IConfiguration config)
        {
            _config = config;
        }

        public async Task RunScraperUpdateAsync()
        {
            await RunNodeScriptAsync("src/scraper/scraperRunner.js --update");
        }

        public async Task RunScraperAllAsync()
        {
            await RunNodeScriptAsync("src/scraper/scraperRunner.js --all");
        }

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
