using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBOT.Presentation.UI.Menus.Teams
{
    /// <summary>
    /// Создаёт inline-меню для выбора конференции и команд в разделе "Команды".
    /// </summary>
    public static class TeamsMenuBuilder
    {
        // ==========================================================
        // ============      МЕНЮ ВЫБОРА КОНФЕРЕНЦИИ     ============
        // ==========================================================

        /// <summary>
        /// Меню выбора конференции.
        /// </summary>
        public static InlineKeyboardMarkup BuildConferenceMenu()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("⬅️ Западная", "teams_conf_west"),
                    InlineKeyboardButton.WithCallbackData("➡️ Восточная", "teams_conf_east")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🏠 В главное меню", "back_to_main")
                }
            });
        }


        // ==========================================================
        // ============         ЗАПАДНАЯ КОНФЕРЕНЦИЯ      ============
        // ==========================================================

        private static readonly (string Name, string Code)[] WesternTeams =
        {
            ("🦌 Торпедо", "Nizhny Novgorod"),
            ("🐉 Шанхай Дрэгонс", "Shanghai"),
            ("🐃 Динамо Минск", "Dinamo Minsk"),
            ("⚒️ Северсталь", "Cherepovets"),
            ("★ ЦСКА", "CSKA Moscow"),
            ("🐆 ХК Сочи", "Sochi"),
            ("🚂 Локомотив", "Lokomotiv Yaroslavl"),
            ("⭐ СКА", "SKA St. Petersburg"),
            ("🔵 Динамо Москва", "Dynamo Moscow"),
            ("🚗 Лада", "Lada"),
            ("♦️ Спартак", "Spartak Moscow")
        };

        // ==========================================================
        // ============         ВОСТОЧНАЯ КОНФЕРЕНЦИЯ     ============
        // ==========================================================

        private static readonly (string Name, string Code)[] EasternTeams =
        {
            ("🚘 Автомобилист", "Yekaterinburg"),
            ("🦅 Авангард", "Avangard Omsk"),
            ("🚜 Трактор", "Tractor Chelyabinsk"),
            ("🐆 Барыс", "Barys Astana"),
            ("⛏️ Металлург", "Magnitogorsk"),
            ("🐅 Амур", "Khabarovsk"),
            ("🐯 Ак Барс", "Bars Kazan"),
            ("⚓ Адмирал", "Vladivostok"),
            ("🐺 Нефтехимик", "Niznekamsk"),
            ("🕌 Салават Юлаев", "Salavat Ufa"),
            ("❄️ Сибирь", "Novosibirsk")
        };

        // ==========================================================
        // ============      МЕНЮ КОМАНД ПО КОНФЕРЕНЦИИ   ============
        // ==========================================================

        /// <summary>
        /// Формирует inline-меню команд выбранной конференции.
        /// </summary>
        public static InlineKeyboardMarkup BuildTeamsMenu(string conference)
        {
            var list = conference == "west"
                ? WesternTeams
                : EasternTeams;

            var rows = list
                .Chunk(2)
                .Select(chunk =>
                    chunk.Select(t =>
                        InlineKeyboardButton.WithCallbackData(t.Name, $"team_{t.Code}")
                    ).ToArray()
                ).ToList();

            rows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Назад", "teams_back_to_conf"),
                InlineKeyboardButton.WithCallbackData("🏠 В главное меню", "back_to_main")
            });

            return new InlineKeyboardMarkup(rows);
        }
    }
}
