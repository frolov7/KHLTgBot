using System.Text;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Entities.Teams;

namespace TelegramBOT.Application.Standings
{
    public static class StandingsHtmlBuilder
    {
        public static string Build(List<KeyValuePair<string, TeamStats>> standings, string title, MappingService mapper)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<html><head><style>");
            sb.AppendLine(@"
            body { 
                background: #1e1e1e; 
                color: white; 
                font-family: 'Segoe UI', sans-serif; 
                padding: 40px; 
            }
            h2 { 
                color: #ffd700; 
                font-size: 36px; 
                margin-bottom: 25px; 
                text-align: left;
            }
            table { 
                width: 1850px;
                border-collapse: collapse; 
                font-size: 25px; 
                margin: 0 auto; 
                table-layout: fixed; 
                white-space: nowrap; 
            }
            th, td { 
                padding: 7px 10px; 
                text-align: center; 
                border-bottom: 1px solid #444; 
            }
            th { 
                color: #00bfff; 
                border-bottom: 3px solid #555; 
            }
            tr:nth-child(even) { background-color: #2a2a2a; }
            tr:hover { background-color: #333; }

            /* --- Ширина колонок --- */
            th:nth-child(1), td:nth-child(1) { width: 60px; text-align: right; }    
            th:nth-child(2), td:nth-child(2) { width: 350px; text-align: left; }    
            th:nth-child(3), td:nth-child(3) { width: 60px; }                       
            th:nth-child(4), td:nth-child(4) { width: 60px; }                       
            th:nth-child(5), td:nth-child(5) { width: 60px; }                       
            th:nth-child(6), td:nth-child(6) { width: 60px; }                       
            th:nth-child(7), td:nth-child(7) { width: 60px; }                       
            th:nth-child(8), td:nth-child(8) { width: 60px; }                       
            th:nth-child(9), td:nth-child(9) { width: 60px; }                       
            th:nth-child(10), td:nth-child(10) { width: 120px; }                    
            th:nth-child(11), td:nth-child(11) { width: 70px; }                     
            th:nth-child(12), td:nth-child(12) { width: 250px; font-size: 22px; }   

            /* --- Легенда под таблицей --- */
.legend {
    width: 100%;
   margin: 0 auto;
    font-size: 13px;
    color: #ccc;
    text-align: left;
}

.legend table {
    border: none;
    border-collapse: collapse;
    width: auto;
}

.legend td {
    border: none;
    padding: 1px 30px 4px 0;
    text-align: left;
    vertical-align: top;
    white-space: nowrap;
    line-height: 1.4;
    font-size: 13px;
}
        ");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine($"<h2>{title}</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>#</th><th>Команда</th><th>И</th><th>В</th><th>ВО</th><th>ВБ</th><th>ПБ</th><th>ПО</th><th>П</th><th>Ш</th><th>О</th><th>Форма</th></tr>");

            int i = 1;
            foreach (var t in standings)
            {
                string teamName = mapper.Map("TeamNames", t.Key);
                string goalsDiff = $"{t.Value.GoalsFor}-{t.Value.GoalsAgainst}";
                string form = t.Value.RecentForm.Count > 0 ? string.Join("", t.Value.RecentForm) : "—";

                sb.AppendLine(
                    $"<tr>" +
                    $"<td>{i++}</td>" +
                    $"<td>{teamName}</td>" +
                    $"<td>{t.Value.GamesPlayed}</td>" +
                    $"<td>{t.Value.Wins}</td>" +
                    $"<td>{t.Value.OvertimeWins}</td>" +
                    $"<td>{t.Value.ShootoutWins}</td>" +
                    $"<td>{t.Value.ShootoutLosses}</td>" +
                    $"<td>{t.Value.OvertimeLosses}</td>" +
                    $"<td>{t.Value.Losses}</td>" +
                    $"<td>{goalsDiff}</td>" +
                    $"<td>{t.Value.Points}</td>" +
                    $"<td>{form}</td>" +
                    $"</tr>");
            }

            sb.AppendLine("</table>");

            // === Легенда ===
            sb.AppendLine(@"
                <div class='legend'>
                    <table>
                        <tr>
                            <td><b>И</b> — количество проведённых игр</td>
                            <td><b>В</b> — выигрыши в основное время</td>
                            <td><b>ВО</b> — выигрыши в овертайме</td>
                            <td><b>ВБ</b> — выигрыши в серии буллитов</td>
                        </tr>
                        <tr>
                            <td><b>ПО</b> — проигрыши в овертайме</td>
                            <td><b>ПБ</b> — проигрыши в серии буллитов</td>
                            <td><b>П</b> — проигрыши в основное время</td>
                            <td><b>Ш</b> — шайбы</td>
                        </tr>
                        <tr>
                            <td><b>О</b> — количество набранных очков</td>
                            <td><b>Форма</b> — последние 5 матчей</td>
                        </tr>
                    </table>
                </div>
            ");

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }
    }
}
