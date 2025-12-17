using System.ComponentModel;
using System.Text;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Entities.Teams;
using TelegramBOT.Domain.Teams.TeamCard;

namespace TelegramBOT.Presentation.Rendering.Html.Standings
{
    public static class StandingsPosterHtmlBuilder
    {
        private static string GetTeamLogo(string teamName)
        {
            string path = Path.Combine(
                "C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons", "teams",
                $"{teamName}_logo.png"
            );

            return File.Exists(path)
                ? Convert.ToBase64String(File.ReadAllBytes(path))
                : "";
        }

        private static string GetSquareBg(MatchResultInfo v, string green64, string greenOT64, string greenPEN64, string red64, string redOT64, string redPEN64)
        {
            if (v.IsWin)
            {
                if (v.IsOT) return greenOT64;
                if (v.IsPEN) return greenPEN64;
                return green64;
            }
            else
            {
                if (v.IsOT) return redOT64;
                if (v.IsPEN) return redPEN64;
                return red64;
            }
        }

        private static string GetOpponentLogo(string opponentName)
        {
            string path = Path.Combine(
                "C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons", "teams",
                $"{opponentName}_logo.png"
            );

            return File.Exists(path)
                ? Convert.ToBase64String(File.ReadAllBytes(path))
                : "";
        }

