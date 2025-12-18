namespace TelegramBOT.Domain.Entities.MatchEvents
{
    /// <summary>
    /// Количество голов команды в конкретном периоде матча.
    /// Используется для формирования счёта по периодам.
    /// </summary>
    public class PeriodGoals
    {
        public string Period { get; set; } = null!;
        public int TeamId { get; set; }
        public int Goals { get; set; }
    }
}
