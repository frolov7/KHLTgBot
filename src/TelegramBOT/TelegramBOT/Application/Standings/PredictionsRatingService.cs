using Serilog;
using TelegramBOT.Application.Telegram;
using TelegramBOT.Domain.Entities.Predictions;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Presentation.Rendering.Html;
using TelegramBOT.Presentation.Rendering.Html.Statistics;
using TelegramBOT.Presentation.UI;

namespace TelegramBOT.Application.Standings
{
    /// <summary>
    /// Сервис формирования и отображения рейтинга прогнозов по источникам.
    /// </summary>
    public class PredictionsRatingService
    {
        private readonly IPredictionRepository _predictionRepository;
        private readonly MessageService _messageService;
        private readonly MenuService _menuService;

        public PredictionsRatingService(
            IPredictionRepository predictionRepository,
            MessageService messageService,
            MenuService menuService)
        {
            _predictionRepository = predictionRepository;
            _messageService = messageService;
            _menuService = menuService;
        }

        // ==========================================================
        // ============      ПУБЛИЧНЫЙ МЕТОД ОТПРАВКИ     ==========
        // ==========================================================

        /// <summary>
        /// Формирует и отправляет рейтинг прогнозов по источникам.
        /// </summary>
        public async Task SendPredictionsRatingAsync(long chatId)
        {
            Log.Information("[SendPredictionsRatingAsync] Старт. chatId={ChatId}", chatId);

            try
            {
                // 1. Загружаем прогнозы
                var predictions = await _predictionRepository.GetAllAsync();

                // 2. Фильтрация (ТОЛЬКО main + WIN/LOSE)
                var valid = predictions
                    .Where(p =>
                        !string.IsNullOrWhiteSpace(p.MainPrediction) &&
                        (p.Result == "WIN" || p.Result == "LOSE"))
                    .ToList();

                var result = new List<SourcePredictionStats>();

                // 3. Группировка по источникам
                foreach (var g in valid.GroupBy(p => p.Source))
                {
                    var items = g.ToList();

                    var win = items.Count(p => p.Result == "WIN");
                    var lose = items.Count(p => p.Result == "LOSE");
                    var total = win + lose;

                    var hitRate = total == 0 ? 0 : Math.Round((double)win / total * 100, 1);
                    var rating = total == 0 ? 0 : Math.Round(hitRate * Math.Log(total), 2);

                    var stats = new SourcePredictionStats
                    {
                        Source = g.Key,
                        Total = total,
                        Win = win,
                        Lose = lose,
                        HitRate = hitRate,
                        Rating = rating
                    };

                    // 4. Статистика по типам (ТОЛЬКО main)
                    foreach (var p in items)
                    {
                        var type = DetectType(p.MainPrediction);
                        if (type == null)
                            continue;

                        if (!stats.Types.ContainsKey(type))
                            stats.Types[type] = new TypeStats();

                        var t = stats.Types[type];
                        t.Total++;

                        if (p.Result == "WIN") t.Win++;
                        else t.Lose++;
                    }

                    foreach (var t in stats.Types.Values)
                    {
                        t.HitRate = t.Total == 0
                            ? 0
                            : Math.Round((double)t.Win / t.Total * 100, 1);
                    }

                    // 5. Лучший тип (>=3 прогнозов)
                    stats.BestType = stats.Types
                        .Where(x => x.Value.Total >= 3)
                        .OrderByDescending(x => x.Value.HitRate)
                        .Select(x => x.Key)
                        .FirstOrDefault();

                    // 6. Форма (последние 10 прогнозов)
                    stats.LastResults = items
                        .OrderByDescending(p => p.Match.MatchDate)
                        .Take(10)
                        .Select(p => p.Result == "WIN")
                        .ToList();

                    result.Add(stats);
                }

                // 7. Сортировка по рейтингу
                result = result
                    .OrderByDescending(x => x.Rating)
                    .ToList();

                // 8. HTML
                var html = SourceAccuracyPosterHtmlBuilder.Build(result);

                // 9. PNG
                var renderer = new HtmlToImageRenderer();
                var png = await renderer.RenderAsync(html, 1900, 900);

                await using var ms = new MemoryStream(png);

                // 10. Отправка
                await _messageService.SendPhotoAsync(
                    chatId,
                    ms,
                    "📊 Рейтинг прогнозов по источникам"
                );

                await _messageService.SendKeyboardAsync(
                    chatId,
                    "Выберите действие:",
                    _menuService.GetTablesMenu()
                );

                Log.Information("[SendPredictionsRatingAsync] Успешно завершено.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[SendPredictionsRatingAsync] Ошибка");
                await _messageService.SendTextAsync(
                    chatId,
                    "⚠️ Ошибка формирования рейтинга прогнозов."
                );
            }
        }

        // ==========================================================
        // ============      ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ      ==========
        // ==========================================================

        private static string? DetectType(string main)
        {
            main = main.ToUpper();

            if (main == "П1" || main == "П2" || main == "X" || main == "1X" || main == "X2")
                return "П1 / П2";

            if (main.StartsWith("Ф1") || main.StartsWith("Ф2"))
                return "Форы";

            if (main.StartsWith("ТБ") || main.StartsWith("ТМ"))
                return "Тоталы";

            if (main.StartsWith("ИТБ") || main.StartsWith("ИТМ"))
                return "ИТБ / ИТМ";

            if (main.StartsWith("ОЗ"))
                return "ОЗ";

            return null;
        }
    }
}
