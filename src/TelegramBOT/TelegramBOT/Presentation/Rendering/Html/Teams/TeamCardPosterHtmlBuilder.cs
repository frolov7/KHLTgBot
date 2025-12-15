using System.Text;
using Microsoft.Extensions.Configuration;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using TelegramBOT.Domain.Teams.TeamCard;
using TelegramBOT.Domain.Entities.Teams;


namespace TelegramBOT.Presentation.Rendering.Html.Teams
{
    public class TeamCardPosterHtmlBuilder
    {
        private readonly IConfiguration _config;

        public TeamCardPosterHtmlBuilder(IConfiguration config)
        {
            _config = config;
        }

        private string GetSquareBg(MatchResultSquare v, string green64, string greenOT64, string greenPEN64, string red64, string redOT64, string redPEN64)
        {
            if (v.IsWin)
            {
                if (v.IsOT)
                    return greenOT64;

                if (v.IsPEN)
                    return greenPEN64;

                return green64; // победа в основное время
            }
            else
            {
                if (v.IsOT)
                    return redOT64;

                if (v.IsPEN)
                    return redPEN64;

                return red64;   // поражение в основное время
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

        private MatchResultSquare ToSquare(MatchResultInfo info)
        {
            return new MatchResultSquare
            {
                IsWin = info.IsWin,
                IsOT = info.IsOT,
                IsPEN = info.IsPEN,
                OpponentTeamName = info.OpponentTeamName,
                OpponentLogoBase64 = GetOpponentLogo(info.OpponentTeamName)
            };
        }

        public string Build(string teamName, string city, TeamCardStats stats)
        {
            string root = Path.Combine(
                "C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons");

            string bgPath = Path.Combine(root, "background.png");
            string teamsDir = Path.Combine(root, "teams");
            string arrowPath = Path.Combine(root, "arrow.png");

            string greenPath = Path.Combine(root, "green.png");
            string redPath = Path.Combine(root, "red.png");

            string greenOTPath = Path.Combine(root, "greenOT.png");
            string greenPENPath = Path.Combine(root, "greenPEN.png");

            string redOTPath = Path.Combine(root, "redOT.png");
            string redPENPath = Path.Combine(root, "redPEN.png");

            string green64 = Convert.ToBase64String(File.ReadAllBytes(greenPath));
            string red64 = Convert.ToBase64String(File.ReadAllBytes(redPath));

            string greenOT64 = Convert.ToBase64String(File.ReadAllBytes(greenOTPath));
            string greenPEN64 = Convert.ToBase64String(File.ReadAllBytes(greenPENPath));

            string redOT64 = Convert.ToBase64String(File.ReadAllBytes(redOTPath));
            string redPEN64 = Convert.ToBase64String(File.ReadAllBytes(redPENPath));

            string bg64 = Convert.ToBase64String(File.ReadAllBytes(bgPath));
            string arrow64 = Convert.ToBase64String(File.ReadAllBytes(arrowPath));

            var teamNameRu = _config
                .GetSection("TeamNamesPlain")
                .Get<Dictionary<string, string>>()?
                .GetValueOrDefault(teamName, teamName);

            string logoPath = Path.Combine(teamsDir, $"{teamName}_logo.png");
            string logo64 = File.Exists(logoPath)
                ? Convert.ToBase64String(File.ReadAllBytes(logoPath))
                : "";

            string totals45Arr = string.Join("",
                stats.Visual.Totals45
                    .AsEnumerable()
                    .Reverse()
                    .Select(v =>
                    {
                        var sq = ToSquare(v);
                        return $@"
                            <div class='square' style='background-image:url(""data:image/png;base64,{(sq.IsWin ? green64 : red64)}"");'>
                                <img src='data:image/png;base64,{sq.OpponentLogoBase64}' />
                            </div>";
                    })
            );

            string totals55Arr = string.Join("",
                stats.Visual.Totals55
                    .AsEnumerable()
                    .Reverse()
                    .Select(v =>
                    {
                        var sq = ToSquare(v);
                        return $@"
                            <div class='square' style='background-image:url(""data:image/png;base64,{(sq.IsWin ? green64 : red64)}"");'>
                                <img src='data:image/png;base64,{sq.OpponentLogoBase64}' />
                            </div>";
                    })
            );

            string results10Arr = string.Join("",
                stats.Visual.Last10
                    .AsEnumerable()
                    .Reverse()
                    .Select(v =>
                    {
                        var sq = ToSquare(v);
                        return $@"
                            <div class='square'
                                style='background-image:url(""data:image/png;base64,{GetSquareBg(sq, green64, greenOT64, greenPEN64, red64, redOT64, redPEN64)}"");'>
                                <img src='data:image/png;base64,{sq.OpponentLogoBase64}' />
                            </div>";
                    })
            );


            // -------------------------------------
            // HTML
            // -------------------------------------

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
                    width: 100vw;
                    min-height: 1500px;
                    overflow: hidden;
                }}

                .bg {{
                    position: absolute;
                    top:0;
                    left:0;
                    width:100vw;
                    height:100%;
                    object-fit:cover;
                    filter: brightness(0.75);
                    z-index:1;
                }}

                .header {{
                    height:250px;
                    display:flex;
                    gap:30px;
                    position:relative;
                    z-index:2;
                    margin-top: 20px;
                }}

                .left-block {{
                    width:40%;
                    background:rgba(255,255,255,0.1);
                    display:flex;
                    flex-direction:column;
                    align-items:center;
                    justify-content:center;
                    border-radius:8px 8px 8px 8px;
                }}

                .team-logo {{
                    width:160px;
                }}

                .team-name {{
                    color:white;
                    font-size:38px;
                    font-weight:900;
                    text-align:center;
                    margin-top:-10px;
                }}

                .team-city {{
                    color:white;
                    font-size:20px;
                    margin-top:2px;
                }}

                .right-block {{
                    width:60%;
                    background:rgba(255,255,255,0.1);
                    display:flex;
                    flex-direction:column;
                    justify-content:center;
                    align-items:center;
                    border-radius:8px 8px 8px 8px;
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

                .content {{
                    width: calc(100% - 40px);
                    max-width: 984px;
                    margin: 0 auto;
                    position: relative;
                    z-index: 2;
                }}

                .main-box {{
                    margin-top:50px;
                    background:rgba(255,255,255,0.1);
                    padding:30px 20px;
                    border-radius:10px;
                }}

                .section-title {{
                    color:white;
                    font-size:32px;
                    font-weight:800;
                    margin-bottom:25px;
                }}

                .grid {{
                    display: grid;
                    grid-template-columns: 1fr 1fr;
                    gap: 20px;
                    margin-bottom: 25px;   /* <-- ДОБАВЛЯЕМ ЭТО */
                }}

                .grid-3 {{margin - top: 25px;
                    display: grid;
                    grid-template-columns: 1fr 1fr 1fr;
                    gap: 20px;
                    margin-bottom: 25px;   /* <-- ТОЖЕ НУЖНО */
                }}

                .cell {{
                    background:none;
                    border-radius:8px;
                    display:flex;
                    flex-direction:column;
                }}

                .cell-title {{
                    background:rgba(220,220,220,0.25);
                    color:white;
                    text-align:center;
                    padding:10px 5px;
                    font-size:26px;
                    font-weight:700;
                    border-radius:8px 8px 0 0;
                }}

                .cell-value {{
                    background:#bfbfbf;
                    color:white;
                    text-align:center;
                    padding:15px 5px;
                    font-size:34px;
                    font-weight:900;
                    border-radius:0 0 8px 8px;
                }}

                .totals-values {{
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    gap: 10px;       /* расстояние между элементами */
                    flex-wrap: nowrap; 
                }}

                .square {{width: 34px;
                    height: 34px;
                    border-radius: 6px;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    overflow: hidden;
                    margin: 0 2px;
                    background-size: cover;
                    background-position: center;
                }}
                .square img {{
                    width: 26px;
                    height: 26px;
                    filter: drop-shadow(0 0 3px white) drop-shadow(0 0 1px white);
                }}

                .result-square {{width: 34px;
                    height: 34px;
                    border-radius: 6px;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    overflow: hidden;
                }}

                .result-square img {{width: 26px;
                    height: 26px;
                    object-fit: contain;
                }}

                .results-row,
                .totals-row {{    
                    display:flex;
                    align-items:center;
                    justify-content:center;
                    gap:3px;
                    min-height:48px; /* было 60 — из-за этого блок стал больше */
                }}

                .totals-values::before,
                .totals-values::after 
                {{
                    opacity: 0.8;
                }}

                .arr {{
                    width: 18px;
                    height: auto;
                    margin: 0 1px;
                    opacity: 0.9;
                }}

                .grid-3 {{
                    margin-top: 25px;
                    display: grid;
                    grid-template-columns: 1fr 1fr 1fr; /* три равных столбца */
                    gap: 20px;
                }}

                </style>
                </head>

                <body>

                <div class='poster'>
                    <img src='data:image/png;base64,{bg64}' class='bg'>

                    <div class='content'>

                        <div class='header'>
                            <div class='left-block'>
                                <img src='data:image/png;base64,{logo64}' class='team-logo'>
                                <div class='team-name'>{teamNameRu}</div>
                                <div class='team-city'>{city}</div>
                            </div>

                            <div class='right-block'>
                                <div class='big-title'>МАТЧ</div>
                                <div class='subtitle'>
                                    Статистика команды<br>за последние {stats.TotalGames} игр
                                </div>
                            </div>
                        </div>

                       <!-- MAIN BOX -->
                        <div class='main-box'>

                            <!-- ========================================================= -->
                            <!-- ===== БЛОК 1 — РЕЗУЛЬТАТЫ МАТЧЕЙ и ИНДЕКС СИЛЫ=========== -->
                            <!-- ========================================================= -->
                            <div class='grid' style='margin-bottom:30px;'>

                                <!-- Результаты 10 матчей -->
                                <div class='cell'>
                                    <div class='cell-title'>Результаты матчей</div>
                                    <div class='cell-value results-row'>
                                        <img src='data:image/png;base64,{arrow64}' class='arr'>
                                        {results10Arr}
                                        <img src='data:image/png;base64,{arrow64}' class='arr'>
                                    </div>

                                </div>


                                <div class='cell'>
                                    <div class='cell-title'>Индекс силы</div>
                                    <div class='cell-value'>NULL</div>
                                </div>
                            </div>



                            <!-- ========================================================= -->
                            <!-- ===== БЛОК 2 — ПОБЕДЫ И ПОРАЖЕНИЯ ======================== -->
                            <!-- ========================================================= -->
                            <div class='grid'>

                                <div class='cell'>
                                    <div class='cell-title'>Побед в основное время / ОТ</div>
                                    <div class='cell-value'>{stats.Results.WinReg} / {stats.Results.WinOT}</div>
                                </div>

                                <div class='cell'>
                                    <div class='cell-title'>Поражений в основное время / ОТ</div>
                                    <div class='cell-value'>{stats.Results.LoseReg} / {stats.Results.LoseOT}</div>
                                </div>

                            </div>



                            <!-- ========================================================= -->
                            <!-- ===== БЛОК 3 — ЗАБИЛИ / ПРОПУСТИЛИ ПЕРВЫМИ и КАМБЭКИ ==== -->
                            <!-- ========================================================= -->
                            <div class='grid'>
                                <div class='cell'>
                                    <div class='cell-title'>Забили / Пропустили первыми</div>
                                    <div class='cell-value'>
                                        {stats.FirstGoal.ScoredFirst} / {stats.FirstGoal.ConcededFirst}
                                    </div>
                                </div>

                                <div class='cell'>
                                    <div class='cell-title'>Камбэк с -2 и не проиграла</div>
                                    <div class='cell-value' style='font-size:32px;'>
                                        {stats.Comebacks.ComebacksNoLossFrom2} / {stats.Comebacks.GamesTrailingBy2}
                                        ({stats.Comebacks.Percent}%)
                                    </div>
                                </div>
                            </div>


                            <!-- ========================================================= -->
                            <!-- ===== БЛОК 4 — ИТ команды и Средний тотал игры ========= -->
                            <!-- ========================================================= -->
                            <div class='grid'>

                                <div class='cell'>
                                    <div class='cell-title'>ИТ команды / соперника (ср.)</div>
                                    <div class='cell-value'>
                                        {stats.Summary.TeamTotal.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}
                                        /
                                        {stats.Summary.OppTotal.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}
                                    </div>
                                </div>

                                <div class='cell'>
                                    <div class='cell-title'>Средний тотал игры</div>
                                    <div class='cell-value'>
                                        {stats.Summary.AvgTotal.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}
                                    </div>
                                </div>

                            </div>



                            <!-- ========================================================= -->
                            <!-- ===== БЛОК 5 — СТАТИСТИКА ПО ПЕРИОДАМ ==================== -->
                            <!-- ========================================================= -->
                            <div class='grid-3'>

                                <div class='cell'>
                                    <div class='cell-title' style='font-size:23px;'>1 период (ИТ/Общ. тотал)</div>
                                    <div class='cell-value'>
                                        {stats.Periods.Period1IT_Avg.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} / {stats.Periods.Period1Total_Avg.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}
                                    </div>
                                </div>

                                <div class='cell'>
                                    <div class='cell-title' style='font-size:23px;'>2 период (ИТ/Общ. тотал)</div>
                                    <div class='cell-value'>
                                        {stats.Periods.Period2IT_Avg.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} / {stats.Periods.Period2Total_Avg.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}
                                    </div>
                                </div>

                                <div class='cell'>
                                    <div class='cell-title' style='font-size:23px;'>3 период (ИТ/Общ. тотал)</div>
                                    <div class='cell-value'>
                                        {stats.Periods.Period3IT_Avg.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} / {stats.Periods.Period3Total_Avg.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}
                                    </div>
                                </div>

                            </div>


                            <!-- ========================================================= -->
                            <!-- ===== БЛОК 6 — ТОТАЛЫ 4.5 и 5.5 =========================== -->
                            <!-- ========================================================= -->
                            <div class='grid' style='margin-top:20px;'>

                                <div class='cell'>
                                    <div class='cell-title'>Тотал больше (4.5)</div>
                                    <div class='cell-value totals-row'>
                                        <img src='data:image/png;base64,{arrow64}' class='arr'>
                                        {totals45Arr}
                                        <img src='data:image/png;base64,{arrow64}' class='arr'>
                                    </div>

                                </div>

                                <div class='cell'>
                                    <div class='cell-title'>Тотал больше (5.5)</div>
                                    <div class='cell-value totals-row'>
                                        <img src='data:image/png;base64,{arrow64}' class='arr'>
                                        {totals55Arr}
                                        <img src='data:image/png;base64,{arrow64}' class='arr'>
                                    </div>

                                </div>

                            </div>

                        </div> <!-- /main-box -->


                    </div> <!-- content -->

                </div> <!-- poster -->

                </body>
                </html>
            ");

            return sb.ToString();
        }
    }
}