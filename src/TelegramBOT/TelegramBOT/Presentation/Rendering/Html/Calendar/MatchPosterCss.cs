using static System.Net.Mime.MediaTypeNames;
using System.ComponentModel;

namespace TelegramBOT.Presentation.Rendering.Html.Calendar
{
    public static class MatchPosterCss
    {
        private enum CssStyle
        {
            BackgroundColor,
            TextColor,
            AccentColor,
            AccentSoft,
            ShadowColor,

            FontMain,
            FontTeams,
            FontVs,
            FontInfoArena,
            FontInfoDate,
        }

        private static string GetValue(CssStyle style) => style switch
        {
            CssStyle.BackgroundColor => "#020817",
            CssStyle.TextColor => "#ffffff",
            CssStyle.AccentColor => "#5CA0D3",
            CssStyle.AccentSoft => "rgba(92,160,211,0.4)",
            CssStyle.ShadowColor => "rgba(0,0,0,0.55)",

            CssStyle.FontMain => "20px",
            CssStyle.FontTeams => "40px",
            CssStyle.FontVs => "36px",
            CssStyle.FontInfoArena => "30px",
            CssStyle.FontInfoDate => "24px",

            _ => ""
        };

        public static string Get() => $@"
            html, body {{
                margin: 0;
                padding: 0;
                background: {GetValue(CssStyle.BackgroundColor)};
                color: {GetValue(CssStyle.TextColor)};
                font-family: 'Segoe UI', system-ui, -apple-system, BlinkMacSystemFont, sans-serif;
                width: 100%;
                height: 100%;
            }}

            /* Обёртка, чтобы постер был по центру */
            .poster-wrapper {{
                width: 100%;
                height: 100%;
                display: flex;
                justify-content: center;
                align-items: center;
                padding: 0;
                box-sizing: border-box;
            }}

