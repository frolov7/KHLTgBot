using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramBOT.Domain.Models
{
    /// <summary>
    /// Замена вратаря
    /// </summary>
    [Table("GoalieChanges")]
    public class GoalieChange
    {
        [Key]
        [Column("change_id")]
        public int ChangeId { get; set; }

        [Required]
        [Column("event_id")]
        public int EventId { get; set; }

        [ForeignKey(nameof(EventId))]
        public MatchEvent MatchEvent { get; set; } = null!;

        [Column("goalie_out")]
        [MaxLength(255)]
        public string? GoalieOut { get; set; }

        [Column("goalie_in")]
        [MaxLength(255)]
        public string? GoalieIn { get; set; }
    }
}
