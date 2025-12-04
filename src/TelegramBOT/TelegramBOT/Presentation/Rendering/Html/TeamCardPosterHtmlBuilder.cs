using System.Text;
using Microsoft.Extensions.Configuration;
using TelegramBOT.Domain.Models;

namespace TelegramBOT.Presentation.Rendering.Html
{
    public class TeamCardPosterHtmlBuilder
    {
        private readonly IConfiguration _config;

        public TeamCardPosterHtmlBuilder(IConfiguration config)
        {
            _config = config;
        }

        public string Build(string teamNameRu, string arena, List<Match> matches)
        {
            string root = Path.Combine(
                "C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons");

            string bgPath = Path.Combine(root, "background.png");
            string teamsDir = Path.Combine(root, "teams");

            string bg64 = Convert.ToBase64String(File.ReadAllBytes(bgPath));
            string logoPath = Path.Combine(teamsDir, $"{matches.First().HomeTeamName}_logo.png");
            string logo64 = Convert.ToBase64String(File.ReadAllBytes(logoPath));

            // ---------------------------
            // ==== СТАТИСТИКА 15 ИГР ====
            // ---------------------------

            int games = matches.Count;

            int winsOT = matches.Count(m =>
                (m.Status == "AFTER OVERTIME" || m.Status == "AFTER PENALTIES") &&
                ((m.HomeTeamName == matches.First().HomeTeamName && m.HomeScore > m.AwayScore) ||
                 (m.AwayTeamName == matches.First().HomeTeamName && m.AwayScore > m.HomeScore)));

            int lossesOT = matches.Count(m =>
                (m.Status == "AFTER OVERTIME" || m.Status == "AFTER PENALTIES") &&
                ((m.HomeTeamName == matches.First().HomeTeamName && m.HomeScore < m.AwayScore) ||
                 (m.AwayTeamName == matches.First().HomeTeamName && m.AwayScore < m.HomeScore)));

            int scoredFirst = matches.Count(m =>
                (m.HomeTeamName == matches.First().HomeTeamName && m.HomeScore > 0 && m.HomeScore > m.AwayScore) ||
                (m.AwayTeamName == matches.First().HomeTeamName && m.AwayScore > 0 && m.AwayScore > m.HomeScore));

            int concededFirst = games - scoredFirst;

double itTeam = matches
    .Select(m => m.HomeTeamName == matches.First().HomeTeamName
        ? (m.HomeScore ?? 0)
        : (m.AwayScore ?? 0))
    .Average();

double itOpp = matches
    .Select(m => m.HomeTeamName == matches.First().HomeTeamName
        ? (m.AwayScore ?? 0)
        : (m.HomeScore ?? 0))
    .Average();

            double avgTotal = (itTeam + itOpp);

            // ======== тоталы ========
            string totals45 = "";
            string totals55 = "";

            foreach (var m in matches)
            {
                int sum = (m.HomeScore ?? 0) + (m.AwayScore ?? 0);

                totals45 += sum >= 5 ? "🟩" : "🟥";
                totals55 += sum >= 6 ? "🟩" : "🟥";
            }

            // ---------------------------
            // ========= HTML ============
            // ---------------------------

            var sb = new StringBuilder();

            sb.Append($@"
<html>
<head>
<meta charset='utf-8'>
<style>
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
    min-height: 1500px;
    overflow: visible;
    z-index: 1;
}}

.bg {{
    position:absolute;
    top:0;
    left:0;
    width:100%;
    height:100%;
    object-fit: cover;
    filter: brightness(0.75);
    z-index: 0;
}}

.header {{
    display:flex;
    width:100%;
    height:250px;
}}

.header,
.main-box {{position: relative;
    z-index: 5;
}}

.left-block {{
    width:40%;
    background:rgba(0,0,0,0.5);
    display:flex;
    flex-direction:column;
    align-items:center;
    justify-content:center;
}}

.team-logo {{
    width:160px;
}}

.team-name {{
    color:white;
    font-size:46px;
    font-weight:900;
    text-align:center;
}}

.team-city {{
    margin-top:-10px;
    color:white;
    font-size:28px;
}}

.right-block {{
    width:60%;
    background:rgba(0,0,0,0.35);
    display:flex;
    flex-direction:column;
    justify-content:center;
    align-items:center;
}}

.big-title {{
    font-size:90px;
    font-weight:900;
    color:white;
}}

.subtitle {{
    margin-top:-10px;
    color:white;
    font-size:30px;
    font-weight:600;
}}

.main-box {{
    margin-top:20px;
    width:100%;
    background:rgba(0,0,0,0.4);
    border-radius:10px;
    padding:30px 20px;
}}

.section-title {{
    color:white;
    font-size:32px;
    font-weight:800;
    margin-bottom:25px;
}}

.grid {{
    display:grid;
    grid-template-columns: 1fr 1fr;
    gap:20px;
}}

.cell {{
    background:#dcdcdc;
    padding:20px;
    border-radius:8px;
    text-align:center;
    font-size:28px;
    font-weight:700;
}}

.value {{
    margin-top:10px;
    font-size:32px;
    font-weight:900;
}}

.totals-row {{
    margin-top:30px;
    display:grid;
    grid-template-columns: 1fr 1fr;
    gap:20px;
}}

.totals-box {{
    background:#dcdcdc;
    padding:20px;
    border-radius:8px;
    font-size:28px;
    font-weight:700;
}}

.totals-values {{
    margin-top:15px;
    font-size:40px;
    letter-spacing:5px;
}}
</style>
</head>

<body>
<div class='poster'>
    <img src='data:image/png;base64,{bg64}' class='bg' />

    <div class='header'>
        <div class='left-block'>
            <img src='data:image/png;base64,{logo64}' class='team-logo'>
            <div class='team-name'>{teamNameRu}</div>
            <div class='team-city'>{arena}</div>
        </div>

        <div class='right-block'>
            <div class='big-title'>МАТЧ</div>
            <div class='subtitle'>Статистика команды<br>за последние 15 игр</div>
        </div>
    </div>

    <div class='main-box'>
        <div class='section-title'>Тотал матча (ср.) : {avgTotal:F1}</div>

        <div class='grid'>
            <div class='cell'>
                Победа в БезОТ/ОТ
                <div class='value'>{winsOT}/{games - winsOT}</div>
            </div>

            <div class='cell'>
                Поражение БезОТ/ОТ
                <div class='value'>{lossesOT}/{games - lossesOT}</div>
            </div>

            <div class='cell'>
                Забили первыми
                <div class='value'>{scoredFirst}/{games}</div>
            </div>

            <div class='cell'>
                Пропустили первыми
                <div class='value'>{concededFirst}/{games}</div>
            </div>

            <div class='cell'>
                ИТ команды (ср.)
                <div class='value'>{itTeam:F1}</div>
            </div>

            <div class='cell'>
                ИТ соперников (ср.)
                <div class='value'>{itOpp:F1}</div>
            </div>
        </div>

        <div class='totals-row'>
            <div class='totals-box'>
                Тотал больше (4.5)
                <div class='totals-values'>{totals45}</div>
            </div>

            <div class='totals-box'>
                Тотал больше (5.5)
                <div class='totals-values'>{totals55}</div>
            </div>
        </div>

    </div>

</div>
</body>
</html>
");

            return sb.ToString();
        }
    }
}
