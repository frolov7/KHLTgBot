using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelegramBOT.Models
{
    /// <summary>
    /// Пользователь бота
    /// </summary>
    [Table("Users")]
    public class User
    {
        /// <summary>
        /// Идентификатор пользователя (Telegram userId)
        /// </summary>
        [Key]
        [Column("userId")]
        public long UserId { get; set; }

        /// <summary>
        /// Имя пользователя (обязательное поле)
        /// </summary>
        [Required]
        [MaxLength(64)]
        [Column("firstName")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Фамилия пользователя (необязательное поле)
        /// </summary>
        [MaxLength(64)]
        [Column("secondName")]
        public string? SecondName { get; set; }

        /// <summary>
        /// Username в Telegram (может быть null)
        /// </summary>
        [MaxLength(64)]
        [Column("username")]
        public string? Username { get; set; }

        /// <summary>
        /// Номер телефона пользователя
        /// </summary>
        [MaxLength(12)]
        [Column("phoneNumber")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Дата и время создания записи
        /// </summary>
        [Required]
        [Column("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата и время последнего обновления записи
        /// </summary>
        [Required]
        [Column("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }
}
