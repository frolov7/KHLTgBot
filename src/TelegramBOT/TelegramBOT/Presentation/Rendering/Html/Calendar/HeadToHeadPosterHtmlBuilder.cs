using System.Text;
using Microsoft.Extensions.Configuration;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Entities.Matches;

namespace TelegramBOT.Presentation.Rendering.Html.Calendar
{
    public class HeadToHeadPosterHtmlBuilder
    {
        private readonly IConfiguration _config;
        private readonly MappingService _mapper;

        public HeadToHeadPosterHtmlBuilder(IConfiguration config, MappingService mapper)
        {
            _config = config;
            _mapper = mapper;
        }

        public string Build(Match mainMatch, IEnumerable<Match> h2hMatches)
        {
            string root = Path.Combine("C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons");

            string bgPath = Path.Combine(root, "background.png");
            string teamsDir = Path.Combine(root, "teams");

            string bg64 = Convert.ToBase64String(File.ReadAllBytes(bgPath));

            // Список матчей
            var list = h2hMatches.OrderBy(m => m.MatchDate).ToList();
            int matchCount = list.Count;

            if (matchCount == 0)
                matchCount = 1; // защита от пустого списка

            // высота строки
            int rowHeight = matchCount switch
            {
                1 => 150,
                2 => 130,
                3 => 110,
                4 => 80,
                5 => 60,
                6 => 40,
                _ => 30
            };

            // вертикальный сдвиг вниз при малом количестве матчей
            int shift = matchCount switch
            {
                1 => 180,
                2 => 120,
                3 => 30,
                _ => 0
            };

            var rows = new StringBuilder();

            foreach (var m in list)
            {
                string homeLogoPath = Path.Combine(teamsDir, $"{m.HomeTeamName}_logo.png");
                string awayLogoPath = Path.Combine(teamsDir, $"{m.AwayTeamName}_logo.png");

                string homeLogo64 = Convert.ToBase64String(File.ReadAllBytes(homeLogoPath));
                string awayLogo64 = Convert.ToBase64String(File.ReadAllBytes(awayLogoPath));

                string score = $"{m.HomeScore}:{m.AwayScore}";
                if (m.Status == "AFTER OVERTIME") score += " (ОТ)";
                if (m.Status == "AFTER PENALTIES") score += " (Б)";

                string dateShort = m.MatchDate.ToString("dd'/'MM", new System.Globalization.CultureInfo("ru-RU"));

                rows.Append($@"
                    <div class='match-row' style='height:{rowHeight}px'>
                        <img class='team-logo-left' src='data:image/png;base64,{homeLogo64}' />

                        <div class='score-box'>
                            <div class='date-wrapper'>
                                <div class='date-badge'>{dateShort}</div>
                            </div>
                            <div class='score-text'>{score}</div>
                        </div>

                        <img class='team-logo-right' src='data:image/png;base64,{awayLogo64}' />
                    </div>
                ");
            }

            // HTML
            var sb = new StringBuilder();
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");

            sb.AppendLine("<style>");
            sb.AppendLine(NewCss());
            sb.AppendLine("</style>");

            sb.AppendLine("</head><body>");

            sb.AppendLine($@"
                <div class='poster'>
                    <img class='bg' src='data:image/png;base64,{bg64}' />

                    <div class='matches' style='--shift:{shift}px'>
                        {rows}
                    </div>

                    <div class='footer-strip'>
                        Игры между собой
                    </div>
                </div>
            ");

            sb.AppendLine("</body></html>");
            return sb.ToString();
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
                height: 810px;
                overflow: hidden;
            }

            .bg {
                position:absolute;
                width:100%;
                height:100%;
                object-fit: cover;
                filter: brightness(0.75);
            }

            /* === ВАЖНО ===
                Область для матчей фиксированная.
                Всё, что внутри, масштабируется. 
            */
            .matches-wrapper {
                position: absolute;
                top: 80px;
                bottom: 170px;
                width: 100%;
                display: flex;
                justify-content: center;
                align-items: center;   /* ← ВОТ ЭТО! Центрирование по вертикали */
                overflow: hidden;
            }

            .matches {
                position: absolute;
                top: 120px;            /* ← регулирует позицию ВСЕГО блока матчей */
                width: 100%;
                display: flex;
                flex-direction: column;
                align-items: center;   /* центрируем по центру */
                gap: 28px;

                /* ДИНАМИЧЕСКИЙ ЦЕНТР ДЛЯ ОДНОГО/ДВУХ МАТЧЕЙ */
                transform: translateY(var(--shift, 0px));
            }

            /* строка матча */
            .match-row {
                width: 82%;                 /* ← как у плашки */
                background: rgba(0,0,0,0.35);
                border-radius: 22px;
                display: flex;
                justify-content: space-between;
                align-items: center;
                padding: 10px 20px;
                backdrop-filter: blur(4px);
            }

            .team-logo-left,
            .team-logo-right {
                width: 130px;
                height: auto;
            }

            .score-box {
                position: relative;
                background: white;
                color: black;
                width: 320px;
                height: 80px;

                border-radius: 18px;
                box-shadow: 0 4px 20px rgba(0,0,0,0.4);

                display: flex;
                align-items: center;
                justify-content: center;
            }

            .date-wrapper {
                position: absolute;
                left: 5px; /* ← ТОЧНО 5 пикселей от края */
                top: 50%;
                transform: translateY(-50%);
            }


            .date-badge {
                background: #000813;
                color: #fff;
                font-size: 10px;      /* уменьшили текст */
                font-weight: 700;
                padding: 4px 10px;    /* уменьшили плашку */
                border-radius: 8px;   /* чуть менее округлая */
                display: inline-flex;
                min-width: 20px;      /* уменьшили ширину */
                justify-content: center;
            }

            .score-text {
                position: absolute;
                left: 50%;
                transform: translateX(-50%);
                font-size: 42px;
                font-weight: 900;
                white-space: nowrap;     /* ← главное! */
                line-height: 1;          /* минимальная высота строки */
            }

            /* Плашка */
            .footer-strip {
                position: absolute;
                left: 50%;
                transform: translateX(-50%);
                bottom: 40px;
                width: 82%;
                padding: 22px 32px;
                border-radius: 18px;
                background: linear-gradient(90deg, rgba(8,20,40,0.95), rgba(6,35,70,0.95));
                box-shadow:
                    0 0 30px rgba(0,0,0,0.9),
                    0 0 40px rgba(0,0,0,0.8);
                display: flex;
                justify-content: center;
                align-items: center;
                z-index: 6;

                font-size: 48px;
                font-weight: 900;
                color: #ffffff;
                text-align: center;
            }";
    }
}
