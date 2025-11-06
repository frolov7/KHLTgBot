namespace TelegramBOT.Presentation.Rendering.Html
{
    public static class MatchEventsCss
    {
        private enum CssStyle
        {
            BackgroundColor,
            TextColor,
            AccentColor,
            BorderColor,
            CardBackground,
            HeaderColor,

            FontMain,
            FontTitle,
            FontTeams,
            FontPeriod,
            FontEvent,
            FontAssist,
            FontTime,
            FontScore,
            FontBadge
        }

        private static string GetValue(CssStyle style) => style switch
        {
            CssStyle.BackgroundColor => "#3A3C42",
            CssStyle.TextColor => "#fff",
            CssStyle.AccentColor => "#5ca0d3",
            CssStyle.BorderColor => "#e0e0e0",
            CssStyle.CardBackground => "rgba(227, 227, 227, 0.08)",
            CssStyle.HeaderColor => "#e0e0e0",

            CssStyle.FontMain => "20px",
            CssStyle.FontTitle => "34px",
            CssStyle.FontTeams => "26px",
            CssStyle.FontPeriod => "24px",
            CssStyle.FontEvent => "18px",
            CssStyle.FontAssist => "16px",
            CssStyle.FontTime => "18px",
            CssStyle.FontScore => "18px",
            CssStyle.FontBadge => "16px",

            _ => ""
        };

        public static string Get() => $@"
            /* === Основной фон === */
            body {{
                background-color: {GetValue(CssStyle.BackgroundColor)};
                color: {GetValue(CssStyle.TextColor)};
                font-family: 'Segoe UI', sans-serif;
                margin: 0;
                padding: 0;
                min-height: 100vh;
                display: flex;
                justify-content: center;
                align-items: flex-start;
                font-size: {GetValue(CssStyle.FontMain)};
            }}

            /* === Карточка контента === */
            .card {{
                background: {GetValue(CssStyle.CardBackground)};
                backdrop-filter: blur(4px);
                box-shadow: inset 0 0 40px rgba(0,0,0,0.4);
                width: 100%;
                padding: 50px;
            }}

            /* === Заголовок === */
            h2 {{
                text-align: center;
                color: {GetValue(CssStyle.HeaderColor)};
                font-size: {GetValue(CssStyle.FontTitle)};
                margin-bottom: 12px;
            }}

            .teams {{
                text-align: center;
                font-size: {GetValue(CssStyle.FontTeams)};
                color: {GetValue(CssStyle.TextColor)};
                margin-bottom: 16px;
            }}
            .teams span {{ font-weight: 600; }}

            /* === Периоды === */
            .period {{
                margin-top: 25px;
                border-top: 2px solid {GetValue(CssStyle.BorderColor)};
                padding-top: 10px;
            }}
            .period-title {{
                font-weight: bold;
                color: {GetValue(CssStyle.TextColor)};
                font-size: {GetValue(CssStyle.FontPeriod)};
                margin-bottom: 14px;
            }}

            /* === Таблица === */
            table {{
                width: 100%;
                border-collapse: collapse;
                table-layout: fixed;
            }}
            td {{
                vertical-align: top;
                padding: 6px 10px;
                font-size: {GetValue(CssStyle.FontEvent)};
            }}
            td.home, td.away {{ width: 45%; }}
            td.center {{
                width: 10%;
                color: #ccc;
                text-align: center;
            }}

            /* === Центр: время и счёт === */
            .time {{
                color: {GetValue(CssStyle.AccentColor)};
                font-size: {GetValue(CssStyle.FontTime)};
                font-weight: 600;
            }}
            .score {{
                font-weight: bold;
                color: #fff;
                background: #333;
                border-radius: 4px;
                padding: 3px 8px;
                font-size: {GetValue(CssStyle.FontScore)};
            }}

            /* === События === */
            .event-block {{
                display: flex;
                flex-direction: column;
                align-items: flex-start;
                margin: 6px 0;
            }}
            .event-block.home {{ align-items: flex-end; text-align: right; }}
            .event-header {{
                display: flex;
                align-items: center;
                gap: 8px;
                flex-wrap: nowrap;
            }}
            .event-icon {{
                width: 28px;
                height: 28px;
                filter: drop-shadow(0 0 4px white);
            }}
            .event-assist {{
                color: #aaa;
                font-size: {GetValue(CssStyle.FontAssist)};
                margin-top: 2px;
            }}

            /* === Бейджи === */
            .badge {{
                display: inline-flex;
                justify-content: center;
                align-items: center;
                width: 28px;
                height: 28px;
                border-radius: 4px;
                font-weight: bold;
                font-size: {GetValue(CssStyle.FontBadge)};
                margin-right: 4px;
            }}
            .b2 {{ background: #ffb703; color: #2B2D31; }}
            .b5 {{ background: #e85d04; color: #2B2D31; }}
            .b10 {{ background: #a00000; color: #2B2D31; }}
            ";
    }
}