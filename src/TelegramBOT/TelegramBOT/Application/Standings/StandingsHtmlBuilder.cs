using System.Text;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Models;

namespace TelegramBOT.Application.Standings
{
    /// <summary>
    /// Отвечает за построение HTML-кода турнирной таблицы.
    /// </summary>
    public static class StandingsHtmlBuilder
    {
        public static string Build(List<KeyValuePair<string, TeamStats>> teams, string title, MappingService mapper)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><head><style>");
            sb.AppendLine("body { background: #1e1e1e; color: white; font-family: 'Segoe UI'; padding: 20px; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; font-size: 25px; }");
            sb.AppendLine("th, td { padding: 6px 8px; }");
            sb.AppendLine("th { color: #00bfff; border-bottom: 2px solid #555; }");
            sb.AppendLine("tr:nth-child(even) { background-color: #2a2a2a; }");
            sb.AppendLine("</style></head><body>");
            sb.AppendLine($"<h2>🏒 {title}</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>#</th><th>Команда</th><th>И</th><th>В</th><th>П</th><th>О</th></tr>");

            int i = 1;
            foreach (var t in teams)
                sb.AppendLine($"<tr><td>{i++}</td><td>{mapper.Map("TeamNames", t.Key)}</td><td>{t.Value.GamesPlayed}</td><td>{t.Value.Wins}</td><td>{t.Value.Losses}</td><td>{t.Value.Points}</td></tr>");

            sb.AppendLine("</table></body></html>");
            return sb.ToString();
        }
    }
}