            /* Сам постер 1024x1536 */
            .poster {{
                position: relative;
                width: 1024px;
                height: 1191px;
                overflow: hidden;
                background: radial-gradient(circle at top, #102a53 0%, #020817 60%, #000000 100%);
                box-shadow: 0 0 40px rgba(0,0,0,0.9);
            }}

            /* Фон (арена/лёд) */
            .poster-bg {{
                position: absolute;
                inset: 0;
                width: 100%;
                height: 100%;
                object-fit: cover;
                opacity: 0.82;
            }}

            /* Крупные тени маскотов на заднем плане */
            .mascot-shadow {{
                position: absolute;
                filter: grayscale(1) brightness(0.7) contrast(1.2);
                opacity: 0.26;
                mix-blend-mode: screen;
                transform-origin: center;
            }}

            .mascot-shadow-home {{
                width: 620px;
                height: auto;
                left: -60px;
                top: 80px;
                transform: scale(1.15);
            }}

            .mascot-shadow-away {{
                width: 620px;
                height: auto;
                right: -60px;
                top: 80px;
                transform: scale(1.15);
            }}

            /* Основные (цветные) маскоты спереди */
            .mascot-main {{
                position: absolute;
                width: 405px;
                z-index: 30;

                top: 55%;
                transform: translateY(-50%);
            }}

            .mascot-main-home {{
                left: 40px;
            }}

            .mascot-main-away {{
                right: 40px;
            }}

            /* Верхняя строка с названиями команд и VS */
            .teams-row {{
                position: absolute;
                top: 70px;
                left: 0;
                right: 0;
                display: flex;
                justify-content: center;
                align-items: center;
                text-align: center;
                gap: 36px;
                z-index: 5;
                text-shadow: 0 0 14px rgba(0,0,0,0.8);
            }}

            .team-name {{
                font-size: {GetValue(CssStyle.FontTeams)};
                font-weight: 900;
                letter-spacing: 1.5px;
                text-transform: uppercase;
                padding: 6px 22px;
                border-radius: 999px;
                background: linear-gradient(90deg, rgba(0,0,0,0.55), rgba(10,30,60,0.9));
                box-shadow: 0 0 18px rgba(0,0,0,0.75);
            }}

            .team-name-home {{
                border: 1px solid rgba(120,220,255,0.5);
            }}

            .team-name-away {{
                border: 1px solid rgba(255,160,160,0.5);
            }}

            .vs-block {{
                font-size: {GetValue(CssStyle.FontVs)};
                font-weight: 800;
                color: {GetValue(CssStyle.AccentColor)};
                letter-spacing: 4px;
                text-shadow:
                    0 0 8px rgba(0,0,0,0.9),
                    0 0 16px rgba(92,160,211,0.8);
            }}

            .team-logo {{position: absolute;
                width: 260px;
                opacity: 0.15;
                filter: drop-shadow(0 0 12px rgba(0,0,0,0.6));
            }}

            .team-logo-home {{
                left: 120px;
                top: 60px;
            }}

            .team-logo-away {{  
                right: 120px;
                top: 60px;
            }}

            /* Нижняя плашка под арену, дату и время */
            .info-strip {{
                position: absolute;
                left: 50%;
                transform: translateX(-50%);
                bottom: 40px;
                width: 82%;
                padding: 22px 32px;
                border-radius: 18px;
                background: linear-gradient(90deg, rgba(8,20,40,0.95), rgba(6,35,70,0.95));
                box-shadow:
                    0 0 30px rgba(0,0,0,0.9),
                    0 0 40px rgba(0,0,0,0.8);
                display: flex;
                flex-direction: column;
                justify-content: center;
                align-items: center;
                gap: 6px;
                z-index: 6;
            }}

            .arena-name {{
                font-size: {GetValue(CssStyle.FontInfoArena)};
                font-weight: 800;
                letter-spacing: 1px;
                text-transform: none;
                color: {GetValue(CssStyle.TextColor)};
                text-align: center;
                margin-bottom: 4px;
            }}

            .match-datetime {{
                font-size: {GetValue(CssStyle.FontInfoDate)};
                color: #d0e4ff;
                display: flex;
                align-items: center;
                justify-content: center;
                gap: 10px;
                text-transform: none;
            }}

            .match-date {{
                font-weight: 600;
            }}

            .match-time {{
                font-weight: 700;
            }}

            /* --- Текст СЧЁТА --- */
            .match_score {{
                font-size: 48px;
                font-weight: 900;
                letter-spacing: 2px;
                text-shadow: 0 0 18px rgba(0,0,0,0.7);
                color: #ffffff;
            }}

            /* --- Текст статуса: «Основное время» --- */
            .result-type {{
                font-size: 28px;
                font-weight: 700;
                margin-top: 4px;
                color: #d0e4ff;
            }}

            .dot {{
                font-size: {GetValue(CssStyle.FontInfoDate)};
                opacity: 0.8;
            }}

            /* ====== Head-to-Head Block (H2H) ====== */
            .h2h-strip {{
                position: absolute;
                left: 50%;
                transform: translateX(-50%);
                bottom: 40px;
                width: 82%;
                padding: 26px 40px;
                border-radius: 18px;
                background: linear-gradient(90deg, rgba(8,20,40,0.95), rgba(6,35,70,0.95));
                box-shadow: 0 0 30px rgba(0,0,0,0.9), 0 0 40px rgba(0,0,0,0.8);
                z-index: 100;
            }}

            .h2h-row {{
                display: grid;
                grid-template-columns: 140px 1fr;
                column-gap: 5px; 
                font-size: 32px;
                font-weight: 700;
                margin: 10px 0;
                color: #ffffff;
            }}

            .h2h-frame {{
                display: grid;
                grid-template-columns: 1fr 110px 1fr;  /* Ровные симметричные зоны */
                column-gap: 20px;  
                margin-left: -50px;
            }}

            .h2h-date {{
                text-align: left;
            }}

            .h2h-home {{
                text-align: right;
                white-space: nowrap;
                padding-right: 1px;   /* ← двигай команду ближе/дальше от счёта */
            }}

            .h2h-score {{
                text-align: center;
                white-space: nowrap;
            }}

            .h2h-away {{
                text-align: left;
                white-space: nowrap;
            }}
        ";
    }
}
