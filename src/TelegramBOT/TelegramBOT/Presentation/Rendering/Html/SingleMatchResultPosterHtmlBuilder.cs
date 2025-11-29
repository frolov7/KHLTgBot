using System.Text;
using Microsoft.Extensions.Configuration;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Models;

namespace TelegramBOT.Presentation.Rendering.Html
{
    public class SingleMatchResultPosterHtmlBuilder
    {
        private readonly IConfiguration _config;
        private readonly MappingService _mapper;

        public SingleMatchResultPosterHtmlBuilder(IConfiguration config, MappingService mapper)
        {
            _config = config;
            _mapper = mapper;
        }

        public string Build(Match match)
        {
            var (homePretty, awayPretty) = _mapper.MapTeamNames(match);

            string status = match.Status switch
            {
                "FINISHED" => "Основное время",
                "AFTER OVERTIME" => "После ОТ",
                "AFTER PENALTIES" => "После буллитов",
                _ => ""
            };

            // ==== Пути ====
            string iconsRoot = Path.Combine("C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons");

            string bgPath = Path.Combine(iconsRoot, "background.png");
            string teamsDir = Path.Combine(iconsRoot, "teams");

            string homeMascotPath = Path.Combine(teamsDir, $"{match.HomeTeamName}_right.png");
            string awayMascotPath = Path.Combine(teamsDir, $"{match.AwayTeamName}_left.png");

            string homeLogoPath = Path.Combine(teamsDir, $"{match.HomeTeamName}_logo.png");
            string awayLogoPath = Path.Combine(teamsDir, $"{match.AwayTeamName}_logo.png");

            // ==== Base64 ====
            string bg = Convert.ToBase64String(File.ReadAllBytes(bgPath));
            string homeMascot = Convert.ToBase64String(File.ReadAllBytes(homeMascotPath));
            string awayMascot = Convert.ToBase64String(File.ReadAllBytes(awayMascotPath));
            string homeLogo = Convert.ToBase64String(File.ReadAllBytes(homeLogoPath));
            string awayLogo = Convert.ToBase64String(File.ReadAllBytes(awayLogoPath));

            var sb = new StringBuilder();

            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine("<style>");
            sb.AppendLine(MatchPosterCss.Get());

            // точечное доп. правило чтобы убрать верхнюю часть
            sb.AppendLine(@"
                .teams-row { display: none; }
            ");

            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            sb.AppendLine("<div class='poster-wrapper'>");
            sb.AppendLine("<div class='poster'>");

            // задний фон
            sb.AppendLine($"<img src='data:image/png;base64,{bg}' class='poster-bg'>");

            // логотипы на фоне
            sb.AppendLine($"<img src='data:image/png;base64,{homeLogo}' class='team-logo team-logo-home'>");
            sb.AppendLine($"<img src='data:image/png;base64,{awayLogo}' class='team-logo team-logo-away'>");

            // маскоты
            sb.AppendLine($"<img src='data:image/png;base64,{homeMascot}' class='mascot-main mascot-main-home'>");
            sb.AppendLine($"<img src='data:image/png;base64,{awayMascot}' class='mascot-main mascot-main-away'>");

            sb.AppendLine($@"
                <div class='info-strip'>
                    <div class='match_score'>{match.HomeScore} : {match.AwayScore}</div>
                    <div class='result-type'>{status}</div>
                </div>
            ");

            sb.AppendLine("</div></div>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }
    }
}
