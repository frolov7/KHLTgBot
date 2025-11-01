using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramBOT.Domain.Models
{
    /// <summary>
    /// Детали гола
    /// </summary>
    [Table("GoalDetails")]
    public class GoalDetail
    {
        [Key]
        [Column("goal_id")]
        public int GoalId { get; set; }

        [Required]
        [Column("event_id")]
        public int EventId { get; set; }

        [ForeignKey(nameof(EventId))]
        public MatchEvent MatchEvent { get; set; } = null!;

        [Column("scorer")]
        [MaxLength(255)]
        public string? Scorer { get; set; }

        [Column("assistants")]
        [MaxLength(500)]
        public string? Assistants { get; set; }

        [Column("goal_type")]
        [MaxLength(50)]
        public string? GoalType { get; set; }

        [Column("score")]
        [MaxLength(10)]
        public string? Score { get; set; }
    }
}
