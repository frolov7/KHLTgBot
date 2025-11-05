using System.ComponentModel;
using System.IO;
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
                /* === Основной фон страницы === */
body {
    background-color: #3A3C42; /* светло-серый фон, теперь заметно */
    color: #fff;
    font-family: 'Segoe UI', sans-serif;
    margin: 0;
    padding: 0;
    min-height: 100vh;
    display: flex;
    justify-content: center;
    align-items: flex-start;
}

                /* === Основная карточка контента (вся таблица и заголовки) === */
.card {
    background: rgba(227, 227, 227, 0.08);
    backdrop-filter: blur(4px);
    border-radius: 0;
    box-shadow: inset 0 0 40px rgba(0,0,0,0.4);
    width: 100%;
    max-width: none;
    padding: 40px;
}



                /* === Заголовок страницы (События матча) === */
                h2 {
                    text-align: center;
                    color: #e0e0e0; /* цвет заголовка */
                    font-size: 28px;
                    margin-bottom: 8px;
                }

.teams {
    text-align: center;
    font-size: 22px;
    color: #e0e0e0; /* основной цвет текста */
}

.teams span.home {
    color: #e0e0e0; /* синий — домашние */
    font-weight: 600;
}

.teams span.away {
    color: #e0e0e0; /* красный — гости */
    font-weight: 600;
}


                /* === Контейнер для каждого периода (1, 2, 3, ОТ) === */
                .period {
                    margin-top: 25px;
                    border-top: 2px solid #e0e0e0; /* линия-разделитель между периодами */
                    padding-top: 10px;
                }

                /* === Заголовок периода (например, ""1️й- ПЕРИОД"") === */
                .period-title {
                    font-weight: bold;
                    color: #e0e0e0;
                    font-size: 20px;
                    margin-bottom: 12px;
                }

                /* === Таблица событий внутри периода === */
                table {
                    width: 100%;
                    border-collapse: collapse; /* убирает двойные границы */
                    table-layout: fixed; /* фиксированная ширина колонок */
                    text-align: center;
                }

                /* === Ячейки таблицы === */
                td {
                    vertical-align: top;
                    padding: 6px 10px;
                    font-size: 16px;
                }

                /* === Домашняя команда (слева) === */
                .home {
                    text-align: right;
                    color: #e0e0e0; /* голубой цвет текста для домашней команды */
                }

                /* === Гостевая команда (справа) === */
                .away {
                    text-align: left;
                    color: #e0e0e0; /* жёлтый цвет текста для гостей */
                }

                /* === Центральная колонка таблицы (время и счёт) === */
                .center {
                    width: 110px;
                    color: #ccc; /* серый текст */
                    font-size: 14px;
                }

                /* === Цвета для разных типов событий === */
                .goal { color: #9cff9c; }     /* гол (зелёный) */
                .penalty { color: #ff6e6e; }  /* удаление (красный) */
                .goalie { color: #80c7ff; }   /* вратари (голубой) */

                /* === Событие (контейнер для текста внутри ячейки) === */
                .event {
                    line-height: 1.3; /* межстрочный интервал */
                }

                /* === Отображение счёта в центре === */
.score {
    font-weight: bold;
    color: #fff;
    background: #333;
    border-radius: 4px;
    padding: 2px 6px;
}


                /* === Время события === */
                .time {
                    color: #5ca0d3; /* небесно-голубой */
                    font-size: 14px;
                    font-weight: 500;
                }

/* === Общий стиль для бейджей (удаления и т.п.) === */
.badge {
    display: inline-flex;              /* flex для точного выравнивания */
    justify-content: center;           /* центрирование по горизонтали */
    align-items: center;               /* центрирование по вертикали */
    width: 24px;
    height: 24px;
    border-radius: 4px;
    font-weight: bold;
    font-size: 13px;
    text-align: center;
    margin-right: 4px;
    vertical-align: middle;
    box-sizing: border-box;
}

/* === Цвета для конкретных типов удалений === */
.b2 {
    background: #ffb703;  /* ярко-жёлтый */
    color: #2B2D31;       /* тёмная цифра под цвет фона страницы */
}

.b5 {
    background: #e85d04;  /* оранжевый */
    color: #2B2D31;
}

.b10 {
    background: #a00000;  /* красный */
    color: #2B2D31;
}


            ");

            sb.AppendLine("</style></head><body>");
            sb.AppendLine("<div class='card'>");

            sb.AppendLine("<h2>События матча</h2>");
            sb.AppendLine($"<div class='teams'><span class='home'>{homePretty}</span> vs <span class='away'>{awayPretty}</span></div>");


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
                        sb.AppendLine($@"
                        <td class='center'>
                            <div class='time'>{time}</div>
                            <div class='score'>{(string.IsNullOrWhiteSpace(score) ? "" : $"({score})")}</div>
                        </td>");

                        sb.AppendLine("<td class='away'></td>");
                    }
                    else
                    {
                        sb.AppendLine("<td class='home'></td>");
                        sb.AppendLine($@"
                        <td class='center'>
                            <div class='time'>{time}</div>
                            <div class='score'>{(string.IsNullOrWhiteSpace(score) ? "" : $"({score})")}</div>
                        </td>");

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
                var x when x.StartsWith("1") => "1-Й ПЕРИОД",
                var x when x.StartsWith("2") => "2-Й ПЕРИОД",
                var x when x.StartsWith("3") => "3-Й ПЕРИОД",
                var x when x.StartsWith("OT") => "ОВЕРТАЙМ",
                var x when x.StartsWith("SO") => "БУЛЛИТЫ",
                _ => "📋 ПРОЧЕЕ"
            };

        // текст события
        private static string GetMainEventText(MatchEvent e, string type)
        {
            switch (type)
            {
                // ======== ГОЛ ========
                case "goal":
                    return BuildGoalHtml(e, isNoGoal: false);

                // ======== НЕЗАСЧИТАННЫЙ ГОЛ ========
                case "goal disallowed":
                case "goal cancelled":
                case "goal not allowed":
                case "no goal":
                    return BuildGoalHtml(e, isNoGoal: true);

                // ======== НЕЗАБИТЫЙ БУЛЛИТ ========
                case "penalty missed":
                case "shootout missed":
                case "so missed":
                case "penalty shot missed":
                case "missed penalty":
                case "missed shot":
                    return BuildGoalHtml(e, isNoGoal: true, isShootout: true);

                // ======== УДАЛЕНИЕ ========
                case "penalty":
                    {
                        var badgeHtml = GetPenaltyBadge(e);
                        string player = e.Penalty?.Player ?? "";
                        string reason = e.Penalty?.Reason ?? "";
                        return $"{badgeHtml}<b>{player}</b><br><span style='color:#aaa;'>({reason})</span>";
                    }

                // ======== ЗАМЕНА ВРАТАРЯ ========
                case "goalie change":
                case "goalkeeper change":
                    return FormatGoalieChange(e);

                // ======== ОСТАЛЬНЫЕ ========
                default:
                    {
                        if (!string.IsNullOrEmpty(e.Details))
                        {
                            // обработка отменённых голов
                            if (e.Details.Contains("goal disallowed", StringComparison.OrdinalIgnoreCase) ||
                                e.Details.Contains("no goal", StringComparison.OrdinalIgnoreCase) ||
                                e.Details.Contains("disallowed", StringComparison.OrdinalIgnoreCase))
                                return BuildGoalHtml(e, isNoGoal: true);

                            // обработка буллита по тексту
                            if (e.Details.Contains("penalty missed", StringComparison.OrdinalIgnoreCase) ||
                                e.Details.Contains("shootout missed", StringComparison.OrdinalIgnoreCase))
                                return BuildGoalHtml(e, isNoGoal: true, isShootout: true);

                            if (e.Details.Contains("goalie", StringComparison.OrdinalIgnoreCase))
                                return FormatGoalieChange(e);

                            return $"📍 {e.Details}";
                        }

                        return "";
                    }
            }
        }

        private static string BuildGoalHtml(MatchEvent e, bool isNoGoal, bool isShootout = false)
        {
            var imagePath = Path.Combine("C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons", "puck.png");

            string imgHtml;

            if (File.Exists(imagePath))
            {
                var bytes = File.ReadAllBytes(imagePath);
                var base64 = Convert.ToBase64String(bytes);
                var dataUri = $"data:image/png;base64,{base64}";

                var glowColor = isNoGoal ? "#ff4d4d" : "white";

                imgHtml = $"<img src='{dataUri}' alt='goal' " +
                          $"style='width:28px;height:28px;vertical-align:middle;margin-right:6px;" +
                          $"filter: drop-shadow(0 0 4px {glowColor}) drop-shadow(0 0 4px {glowColor});'>";
            }
            else
            {
                imgHtml = isNoGoal ? "🚫🏒 " : "🏒 ";
            }

            var scorer = e.GoalDetail?.Scorer ?? e.Player ?? "";
            var details = e.Details ?? "";
            var assistants = e.GoalDetail?.Assistants?.Trim();

            string assistsLine = "";
            if (!string.IsNullOrWhiteSpace(assistants))
                assistsLine = $"<div style='margin-left:34px;color:#aaa;font-size:14px;'>({assistants})</div>";

            // === ОБРАБОТКА НЕЗАБИТОГО БУЛЛИТА ===
            if (isShootout)
            {
                details = string.IsNullOrWhiteSpace(details) ? "Penalty missed" : details;

                // теперь визуально как обычный гол, справа от времени
                return $@"
            <div style='display:flex;align-items:center;gap:6px;'>
                {imgHtml}
                <span style='color:#ff4d4d;font-weight:600;font-size:15px;'>{details}</span>
            </div>
            {(string.IsNullOrWhiteSpace(scorer) ? "" : $"<div style='margin-left:34px;'><b>{scorer}</b></div>")}";
            }

            // === НЕЗАСЧИТАННЫЙ ГОЛ ===
            if (isNoGoal)
            {
                var firstLine = $@"
            <div style='display:flex;align-items:center;gap:6px;margin-bottom:4px;'>
                {imgHtml}
                <span style='color:#ff6e6e;font-weight:600;font-size:15px;'>{details}</span>
            </div>";

                var scorerLine = string.IsNullOrWhiteSpace(scorer)
                    ? ""
                    : $"<div style='margin-left:34px;'><b>{scorer}</b></div>";

                return firstLine + scorerLine + assistsLine;
            }

            // === ЗАСЧИТАННЫЙ ГОЛ ===
            var goalLine = $"{imgHtml}<b>{scorer}</b>";
            if (!string.IsNullOrEmpty(assistsLine))
                goalLine += $"<br>{assistsLine}";

            return goalLine;
        }

        private static string FormatGoalieChange(MatchEvent e)
        {
            // путь к иконке
            var imagePath = Path.Combine(
                "C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons", "substitution.png");

            string iconHtml;

            if (File.Exists(imagePath))
            {
                // читаем файл и кодируем в base64
                var bytes = File.ReadAllBytes(imagePath);
                var base64 = Convert.ToBase64String(bytes);
                var dataUri = $"data:image/png;base64,{base64}";

                // стиль аналогичный шайбе и удалениям
                iconHtml = $"<img src='{dataUri}' alt='substitution' " +
                           "style='width:28px;height:28px;vertical-align:middle;margin-right:6px;" +
                           "filter: drop-shadow(0 0 2px white) drop-shadow(0 0 2px white);'>";
            }
            else
            {
                iconHtml = "🧤 "; // fallback, если файл не найден
            }

            string text;

            if (!string.IsNullOrEmpty(e.GoalieChange?.GoalieOut) || !string.IsNullOrEmpty(e.GoalieChange?.GoalieIn))
            {
                var outGoalie = e.GoalieChange?.GoalieOut ?? "";
                var inGoalie = e.GoalieChange?.GoalieIn ?? "";
                text = $"{iconHtml}<span class='goalie'><b>{outGoalie}</b> → <b>{inGoalie}</b></span>";
            }
            else if (!string.IsNullOrEmpty(e.Details))
            {
                var t = e.Details;
                if (t.Contains("replaced by"))
                {
                    var parts = t.Split("replaced by", StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        var outGoalie = parts[0].Trim();
                        var inGoalie = parts[1].Trim();
                        text = $"{iconHtml}<span class='goalie'><b>{outGoalie}</b> → <b>{inGoalie}</b></span>";
                    }
                    else
                        text = $"{iconHtml}<span class='goalie'>{t}</span>";
                }
                else
                    text = $"{iconHtml}<span class='goalie'>{t}</span>";
            }
            else
                text = $"{iconHtml}<span class='goalie'>Замена вратаря</span>";

            return text;
        }

        private static string GetPenaltyBadge(MatchEvent e)
        {
            if (string.IsNullOrWhiteSpace(e.Penalty?.Duration))
                return "";

            var parts = e.Penalty.Duration.Split('+', StringSplitOptions.RemoveEmptyEntries);
            var badges = new List<string>();

            foreach (var p in parts)
            {
                var trimmed = p.Trim();

                // файлы типа 2min.png, 5min.png, 10min.png, 20min.png
                string fileName = $"{trimmed}min.png";
                string imagePath = Path.Combine(
                    "C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                    "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons", fileName);

                if (File.Exists(imagePath))
                {
                    // читаем файл и кодируем в base64
                    var bytes = File.ReadAllBytes(imagePath);
                    var base64 = Convert.ToBase64String(bytes);
                    var dataUri = $"data:image/png;base64,{base64}";

                    // стиль — как у шайбы (drop-shadow белый, тот же размер и отступ)
                    var imgHtml = $"<img src='{dataUri}' alt='{trimmed}min' " +
                                  "style='width:20px;height:20px;vertical-align:middle;margin-right:4px;" +
                                  "filter: drop-shadow(0 0 2px white) drop-shadow(0 0 2px white);'>";

                    badges.Add(imgHtml);
                }
                else
                {
                    // fallback, если файла нет
                    var colorClass = trimmed switch
                    {
                        "2" => "b2",
                        "5" => "b5",
                        "10" => "b10",
                        "20" => "b10",
                        _ => ""
                    };

                    badges.Add($"<span class='badge {colorClass}'>{trimmed}</span>");
                }
            }

            // картинка "плюс" между удалениями (если "2+2")
            string plusPath = Path.Combine(
                "C:", "Users", "gimna", "Desktop", "BMSTU", "MyProjects", "bot",
                "src", "TelegramBOT", "TelegramBOT", "wwwroot", "icons", "plus.png");

            string plus;

            if (File.Exists(plusPath))
            {
                var bytes = File.ReadAllBytes(plusPath);
                var base64 = Convert.ToBase64String(bytes);
                var dataUri = $"data:image/png;base64,{base64}";

                plus = $"<img src='{dataUri}' alt='plus' " +
                       "style='width:12px;height:12px;vertical-align:middle;" +
                       "margin-left:0px;margin-right:3px;" +
                       "position:relative;top:-1px;filter: drop-shadow(0 0 1px white);'>";

            }
            else
            {
                // fallback на текст "+"
                plus = "<span style='color:#ccc;margin:0 2px;'>+</span>";
            }

            return string.Join(plus, badges) + " ";
        }


    }
}