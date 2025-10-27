using System.Text;
using TelegramBOT.Models;
using TelegramBOT.Services.Core;
using TelegramBOT.Services.Utils;

namespace TelegramBOT.Services.Teams
{
    public class TeamsService
    {
        private readonly ITeamsRepository _repository;
        private readonly MappingService _mappingService;

        public TeamsService(ITeamsRepository repository, MappingService mappingService)
        {
            _repository = repository;
            _mappingService = mappingService;
        }

        // ==========================================================
        // ============      ПОЛУЧЕНИЕ РЕЗУЛЬТАТОВ       ============
        // ==========================================================

        /// <summary>
        /// Получает последние сыгранные матчи указанной команды.
        /// </summary>
        /// <param name="teamName">Название команды (внутреннее имя).</param>
        public async Task<List<Match>> GetResultsByTeamAsync(string teamName)
        {
            return await _repository.GetRecentMatchesByTeamAsync(teamName);
        }

        // ==========================================================
        // ============      ПОСТРОЕНИЕ СООБЩЕНИЯ         ============
        // ==========================================================

        /// <summary>
        /// Формирует сообщение со списком последних матчей выбранной команды.
        /// </summary>
        /// <param name="matches">Список матчей.</param>
        /// <param name="teamName">Название команды (внутреннее имя).</param>
        public string BuildTeamResultsMessage(IEnumerable<Match> matches, string teamName)
        {
            if (matches == null || !matches.Any())
                return "❌ Нет сыгранных матчей для этой команды.";

            var sb = new StringBuilder();
            sb.AppendLine($"⚡ Последние результаты команды {_mappingService.Map("TeamNames", teamName)}:\n");

            foreach (var match in matches)
            {
                var (home, away) = _mappingService.MapTeamNames(match);

                bool isHome = match.HomeTeamName == teamName;
                int homeScore = match.HomeScore ?? 0;
                int awayScore = match.AwayScore ?? 0;
                bool isWin = (isHome && homeScore > awayScore) || (!isHome && awayScore > homeScore);

                string statusShort = _mappingService.Map("MatchStatusesShort", match.Status);
                string resultEmoji = isWin ? "🏆 Победа" : "❌ Поражение";

                sb.AppendLine($"📅 {match.MatchDate:dd.MM.yyyy} ⏰ {match.MatchDate:HH:mm} (МСК)");
                sb.AppendLine($"{home} <b>{homeScore} : {awayScore}</b> {away}");
                sb.AppendLine($"{resultEmoji} ({statusShort})");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        // ==========================================================
        // ============      СЛОВАРЬ КОМАНД              ============
        // ==========================================================

        /// <summary>
        /// Возвращает словарь доступных команд (русское название → системное имя).
        /// </summary>
        public Dictionary<string, string> GetTeamsDictionary() => new()
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
    }
}
