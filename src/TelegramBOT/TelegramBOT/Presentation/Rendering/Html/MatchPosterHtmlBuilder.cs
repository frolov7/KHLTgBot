using System.Text;
using Microsoft.Extensions.Configuration;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Models;

namespace TelegramBOT.Presentation.Rendering.Html
{
    public class MatchPosterHtmlBuilder
    {
        private readonly IConfiguration _config;
        private readonly MappingService _mapper;

        public MatchPosterHtmlBuilder(IConfiguration config, MappingService mapper)
        {
            _config = config;
            _mapper = mapper;
        }

        public string Build(Match match)
        {
            var (homePretty, awayPretty) = _mapper.MapTeamNames(match);

            // ===== ДАТА, ВРЕМЯ, АРЕНА =====
            string date = match.MatchDate.ToString("dd.MM.yyyy");
            string time = match.MatchDate.ToString("HH:mm");

            // Словарь арен из конфигурации
            var arenaDict = _config.GetSection("Arenas").Get<Dictionary<string, string>>();

            string arena = arenaDict != null && arenaDict.ContainsKey(match.HomeTeamName)
                ? arenaDict[match.HomeTeamName]
                : "Арена неизвестна";

            // ===== ПУТИ К ФАЙЛАМ =====
            // Базовая папка иконок
            string iconsRoot = Path.Combine("C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons");

            // Фон
            string backgroundPath = Path.Combine(iconsRoot, "background.png");

            // Папка с командами
            string teamsDir = Path.Combine(iconsRoot, "teams");

            // Домашняя команда — RIGHT
            string homeMascotFile = $"{match.HomeTeamName}_right.png";
            // Гостевая команда — LEFT
            string awayMascotFile = $"{match.AwayTeamName}_left.png";

            string homeMascotPath = Path.Combine(teamsDir, homeMascotFile);
            string awayMascotPath = Path.Combine(teamsDir, awayMascotFile);

            // ===== ЗАГРУЗКА ИЗОБРАЖЕНИЙ В Base64 =====

            string backgroundBase64 = File.Exists(backgroundPath)
                ? Convert.ToBase64String(File.ReadAllBytes(backgroundPath))
                : "";

            string homeMascotBase64 = File.Exists(homeMascotPath)
                ? Convert.ToBase64String(File.ReadAllBytes(homeMascotPath))
                : "";

            string awayMascotBase64 = File.Exists(awayMascotPath)
                ? Convert.ToBase64String(File.ReadAllBytes(awayMascotPath))
                : "";

            // Если вдруг каких-то файлов нет — просто не отрисовываем картинки
            string backgroundHtml = string.IsNullOrEmpty(backgroundBase64)
                ? ""
                : $"<img src='data:image/png;base64,{backgroundBase64}' class='poster-bg'>";

            // Логотипы
            string homeLogoFile = $"{match.HomeTeamName}_logo.png";
            string awayLogoFile = $"{match.AwayTeamName}_logo.png";

            string homeLogoPath = Path.Combine(teamsDir, homeLogoFile);
            string awayLogoPath = Path.Combine(teamsDir, awayLogoFile);

            string homeLogoBase64 = File.Exists(homeLogoPath)
                ? Convert.ToBase64String(File.ReadAllBytes(homeLogoPath))
                : "";

            string awayLogoBase64 = File.Exists(awayLogoPath)
                ? Convert.ToBase64String(File.ReadAllBytes(awayLogoPath))
                : "";

            string homeLogoHtml = string.IsNullOrEmpty(homeLogoBase64)
                ? ""
                : $"<img src='data:image/png;base64,{homeLogoBase64}' class='team-logo team-logo-home'>";

            string awayLogoHtml = string.IsNullOrEmpty(awayLogoBase64)
                ? ""
                : $"<img src='data:image/png;base64,{awayLogoBase64}' class='team-logo team-logo-away'>";

            string homeMainHtml = string.IsNullOrEmpty(homeMascotBase64)
                ? ""
                : $"<img src='data:image/png;base64,{homeMascotBase64}' class='mascot-main mascot-main-home'>";

            string awayMainHtml = string.IsNullOrEmpty(awayMascotBase64)
                ? ""
                : $"<img src='data:image/png;base64,{awayMascotBase64}' class='mascot-main mascot-main-away'>";

            // ===== СБОРКА HTML =====
            var sb = new StringBuilder();

            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine("<style>");
            sb.AppendLine(MatchPosterCss.Get());
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='poster-wrapper'>");
            sb.AppendLine("<div class='poster'>");

            // Фон
            sb.AppendLine(backgroundHtml);

            // Логотипы команд вместо затемнённых маскотов
            sb.AppendLine(homeLogoHtml);
            sb.AppendLine(awayLogoHtml);

            // Основные (цветные) маскоты
            sb.AppendLine(homeMainHtml);
            sb.AppendLine(awayMainHtml);

            // Нижняя плашка с ареной, датой и временем — место под текст, как ты просил
            sb.AppendLine(@$"
                <div class='info-strip'>
                    <div class='arena-name'>«{arena}»</div>
                    <div class='match-datetime'>
                        <span class='match-date'>{date}</span>
                        <span class='dot'>•</span>
                        <span class='match-time'>{time}</span>
                    </div>
                </div>
            ");

            sb.AppendLine("</div>"); // .poster
            sb.AppendLine("</div>"); // .poster-wrapper
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}
