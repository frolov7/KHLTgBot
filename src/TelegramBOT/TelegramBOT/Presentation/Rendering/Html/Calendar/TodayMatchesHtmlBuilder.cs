using System.Text;
using Microsoft.Extensions.Configuration;
using TelegramBOT.Domain.Entities.Matches;
using TelegramBOT.Domain.Entities.MatchEvents;

namespace TelegramBOT.Presentation.Rendering.Html.Calendar
{
    public class TodayMatchesHtmlBuilder
    {
        private readonly IConfiguration _config;

        public TodayMatchesHtmlBuilder(IConfiguration config)
        {
            _config = config;
        }

        public string Build(IEnumerable<Match> matches, DateTime day, Dictionary<string, List<PeriodGoals>> goalsByMatch)
        {
            // ==== Пути ====
            string root = Path.Combine(
                "C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons");

            string bgPath = Path.Combine(root, "background.png");
            string teamsDir = Path.Combine(root, "teams");

            string bg64 = Convert.ToBase64String(File.ReadAllBytes(bgPath));

            var list = matches.OrderBy(m => m.MatchDate).ToList();
            int count = Math.Clamp(list.Count == 0 ? 1 : list.Count, 1, 68);

            // ==== BASE КОНСТАНТЫ ====
            const int BASE_ROW_HEIGHT = 100;
            const int BASE_LOGO_SIZE = 130;
            const int BASE_SCORE_BOX_WIDTH = 330;
            const int BASE_SCORE_FONT = 45;
            const int BASE_PERIODS_FONT = 18;
            const int BASE_GAP = 25;

            double scale = count switch
            {
                <= 2 => 1.25,
                <= 4 => 1.0,
                5 => 0.88,
                6 => 0.7,
                7 => 0.58,
                _ => 0.50
            };

            int rowHeight = (int)(BASE_ROW_HEIGHT * scale);
            int logoSize = (int)(BASE_LOGO_SIZE * scale);
            int scoreBoxWidth = (int)(BASE_SCORE_BOX_WIDTH * scale);
            int scoreFontSize = (int)(BASE_SCORE_FONT * scale);
            int periodsFontSize = (int)(BASE_PERIODS_FONT * scale);
            int gap = (int)(BASE_GAP * scale);

            string justifyContent = count <= 3 ? "center" : "flex-start";

            // ==== ROWS ====
            var rowsSb = new StringBuilder();
            foreach (var m in list)
            {
                rowsSb.AppendLine(
                    BuildRow(
                        m,
                        teamsDir,
                        rowHeight,
                        logoSize,
                        scoreBoxWidth,
                        scoreFontSize,
                        periodsFontSize,
                        goalsByMatch.TryGetValue(m.MatchId, out var g) ? g : new List<PeriodGoals>()
                    )
                );
            }

            // ==== HTML ====
            var sb = new StringBuilder();

            sb.AppendLine("<html><head><meta charset='utf-8'>");
            sb.AppendLine("<style>");
            sb.AppendLine(NewCss(gap, justifyContent));
            sb.AppendLine("</style>");
            sb.AppendLine("</head><body>");

            sb.AppendLine("<div class='poster'>");
            sb.AppendLine($"<img class='bg' src='data:image/png;base64,{bg64}' />");

            sb.AppendLine("<div class='matches'>");
            sb.AppendLine(rowsSb.ToString());
            sb.AppendLine("</div>");

            string dateStr = day.ToString("dd.MM.yyyy").Replace(".", "/");
            sb.AppendLine($@"<div class='footer-strip'>Матчи на {dateStr}</div>");

            sb.AppendLine("</div></body></html>");

            return sb.ToString();
        }

        private string BuildPeriodsText(Match match, List<PeriodGoals> goals)
        {
            var periods = new[]
            {
                "1st period",
                "2nd period",
                "3rd period"
            };

            var parts = new List<string>();

            foreach (var p in periods)
            {
                int home = goals
                    .Where(g => g.Period == p && g.TeamId == match.HomeTeamId)
                    .Select(g => g.Goals)
                    .FirstOrDefault();

                int away = goals
                    .Where(g => g.Period == p && g.TeamId == match.AwayTeamId)
                    .Select(g => g.Goals)
                    .FirstOrDefault();

                parts.Add($"{home}:{away}");
            }

            return $"({string.Join("; ", parts)})";
        }

