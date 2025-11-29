using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using TelegramBOT.Domain.Models;

namespace TelegramBOT.Presentation.Rendering.Html
{
    public class HistoryPosterHtmlBuilder
    {
        private readonly IConfiguration _config;

        public HistoryPosterHtmlBuilder(IConfiguration config)
        {
            _config = config;
        }

        public string Build(
            string homeTeamName,
            string awayTeamName,
            IEnumerable<Match> homeMatches,
            IEnumerable<Match> awayMatches)
        {
            string root = Path.Combine("C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons");

            string bgPath = Path.Combine(root, "background.png");
            string teamsDir = Path.Combine(root, "teams");

            string bg64 = Convert.ToBase64String(File.ReadAllBytes(bgPath));

            string homeBgLogo = TryLoadLogo(teamsDir, homeTeamName);
            string awayBgLogo = TryLoadLogo(teamsDir, awayTeamName);

            var sb = new StringBuilder();

            // HTML START
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");

            sb.AppendLine("<style>");
            sb.AppendLine(NewCss());
            sb.AppendLine("</style>");

            sb.AppendLine("</head><body>");

            sb.AppendLine(@"
                <div class='poster'>
                    <img class='bg' src='data:image/png;base64," + bg64 + @"' />
            ");

            if (homeBgLogo != null)
                sb.AppendLine($"<img class='logo-bg-left' src='data:image/png;base64,{homeBgLogo}' />");

            if (awayBgLogo != null)
                sb.AppendLine($"<img class='logo-bg-right' src='data:image/png;base64,{awayBgLogo}' />");

            sb.AppendLine("<div class='columns'>");

            // Left column — home team
            sb.AppendLine("<div class='col'>");
            foreach (var m in homeMatches.Take(7))
                sb.AppendLine(BuildRow(m, teamsDir, homeTeamName));
            sb.AppendLine("</div>");

            // Right column — away team
            sb.AppendLine("<div class='col'>");
            foreach (var m in awayMatches.Take(7))
                sb.AppendLine(BuildRow(m, teamsDir, awayTeamName));
            sb.AppendLine("</div>");

            sb.AppendLine(@"
                </div>
                <div class='footer'>Прошлые игры</div>
            </div>
            ");

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private string BuildRow(Match m, string teamsDir, string teamName)
        {
            bool isHome = m.HomeTeamName == teamName;
            bool win = (isHome && m.HomeScore > m.AwayScore) ||
                       (!isHome && m.AwayScore > m.HomeScore);

            string oppName = isHome ? m.AwayTeamName : m.HomeTeamName;

            string teamLogo = TryLoadLogo(teamsDir, teamName) ?? "";
            string oppLogo = TryLoadLogo(teamsDir, oppName) ?? "";

            string teamCss = win ? "" : "gray";
            string oppCss = win ? "gray" : "";

            string date = m.MatchDate.ToString("dd'/'MM", new CultureInfo("ru-RU"));

            string score = $"{m.HomeScore}:{m.AwayScore}";
            if (m.Status == "AFTER OVERTIME") score += " (ОТ)";
            if (m.Status == "AFTER PENALTIES") score += " (Б)";

            if (isHome)
            {
                // домашний матч
                return $@"
                    <div class='row'>
                        <img class='team-logo {teamCss}' src='data:image/png;base64,{teamLogo}' />

                        <div class='score-box'>
                            <div class='date'>{date}</div>
                            <div class='score-text'>{score}</div>
                        </div>

                        <img class='team-logo {oppCss}' src='data:image/png;base64,{oppLogo}' />
                    </div>
                ";
            }
            else
            {
                // гостевой матч
                return $@"
                    <div class='row'>
                        <img class='team-logo {oppCss}' src='data:image/png;base64,{oppLogo}' />

                        <div class='score-box'>
                            <div class='date'>{date}</div>
                            <div class='score-text'>{score}</div>
                        </div>

                        <img class='team-logo {teamCss}' src='data:image/png;base64,{teamLogo}' />
                    </div>
                ";
            }
        }

        private string? TryLoadLogo(string dir, string teamName)
        {
            string path = Path.Combine(dir, $"{teamName}_logo.png");
            if (!File.Exists(path))
                return null;

            return Convert.ToBase64String(File.ReadAllBytes(path));
        }

        private string NewCss() => @"
            body, html {
                margin:0;
                padding:0;
                width:100%;
                height:100%;
                font-family: Inter, Arial, sans-serif;
            }

            .poster {
                position: relative;
                width: 1024px;
                height: 900px;
                overflow: hidden;
            }

            .bg {
                position:absolute;
                width:100%;
                height:100%;
                object-fit: cover;
                filter: brightness(0.70);
            }

            .logo-bg-left {
                position:absolute;
                left:0;
                top:100px;
                width:450px;
                opacity:0.22;
            }

            .logo-bg-right {
                position:absolute;
                right:0;
                top:100px;
                width:450px;
                opacity:0.22;
            }

            .columns {
                position:absolute;
                top:90px;
                width:100%;
                display:flex;
                justify-content:center;
                gap:100px;
            }

            .col {
                width:40%;
            }

            .row {
                background: rgba(0,0,0,0.35);
                border-radius: 18px;
                height:80px;
                display:flex;
                align-items:center;
                justify-content:space-between;
                padding: 0 24px;
                backdrop-filter:blur(4px);
                margin-bottom: 10px;
            }

            .team-logo {
                width:60px;
                height:auto;
            }

            .gray {
                filter: grayscale(55%) brightness(0.85);
                opacity:0.85;
            }

            .score-box {
                position: relative;
                background: white;
                width: 235px;
                height: 60px;
                border-radius: 16px;
                box-shadow: 0 4px 20px rgba(0,0,0,0.35);
                font-weight: 900;
                display:flex;
                align-items:center;
            }

            .score-text {
                flex:1;
                text-align:center;
                font-size:33px;
            }

            .date {
                position:absolute;
                left:6px;
                top:50%;
                transform:translateY(-50%);
                background:#000813;
                color:white;
                font-size:8px;
                padding:3px 8px;
                border-radius:6px;
            }

            .footer {
                position:absolute;
                bottom:40px;
                width:82%;
                left:50%;
                transform:translateX(-50%);
                background:rgba(8,20,40,0.95);
                padding:22px 32px;
                border-radius:18px;
                text-align:center;
                font-size:48px;
                font-weight:900;
                color:white;
                box-shadow:
                    0 0 30px rgba(0,0,0,0.85),
                    0 0 40px rgba(0,0,0,0.85);
            }
        ";
    }
}
