using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramBOT.Models
{
    /// <summary>
    /// Прогноз на матч
    /// </summary>
    [Table("Predictions")]
    public class Prediction
    {
        /// <summary>
        /// Уникальный идентификатор прогноза (PRIMARY KEY, автогенерация)
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("prediction_id")]
        public int PredictionId { get; set; }

        /// <summary>
        /// ID матча (связь с Matches)
        /// </summary>
        [Required]
        [Column("match_id")]
        public string MatchId { get; set; } = string.Empty;

        /// <summary>
        /// Источник прогноза (например: legalbet.kz, stavka.tv)
        /// </summary>
        [Required]
        [MaxLength(255)]
        [Column("source")]
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Основной прогноз
        /// </summary>
        [Column("main_prediction")]
        public string? MainPrediction { get; set; }

        /// <summary>
        /// Альтернативный прогноз (если есть)
        /// </summary>
        [Column("alt_prediction")]
        public string? AltPrediction { get; set; }

        /// <summary>
        /// Прогнозируемый счет
        /// </summary>
        [MaxLength(50)]
        [Column("score")]
        public string? Score { get; set; }

        /// <summary>
        /// Общий текст прогноза
        /// </summary>
        [Column("general_text")]
        public string? GeneralText { get; set; }

        /// <summary>
        /// Результат прогноза (WIN, LOSE, DRAW, UNKNOWN)
        /// </summary>
        [MaxLength(255)]
        [Column("result")]
        public string? Result { get; set; }

        /// <summary>
        /// Текст прогноза, связанный с домашней командой
        /// </summary>
        [Column("home_team_text")]
        public string? HomeTeamText { get; set; }

        /// <summary>
        /// Текст прогноза, связанный с гостевой командой
        /// </summary>
        [Column("away_team_text")]
        public string? AwayTeamText { get; set; }
    }
}
