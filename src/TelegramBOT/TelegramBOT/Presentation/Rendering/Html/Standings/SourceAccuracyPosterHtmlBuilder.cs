using System.Text;
using TelegramBOT.Domain.Entities.Predictions;

namespace TelegramBOT.Presentation.Rendering.Html.Statistics
{
    public static class SourceAccuracyPosterHtmlBuilder
    {
        public static string Build(List<SourcePredictionStats> sources)
        {
            string root = Path.Combine(
                "C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons"
            );

            string bg64 = Convert.ToBase64String(
                File.ReadAllBytes(Path.Combine(root, "background.png"))
            );

            string green64 = Convert.ToBase64String(
                File.ReadAllBytes(Path.Combine(root, "green.png"))
            );

            string red64 = Convert.ToBase64String(
                File.ReadAllBytes(Path.Combine(root, "red.png"))
            );

            var sb = new StringBuilder();

            sb.Append($@"
                <html>
                <head>
                <meta charset='utf-8'>
                <style>

                @import url('https://fonts.googleapis.com/css2?family=Montserrat:wght@600;700&family=Inter:wght@400;500;600&display=swap');

                body {{
                    margin: 0;
                    font-family: 'Inter', Arial, sans-serif;
                    color: #fff;
                }}

                .bg {{
                    position: fixed;
                    inset: 0;
                    width: 100%;
                    height: 100%;
                    object-fit: cover;
                    filter: brightness(0.75);
                    z-index: -1;
                }}

                .wrapper {{
                    padding: 40px;
                }}

                h2 {{
                    color: #ffd700;
                    font-size: 36px;
                    margin-bottom: 25px;
                }}

                table {{
                    width: 1850px;
                    border-collapse: collapse;
                    font-size: 24px;
                }}

                th, td {{
                    padding: 10px;
                    text-align: center;
                    border-bottom: 1px solid #444;
                }}

                th {{
                    color: #00bfff;
                    font-weight: 600;
                }}

                tr:nth-child(even) {{
                    background: rgba(40,40,40,0.85);
                }}

                .source {{
                    display: flex;
                    align-items: center;
                    gap: 12px;
                    font-weight: 600;
                }}

                .source img {{
                    width: 120px;
                }}

                .form {{
                    display: flex;
                    gap: 4px;
                    justify-content: center;
                }}

                .square {{
                    width: 22px;
                    height: 22px;
                    border-radius: 4px;
                    background-size: cover;
                }}

                </style>
                </head>

                <body>
                <img class='bg' src='data:image/png;base64,{bg64}' />

                <div class='wrapper'>
                <h2>Рейтинг прогнозов по источникам</h2>

                <table>
                <tr>
                    <th>Источник</th>
                    <th>Всего</th>
                    <th>WIN</th>
                    <th>LOSE</th>
                    <th>%</th>
                    <th>Лучший тип</th>
                    <th>Рейтинг</th>
                    <th>Форма</th>
                </tr>
                ");

            foreach (var s in sources)
            {
                string logoPath = Path.Combine(root, s.Source + ".png");
                string logo64 = File.Exists(logoPath)
                    ? Convert.ToBase64String(File.ReadAllBytes(logoPath))
                    : "";

                string form = string.Join("", s.LastResults.Select(r =>
                {
                    var bg = r ? green64 : red64;
                    return $"<div class='square' style='background-image:url(data:image/png;base64,{bg})'></div>";
                }));

                sb.Append($@"
                    <tr>
                        <td>
                            <div class='source'>
                                <img src='data:image/png;base64,{logo64}' />
                                {s.Source.ToUpper()}
                            </div>
                        </td>
                        <td>{s.Total}</td>
                        <td>{s.Win}</td>
                        <td>{s.Lose}</td>
                        <td>{s.HitRate}%</td>
                        <td>{s.BestType ?? "-"}</td>
                        <td>{s.Rating}</td>
                        <td>
                            <div class='form'>{form}</div>
                        </td>
                    </tr>
                ");
            }

            sb.Append(@"
                </table>
                </div>
                </body>
                </html>
            ");

            return sb.ToString();
        }
    }
}
