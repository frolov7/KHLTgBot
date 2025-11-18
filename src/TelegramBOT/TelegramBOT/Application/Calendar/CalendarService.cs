using System.Text;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBOT.Domain.Interfaces;
using TelegramBOT.Application.Utils;
using TelegramBOT.Infrastructure.Telegram;
using TelegramBOT.Presentation.UI;
using Serilog;

namespace TelegramBOT.Application.Calendar
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
            Log.Information("[SendMatchesAsync] Начало работы метода. chatId={ChatId}, from={From}, to={To}", chatId, from, to);

            var matches = await _calendarRepository.GetMatchesByDateRangeAsync(from, to);

            if (matches.Count == 0)
            {
                Log.Information("[SendMatchesAsync] Матчи не найдены. chatId={ChatId}, from={From}, to={To}", chatId, from, to);
                await _messageService.SendTextAsync(chatId, "Матчи не найдены.");
                return;
            }

            Log.Information("[SendMatchesAsync] Получено {Count} матчей. chatId={ChatId}", matches.Count, chatId);

            var sb = new StringBuilder();
            sb.AppendLine(from == to
                ? $"📅 Матчи на {from:dd.MM.yyyy}\n"
                : $"📅 Матчи с {from:dd.MM.yyyy} по {to:dd.MM.yyyy}\n");

            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var match in matches)
            {
                var (home, away) = _mappingService.MapTeamNames(match);
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

            Log.Information("[SendMatchesAsync] Отправка списка матчей. chatId={ChatId}", chatId);
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
            Log.Information("[SendMatchMenuAsync] Начало работы метода. chatId={ChatId}, matchId={MatchId}", chatId, matchId);

            var match = await _calendarRepository.GetMatchAsync(matchId);
            if (match == null)
            {
                Log.Information("[SendMatchMenuAsync] Матч не найден. chatId={ChatId}, matchId={MatchId}", chatId, matchId);
                await _messageService.SendTextAsync(chatId, "Матч не найден.");
                return;
            }

            var (home, away) = _mappingService.MapTeamNames(match);

            var text = $"<b>{home}</b> vs <b>{away}</b>";
            await _messageService.SendKeyboardAsync(chatId, text, menuService.GetMatchMenu(match));
        }
    }
}
