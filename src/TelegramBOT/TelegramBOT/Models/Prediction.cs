using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramBOT.Models
{
    /// <summary>
    /// Прогноз по матчу
    /// </summary>
    [Table("Predictions")]
    public class Prediction
    {
        /// <summary>
        /// Идентификатор прогноза
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("prediction_id")]
        public int PredictionId { get; set; }

        /// <summary>
        /// Внешний ключ на матч
        /// </summary>
        [Required]
        [Column("match_id")]
        [MaxLength(50)]
        public string MatchId { get; set; } = string.Empty;

        /// <summary>
        /// Источник прогноза
        /// </summary>
        [Required]
        [MaxLength(255)]
        [Column("source")]
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Ссылка на страницу прогноза
        /// </summary>
        [MaxLength(500)]
        [Column("url")]
        public string? Url { get; set; }

        /// <summary>
        /// Основной прогноз
        /// </summary>
        [Column("main_prediction")]
        public string? MainPrediction { get; set; }

        /// <summary>
        /// Альтернативный прогноз
        /// </summary>
        [Column("alt_prediction")]
        public string? AltPrediction { get; set; }

        /// <summary>
        /// Примерный счёт
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
        /// Результат прогноза
        /// </summary>
        [MaxLength(255)]
        [Column("result")]
        public string? Result { get; set; }

        /// <summary>
        /// Анализ домашней команды
        /// </summary>
        [Column("home_team_text")]
        public string? HomeTeamText { get; set; }

        /// <summary>
        /// Анализ гостевой команды
        /// </summary>
        [Column("away_team_text")]
        public string? AwayTeamText { get; set; }

        /// <summary>
        /// Связанный матч
        /// </summary>
        [ForeignKey("MatchId")]
        public Match? Match { get; set; }
    }
}
