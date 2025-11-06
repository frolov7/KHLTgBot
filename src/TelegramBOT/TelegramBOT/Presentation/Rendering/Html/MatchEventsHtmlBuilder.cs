using System.IO;
using System.Text;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Models;

namespace TelegramBOT.Presentation.Rendering.Html
{
    public static class MatchEventsHtmlBuilder
    {
        public static string Build(Match match, IEnumerable<MatchEvent> events, MappingService mapper)
        {
            var (homePretty, awayPretty) = mapper.MapTeamNames(match);
            var sb = new StringBuilder();

            sb.AppendLine("<html><head><meta charset='utf-8'><style>");
            sb.AppendLine(MatchEventsCss.Get());
            sb.AppendLine("</style></head><body><div class='card'>");

            sb.AppendLine("<h2>События матча</h2>");
            sb.AppendLine($"<div class='teams'><span class='home'>{homePretty}</span> vs <span class='away'>{awayPretty}</span></div>");

            // === ГРУППИРОВКА ПО ПЕРИОДАМ ===
            foreach (var periodGroup in events.OrderBy(e => e.Period).GroupBy(e => e.Period))
            {
                sb.AppendLine("<div class='period'>");
                sb.AppendLine($"<div class='period-title'>{PeriodTitle(periodGroup.Key)}</div>");
                sb.AppendLine("<table>");

                var periodList = periodGroup.OrderBy(e => e.Time).ToList();
                var groupedByTime = periodList
                    .GroupBy(x => x.Time)
                    .OrderBy(g => g.Key)
                    .ToList();

                foreach (var timeGroup in groupedByTime)
                {
                    string time = timeGroup.Key ?? "";
                    string score = timeGroup.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.GoalDetail?.Score))?.GoalDetail?.Score ?? "";

                    var homeEvents = timeGroup.Where(e => e.Team?.Name == match.HomeTeamName).ToList();
                    var awayEvents = timeGroup.Where(e => e.Team?.Name == match.AwayTeamName).ToList();
                    var neutralEvents = timeGroup.Where(e => string.IsNullOrWhiteSpace(e.Team?.Name)).ToList();

                    sb.AppendLine("<tr>");

                    // Левая колонка (хозяева)
                    sb.AppendLine("<td class='home'>");
                    foreach (var e in homeEvents)
                    {
                        string type = e.EventType?.Name?.ToLower() ?? "";
                        sb.AppendLine(GetEventHtml(e, type, true));
                    }
                    sb.AppendLine("</td>");

                    // Центр — время и счёт
                    sb.AppendLine($@"<td class='center'>
                        <div class='time'>{time}</div>
                        <div class='score'>{(string.IsNullOrWhiteSpace(score) ? "" : $"({score})")}</div>
                    </td>");

                    // Правая колонка (гости)
                    sb.AppendLine("<td class='away'>");
                    foreach (var e in awayEvents)
                    {
                        string type = e.EventType?.Name?.ToLower() ?? "";
                        sb.AppendLine(GetEventHtml(e, type, false));
                    }
                    sb.AppendLine("</td>");

                    sb.AppendLine("</tr>");

                    // Нейтральные события (например, объявления)
                    foreach (var e in neutralEvents)
                    {
                        string type = e.EventType?.Name?.ToLower() ?? "";
                        string html = GetEventHtml(e, type, true);
                        sb.AppendLine($@"<tr>
                            <td class='home'></td>
                            <td class='center'>{html}</td>
                            <td class='away'></td>
                        </tr>");
                    }
                }

                sb.AppendLine("</table></div>");
            }

            sb.AppendLine("</div></body></html>");
            return sb.ToString();
        }

        // === Заголовок периода ===
        private static string PeriodTitle(string? p) =>
            (p ?? "").ToUpper() switch
            {
                var x when x.StartsWith("1") => "1-Й ПЕРИОД",
                var x when x.StartsWith("2") => "2-Й ПЕРИОД",
                var x when x.StartsWith("3") => "3-Й ПЕРИОД",
                var x when x.StartsWith("OT") => "ОВЕРТАЙМ",
                var x when x.StartsWith("SO") => "БУЛЛИТЫ",
                _ => "📋 ПРОЧЕЕ"
            };

        // === Определение типа события ===
        private static string GetEventHtml(MatchEvent e, string type, bool isHome) =>
            type switch
            {
                "goal" => 
                    BuildGoalBlock(e, isHome, false),
                
                "goal disallowed" or "no goal" =>
                    BuildGoalBlock(e, isHome, true),
                
                "penalty missed" or "shootout missed" or "so missed" =>
                    BuildMissedPenalty(e, isHome),
                
                "penalty" =>
                    BuildPenaltyBlock(e, isHome),

                "goalie change" or "goalkeeper change" or "goalie substitution" or "goalkeeper substitution" =>
                    BuildGoalieChangeBlock(e, isHome),
                _ => !string.IsNullOrEmpty(e.Details)
                        ? BuildInfoBlock(e, isHome)
                        : ""
            };

