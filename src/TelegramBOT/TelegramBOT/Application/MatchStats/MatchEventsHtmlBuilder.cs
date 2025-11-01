using System.Text;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Models;

namespace TelegramBOT.Application.MatchStats
{
    public static class MatchEventsHtmlBuilder
    {
        public static string Build(Match match, IEnumerable<MatchEvent> events, MappingService mapper)
        {
            var (homePretty, awayPretty) = mapper.MapTeamNames(match);

            var sb = new StringBuilder();
            sb.AppendLine("<html><head><meta charset='utf-8'><style>");
            sb.AppendLine(@"
                body {
                    background: #0f1115;
                    color: #fff;
                    font-family: 'Segoe UI', sans-serif;
                    padding: 35px;
                }
                .card {
                    background: #181b20;
                    border-radius: 16px;
                    padding: 30px;
                    box-shadow: 0 4px 20px rgba(0,0,0,0.4);
                    max-width: 1200px;
                    margin: auto;
                }
                h2 {
                    text-align: center;
                    color: #ffd54f;
                    font-size: 28px;
                    margin-bottom: 8px;
                }
                .teams {
                    text-align: center;
                    font-size: 22px;
                    color: #ccc;
                    margin-bottom: 25px;
                }
                .period {
                    margin-top: 25px;
                    border-top: 2px solid #2a2d34;
                    padding-top: 10px;
                }
                .period-title {
                    font-weight: bold;
                    color: #ddd;
                    font-size: 20px;
                    margin-bottom: 12px;
                }
                table {
                    width: 100%;
                    border-collapse: collapse;
                    table-layout: fixed;
                    text-align: center;
                }
                td {
                    vertical-align: top;
                    padding: 6px 10px;
                    font-size: 16px;
                }
                .home {
                    text-align: right;
                    color: #4fc3f7;
                }
                .away {
                    text-align: left;
                    color: #ffd54f;
                }
                .center {
                    width: 110px;
                    color: #ccc;
                    font-size: 14px;
                }
                .goal { color: #9cff9c; }
                .penalty { color: #ff6e6e; }
                .goalie { color: #80c7ff; }
                .event { line-height: 1.3; }
                .score {
                    font-weight: bold;
                    color: #fff;
                    margin-top: 4px;
                    display: block;
                }
                .time { color: #aaa; font-size: 14px; }
                .badge {
                    display: inline-block;
                    min-width: 24px;
                    height: 24px;
                    line-height: 24px;
                    border-radius: 4px;
                    background: #444;
                    color: #fff;
                    font-weight: bold;
                    font-size: 13px;
                    text-align: center;
                    margin-right: 4px;
                }
                .b2 { background: #ffb703; }
                .b5 { background: #e85d04; }
                .b10 { background: #a00000; }
            ");
            sb.AppendLine("</style></head><body>");
            sb.AppendLine("<div class='card'>");

            sb.AppendLine("<h2>🎯 События матча</h2>");
            sb.AppendLine($"<div class='teams'>{homePretty} vs {awayPretty}</div>");

            foreach (var periodGroup in events
                .OrderBy(e => e.Period)
                .GroupBy(e => e.Period))
            {
                sb.AppendLine("<div class='period'>");
                sb.AppendLine($"<div class='period-title'>{PeriodTitle(periodGroup.Key)}</div>");
                sb.AppendLine("<table>");

                foreach (var e in periodGroup.OrderBy(e => e.Time))
                {
                    var type = e.EventType?.Name?.ToLower() ?? "";
                    var isHome = e.Team?.Name == match.HomeTeamName;

                    string main = GetMainEventText(e, type);
                    string score = e.GoalDetail?.Score ?? "";
                    string time = e.Time ?? "";
                    string badge = GetPenaltyBadge(e);

                    sb.AppendLine("<tr>");
                    if (isHome)
                    {
                        sb.AppendLine($"<td class='home event'>{main}</td>");
                        sb.AppendLine($"<td class='center'><div class='time'>{time}</div><div class='score'>{score}</div></td>");
                        sb.AppendLine("<td class='away'></td>");
                    }
                    else
                    {
                        sb.AppendLine("<td class='home'></td>");
                        sb.AppendLine($"<td class='center'><div class='time'>{time}</div><div class='score'>{score}</div></td>");
                        sb.AppendLine($"<td class='away event'>{main}</td>");
                    }
                    sb.AppendLine("</tr>");
                }

                sb.AppendLine("</table>");
                sb.AppendLine("</div>");
            }

            sb.AppendLine("</div></body></html>");
            return sb.ToString();
        }

        // тип периода
        private static string PeriodTitle(string? p) =>
            (p ?? "").ToUpper() switch
            {
                var x when x.StartsWith("1") => "1️⃣ ПЕРИОД",
                var x when x.StartsWith("2") => "2️⃣ ПЕРИОД",
                var x when x.StartsWith("3") => "3️⃣ ПЕРИОД",
                var x when x.StartsWith("OT") => "🕓 ОВЕРТАЙМ",
                var x when x.StartsWith("SO") => "🎯 БУЛЛИТЫ",
                _ => "📋 ПРОЧЕЕ"
            };

        // текст события
        // 🔧 заменяем GetMainEventText и GetPenaltyBadge
        private static string GetMainEventText(MatchEvent e, string type)
        {
            switch (type)
            {
                case "goal":
                    return $"🥅 <b>{e.GoalDetail?.Scorer}</b><br><span style='color:#aaa;'>({e.GoalDetail?.Assistants})</span>";

                case "penalty":
                    var badgeHtml = GetPenaltyBadge(e);
                    return $"{badgeHtml}<b>{e.Penalty?.Player}</b><br><span style='color:#aaa;'>({e.Penalty?.Reason})</span>";

                case "goalie change":
                case "goalkeeper change":
                    return FormatGoalieChange(e);

                default:
                    if (!string.IsNullOrEmpty(e.Details) && e.Details.ToLower().Contains("goalie"))
                        return FormatGoalieChange(e);
                    return $"📍 {e.Details}";
            }
        }

        private static string FormatGoalieChange(MatchEvent e)
        {
            // Если данные о вратарях есть — используем их
            if (!string.IsNullOrEmpty(e.GoalieChange?.GoalieOut) || !string.IsNullOrEmpty(e.GoalieChange?.GoalieIn))
            {
                var outGoalie = e.GoalieChange?.GoalieOut ?? "";
                var inGoalie = e.GoalieChange?.GoalieIn ?? "";
                return $"🧤 <span class='goalie'><b>{outGoalie}</b> → <b>{inGoalie}</b></span>";
            }

            // Если структура GoalieChange не заполнена, пробуем достать из деталей
            if (!string.IsNullOrEmpty(e.Details))
            {
                var text = e.Details;
                if (text.Contains("replaced by"))
                {
                    var parts = text.Split("replaced by", StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        var outGoalie = parts[0].Trim();
                        var inGoalie = parts[1].Trim();
                        return $"🧤 <span class='goalie'><b>{outGoalie}</b> → <b>{inGoalie}</b></span>";
                    }
                }
                return $"🧤 <span class='goalie'>{text}</span>";
            }

            // Если ничего нет — просто иконка
            return "🧤 Замена вратаря";
        }


        private static string GetPenaltyBadge(MatchEvent e)
        {
            if (string.IsNullOrWhiteSpace(e.Penalty?.Reason))
                return "";

            string reason = e.Penalty.Reason.ToLower();

            // Определяем тип штрафа по тексту причины
            if (reason.Contains("10") || reason.Contains("misconduct"))
                return "<span class='badge b10'>10</span> ";
            if (reason.Contains("5") || reason.Contains("fight") || reason.Contains("major"))
                return "<span class='badge b5'>5</span> ";
            if (reason.Contains("2") || reason.Contains("minor") || reason.Contains("tripping") || reason.Contains("hook") || reason.Contains("delay"))
                return "<span class='badge b2'>2</span> ";

            // Если не удалось определить — без бейджа
            return "";
        }

    }
}
