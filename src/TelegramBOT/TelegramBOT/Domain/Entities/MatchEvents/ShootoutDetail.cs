using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramBOT.Domain.Entities.MatchEvents
{
    /// <summary>
    /// Детали буллитов
    /// </summary>
    [Table("ShootoutDetails")]
    public class ShootoutDetail
    {
        [Key]
        [Column("shootout_id")]
        public int ShootoutId { get; set; }

        [Required]
        [Column("event_id")]
        public int EventId { get; set; }

        [ForeignKey(nameof(EventId))]
        public MatchEvent MatchEvent { get; set; } = null!;

        [Required]
        [Column("result")]
        [MaxLength(50)]
        public string Result { get; set; } = string.Empty; // "Scored" / "Missed"

        [Required]
        [Column("shooter")]
        [MaxLength(255)]
        public string Shooter { get; set; } = string.Empty;
    }
}
