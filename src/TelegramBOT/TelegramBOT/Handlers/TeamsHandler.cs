using TelegramBOT.Services;

namespace TelegramBOT.Handlers
{
    /// <summary>
    /// Обработчик команд, связанных с выбором команд.
    /// Отвечает за вывод последних результатов для выбранной команды.
    /// </summary>
    public class TeamsHandler
    {
        private readonly MessageService _messageService;
        private readonly MatchService _matchService;

        /// <summary>
        /// Словарь команд: ключ — отображаемое название, 
        /// значение — внутреннее название для поиска в БД
        /// </summary>
        private readonly Dictionary<string, string> _teams = new()
        {
            { "⭐ СКА Санкт-Петербург", "SKA St. Petersburg" },
            { "★ ЦСКА Москва", "CSKA Moscow" },
            { "🔵 Динамо Москва", "Dynamo Moscow" },
            { "♦️ Спартак Москва", "Spartak Moscow" },
            { "🚂 Локомотив Ярославль", "Lokomotiv Yaroslavl" },
            { "🦌 Торпедо Нижний Новгород", "Nizhny Novgorod" },
            { "⚒️ Северсталь Череповец", "Cherepovets" },
            { "🐆 ХК Сочи", "Sochi" },
            { "🐃 Динамо Минск", "Dinamo Minsk" },
            { "🚗 Лада Тольятти", "Lada" },
            { "🐉 Куньлунь Ред Стар", "Shanghai" },
            { "🦅 Авангард Омск", "Avangard Omsk" },
            { "🐯 Ак Барс Казань", "Bars Kazan" },
            { "⛏️ Металлург Магнитогорск", "Magnitogorsk" },
            { "🕌 Салават Юлаев Уфа", "Salavat Ufa" },
            { "🚘 Автомобилист Екатеринбург", "Yekaterinburg" },
            { "🚜 Трактор Челябинск", "Tractor Chelyabinsk" },
            { "⚓ Адмирал Владивосток", "Vladivostok" },
            { "❄️ Сибирь Новосибирск", "Novosibirsk" },
            { "🐺 Нефтехимик Нижнекамск", "Niznekamsk" },
            { "🐅 Амур Хабаровск", "Khabarovsk" }
        };

        public TeamsHandler(MessageService messageService, MatchService matchService)
        {
            _messageService = messageService;
            _matchService = matchService;
        }

        /// <summary>
        /// Обрабатывает команду, связанную с выбором команды.
        /// выводятся её последние результаты.
        /// </summary>
        /// <param name="chatId">ID чата.</param>
        /// <param name="text">Текст команды (название команды).</param>
        public async Task HandleTeamCommand(long chatId, string text)
        {
            if (_teams.TryGetValue(text, out var teamCode))
            {
                var teamResults = await _matchService.GetAllResultsByTeamAsync(teamCode);
                await _messageService.SendResultsAsync(chatId, teamResults, null, teamCode);
            }
        }
    }
}
