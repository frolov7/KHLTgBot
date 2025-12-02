using System.Text;
using Microsoft.Extensions.Configuration;
using TelegramBOT.Domain.Models;

namespace TelegramBOT.Presentation.Rendering.Html
{
    public class TodayMatchesHtmlBuilder
    {
        private readonly IConfiguration _config;

        public TodayMatchesHtmlBuilder(IConfiguration config)
        {
            _config = config;
        }

        public string Build(IEnumerable<Match> matches, DateTime day)
        {
            // ==== Пути ====
            string root = Path.Combine("C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons");

            string bgPath = Path.Combine(root, "background.png");
            string teamsDir = Path.Combine(root, "teams");

            // ==== Base64 ====
            string bg64 = Convert.ToBase64String(File.ReadAllBytes(bgPath));

            // ==== Матчи ====
            var list = matches.OrderBy(m => m.MatchDate).ToList();
            int count = list.Count == 0 ? 1 : list.Count;

            // ==== Адаптивные размеры ====
            int rowHeight = count switch
            {
                1 => 150,
                2 => 130,
                3 => 110,
                4 => 90,
                5 => 70,
                6 => 55,
                7 => 50,
                8 => 45,
                _ => 40
            };

            int logoSize = count switch
            {
                1 => 150,
                2 => 140,
                3 => 130,
                4 => 120,
                5 => 110,
                6 => 100,
                7 => 95,
                8 => 90,
                9 => 85,
                10 => 80,
                _ => 70
            };

            int shift = count switch
            {
                1 => 200,
                2 => 140,
                3 => 60,
                _ => 0
            };

            // ==== Строки матчей ====
            var rowsSb = new StringBuilder();
            foreach (var m in list)
                rowsSb.AppendLine(BuildRow(m, teamsDir, rowHeight, logoSize));

            // ==== Сборка HTML ====
            var sb = new StringBuilder();

            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");

            sb.AppendLine("<style>");
            sb.AppendLine(NewCss(shift));
            sb.AppendLine("</style>");

            sb.AppendLine("</head><body>");

            sb.AppendLine(@"
                <div class='poster'>
                    <img class='bg' src='data:image/png;base64," + bg64 + @"' />
            ");

            sb.AppendLine("<div class='matches'>");
            sb.AppendLine(rowsSb.ToString());
            sb.AppendLine("</div>");

            string dateStr = day.ToString("dd.MM.yyyy").Replace(".", "/");

            sb.AppendLine($@"
                <div class='footer-strip'>Матчи на {dateStr}</div>
            ");

            sb.AppendLine("</div>");

            sb.AppendLine("</body></html>");

            return sb.ToString();
        }

        // ======================== BuildRow ========================

        private string BuildRow(Match m, string teamsDir, int rowHeight, int logoSize)
        {
            string home = TryLoadLogo(teamsDir, m.HomeTeamName) ?? "";
            string away = TryLoadLogo(teamsDir, m.AwayTeamName) ?? "";

            string centerText =
                m.Status == "FINISHED"
                    ? $"{m.HomeScore}:{m.AwayScore}"
                    : m.MatchDate.ToString("HH:mm");

            return $@"
                <div class='match-row' style='height:{rowHeight}px'>
                    <img class='team-logo-left' style='width:{logoSize}px' src='data:image/png;base64,{home}' />

                    <div class='score-box'>
                        <div class='score-text'>{centerText}</div>
                    </div>

                    <img class='team-logo-right' style='width:{logoSize}px' src='data:image/png;base64,{away}' />
                </div>
            ";
        }

        // ======================== TryLoadLogo ========================

        private string? TryLoadLogo(string dir, string teamName)
        {
            string path = Path.Combine(dir, $"{teamName}_logo.png");
            if (!File.Exists(path))
                return null;

            return Convert.ToBase64String(File.ReadAllBytes(path));
        }

        // ========================= CSS ==========================

        private string NewCss(int shift) => @"
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
                filter: brightness(0.75);
            }

            .matches {
                position:absolute;
                top:120px;
                width:100%;
                display:flex;
                flex-direction:column;
                align-items:center;
                gap:30px;
                transform:translateY(" + shift + @"px);
            }

            .match-row {
                width:82%;
                background:rgba(0,0,0,0.35);
                border-radius:22px;
                display:flex;
                justify-content:space-between;
                align-items:center;
                padding:10px 20px;
                backdrop-filter:blur(4px);
            }

            .team-logo-left,
            .team-logo-right {
                height:auto;
            }

            .score-box {
                background:white;
                width:320px;
                height:80%;
                border-radius:18px;
                display:flex;
                align-items:center;
                justify-content:center;
                box-shadow:0 4px 20px rgba(0,0,0,0.4);
            }

            .score-text {
                font-size:42px;
                font-weight:900;
            }

            .footer-strip {
                position:absolute;
                bottom:40px;
                left:50%;
                transform:translateX(-50%);
                width:82%;
                background:rgba(8,20,40,0.95);
                padding:22px 32px;
                border-radius:18px;
                color:white;
                text-align:center;
                font-size:48px;
                font-weight:900;
                box-shadow:
                    0 0 30px rgba(0,0,0,0.9),
                    0 0 40px rgba(0,0,0,0.8);
            }
        ";
    }
}
