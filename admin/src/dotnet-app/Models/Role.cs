using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityDoc.API.Models
{
    [Table("Roles")]
    public class Role : BaseEntity
    {
        [Key]
        [Column("role_id")]
        public int RoleId { get; set; }

        [Required]
        [Column("role_name")]
        [MaxLength(50)]
        public string RoleName { get; set; }

        // Propiedad de navegación
        public ICollection<User> Users { get; set; }
    }
}