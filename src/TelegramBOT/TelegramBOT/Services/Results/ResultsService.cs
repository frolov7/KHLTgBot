using Serilog;
using System.Text;
using TelegramBOT.Models;
using TelegramBOT.Services.Core;
using TelegramBOT.Services.Utils;

namespace TelegramBOT.Services.Results
{
    /// <summary>
    /// Сервис бизнес-логики для работы с результатами матчей.
    /// Отвечает за получение, обновление и форматирование данных о результатах.
    /// </summary>
    public class ResultsService
    {
        private readonly IResultsRepository _resultRepository;
        private readonly ScriptService _scriptService;
        private readonly MappingService _mappingService;

        public ResultsService(
            IResultsRepository resultRepository,
            ScriptService scriptService,
            MappingService mappingService)
        {
            _resultRepository = resultRepository;
            _scriptService = scriptService;
            _mappingService = mappingService;
        }

        // ==========================================================
        // ===============      БЛОК ЗАГРУЗКИ ДАННЫХ     =============
        // ==========================================================

        /// <summary>
        /// Возвращает список результатов за указанную дату.
        /// </summary>
        /// <param name="date">Дата, за которую требуется получить результаты.</param>
        /// <returns>Список матчей с результатами.</returns>
        public async Task<IEnumerable<Match>> GetResultsByDateAsync(DateTime date)
        {
            return await _resultRepository.GetResultsByDateAsync(date);
        }

        /// <summary>
        /// Возвращает результаты всех матчей определённой команды.
        /// </summary>
        /// <param name="teamName">Название команды.</param>
        /// <returns>Список матчей с участием указанной команды.</returns>
        public async Task<IEnumerable<Match>> GetResultsByTeamAsync(string teamName)
        {
            return await _resultRepository.GetResultsByTeamAsync(teamName);
        }

        /// <summary>
        /// Возвращает результат конкретного матча по его идентификатору.
        /// </summary>
        /// <param name="matchId">Идентификатор матча.</param>
        /// <returns>Объект <see cref="Match"/> или <c>null</c>, если матч не найден.</returns>
        public async Task<Match?> GetResultByIdAsync(string matchId)
        {
            return await _resultRepository.GetResultByIdAsync(matchId);
        }

        // ==========================================================
        // ===============      ОБНОВЛЕНИЕ ДАННЫХ       =============
        // ==========================================================

        /// <summary>
        /// Запускает обновление данных о результатах и прогнозах.
        /// </summary>
        /// <returns><c>true</c>, если обновление завершилось успешно; иначе <c>false</c>.</returns>
        public async Task<bool> UpdateResultsAsync()
        {
            try
            {
                await _scriptService.RunScrapersAsync();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ошибка при обновлении данных (результаты/прогнозы)");
                return false;
            }
        }

        // ==========================================================
        // ===============      ФОРМАТИРОВАНИЕ ДАННЫХ   =============
        // ==========================================================

        /// <summary>
        /// Формирует красивое текстовое сообщение с результатами матчей.
        /// </summary>
        /// <param name="matches">Список матчей.</param>
        /// <param name="date">Дата (опционально).</param>
        /// <param name="teamName">Название команды (если нужно показать результаты конкретной команды).</param>
        /// <returns>Готовый текст сообщения для Telegram.</returns>
        public string BuildResultsMessage(IEnumerable<Match> matches, DateTime? date = null, string? teamName = null)
        {
            if (matches == null || !matches.Any())
                return "Результатов не найдено.";

            var sb = new StringBuilder();

            // Заголовок
            if (date != null)
                sb.AppendLine($"⚡ Результаты матчей за {date:dd.MM.yyyy}\n");
            else
                sb.AppendLine("⚡ Результаты матчей:\n");

            foreach (var match in matches)
            {
                var homeName = _mappingService.Map("TeamNames", match.HomeTeamName);
                var awayName = _mappingService.Map("TeamNames", match.AwayTeamName);

                string statusText;

                // Победа / поражение (если указана команда)
                if (teamName != null && match.Status != "SCHEDULED" &&
                    !(match.Status.Contains("PERIOD") || match.Status == "OVERTIME" || match.Status == "PENALTIES"))
                {
                    bool isHome = match.HomeTeamName == teamName;
                    int homeScore = match.HomeScore ?? 0;
                    int awayScore = match.AwayScore ?? 0;
                    bool isWin = (isHome && homeScore > awayScore) || (!isHome && awayScore > homeScore);

                    var shortStatus = _mappingService.Map("MatchStatusesShort", match.Status);
                    statusText = isWin
                        ? $"🏆 Победа ({shortStatus})"
                        : $"❌ Поражение ({shortStatus})";
                }
                else
                {
                    statusText = _mappingService.Map("MatchStatuses", match.Status);
                }

                // Время или дата
                if (date != null)
                    sb.AppendLine($"⏰ {match.MatchDate:HH:mm} (МСК)");
                else
                    sb.AppendLine($"📅 {match.MatchDate:dd.MM.yyyy}");

                // Счёт или анонс
                if (match.Status != "SCHEDULED")
                    sb.AppendLine($"{homeName} <b>{match.HomeScore ?? 0} : {match.AwayScore ?? 0}</b> {awayName}");
                else
                    sb.AppendLine($"{homeName} vs {awayName}");

                sb.AppendLine(statusText);
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
