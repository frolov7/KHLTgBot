using PuppeteerSharp;
using Serilog;
using System.Text;
using TelegramBOT.Application.MatchEvents;
using TelegramBOT.Application.Utils;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Domain.Models;
using TelegramBOT.Infrastructure.Telegram;
using TelegramBOT.Presentation.UI.Menus.Calendar;
using TelegramBOT.Presentation.UI.Menus.Predictions;

namespace TelegramBOT.Application.MatchStats
{
    public class MatchStatsService
    {
        private readonly IMatchStatsServiceRepository _matchStatsRepository;
        private readonly IResultsRepository _resultsRepository;
        private readonly MappingService _mappingService;
        private readonly MessageService _messageService;

        public MatchStatsService(
            IMatchStatsServiceRepository matchStatsRepository,
            IResultsRepository resultsRepository,
            MappingService mappingService,
            MessageService messageService)
        {
            _matchStatsRepository = matchStatsRepository;
            _resultsRepository = resultsRepository;
            _mappingService = mappingService;
            _messageService = messageService;
        }

        // ==========================================================
        // ============      ОЧНЫЕ ВСТРЕЧИ КОМАНД       ============
        // ==========================================================

        /// <summary>
        /// Загружает очные встречи и отправляет пользователю результаты сыгранных матчей.
        /// </summary>
        public async Task SendHeadToHeadAsync(long chatId, string matchId)
        {
            Log.Information("[SendHeadToHeadAsync] Старт. chatId={ChatId}, matchId={MatchId}", chatId, matchId);

            var match = await _matchStatsRepository.GetMatchByIdAsync(matchId);
            if (match == null)
            {
                Log.Information("[SendHeadToHeadAsync] Матч не найден. matchId={MatchId}", matchId);
                await _messageService.SendTextAsync(chatId, "Матч не найден.");
                return;
            }

            var matches = await _matchStatsRepository.GetHeadToHeadMatchesAsync(match.HomeTeamName, match.AwayTeamName);
            if (!matches.Any())
            {
                Log.Information("[SendHeadToHeadAsync] Очных матчей нет. matchId={MatchId}", matchId);
                await _messageService.SendTextAsync(chatId, "Эти команды ещё не встречались.");
                return;
            }

            Log.Information("[SendHeadToHeadAsync] Найдено {Count} очных встреч. matchId={MatchId}", matches.Count(), matchId);

            var (home, away) = _mappingService.MapTeamNames(match);

            var sb = new StringBuilder();
            sb.AppendLine($"<b>Игры между собой {home} и {away}:</b>\n");
            BuildMatchList(sb, matches, match.HomeTeamName, home, includeOutcome: false);

            await _messageService.SendTextAsync(chatId, sb.ToString());

            var menu = new MatchMenuBuilder().Build(match);
            await _messageService.SendTextWithKeyboardAsync(chatId, $"{home} vs {away}", menu);
        }

        // ==========================================================
        // ============      ИСТОРИЯ ПОСЛЕДНИХ ИГР      ============
        // ==========================================================

        /// <summary>
        /// Загружает историю последних сыгранных матчей обеих команд и отправляет пользователю.
        /// Отображает исход каждого матча (🏆 победа / ❌ поражение).
        /// </summary>
        public async Task SendTeamsHistoryAsync(long chatId, string matchId)
        {
            Log.Information("[SendTeamsHistoryAsync] Старт. chatId={ChatId}, matchId={MatchId}", chatId, matchId);

            var match = await _matchStatsRepository.GetMatchByIdAsync(matchId);
            if (match == null)
            {
                Log.Information("[SendTeamsHistoryAsync] Матч не найден. matchId={MatchId}", matchId);
                await _messageService.SendTextAsync(chatId, "Матч не найден.");
                return;
            }

            var homeResults = (await _resultsRepository.GetResultsByTeamAsync(match.HomeTeamName)).ToList();
            var awayResults = (await _resultsRepository.GetResultsByTeamAsync(match.AwayTeamName)).ToList();

            if (!homeResults.Any() && !awayResults.Any())
            {
                Log.Information("[SendTeamsHistoryAsync] Нет данных по последним играм. matchId={MatchId}", matchId);
                await _messageService.SendTextAsync(chatId, "Нет данных по прошлым играм.");
                return;
            }

            Log.Information("[SendTeamsHistoryAsync] Найдены данные: home={Home}, away={Away}. matchId={MatchId}", homeResults.Count, awayResults.Count, matchId);

            var (home, away) = _mappingService.MapTeamNames(match);

            var sb = new StringBuilder();
            sb.AppendLine("<b>Последние матчи команд:</b>\n");

            sb.AppendLine($"<b>{home}</b> (последние {homeResults.Count}):");
            BuildMatchList(sb, homeResults, match.HomeTeamName, home, includeOutcome: true);
            sb.AppendLine();

            sb.AppendLine($"<b>{away}</b> (последние {awayResults.Count}):");
            BuildMatchList(sb, awayResults, match.AwayTeamName, away, includeOutcome: true);

            await _messageService.SendTextAsync(chatId, sb.ToString());

            var menu = new MatchMenuBuilder().Build(match);
            await _messageService.SendTextWithKeyboardAsync(chatId, $"{home} vs {away}", menu);
        }

