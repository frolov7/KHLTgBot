namespace TelegramBOT.Services.Utils
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
    }
}