        // ======== ЗАМЕНА ВРАТАРЯ ========
        private static string BuildGoalieChangeBlock(MatchEvent e, bool isHome)
        {
            string side = isHome ? "home" : "away";
            string goalieOut = e.GoalieChange?.GoalieOut ?? "";
            string goalieIn = e.GoalieChange?.GoalieIn ?? "";

            string iconPath = Path.Combine("C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons", "substitution.png");

            string base64 = File.Exists(iconPath) ? Convert.ToBase64String(File.ReadAllBytes(iconPath)) : "";
            string iconHtml = File.Exists(iconPath)
                ? $"<img src='data:image/png;base64,{base64}' class='event-icon'>"
                : "🧤";

            string changeText;
            if (!string.IsNullOrWhiteSpace(goalieOut) && !string.IsNullOrWhiteSpace(goalieIn))
                changeText = $"{goalieOut} → {goalieIn}";
            else if (!string.IsNullOrWhiteSpace(goalieIn))
                changeText = $"Entered: {goalieIn}";
            else if (!string.IsNullOrWhiteSpace(goalieOut))
                changeText = $"Left: {goalieOut}";
            else
                changeText = "Goalie change";

            return $@"
                <div class='event-block {side}'>
                  <div class='event-header'>
                    {iconHtml}
                    <span class='event-player'>{changeText}</span>
                  </div>
                </div>";
        }

        // ======== ГОЛ ========
        private static string BuildGoalBlock(MatchEvent e, bool isHome, bool isNoGoal)
        {
            string puckPath = Path.Combine("C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons", "puck.png");
            string base64 = File.Exists(puckPath) ? Convert.ToBase64String(File.ReadAllBytes(puckPath)) : "";
            string imgHtml = File.Exists(puckPath)
                ? $"<img src='data:image/png;base64,{base64}' class='event-icon' style='filter: drop-shadow(0 0 4px {(isNoGoal ? "#ff4d4d" : "white")});'>"
                : "🏒";

            string scorer = e.GoalDetail?.Scorer ?? e.Player ?? "";
            string assists = e.GoalDetail?.Assistants;
            string assistsHtml = string.IsNullOrWhiteSpace(assists) ? "" : $"<div class='event-assist'>({assists})</div>";
            string side = isHome ? "home" : "away";
            string details = isNoGoal && !string.IsNullOrWhiteSpace(e.Details) ? e.Details : "";

            if (isNoGoal)
            {
                string scorerHtml = string.IsNullOrWhiteSpace(scorer) ? "" : $"<div class='event-assist'>({scorer})</div>";
                return $@"
                    <div class='event-block {side}'>
                      <div class='event-header'>
                        {imgHtml}
                        <span class='event-player' style='color:#ff4d4d;'>{details}</span>
                      </div>
                      {scorerHtml}
                    </div>";
            }

            return $@"
                <div class='event-block {side}'>
                  <div class='event-header'>
                    {imgHtml}
                    <span class='event-player'>{scorer}</span>
                  </div>
                  {assistsHtml}
                </div>";
        }

        // ======== УДАЛЕНИЕ ========
        private static string BuildPenaltyBlock(MatchEvent e, bool isHome)
        {
            string player = e.Penalty?.Player ?? e.Player ?? "";
            string reason = e.Penalty?.Reason ?? "";
            string side = isHome ? "home" : "away";
            string duration = e.Penalty?.Duration?.Trim() ?? "2";
            string iconFile = $"{duration}min.png";

            string iconPath = Path.Combine("C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons", iconFile);

            string base64 = File.Exists(iconPath) ? Convert.ToBase64String(File.ReadAllBytes(iconPath)) : "";
            string iconHtml = File.Exists(iconPath)
                ? $"<img src='data:image/png;base64,{base64}' class='event-icon'>"
                : $"<span class='badge b{duration}'>{duration}</span>";

            if (!string.IsNullOrWhiteSpace(reason) &&
                reason.Contains("too many men on the ice", StringComparison.OrdinalIgnoreCase))
            {
                player = "Bench minor penalty";
            }

            return $@"
                <div class='event-block {side}'>
                  <div class='event-header'>
                    {iconHtml}
                    <span class='event-player'>{player}</span>
                  </div>
                  <div class='event-assist'>({reason})</div>
                </div>";
        }

        // ======== НЕЗАБИТЫЙ БУЛЛИТ ========
        private static string BuildMissedPenalty(MatchEvent e, bool isHome)
        {
            string puckPath = Path.Combine("C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons", "puck.png");
            string base64 = File.Exists(puckPath) ? Convert.ToBase64String(File.ReadAllBytes(puckPath)) : "";
            string imgHtml = File.Exists(puckPath)
                ? $"<img src='data:image/png;base64,{base64}' class='event-icon' style='filter: drop-shadow(0 0 4px #ff4d4d);'>"
                : "🚫🏒";

            string side = isHome ? "home" : "away";
            string player = e.Player ?? "";
            string playerHtml = string.IsNullOrWhiteSpace(player)
                ? ""
                : $"<div class='event-assist'>({player})</div>";

            return $@"
                <div class='event-block {side}'>
                  <div class='event-header'>
                    {imgHtml}
                    <span class='event-player' style='color:#ff4d4d;'>Penalty missed</span>
                  </div>
                  {playerHtml}
                </div>";
        }

        // ======== ИНФО / ПРОЧИЕ ========
        private static string BuildInfoBlock(MatchEvent e, bool isHome)
        {
            string side = isHome ? "home" : "away";
            return $@"
                <div class='event-block {side}'>
                    <div class='event-player'>{e.Details}</div>
                </div>";
        }
    }
}