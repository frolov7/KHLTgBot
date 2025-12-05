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

        public string Build(string teamName, string city, TeamCardStats stats)
        {
            string root = Path.Combine(
                "C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons");

            string bgPath = Path.Combine(root, "background.png");
            string teamsDir = Path.Combine(root, "teams");
            string arrowPath = Path.Combine(root, "arrow.png");

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

            // Формируем emoji-полоски тоталов
            string totals45 = string.Concat(stats.Totals45
                .Select(v => v ? "🟩" : "🟥")
                .Reverse());

            string totals55 = string.Concat(stats.Totals55
                .Select(v => v ? "🟩" : "🟥")
                .Reverse());

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
                    font-size:46px;
                    font-weight:900;
                    text-align:center;
                    margin-top:-10px;
                }}

                .team-city {{
                    color:white;
                    font-size:22px;
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
                    display:grid;
                    grid-template-columns: 1fr 1fr;
                    gap:20px;
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

                .totals-values::before,
                .totals-values::after 
                {{
                    opacity: 0.8;
                }}

                .arr {{
                    width: 28px;
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
        
                            <div class='section-title'>
                                Тотал матча (ср.) : {stats.AvgTotal:F1}
                            </div>

                            <!-- Основные показатели -->
                            <div class='grid'>

                                <div class='cell'>
                                    <div class='cell-title'>Побед в осн./ОТ</div>
                                    <div class='cell-value'>{stats.WinReg}/{stats.WinOT}</div>
                                </div>

                                <div class='cell'>
                                    <div class='cell-title'>Поражений в осн./ОТ</div>
                                    <div class='cell-value'>{stats.LoseReg}/{stats.LoseOT}</div>
                                </div>

                                <div class='cell'>
                                    <div class='cell-title'>Забили первыми</div>
                                    <div class='cell-value'>{stats.ScoredFirst}/{stats.TotalGames}</div>
                                </div>

                                <div class='cell'>
                                    <div class='cell-title'>Пропустили первыми</div>
                                    <div class='cell-value'>{stats.ConcededFirst}/{stats.TotalGames}</div>
                                </div>

                                <div class='cell'>
                                    <div class='cell-title'>ИТ команды (ср.)</div>
                                    <div class='cell-value'>{stats.TeamTotal:F1}</div>
                                </div>

                                <div class='cell'>
                                    <div class='cell-title'>ИТ соперника (ср.)</div>
                                    <div class='cell-value'>{stats.OppTotal:F1}</div>
                                </div>

                            </div>

                            <!-- ===== ТАБЛИЦА ПО ПЕРИОДАМ ===== -->
                            <div class='grid-3'>
                                <div class='cell'>
                                    <div class='cell-title' style='font-size:23px;'>1 период (ИТ/Общ. Тотал)</div>
                                    <div class='cell-value'>{stats.Period1IT_Avg} / {stats.Period1Total_Avg}</div>
                                </div>

                                <div class='cell'>
                                    <div class='cell-title' style='font-size:23px;'>2 период (ИТ/Общ. Тотал)</div>
                                    <div class='cell-value'>{stats.Period2IT_Avg} / {stats.Period2Total_Avg}</div>
                                </div>

                                <div class='cell'>
                                    <div class='cell-title' style='font-size:23px;'>3 период (ИТ/Общ. Тотал)</div>
                                    <div class='cell-value'>{stats.Period3IT_Avg} / {stats.Period3Total_Avg}</div>
                                </div>
                            </div>

                            <!-- Таблица тоталов -->
                            <div class='grid' style='margin-top:20px;'>

                             <div class='cell'>
                                <div class='cell-title'>Тотал больше (4.5)</div>
                                <div class='cell-value totals-values'>
                                    <img src='data:image/png;base64,{arrow64}' class='arr'>
                                    {totals45}
                                    <img src='data:image/png;base64,{arrow64}' class='arr'>
                                </div>
                            </div>

                            <div class='cell'>
                                <div class='cell-title'>Тотал больше (5.5)</div>
                                <div class='cell-value totals-values'>
                                    <img src='data:image/png;base64,{arrow64}' class='arr'>
                                    {totals55}
                                    <img src='data:image/png;base64,{arrow64}' class='arr'>
                                </div>
                            </div>

                            </div>

                        </div> <!-- main-box -->

                    </div> <!-- content -->

                </div> <!-- poster -->

                </body>
                </html>
            ");

            return sb.ToString();
        }
    }
}