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
            { "⭐ СКА", "SKA St. Petersburg" },
            { "★ ЦСКА", "CSKA Moscow" },
            { "🔵 Динамо Москва", "Dynamo Moscow" },
            { "♦️ Спартак", "Spartak Moscow" },
            { "🚂 Локомотив", "Lokomotiv Yaroslavl" },
            { "🦌 Торпедо", "Nizhny Novgorod" },
            { "⚒️ Северсталь", "Cherepovets" },
            { "🐆 ХК Сочи", "Sochi" },
            { "🐃 Динамо Минск", "Dinamo Minsk" },
            { "🚗 Лада", "Lada" },
            { "🐉 Шанхай Дрэгонс", "Shanghai" },
            { "🦅 Авангард", "Avangard Omsk" },
            { "🐯 Ак Барс", "Bars Kazan" },
            { "⛏️ Металлург", "Magnitogorsk" },
            { "🕌 Салават Юлаев", "Salavat Ufa" },
            { "🚘 Автомобилист", "Yekaterinburg" },
            { "🚜 Трактор", "Tractor Chelyabinsk" },
            { "⚓ Адмирал", "Vladivostok" },
            { "❄️ Сибирь", "Novosibirsk" },
            { "🐺 Нефтехимик", "Niznekamsk" },
            { "🐆 Барыс", "Barys Astana" },
            { "🐅 Амур", "Khabarovsk" }
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
