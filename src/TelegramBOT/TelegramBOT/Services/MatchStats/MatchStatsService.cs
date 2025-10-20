using System.Text;
using TelegramBOT.Models;
using TelegramBOT.Services.Core;
using TelegramBOT.Services.Utils;
using TelegramBOT.UI;
using TelegramBOT.UI.Menus.Calendar;
using TelegramBOT.UI.Menus.Predictions;

namespace TelegramBOT.Services.Stats
{
    public class MatchStatsService
    {
        private readonly IMatchStatsServiceRepository _statsRepository;
        private readonly MappingService _mappingService;
        private readonly MessageService _messageService;

        public MatchStatsService(
            IMatchStatsServiceRepository statsRepository,
            MappingService mappingService,
            MessageService messageService)
        {
            _statsRepository = statsRepository;
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
            var match = await _statsRepository.GetMatchByIdAsync(matchId);
            if (match == null)
            {
                await _messageService.SendTextAsync(chatId, "❌ Матч не найден.");
                return;
            }

            // Получаем очные встречи через репозиторий
            var matches = await _statsRepository.GetHeadToHeadMatchesAsync(match.HomeTeamName, match.AwayTeamName);
            if (!matches.Any())
            {
                await _messageService.SendTextAsync(chatId, "❌ Эти команды ещё не встречались.");
                return;
            }

            var (home, away) = MapTeamNames(match);
            var sb = new StringBuilder();
            sb.AppendLine($"Игры между собой <b>{home}</b> и <b>{away}</b>:\n");

            foreach (var m in matches)
            {
                var homeTeam = _mappingService.Map("TeamNames", m.HomeTeamName);
                var awayTeam = _mappingService.Map("TeamNames", m.AwayTeamName);

                sb.AppendLine($"📅 {m.MatchDate:dd.MM.yyyy} — {homeTeam} <b>{m.HomeScore}:{m.AwayScore}</b> {awayTeam}");
            }

            await _messageService.SendTextAsync(chatId, sb.ToString());

            // После списка — вернуть inline-меню матча
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
            var match = await _statsRepository.GetMatchByIdAsync(matchId);
            if (match == null)
            {
                await _messageService.SendTextAsync(chatId, "❌ Матч не найден.");
                return;
            }

            var homeResults = (await _statsRepository.GetRecentMatchesByTeamAsync(match.HomeTeamName)).ToList();
            var awayResults = (await _statsRepository.GetRecentMatchesByTeamAsync(match.AwayTeamName)).ToList();

            if (!homeResults.Any() && !awayResults.Any())
            {
                await _messageService.SendTextAsync(chatId, "❌ Нет данных по прошлым играм.");
                return;
            }

            var (home, away) = MapTeamNames(match);
            var sb = new StringBuilder();
            sb.AppendLine("📈 <b>Последние матчи команд:</b>\n");

            // ------------------ Домашняя команда ------------------
            sb.AppendLine($"<b>{home}</b> (последние {homeResults.Count}):");
            AppendMatchList(sb, homeResults, match.HomeTeamName, home);
            sb.AppendLine();

            // ------------------ Гостевая команда ------------------
            sb.AppendLine($"<b>{away}</b> (последние {awayResults.Count}):");
            AppendMatchList(sb, awayResults, match.AwayTeamName, away);

            await _messageService.SendTextAsync(chatId, sb.ToString());

            // Меню матча (возврат)
            var menu = new MatchMenuBuilder().Build(match);
            await _messageService.SendTextWithKeyboardAsync(chatId, $"{home} vs {away}", menu);
        }

        /// <summary>
        /// Форматирует и добавляет в сообщение список сыгранных матчей указанной команды.
        /// Используется для вывода последних игр с указанием исхода (🏆 победа / ❌ поражение).
        /// </summary>
        /// <param name="sb">StringBuilder, в который добавляются строки с результатами матчей.</param>
        /// <param name="matches">Коллекция сыгранных матчей команды.</param>
        /// <param name="teamName">Системное имя команды (из базы данных), для которой выводится статистика.</param>
        /// <param name="mappedName">Человекочитаемое имя команды (с эмодзи), отображаемое в сообщении.</param>
        private void AppendMatchList(StringBuilder sb, IEnumerable<Match> matches, string teamName, string mappedName)
        {
            foreach (var m in matches)
            {
                var emoji = GetMatchOutcomeEmoji(m, teamName);
                var opponent = m.HomeTeamName == teamName
                    ? _mappingService.Map("TeamNames", m.AwayTeamName)
                    : _mappingService.Map("TeamNames", m.HomeTeamName);

                var line = m.HomeTeamName == teamName
                    ? $"{emoji} {m.MatchDate:dd.MM} — {mappedName} <b>{m.HomeScore}:{m.AwayScore}</b> {opponent}"
                    : $"{emoji} {m.MatchDate:dd.MM} — {opponent} <b>{m.HomeScore}:{m.AwayScore}</b> {mappedName}";

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
            int homeScore = match.HomeScore ?? 0;
            int awayScore = match.AwayScore ?? 0;

            bool isWin = (isHome && homeScore > awayScore) || (!isHome && awayScore > homeScore);

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
            var match = await _statsRepository.GetMatchByIdAsync(matchId);
            if (match == null)
            {
                await _messageService.SendTextAsync(chatId, "❌ Матч не найден.");
                return;
            }

            var predictions = await _statsRepository.GetPredictionsByMatchIdAsync(matchId);
            if (!predictions.Any())
            {
                await _messageService.SendTextAsync(chatId, "❌ Прогнозов пока нет.");
                return;
            }

            var (home, away) = MapTeamNames(match);
            var text = $"🔮 Прогнозы на матч <b>{home}</b> vs <b>{away}</b>";

            var menu = new PredictionsMenuBuilder().Build(matchId);
            await _messageService.SendTextWithKeyboardAsync(chatId, text, menu);
        }

        // ==========================================================
        // ============      МАППИНГ ДАННЫХ              ============
        // ==========================================================

        /// <summary>
        /// Преобразует системные имена команд в человекочитаемые.
        /// </summary>
        public (string Home, string Away) MapTeamNames(Match match)
        {
            var home = _mappingService.Map("TeamNames", match.HomeTeamName);
            var away = _mappingService.Map("TeamNames", match.AwayTeamName);
            return (home, away);
        }
    }
}