        public static string Build(List<KeyValuePair<string, TeamStats>> standings, string title, MappingService mapper)
        {
            string root = Path.Combine(
                "C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons");

            string bgPath = Path.Combine(root, "background.png");
            string bg64 = Convert.ToBase64String(File.ReadAllBytes(bgPath));

            string green64 = Convert.ToBase64String(File.ReadAllBytes(Path.Combine(root, "green.png")));
            string red64 = Convert.ToBase64String(File.ReadAllBytes(Path.Combine(root, "red.png")));

            string greenOT64 = Convert.ToBase64String(File.ReadAllBytes(Path.Combine(root, "greenOT.png")));
            string greenPEN64 = Convert.ToBase64String(File.ReadAllBytes(Path.Combine(root, "greenPEN.png")));

            string redOT64 = Convert.ToBase64String(File.ReadAllBytes(Path.Combine(root, "redOT.png")));
            string redPEN64 = Convert.ToBase64String(File.ReadAllBytes(Path.Combine(root, "redPEN.png")));

            var sb = new StringBuilder();

            sb.Append($@"
                <html>
                <head>
                <meta charset='utf-8'>
                <style>

                @import url('https://fonts.googleapis.com/css2?family=Montserrat:wght@500;600;700&family=Inter:wght@400;500;600&display=swap');

                body {{ 
                    position: relative;
                    margin: 0;
                    padding: 0;
                    min-width: max-content;
                    font-family: 'Inter', Arial, sans-serif;
                }}

                .bg {{
                    position: absolute;
                    top: 0;
                    left: 0;
                    width: 100%;
                    height: 100%;
                    object-fit: cover;
                    z-index: -1;
                    filter: brightness(0.75);
                }}

                .content {{padding: 40px;
                    width: max-content;      /* ← растягивается по таблице */
                }}

                h2 {{ 
                    color: #ffd700; 
                    font-size: 36px; 
                    margin-bottom: 25px; 
                    text-align: left;
                }}

                table {{ 
                    width: 1850px;
                    border-collapse: collapse; 
                    font-size: 25px; 
                    margin: 0; 
                    table-layout: fixed; 
                    white-space: nowrap; 
                }}

                th, td {{ 
                    padding: 7px 10px; 
                    text-align: center; 
                    border-bottom: 1px solid #444; 
                    color: #FFFFFF;           /* ← ВОТ ЭТОГО НЕ ХВАТАЛО */
                }}

                th {{ 
                    font-family: 'Montserrat', Arial, sans-serif;
                    font-weight: 600;
                    color: #00bfff;
                    border-bottom: 3px solid #555;
                    letter-spacing: 0.5px;
                }}

                tr:nth-child(even) {{ background-color: rgba(42,42,42,0.85); }}
                tr:hover {{ background-color: rgba(51,51,51,0.9); }}

                /* --- Ширина колонок --- */
                th:nth-child(1), td:nth-child(1) {{ width: 60px; text-align: right; }}    
                th:nth-child(2), td:nth-child(2) {{ width: 350px; text-align: left; }}    
                th:nth-child(3), td:nth-child(3) {{ width: 60px; }}                       
                th:nth-child(4), td:nth-child(4) {{ width: 60px; }}                       
                th:nth-child(5), td:nth-child(5) {{ width: 60px; }}                       
                th:nth-child(6), td:nth-child(6) {{ width: 60px; }}                       
                th:nth-child(7), td:nth-child(7) {{ width: 60px; }}                       
                th:nth-child(8), td:nth-child(8) {{ width: 60px; }}                       
                th:nth-child(9), td:nth-child(9) {{ width: 60px; }}                       
                th:nth-child(10), td:nth-child(10) {{ width: 120px; }}                    
                th:nth-child(11), td:nth-child(11) {{ width: 70px; }}                     
                th:nth-child(12), td:nth-child(12) {{ width: 250px; font-size: 22px; text-align: center;}}   

                /* --- Легенда под таблицей --- */
                .legend {{
                    width: 1850px;
                    margin-top: 12px;

                    display: grid;
                    grid-template-columns: repeat(4, max-content);
                    gap: 6px 22px;

                    font-family: 'Inter', Arial, sans-serif;
                    font-size: 13px;
                    font-weight: 400;
                    color: #E0E0E0;
                }}

                .legend > div {{
                    white - space: nowrap;
                }}

                .legend table {{
                    border: none;
                    border-collapse: collapse;
                    width: auto;
                }}

                .legend td {{
                    border: none;
                    padding: 1px 20px 4px 0;
                    text-align: left;
                    vertical-align: top;
                    white-space: nowrap;
                    line-height: 1.4;
                    font-size: 13px;
                }}

                .form-row {{
                    display: flex;
                    gap: 4px;
                    justify-content: center;
                    align-items: center;
                }}

                .square {{
                    width: 26px;
                    height: 26px;
                    border-radius: 4px;
                    background-size: cover;
                    background-position: center;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                }}

                .square img {{
                    width: 18px;
                    height: 18px;
                    filter: drop-shadow(0 0 3px white) drop-shadow(0 0 1px white);
                }}

                .team-cell {{
                    display: flex;
                    align-items: center;
                    gap: 10px;
                }}

                .team-logo {{
                    width: 32px;
                    height: 32px;
                    object-fit: contain;
                    filter: drop-shadow(0 0 1px white) drop-shadow(0 0 1px white);
                }}

                .team-name {{
                    white-space: nowrap;
                    font-family: 'Montserrat', Arial, sans-serif;
                    font-weight: 600;
                    font-size: 24px;
                    letter-spacing: 0.3px;
                }}

                </style>
                </head>

                <body>

                <img src='data:image/png;base64,{bg64}' class='bg' />
                <div class='content'>
                <h2>{title}</h2>
                <table>
                <tr>
                    <th>#</th>
                    <th>Команда</th>
                    <th>И</th>
                    <th>В</th>
                    <th>ВО</th>
                    <th>ВБ</th>
                    <th>ПБ</th>
                    <th>ПО</th>
                    <th>П</th>
                    <th>Ш</th>
                    <th>О</th>
                    <th>Форма</th>
                </tr>
            ");

            int i = 1;
            foreach (var t in standings)
            {
                string teamNamePlain = mapper.Map("TeamNamesPlain", t.Key);
                string teamLogo = GetTeamLogo(t.Key);

                string goalsDiff = $"{t.Value.GoalsFor}-{t.Value.GoalsAgainst}";
                string formHtml = string.Join("",
                t.Value.RecentForm
                    .TakeLast(7)
                    .Select(v =>
                    {
                        var bg = GetSquareBg(
                            v,
                            green64, greenOT64, greenPEN64,
                            red64, redOT64, redPEN64
                        );

                        var oppLogo = GetOpponentLogo(v.OpponentTeamName);

                        return $@"
                            <div class='square'
                                    style='background-image:url(""data:image/png;base64,{bg}"");'>
                                <img src='data:image/png;base64,{oppLogo}' />
                            </div>";
                    })
                );

                sb.Append($@"
                    <tr>
                        <td>{i++}</td>
                        <td>
                            <div class='team-cell'>
                                <img class='team-logo' src='data:image/png;base64,{teamLogo}' />
                                <span class='team-name'>{teamNamePlain}</span>
                            </div>
                        </td>

                        <td>{t.Value.GamesPlayed}</td>
                        <td>{t.Value.Wins}</td>
                        <td>{t.Value.OvertimeWins}</td>
                        <td>{t.Value.ShootoutWins}</td>
                        <td>{t.Value.ShootoutLosses}</td>
                        <td>{t.Value.OvertimeLosses}</td>
                        <td>{t.Value.Losses}</td>
                        <td>{goalsDiff}</td>
                        <td>{t.Value.Points}</td>
                        <td>
                            <div class='form-row'>
                                {formHtml}
                            </div>
                        </td>

                    </tr>
                ");
            }

            sb.Append(@"
                </table>

                <div class='legend'>
                    <div><b>И</b> — количество проведённых игр</div>
                    <div><b>В</b> — выигрыши в основное время</div>
                    <div><b>ВО</b> — выигрыши в овертайме</div>
                    <div><b>ВБ</b> — выигрыши в серии буллитов</div>

                    <div><b>ПО</b> — проигрыши в овертайме</div>
                    <div><b>ПБ</b> — проигрыши в серии буллитов</div>
                    <div><b>П</b> — проигрыши в основное время</div>
                    <div><b>Ш</b> — шайбы</div>

                    <div><b>О</b> — количество набранных очков</div>
                    <div><b>Форма</b> — последние 5 матчей</div>
                </div>

                </div> 
                </body>
                </html>
            ");

            return sb.ToString();
        }
    }
}
