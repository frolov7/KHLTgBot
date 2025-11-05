using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramBOT.Domain.Models
{
    /// <summary>
    /// Штраф (удаление)
    /// </summary>
    [Table("Penalties")]
    public class Penalty
    {
        [Key]
        [Column("penalty_id")]
        public int PenaltyId { get; set; }

        [Required]
        [Column("event_id")]
        public int EventId { get; set; }

        [ForeignKey(nameof(EventId))]
        public MatchEvent MatchEvent { get; set; } = null!;

        [Column("player")]
        [MaxLength(255)]
        public string? Player { get; set; }

        [Column("reason")]
        [MaxLength(255)]
        public string? Reason { get; set; }
        [Column("duration")]
        [MaxLength(10)]
        public string? Duration { get; set; }
    }
}
