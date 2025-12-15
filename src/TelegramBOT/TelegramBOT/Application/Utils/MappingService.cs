using TelegramBOT.Domain.Entities.Matches;

namespace TelegramBOT.Application.Utils
{
    /// <summary>
    /// Универсальный сервис для преобразования "сырых" данных 
    /// (команды, статусы и т.д.) в читаемые (локализованные).
    /// Данные берутся из секций appsettings.json.
    /// </summary>
    public class MappingService
    {
        private readonly IConfiguration _config;

        public MappingService(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Получить локализованное значение из указанной секции.
        /// Если соответствия нет — вернётся оригинальное значение.
        /// </summary>
        /// <param name="section">Секция в appsettings.json (например, "TeamNames" или "MatchStatuses")</param>
        /// <param name="rawValue">Оригинальное значение</param>
        public string Map(string section, string rawValue)
        {
            var map = _config.GetSection(section).Get<Dictionary<string, string>>()
                      ?? new Dictionary<string, string>();

            return map.TryGetValue(rawValue, out var pretty) ? pretty : rawValue;
        }

        /// <summary>
        /// Выполняет обратное отображение локализованного значения (например, с эмодзи и русским названием)
        /// в исходное «сырье» из конфигурации (например, английское имя команды).
        /// </summary>
        /// <param name="section">
        /// Название секции в файле <c>appsettings.json</c>, из которой берётся словарь отображений.
        /// </param>
        /// <param name="localizedValue">
        /// Локализованное значение, которое нужно преобразовать обратно в исходное.
        /// </param>
        /// <returns>
        /// Исходное (английское) значение из конфигурации, соответствующее переданному локализованному.
        /// Если соответствие не найдено — возвращает исходный параметр <paramref name="localizedValue"/> без изменений.
        /// </returns>
        public string ReverseMap(string section, string localizedValue)
        {
            var map = _config.GetSection(section).Get<Dictionary<string, string>>()
                      ?? new Dictionary<string, string>();

            // Пытаемся найти точное совпадение
            var exact = map.FirstOrDefault(x => x.Value == localizedValue);
            if (!string.IsNullOrEmpty(exact.Key))
                return exact.Key;

            // Если не нашли — ищем частичное 
            var partial = map.FirstOrDefault(x => x.Value.Contains(localizedValue, StringComparison.OrdinalIgnoreCase));
            return !string.IsNullOrEmpty(partial.Key) ? partial.Key : localizedValue;
        }

        /// <summary>
        /// Возвращает локализованные названия команд для конкретного матча.
        /// </summary>
        /// <param name="match">Объект матча с исходными названиями команд.</param>
        /// <returns>Кортеж (home, away) с локализованными названиями.</returns>
        public (string home, string away) MapTeamNames(Match match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            var home = Map("TeamNames", match.HomeTeamName);
            var away = Map("TeamNames", match.AwayTeamName);
            return (home, away);
        }
    }
}
