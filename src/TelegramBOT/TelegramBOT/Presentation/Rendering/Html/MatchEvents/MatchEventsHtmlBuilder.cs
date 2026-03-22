using System.IO;
using System.Text;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Entities.Matches;
using TelegramBOT.Domain.Entities.MatchEvents;

namespace TelegramBOT.Presentation.Rendering.Html.MatchEvents
{
    public class MatchEventsHtmlBuilder
    {
        private readonly IConfiguration _config;
        private readonly MappingService _mapper;
        private readonly Dictionary<string, Dictionary<string, string>> _eventDict;


        public MatchEventsHtmlBuilder(IConfiguration config, MappingService mapper)
        {
            _config = config;
            _mapper = mapper;

            _eventDict = _config
                .GetSection("EventTranslations")
                .Get<Dictionary<string, Dictionary<string, string>>>()
                ?? new();
        }

        private string T(string category, string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return "";

            key = key.Trim().ToLowerInvariant();

            return _eventDict.TryGetValue(category, out var cat) &&
                   cat.TryGetValue(key, out var value)
                ? value
                : key; // fallback — покажем оригинал
        }

        private static string N(string? s) => string.IsNullOrWhiteSpace(s) ? "" : s.Trim().ToLowerInvariant();

        public string Build(Match match, IEnumerable<MatchEvent> events)
        {
            var (homePretty, awayPretty) = _mapper.MapTeamNames(match);

            // Загружаем словарь арен
            var arenaDict = _config.GetSection("Arenas").Get<Dictionary<string, string>>();

            // ===== ДАТА, ВРЕМЯ, АРЕНА =====
            string date = match.MatchDate.ToString("dd.MM.yyyy");
            string time = match.MatchDate.ToString("HH:mm");

            string arena = arenaDict != null && arenaDict.ContainsKey(match.HomeTeamName)
                ? arenaDict[match.HomeTeamName]
                : "Арена неизвестна";

            var sb = new StringBuilder();

            sb.AppendLine("<html><head><meta charset='utf-8'><style>");
            sb.AppendLine(MatchEventsCss.Get());
            sb.AppendLine("</style></head><body><div class='card'>");

            // =====================================================================
            // =====================   ШАПКА С ЛОГОТИПАМИ   =========================
            // =====================================================================

            string iconsDir = Path.Combine("C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons", "teams");

            string homeLogoFile = $"{match.HomeTeamName}_right.png";
            string awayLogoFile = $"{match.AwayTeamName}_left.png";

            string homeLogoPath = Path.Combine(iconsDir, homeLogoFile);
            string awayLogoPath = Path.Combine(iconsDir, awayLogoFile);

            string homeLogoBase64 = File.Exists(homeLogoPath)
                ? Convert.ToBase64String(File.ReadAllBytes(homeLogoPath))
                : "";

            string awayLogoBase64 = File.Exists(awayLogoPath)
                ? Convert.ToBase64String(File.ReadAllBytes(awayLogoPath))
                : "";

            string homeLogoHtml = File.Exists(homeLogoPath)
                ? $"<img src='data:image/png;base64,{homeLogoBase64}' class='team-logo'>"
                : $"<span class='team-name'>{homePretty}</span>";

            string awayLogoHtml = File.Exists(awayLogoPath)
                ? $"<img src='data:image/png;base64,{awayLogoBase64}' class='team-logo'>"
                : $"<span class='team-name'>{awayPretty}</span>";


            // ===== ВЕРХНЯЯ ПАНЕЛЬ =====
            sb.AppendLine("<div class='header-row'>");
            sb.AppendLine($"  <div class='logo-left'>{homeLogoHtml}</div>");
            sb.AppendLine("  <div class='header-title'>События матча</div>");
            sb.AppendLine($"  <div class='logo-right'>{awayLogoHtml}</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class='title-line'></div>");

            // =====================================================================
            // ================   НОВЫЙ БЛОК ДАТА — ВРЕМЯ — АРЕНА   ================
            // =====================================================================
            sb.AppendLine(@$"
                <div class='match-info'>
                    <div class='match-arena'>«{arena}»</div>
                    <div class='match-date'>{date}</div>
                    <div class='match-time'>{time}</div>
                </div>
            ");

            // =====================================================================
            // ===================      ПЕРИОДЫ И СОБЫТИЯ       ====================
            // =====================================================================

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
                    string timeStr = timeGroup.Key ?? "";
                    string score = timeGroup.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.GoalDetail?.Score))?.GoalDetail?.Score ?? "";

