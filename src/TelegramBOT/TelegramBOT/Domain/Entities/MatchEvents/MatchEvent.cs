using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TelegramBOT.Domain.Entities.Matches;
using TelegramBOT.Domain.Entities.Teams;

namespace TelegramBOT.Domain.Entities.MatchEvents
{
    /// <summary>
    /// Основная таблица событий матча (MatchEvents)
    /// </summary>
    [Table("MatchEvents")]
    public class MatchEvent
    {
        [Key]
        [Column("event_id")]
        public int EventId { get; set; }

        [Required]
        [Column("match_id")]
        [MaxLength(50)]
        public string MatchId { get; set; } = string.Empty;

        [ForeignKey(nameof(MatchId))]
        public Match Match { get; set; } = null!;

        [Column("team_id")]
        public int? TeamId { get; set; }

        [ForeignKey(nameof(TeamId))]
        public Team? Team { get; set; }

        [Required]
        [Column("event_type_id")]
        public int EventTypeId { get; set; }

        [ForeignKey(nameof(EventTypeId))]
        public EventType EventType { get; set; } = null!;

        [Column("period")]
        [MaxLength(20)]
        public string? Period { get; set; }

        [Column("time")]
        [MaxLength(10)]
        public string? Time { get; set; }

        [Column("details")]
        [MaxLength(255)]
        public string? Details { get; set; }

        [Column("player")]
        [MaxLength(255)]
        public string? Player { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Навигационные свойства
        public GoalDetail? GoalDetail { get; set; }
        public GoalieChange? GoalieChange { get; set; }
        public ShootoutDetail? ShootoutDetail { get; set; }
        public Penalty? Penalty { get; set; }
    }
}
