using Serilog;
using TelegramBOT.Application.Telegram;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Presentation.Rendering.Html;
using TelegramBOT.Presentation.UI;
using System.Text;
using TelegramBOT.Presentation.Rendering.Html.Teams;

namespace TelegramBOT.Application.Teams
{
    public class TeamsService
    {
        private readonly ITeamStatsRepository _teamStatsRepository;
        private readonly MappingService _mapping;
        private readonly MessageService _messageService;
        private readonly MenuService _menuService;
        private readonly TeamCardPosterHtmlBuilder _htmlBuilder;
        private readonly TeamStatsCalculator _teamStatsCalculator;

        public TeamsService(
            ITeamStatsRepository teamStatsRepository,
            TeamStatsCalculator teamStatsCalculator,
            MappingService mappingService,
            MessageService messageService,
            MenuService menuService,
            TeamCardPosterHtmlBuilder htmlBuilder)
        {
            _teamStatsRepository = teamStatsRepository;
            _teamStatsCalculator = teamStatsCalculator;
            _mapping = mappingService;
            _messageService = messageService;
            _menuService = menuService;
            _htmlBuilder = htmlBuilder;
        }


        public async Task SendTeamCardAsync(long chatId, string teamCode)
        {
            try
            {
                Log.Information("[TeamsService] Запрос карточки: {TeamCode}", teamCode);

                // 1. Превращаем русское название кнопки → английское имя команды
                string teamName = _mapping.ReverseMap("TeamNamesPlain", teamCode);

                // 2. Загружаем 15 последних матчей
                var matches10 = await _teamStatsRepository.GetLastMatchesAsync(teamName, 10);

                if (matches10.Count == 0)
                {
                    await _messageService.SendTextAsync(chatId, "Данные по команде пока недоступны.");
                    return;
                }

                // 3. Считаем агрегированную статистику
                var stats = await _teamStatsCalculator.CalculateAsync(teamName, matches10);

                // 4. Получаем русский выводимый заголовок команды
                string teamNameRu = _mapping.Map("TeamNamesPlain", teamName);

                // 5. Локализуем город
                string city = _mapping.Map("Cities", teamName);

                // 6. HTML постер
                string html = _htmlBuilder.Build(teamName, city, stats);

                // 7. Рендер → PNG
                var renderer = new HtmlToImageRenderer();
                byte[] png = await renderer.RenderAsync(html, 1024, 1500);

                using var ms = new MemoryStream(png);

                await _messageService.SendPhotoAsync(chatId, ms, $"{teamNameRu} — статистика");

                // 8. Показываем меню
                var menu = _menuService.GetTeamsConferenceMenu();
                await _messageService.SendTextWithKeyboardAsync(chatId, "Выберите конференцию", menu);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[TeamsService] Ошибка генерации карточки: {TeamCode}", teamCode);
                await _messageService.SendTextAsync(chatId, "Произошла ошибка при формировании карточки.");
            }
        }
    }
}