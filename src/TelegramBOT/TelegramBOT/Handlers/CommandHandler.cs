using Serilog;
using Telegram.Bot.Types;
using TelegramBOT.Services;
using TelegramBOT.Utils;

namespace TelegramBOT.Handlers
{
    /// <summary>
    /// Обработчик входящих сообщений и команд.
    /// Отвечает за маршрутизацию пользовательских действий к нужным сервисам.
    /// </summary>
    public class CommandHandler
    {
        private readonly MessageService _messageService;
        private readonly MatchService _matchService;
        private readonly MenuService _menuService;
        private readonly ScriptService _scriptService;

        private readonly MappingService _mappingService;

        private bool _isUpdatingResults = false;

        public CommandHandler(
            MessageService messageService,
            MatchService matchService,
            MenuService menuService,
            MappingService mappingService,
            ScriptService scriptService
            )
        {
            _messageService = messageService;
            _matchService = matchService;
            _menuService = menuService;
            _mappingService = mappingService;
            _scriptService = scriptService;
        }

        /// <summary>
        /// Главный метод обработки входящего сообщения.
        /// </summary>
        public async Task HandleAsync(Update update)
        {
            Log.Information("Запущен метод {Method}", nameof(HandleAsync));

            if (update.Message?.Text == null)
                return;

            // Игнорируем сообщения от самого бота
            if (update.Message.From?.IsBot == true)
                return;

            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text;

            Log.Information("Пользователь ({@User}) написал: {Text}",
                new
                {
                    Name = $"{update.Message.From?.FirstName} {update.Message.From?.LastName}".Trim(),
                    Username = update.Message.From?.Username ?? "null",
                    UserId = update.Message.From?.Id
                },
                text
            );

            switch (text)
            {
                // -------------------------------
                // Главное меню
                // -------------------------------
                case "/start":
                    await _messageService.SendKeyboardAsync(
                        chatId,
                        "Добро пожаловать! Выберите действие.",
                        _menuService.GetMainMenu()
                    );
                    break;

                // -------------------------------
                // Календарь
                // -------------------------------
                case "📅 Календарь":
                    await _messageService.SendKeyboardAsync(
                        chatId,
                        "Выберите день",
                        _menuService.GetCalendarMenu()
                    );
                    break;

                case "Сегодня":
                    var todayMatches = await _matchService.GetMatchesTodayAsync();
                    await _messageService.SendCalendarAsync(chatId, todayMatches, DateTime.Today);
                    break;

                case "Завтра":
                    var tomorrowMatches = await _matchService.GetMatchesTomorrowAsync();
                    await _messageService.SendCalendarAsync(chatId, tomorrowMatches, DateTime.Today.AddDays(1));
                    break;

                case "Следующие 5 дней":
                    var nextMatches = await _matchService.GetMatchesNextDaysAsync(5);
                    await _messageService.SendCalendarAsync(chatId, nextMatches, DateTime.Today, DateTime.Today.AddDays(5));
                    break;

                // -------------------------------
                // Статистика
                // -------------------------------
                case "📊 Статистика":
                    await _messageService.SendKeyboardAsync(
                        chatId,
                        " ",
                        _menuService.GetStatsMenu()
                    );
                    break;

                case "Статистика команд":
                    await _messageService.SendTextAsync(chatId, "Здесь будет статистика команд 🏒");
                    break;

                case "Статистика игроков":
                    await _messageService.SendTextAsync(chatId, "Здесь будет статистика игроков 👤");
                    break;

                // -------------------------------
                // Результаты
                // -------------------------------
                case "⚡ Результаты":
                    await _messageService.SendKeyboardAsync(
                        chatId,
                        "Выберите день",
                        _menuService.GetResultsKeyboard()
                    );
                    break;

                case "🔄 Обновить данные":
                    if (_isUpdatingResults)
                    {
                        await _messageService.SendTextAsync(chatId, "⏳ Уже идёт обновление, подождите...");
                        break;
                    }

                    _isUpdatingResults = true;

                    // 1. Убираем клавиатуру, чтобы пользователь не мог жать кнопки
                    await _messageService.RemoveKeyboardAsync(chatId, "⏳ Обновляем результаты, подождите...");

                    try
                    {
                        // 2. Запускаем скрипт и ждём завершения
                        await _scriptService.RunScraperUpdateAsync();

                        // 3. Возвращаем меню после обновления
                        await _messageService.SendKeyboardAsync(
                            chatId,
                            "✅ Результаты обновлены!",
                            _menuService.GetResultsKeyboard()
                        );
                    }
                    catch (Exception ex)
                    {
                        await _messageService.SendTextAsync(chatId, $"❌ Ошибка при обновлении: {ex.Message}");
                    }
                    finally
                    {
                        _isUpdatingResults = false;
                    }
                    break;

                case "📅 Сегодня":
                    var todayResults = await _matchService.GetResultsTodayAsync();
                    await _messageService.SendResultsAsync(chatId, todayResults, DateTime.Today);
                    break;

                case "📅 Вчера":
                    var yesterdayResults = await _matchService.GetResultsYesterdayAsync();
                    await _messageService.SendResultsAsync(chatId, yesterdayResults, DateTime.Today.AddDays(-1));
                    break;

                // --- Меню выбора конференции ---
                case "⬅️ Запад":
                    await _messageService.SendKeyboardAsync(
                        chatId,
                        "Выберите команду (Запад)",
                        _menuService.GetWesternTeamsMenu()
                    );
                    break;

                case "➡️ Восток":
                    await _messageService.SendKeyboardAsync(
                        chatId,
                        "Выберите команду (Восток)",
                        _menuService.GetEasternTeamsMenu()
                    );
                    break;

                // -------------------------------
                // Навигация
                // -------------------------------
                case "🏠 В главное меню":
                    await _messageService.SendKeyboardAsync(
                        chatId,
                        "Возврат в главное меню",
                        _menuService.GetMainMenu()
                    );
                    break;

                // -------------------------------
                // По умолчанию
                // -------------------------------
                default:
                    var allTeams = new Dictionary<string, string>
                    {
                        // Запад
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

                        // Восток
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

                    if (allTeams.ContainsKey(text))
                    {
                        var teamResults = await _matchService.GetAllResultsByTeamAsync(allTeams[text]);
                        //await _messageService.SendResultsAsync(chatId, teamResults, null);
                        await _messageService.SendResultsAsync(chatId, teamResults, null, allTeams[text]);

                    }
                    else
                    {
                        await _messageService.SendTextAsync(chatId, "Я тебя понял 😉");
                    }
                    break;
            }


            Log.Information("Метод {Method} завершил работу.", nameof(HandleAsync));
        }
    }
}
