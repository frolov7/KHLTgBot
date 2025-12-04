using Serilog;
using TelegramBOT.Application.Telegram;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Presentation.Rendering.Html;
using TelegramBOT.Presentation.UI;
using System.Text;

namespace TelegramBOT.Application.Teams
{
    public class TeamsService
    {
        private readonly IMatchStatsServiceRepository _statsRepository;
        private readonly MappingService _mapping;
        private readonly MessageService _messageService;
        private readonly MenuService _menuService;
        private readonly TeamCardPosterHtmlBuilder _htmlBuilder;

        public TeamsService(
            IMatchStatsServiceRepository statsRepository,
            MappingService mappingService,
            MessageService messageService,
            MenuService menuService,
            TeamCardPosterHtmlBuilder htmlBuilder)
        {
            _statsRepository = statsRepository;
            _mapping = mappingService;
            _messageService = messageService;
            _menuService = menuService;
            _htmlBuilder = htmlBuilder;
        }

        // ============================================================================

        /// <summary>
        /// Приватный словарь код → английское имя для БД
        /// </summary>
        private string ResolveEnglishTeamName(string teamCode)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["cska"] = "CSKA Moscow",
                ["ska"] = "SKA St. Petersburg",
                ["dynamo_moscow"] = "Dynamo Moscow",
                ["spartak"] = "Spartak Moscow",
                ["severstal"] = "Cherepovets",
                ["lokomotiv"] = "Lokomotiv Yaroslavl",
                ["torpedo"] = "Nizhny Novgorod",
                ["dynamo_minsk"] = "Dinamo Minsk",
                ["sochi"] = "Sochi",
                ["lada"] = "Lada",
                ["dragons"] = "Shanghai",

                ["avtomobilist"] = "Yekaterinburg",
                ["avangard"] = "Avangard Omsk",
                ["traktor"] = "Tractor Chelyabinsk",
                ["barys"] = "Barys Astana",
                ["metallurg"] = "Magnitogorsk",
                ["amur"] = "Khabarovsk",
                ["ak_bars"] = "Bars Kazan",
                ["admiral"] = "Vladivostok",
                ["neftekhimik"] = "Niznekamsk",
                ["salavat"] = "Salavat Ufa",
                ["sibir"] = "Novosibirsk"
            };

            return map.TryGetValue(teamCode, out var eng) ? eng : teamCode;
        }

        // ============================================================================

        public async Task SendTeamCardAsync(long chatId, string teamCode)
        {
            /*
            try
            {
                Log.Information("[TeamsService] Запрос карточки команды: {TeamCode}", teamCode);

                // 1. teamCode → EnglishName
                string englishName = ResolveEnglishTeamName(teamCode);

                // 2. Загружаем матчи
                var matches = (await _statsRepository.GetLastMatchesByTeamAsync(englishName))
                    .OrderByDescending(m => m.MatchDate)
                    .Take(15)
                    .ToList();

                if (!matches.Any())
                {
                    await _messageService.SendTextAsync(chatId, "Данные по команде пока недоступны.");
                    return;
                }

                // 3. Маппинг имени и арены
                string teamNameRu = _mapping.Map("TeamNamesPlain", englishName);
                string arena = _mapping.Map("Arenas", englishName);

                // 4. HTML
                string html = _htmlBuilder.Build(teamNameRu, arena, matches);

                // 5. Рендер PNG
                var renderer = new HtmlToImageRenderer();
                byte[] png = await renderer.RenderAsync(html, 1024, 1500);

                using var ms = new MemoryStream(png);

                await _messageService.SendPhotoAsync(chatId, ms, $"{teamNameRu} — статистика");

                // 6. Навигация
                var backMenu = _menuService.GetTeamsConferenceMenu();
                await _messageService.SendTextWithKeyboardAsync(chatId, "Выберите конференцию:", backMenu);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[TeamsService] Ошибка при генерации карточки команды: {TeamCode}", teamCode);
                await _messageService.SendTextAsync(chatId, "Произошла ошибка при формировании карточки.");
            }
            */
        }
    }
}