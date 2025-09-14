using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramBOT.Models
{
    /// <summary>
    /// Матч
    /// </summary>
    [Table("Matches")]
    public class Match
    {
        /// <summary>
        /// Идентификатор матча (PRIMARY KEY)
        /// </summary>
        [Key]
        [Column("match_id")]
        [MaxLength(50)]
        public string MatchId { get; set; } = string.Empty;

        /// <summary>
        /// Дата и время проведения матча
        /// </summary>
        [Required]
        [Column("match_date")]
        public DateTime MatchDate { get; set; }

        /// <summary>
        /// Статус матча (SCHEDULED, LIVE, FINISHED)
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Название домашней команды
        /// </summary>
        [Required]
        [MaxLength(255)]
        [Column("home_team_name")]
        public string HomeTeamName { get; set; } = string.Empty;

        /// <summary>
        /// Идентификатор домашней команды (связь с таблицей Teams)
        /// </summary>
        [Column("home_team_id")]
        public int HomeTeamId { get; set; }

        /// <summary>
        /// Название гостевой команды
        /// </summary>
        [Required]
        [MaxLength(255)]
        [Column("away_team_name")]
        public string AwayTeamName { get; set; } = string.Empty;

        /// <summary>
        /// Идентификатор гостевой команды (связь с таблицей Teams)
        /// </summary>
        [Column("away_team_id")]
        public int AwayTeamId { get; set; }

        /// <summary>
        /// Счёт домашней команды (может быть null, если матч не сыгран)
        /// </summary>
        [Column("home_score")]
        public int? HomeScore { get; set; }

        /// <summary>
        /// Счёт гостевой команды (может быть null, если матч не сыгран)
        /// </summary>
        [Column("away_score")]
        public int? AwayScore { get; set; }
    }
}
