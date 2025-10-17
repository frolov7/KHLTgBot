using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramBOT.Models
{
    /// <summary>
    /// Видеообзор матча КХЛ (YouTube)
    /// </summary>
    [Table("MatchVideos")]
    public class MatchVideo
    {
        /// <summary>
        /// Уникальный идентификатор видео (PRIMARY KEY)
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("video_id")]
        public int VideoId { get; set; }

        /// <summary>
        /// Внешний ключ на матч
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("match_id")]
        public string MatchId { get; set; } = string.Empty;

        /// <summary>
        /// Название видео (заголовок на YouTube)
        /// </summary>
        [Required]
        [MaxLength(255)]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Прямая ссылка на YouTube-видео
        /// </summary>
        [Required]
        [MaxLength(500)]
        [Column("url")]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Связанный матч
        /// </summary>
        [ForeignKey("MatchId")]
        public Match? Match { get; set; }
    }
}