        /// <summary>
        /// Формирует текстовый список матчей для отображения в сообщении Telegram.
        /// Универсальный метод для истории последних игр и очных встреч.
        /// </summary>
        /// <param name="sb">StringBuilder, в который добавляется форматированный список матчей.</param>
        /// <param name="matches">Коллекция матчей, которые необходимо вывести.</param>
        /// <param name="teamName">Системное имя команды (из базы данных), относительно которой строится список.</param>
        /// <param name="mappedName">Отображаемое (человекочитаемое) имя команды с эмодзи.</param>
        /// <param name="includeOutcome">Если true — добавляются эмодзи исходов (🏆 / ❌). Используется для последних матчей команд.</param>
        private void BuildMatchList(StringBuilder sb, IEnumerable<Match> matches, string teamName, string mappedName, bool includeOutcome = true)
        {
            foreach (var m in matches)
            {
                var emoji = includeOutcome ? GetMatchOutcomeEmoji(m, teamName) : "📅";
                var (home, away) = _mappingService.MapTeamNames(m);

                bool isHome = m.HomeTeamName == teamName;
                var opponent = isHome ? away : home;

                string extraStatus = m.Status switch
                {
                    "AFTER OVERTIME" => " (ОТ)",
                    "AFTER PENALTIES" => " (Бул)",
                    _ => string.Empty
                };

                var line = isHome
                    ? $"{emoji} {m.MatchDate:dd.MM} — {mappedName} <b>{m.HomeScore}:{m.AwayScore}</b>{extraStatus} {opponent}"
                    : $"{emoji} {m.MatchDate:dd.MM} — {opponent} <b>{m.HomeScore}:{m.AwayScore}</b>{extraStatus} {mappedName}";

                sb.AppendLine(line);
            }
        }

        /// <summary>
        /// Возвращает эмодзи исхода матча для указанной команды.
        /// </summary>
        private string GetMatchOutcomeEmoji(Match match, string teamName)
        {
            if (match.HomeScore == null || match.AwayScore == null)
                return "📅";

            bool isHome = match.HomeTeamName == teamName;
            bool isWin = (isHome && match.HomeScore > match.AwayScore) || (!isHome && match.AwayScore > match.HomeScore);

            return isWin ? "🏆" : "❌";
        }

        // ==========================================================
        // ============      ПРОГНОЗЫ НА МАТЧ            ============
        // ==========================================================

        /// <summary>
        /// Загружает прогнозы по матчу и отправляет меню с выбором источников.
        /// </summary>
        public async Task SendPredictionsAsync(long chatId, string matchId)
        {
            Log.Information("[SendPredictionsAsync] Старт. chatId={ChatId}, matchId={MatchId}", chatId, matchId);

            var match = await _matchStatsRepository.GetMatchByIdAsync(matchId);
            if (match == null)
            {
                Log.Information("[SendPredictionsAsync] Матч не найден. matchId={MatchId}", matchId);
                await _messageService.SendTextAsync(chatId, "Матч не найден.");
                return;
            }

            var predictions = await _matchStatsRepository.GetPredictionsByMatchIdAsync(matchId);
            if (!predictions.Any())
            {
                Log.Information("[SendPredictionsAsync] Прогнозы отсутствуют. matchId={MatchId}", matchId);
                await _messageService.SendTextAsync(chatId, "Прогнозов пока нет.");
                return;
            }

            Log.Information("[SendPredictionsAsync] Найдено {Count} прогнозов. matchId={MatchId}", predictions.Count(), matchId);

            var (home, away) = _mappingService.MapTeamNames(match);
            var text = $"🔮 Прогнозы на матч <b>{home}</b> vs <b>{away}</b>";

            var menu = new PredictionsMenuBuilder().Build(matchId);
            await _messageService.SendTextWithKeyboardAsync(chatId, text, menu);
        }
    }
}