                    var homeEvents = timeGroup.Where(e => e.Team?.Name == match.HomeTeamName).ToList();
                    var awayEvents = timeGroup.Where(e => e.Team?.Name == match.AwayTeamName).ToList();
                    var neutralEvents = timeGroup.Where(e => string.IsNullOrWhiteSpace(e.Team?.Name)).ToList();

                    sb.AppendLine("<tr>");

                    // Левая колонка
                    sb.AppendLine("<td class='home'>");
                    foreach (var e in homeEvents)
                    {
                        string type = N(e.EventType?.Name);
                        sb.AppendLine(GetEventHtml(e, type, true));
                    }
                    sb.AppendLine("</td>");

                    // Центр
                    sb.AppendLine($@"
                        <td class='center'>
                            <div class='time'>{timeStr}</div>
                            <div class='score'>{(string.IsNullOrWhiteSpace(score) ? "" : $"({score})")}</div>
                        </td>
                    ");

                    // Правая колонка
                    sb.AppendLine("<td class='away'>");
                    foreach (var e in awayEvents)
                    {
                        string type = N(e.EventType?.Name);
                        sb.AppendLine(GetEventHtml(e, type, false));
                    }
                    sb.AppendLine("</td>");
                    sb.AppendLine("</tr>");

                    // Нейтральные события
                    foreach (var e in neutralEvents)
                    {
                        string type = N(e.EventType?.Name);
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
        private string PeriodTitle(string? p) =>
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
        private string GetEventHtml(MatchEvent e, string type, bool isHome) =>
            type switch
            {
                "goal" =>
                    BuildGoalBlock(e, isHome),

                "goal disallowed" =>
                    BuildGoalDisallowedBlock(e, isHome),

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
        private string BuildGoalieChangeBlock(MatchEvent e, bool isHome)
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
                changeText = $"{T("goalie", "entered")}: {goalieIn}";
            else if (!string.IsNullOrWhiteSpace(goalieOut))
                changeText = T("goalie", "goalie change");
            else
                changeText = T("goalie", "goalie change");

            return $@"
                <div class='event-block {side}'>
                  <div class='event-header'>
                    {iconHtml}
                    <span class='event-player'>{changeText}</span>
                  </div>
                </div>";
        }

        // ======== ГОЛ ========
        private string BuildGoalBlock(MatchEvent e, bool isHome)
        {
            string player = e.GoalDetail?.Scorer ?? e.Player ?? "";
            string assists = e.GoalDetail?.Assistants ?? "";
            string score = e.GoalDetail?.Score ?? "";
            string goalType = e.GoalDetail?.GoalType ?? "";
            string side = isHome ? "home" : "away";

            bool isShootout = !string.IsNullOrWhiteSpace(e.Period) && e.Period.Trim().StartsWith("SO", StringComparison.OrdinalIgnoreCase);

            string goalTypeText = "";

            if (isShootout)
            {
                // ✅ ЯВНО помечаем буллит
                goalTypeText = $"({T("eventType", "shootout")})";
            }
            else if (!string.IsNullOrWhiteSpace(goalType))
            {
                var gt = goalType.Trim().ToLowerInvariant();

                // ❗ Гол в равных составах — ничего не выводим
                if (gt != "even strength")
                {
                    goalTypeText = $"({T("goalType", gt)})";
                }
            }

            string playerHtml = string.IsNullOrEmpty(goalTypeText)
                ? player
                : $"{player} <span class='goal-type'>{goalTypeText}</span>";

            string iconPath = Path.Combine("C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons", "puck.png");

            string base64 = File.Exists(iconPath) ? Convert.ToBase64String(File.ReadAllBytes(iconPath)) : "";
            string iconHtml = File.Exists(iconPath)
                ? $"<img src='data:image/png;base64,{base64}' class='event-icon'>"
                : "🥅";

            string assistsHtml = string.IsNullOrWhiteSpace(assists)
                ? ""
                : $"<div class='event-assist'>({assists})</div>";

            return $@"
                <div class='event-block {side}'>
                    <div class='event-header'>
                        {iconHtml}
                        <span class='event-player'>{playerHtml}</span>
                    </div>
                    {assistsHtml}
                </div>";
        }

        // ======== НЕЗАСЧИТАННЫЙ ГОЛ ========
        private string BuildGoalDisallowedBlock(MatchEvent e, bool isHome)
        {
            string side = isHome ? "home" : "away";
            string reasonRaw = e.Details ?? e.EventType?.Name ?? "goal disallowed";
            string reason = T("details", reasonRaw);

            // Иконка крестика на шайбе
            string iconPath = Path.Combine("C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons", "puck.png");

            string base64 = File.Exists(iconPath) ? Convert.ToBase64String(File.ReadAllBytes(iconPath)) : "";
            string iconHtml = File.Exists(iconPath)
                ? $"<img src='data:image/png;base64,{base64}' class='event-icon' style='filter: drop-shadow(0 0 4px #ff4d4d);'>"
                : "❌🥅";

            return $@"
        <div class='event-block {side}'>
            <div class='event-header'>
                {iconHtml}
                <span class='event-player' style='color:#ff4d4d;'>{reason}</span>
            </div>
        </div>";
        }

        // ======== УДАЛЕНИЕ ========
        private string BuildPenaltyBlock(MatchEvent e, bool isHome)
        {
            string player = e.Penalty?.Player ?? e.Player ?? "";
            string reasonRaw = e.Penalty?.Reason ?? "";
            string reason = T("penalty", reasonRaw);
            string side = isHome ? "home" : "away";
            string durationRaw = e.Penalty?.Duration?.Trim() ?? "2";

            // Получаем HTML всех иконок (например 5+10 -> две картинки)
            string iconsHtml = BuildPenaltyIcons(durationRaw);

            if (reasonRaw.Contains("too many men on the ice", StringComparison.OrdinalIgnoreCase))
            {
                player = T("common", "bench minor penalty");
            }

            return $@"
        <div class='event-block {side}'>
          <div class='event-header'>
            {iconsHtml}
            <span class='event-player'>{player}</span>
          </div>
          <div class='event-assist'>({reason})</div>
        </div>";
        }

        private string BuildPenaltyIcons(string durationRaw)
        {
            var parts = durationRaw
                .Replace("мин", "")
                .Replace("min", "")
                .Split('+', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();

            if (parts.Count == 0)
                parts.Add("2");

            var iconsHtml = new StringBuilder();

            string baseIconsDir = Path.Combine("C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons");

            string plusIconPath = Path.Combine(baseIconsDir, "plus.png");

            string plusHtml = File.Exists(plusIconPath)
                ? $"<img src='data:image/png;base64,{Convert.ToBase64String(File.ReadAllBytes(plusIconPath))}' class='penalty-plus-img'>"
                : "<span class='penalty-plus'>+</span>";

            for (int i = 0; i < parts.Count; i++)
            {
                string dur = parts[i];
                string iconFile = $"{dur}min.png";
                string iconPath = Path.Combine(baseIconsDir, iconFile);

                string iconHtml;
                if (File.Exists(iconPath))
                {
                    string base64 = Convert.ToBase64String(File.ReadAllBytes(iconPath));
                    iconHtml = $"<img src='data:image/png;base64,{base64}' class='event-icon'>";
                }
                else
                {
                    iconHtml = $"<span class='badge b{dur}'>{dur}</span>";
                }

                iconsHtml.Append(iconHtml);

                // вставляем картинку плюса, если есть ещё части
                if (i < parts.Count - 1)
                {
                    iconsHtml.Append(plusHtml);
                }
            }

            return iconsHtml.ToString();
        }

        // ======== НЕЗАБИТЫЙ БУЛЛИТ ========
        private string BuildMissedPenalty(MatchEvent e, bool isHome)
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
                    <span class='event-player' style='color:#ff4d4d;'>
                        {T("details", "penalty missed")}
                    </span>
                  </div>
                  {playerHtml}
                </div>";
        }

        // ======== ИНФО / ПРОЧИЕ ========
        private string BuildInfoBlock(MatchEvent e, bool isHome)
        {
            string side = isHome ? "home" : "away";
            return $@"
                <div class='event-block {side}'>
                    <div class='event-player'>{T("details", e.Details)}</div>
                </div>";
        }
    }
}