        // ======================= ROW =======================

        private string BuildRow(
            Match m,
            string dir,
            int rowHeight,
            int logoSize,
            int scoreBoxWidth,
            int scoreFontSize,
            int periodsFontSize,
            List<PeriodGoals> periodGoals)
        {
            string homeLogo = TryLoadLogo(dir, m.HomeTeamName) ?? "";
            string awayLogo = TryLoadLogo(dir, m.AwayTeamName) ?? "";
            bool showPeriods = m.Status == "FINISHED";
            
            string score =
                showPeriods
                    ? $"{m.HomeScore} : {m.AwayScore}"
                    : m.MatchDate.ToString("HH:mm");

            string periods = showPeriods
                ? BuildPeriodsText(m, periodGoals)
                : "";   // ← пусто, но div будет


            return $@"
                <div class='match-row' style='height:{rowHeight}px'>
                    <img class='team-logo-left'
                         style='width:{logoSize}px'
                         src='data:image/png;base64,{homeLogo}' />

                    <div class='score-box' style='width:{scoreBoxWidth}px'>
                        <div class='score-text' style='font-size:{scoreFontSize}px'>
                            {score}
                        </div>
                        <div class='periods {(showPeriods ? "" : "hidden")}'
                             style='font-size:{periodsFontSize}px'>
                            {periods}
                        </div>
                    </div>

                    <img class='team-logo-right'
                         style='width:{logoSize}px'
                         src='data:image/png;base64,{awayLogo}' />
                </div>";

        }

        // ======================= LOGO =======================

        private string? TryLoadLogo(string dir, string teamName)
        {
            string path = Path.Combine(dir, $"{teamName}_logo.png");
            return File.Exists(path)
                ? Convert.ToBase64String(File.ReadAllBytes(path))
                : null;
        }

        // ======================= CSS =======================

        private string NewCss(int gap, string justifyContent) => $@"
            body, html {{
                margin:0;
                padding:0;
                width:100%;
                height:100%;
                font-family: Inter, Arial, sans-serif;
            }}

            .poster {{
                position: relative;
                width: 1024px;
                height: 900px;
                overflow: hidden;
            }}

            .bg {{
                position:absolute;
                width:100%;
                height:100%;
                object-fit: cover;
                filter: brightness(0.75);
            }}

            .matches {{
                position:absolute;
                top:90px;
                bottom:200px;
                width:100%;
                display:flex;
                flex-direction:column;
                align-items:center;
                justify-content:{justifyContent};
                gap:{gap}px;
            }}

            .match-row {{
                width:82%;
                background: rgba(0,0,0,0.35);
                border-radius:22px;
                display:flex;
                justify-content:space-between;
                align-items:center;
                padding:10px 20px;
                backdrop-filter: blur(4px);
            }}

            .team-logo-left,
            .team-logo-right {{
                height:auto;
            }}

            .score-box {{
                background:white;
                border-radius:18px;
                display:flex;
                flex-direction: column;
                align-items:center;
                justify-content:center;
                padding:4px 0;
                box-shadow:0 4px 20px rgba(0,0,0,0.4);
            }}

            .score-text {{
                font-weight:700;
            }}

            .periods.hidden 
            {{
                visibility: hidden;
            }}

            .footer-strip {{
                position:absolute;
                left:50%;
                transform:translateX(-50%);
                bottom:40px;
                width:82%;
                padding:22px 32px;
                border-radius:18px;
                background: rgba(8,20,40,0.95);
                box-shadow:
                    0 0 30px rgba(0,0,0,0.9),
                    0 0 40px rgba(0,0,0,0.8);
                display:flex;
                justify-content:center;
                align-items:center;
                font-size:48px;
                font-weight:900;
                color:#fff;
            }}
        ";
    }
}
