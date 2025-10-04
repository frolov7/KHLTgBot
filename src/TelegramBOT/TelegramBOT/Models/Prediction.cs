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
        /// Идентификатор прогноза (PRIMARY KEY, автоинкремент)
        /// </summary>
        [Key]
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
        /// Источник прогноза (Legalbet, Metaratings, Vseprosport и т.д.)
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
        /// Результат прогноза (например, «Выиграл», «Не зашёл»)
        /// </summary>
        [MaxLength(255)]
        [Column("result")]
        public string? Result { get; set; }

        /// <summary>
        /// Текст анализа для домашней команды
        /// </summary>
        [Column("home_team_text")]
        public string? HomeTeamText { get; set; }

        /// <summary>
        /// Текст анализа для гостевой команды
        /// </summary>
        [Column("away_team_text")]
        public string? AwayTeamText { get; set; }

        // ================================
        // Навигационное свойство (связь с матчем)
        // ================================
        [ForeignKey("MatchId")]
        public Match? Match { get; set; }
    }
}
