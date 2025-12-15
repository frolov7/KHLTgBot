using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramBOT.Domain.Entities.MatchEvents
{
    /// <summary>
    /// Тип события (гол, штраф, буллит и т.д.)
    /// </summary>
    [Table("EventTypes")]
    public class EventType
    {
        [Key]
        [Column("event_type_id")]
        public int EventTypeId { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public ICollection<MatchEvent>? MatchEvents { get; set; }
    }
}
