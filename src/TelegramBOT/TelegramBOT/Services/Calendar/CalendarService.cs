using TelegramBOT.Services.Core;
using TelegramBOT.Models;
using TelegramBOT.Data.Repositories;
using TelegramBOT.UI;
using TelegramBOT.Services.Utils;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBOT.Services.Calendar
{
    /// <summary>
    /// Сервис календаря матчей.
    /// Отвечает за бизнес-логику, связанную с отображением расписания матчей
    /// и взаимодействием с пользователем через Telegram.
    /// </summary>
    public class CalendarService
    {
        private readonly ICalendarRepository _calendarRepository;
        private readonly MessageService _messageService;
        private readonly MappingService _mappingService;

        public CalendarService(
            ICalendarRepository calendarRepository,
            MessageService messageService,
            MappingService mappingService)
        {
            _calendarRepository = calendarRepository;
            _messageService = messageService;
            _mappingService = mappingService;
        }

        // ==========================================================
        // ============             УТИЛИТЫ              ============
        // ==========================================================

        /// <summary>
        /// Выполняет сопоставление названий команд в соответствии с настройками отображения.
        /// </summary>
        /// <param name="match">Объект матча с исходными данными.</param>
        /// <returns>Кортеж с локализованными названиями команд: (home, away).</returns>
        public (string home, string away) MapTeamNames(Match match)
        {
            return (
                _mappingService.Map("TeamNames", match.HomeTeamName),
                _mappingService.Map("TeamNames", match.AwayTeamName)
            );
        }

        /// <summary>
        /// Пытается преобразовать callback-строку в дату (используется при навигации по календарю).
        /// </summary>
        /// <param name="callback">Строка callback (например, "20250120").</param>
        /// <param name="date">Распознанная дата (если успешно).</param>
        /// <returns><see langword="true"/>, если дата успешно распознана; иначе <see langword="false"/>.</returns>
        public bool TryParseCallbackDate(string callback, out DateTime date)
        {
            return DateTime.TryParseExact(callback, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out date);
        }

        // ==========================================================
        // ============        ФОРМИРОВАНИЕ СООБЩЕНИЙ     ============
        // ==========================================================

        /// <summary>
        /// Загружает список матчей за указанный диапазон дат и отправляет календарь пользователю.
        /// </summary>
        /// <param name="chatId">ID Telegram-чата.</param>
        /// <param name="from">Начальная дата диапазона.</param>
        /// <param name="to">Конечная дата диапазона.</param>
        public async Task SendMatchesAsync(long chatId, DateTime from, DateTime to)
        {
            var matches = await _calendarRepository.GetMatchesByDateRangeAsync(from, to);

            if (matches.Count == 0)
            {
                await _messageService.SendTextAsync(chatId, "❌ Матчи не найдены.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine(from == to
                ? $"📅 Матчи на {from:dd.MM.yyyy}\n"
                : $"📅 Матчи с {from:dd.MM.yyyy} по {to:dd.MM.yyyy}\n");

            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var match in matches)
            {
                var home = _mappingService.Map("TeamNames", match.HomeTeamName);
                var away = _mappingService.Map("TeamNames", match.AwayTeamName);
                var status = _mappingService.Map("MatchStatuses", match.Status);

                sb.AppendLine($"⏰ {match.MatchDate:HH:mm} — {home} vs {away}");
                sb.AppendLine(status);
                sb.AppendLine();

                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData($"{home} vs {away}", $"match_{match.MatchId}")
                });
            }

            var keyboard = new InlineKeyboardMarkup(buttons);
            await _messageService.SendTextWithKeyboardAsync(chatId, sb.ToString(), keyboard);
        }

        /// <summary>
        /// Загружает данные конкретного матча и отправляет пользователю меню действий.
        /// </summary>
        /// <param name="chatId">ID Telegram-чата.</param>
        /// <param name="matchId">Уникальный идентификатор матча.</param>
        /// <param name="menuService">Сервис для построения контекстного меню.</param>
        public async Task SendMatchMenuAsync(long chatId, string matchId, MenuService menuService)
        {
            var match = await _calendarRepository.GetMatchAsync(matchId);
            if (match == null)
            {
                await _messageService.SendTextAsync(chatId, "❌ Матч не найден.");
                return;
            }

            var (home, away) = MapTeamNames(match);
            var text = $"⚔️ <b>{home}</b> vs <b>{away}</b>";

            await _messageService.SendKeyboardAsync(chatId, text, menuService.GetMatchMenu(match));
        }
    }
}
