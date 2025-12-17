using System.Text;
using Microsoft.Extensions.Configuration;
using TelegramBOT.Domain.Entities.Predictions;

namespace TelegramBOT.Presentation.Rendering.Html.Calendar
{
    public class MatchPredictionPosterHtmlBuilder
    {
        private readonly IConfiguration _config;

        public MatchPredictionPosterHtmlBuilder(IConfiguration config)
        {
            _config = config;
        }

        public enum PredictionPosterMode
        {
            Calendar,
            Result
        }

        private static string GetFooterText(PredictionPosterMode mode) => mode switch
        {
            PredictionPosterMode.Calendar => "Общий прогноз на матч",
            PredictionPosterMode.Result => "Результаты прогнозов",
            _ => "Прогнозы на матч"
        };

        public string Build(IEnumerable<Prediction> predictions, string home, string away, PredictionPosterMode mode = PredictionPosterMode.Calendar)
        {
            // ==== Пути ====
            string root = Path.Combine("C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons");

            string bgPath = Path.Combine(root, "background.png");
            string sourcesDir = root; // логотипы тоже лежат тут

            string teamsDir = Path.Combine(root, "teams");
            string homeBgLogo = TryLoadLogo(teamsDir, home + "_logo");
            string awayBgLogo = TryLoadLogo(teamsDir, away + "_logo");

            string footerText = GetFooterText(mode);

            // ==== Base64 ====
            string bg64 = Convert.ToBase64String(File.ReadAllBytes(bgPath));

            // Все источники в жёстком порядке
            var allSources = new[]
            {
                "vseprosport",
                "vprognoze",
                "stavkatv",
                "betzona",
                "legalbet",
                "metaratings",
                "livesport"
            };

            var rows = new StringBuilder();

            foreach (var src in allSources)
            {
                var p = predictions.FirstOrDefault(x =>
                    x.Source.Equals(src, StringComparison.OrdinalIgnoreCase));

                string logo = TryLoadLogo(sourcesDir, src) ?? "";
                string value = "-";

                if (p != null)
                {
                    var main = string.IsNullOrWhiteSpace(p.MainPrediction) ? "-" : p.MainPrediction.Trim();
                    var alt = string.IsNullOrWhiteSpace(p.AltPrediction) ? "" : $", {p.AltPrediction.Trim()}";
                    value = $"{main}{alt}";
                }

                rows.AppendLine(BuildRow(logo, value, p, mode));
            }

            var sb = new StringBuilder();

            // ==== HTML ====
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine("<style>");
            sb.AppendLine(NewCss());
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            sb.AppendLine(@"
                <div class='poster'>
                    <img class='bg' src='data:image/png;base64," + bg64 + @"'/>
            ");

            // вставляем большие прозрачные логотипы поверх фона
            if (homeBgLogo != null)
                sb.AppendLine($"<img class='logo-bg-left' src='data:image/png;base64,{homeBgLogo}' />");

            if (awayBgLogo != null)
                sb.AppendLine($"<img class='logo-bg-right' src='data:image/png;base64,{awayBgLogo}' />");

            sb.AppendLine("<div class='table'>");
            sb.AppendLine("<div class='table-inner'>");   // ← ВАЖНО
            sb.AppendLine(rows.ToString());
            sb.AppendLine("</div>");                      // ← закрываем table-inner
            sb.AppendLine("</div>");                      // ← закрываем table

            sb.AppendLine($@"
                <div class='footer'>
                    {footerText}
                </div>
            ");

            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        // ==== Одна строка таблицы ====
        private string BuildRow(string logoBase64, string value, Prediction? prediction, PredictionPosterMode mode)
        {
            string iconFile;

            if (mode == PredictionPosterMode.Calendar)
                iconFile = "null";
            else
            {
                if (prediction == null || value == "-")
                    iconFile = "empty";
                else
                {
                    iconFile = prediction.Result switch
                    {
                        "WIN" => "win",
                        "LOSE" => "lose",
                        "DRAW" => "draw",
                        _ => "empty"
                    };
                }
            }

            string resultIconHtml = "";

            string iconPath = Path.Combine(
                "C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons",
                iconFile + ".png"
            );

            if (File.Exists(iconPath))
            {
                var icon64 = Convert.ToBase64String(File.ReadAllBytes(iconPath));
                resultIconHtml = $"<img class='result-icon' src='data:image/png;base64,{icon64}' />";
            }

            return $@"
                <div class='row'>
                    {resultIconHtml}
                    <img class='icon' src='data:image/png;base64,{logoBase64}' />
                    <div class='value'>: {value}</div>
                </div>
            ";
        }

        // ==== Логотип ====
        private string? TryLoadLogo(string dir, string file)
        {
            string path = Path.Combine(dir, file + ".png");
            if (!File.Exists(path))
                return null;

            return Convert.ToBase64String(File.ReadAllBytes(path));
        }

        // ==== CSS ====
        private string NewCss() => @"
            body, html {
                margin:0;
                padding:0;
                width:100%;
                height:100%;
                font-family: Inter, Arial, sans-serif;
            }

            .poster {
                position:relative;
                width:1100px;
                height:900px;
                overflow:hidden;
            }

            .bg {
                position:absolute;
                width:100%;
                height:100%;
                object-fit:cover;
                filter:brightness(0.75);
            }

            .logo-bg-left {
                position:absolute;
                left:10px;
                top:120px;
                width:420px;
                opacity:0.18;
                pointer-events:none;
            }

            .logo-bg-right {
                position:absolute;
                right:10px;
                top:120px;
                width:420px;
                opacity:0.18;
                pointer-events:none;
            }

            .table {
                position: absolute;
                top: 50px;
                left: 50%;
                transform: translateX(-50%);
                width: 820px;
                display: flex;
                justify-content: center;
                background: rgba(0,0,0,0.40);
                padding: 40px;
                border-radius: 25px;
                backdrop-filter: blur(4px);
            }

            .table-inner {
                width: 960px;
                margin-left: -50px;
    
                display: flex;
                flex-direction: column;
                gap: 25px;
            }

            .row {
                display: flex;
                align-items: center;
                gap: 20px;
                padding: 12px 20px;
            }

            .icon {
                width:190px;
                height:auto;
            }

            .value {
                font-size:36px;
                font-weight:700;
                color:white;
                transform: translateY(-4px);
            }

            .footer {
                position:absolute;
                bottom:40px;
                left:50%;
                transform:translateX(-50%);
                width:80%;
                padding:22px 32px;
                background:rgba(8,20,40,0.95);
                border-radius:18px;
                text-align:center;
                font-size:48px;
                font-weight:900;
                color:white;
                box-shadow:
                    0 0 30px rgba(0,0,0,0.85),
                    0 0 40px rgba(0,0,0,0.85);
            }

            .result-icon {
                width:30px;
                height:30px;
                margin-left:18px;
            }
        ";
    }
}
