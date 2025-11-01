using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramBOT.Domain.Models
{
    /// <summary>
    /// Команда
    /// </summary>
    [Table("Teams")]
    public class Team
    {
        /// <summary>
        /// Уникальный идентификатор команды (PRIMARY KEY, автогенерация)
        /// </summary>
        [Key]
        [Column("team_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TeamId { get; set; }

        /// <summary>
        /// Название команды
        /// </summary>
        [Required]
        [MaxLength(255)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;
    }
}
