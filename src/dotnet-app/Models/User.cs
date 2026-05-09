using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityDoc.API.Models
{
    [Table("Users")]
    public class User : BaseEntity
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        // 🚀 MODIFICACIÓN 1: Ya no es [Required] y ahora es int? (acepta null)
        [Column("company_id")]
        public int? CompanyId { get; set; }

        [Column("dept_id")]
        public int? DeptId { get; set; }

        [Required]
        [Column("role_id")]
        public int RoleId { get; set; }

        [Required]
        [Column("full_name")]
        [MaxLength(200)]
        public string FullName { get; set; }

        [Required]
        [Column("email")]
        [MaxLength(150)]
        public string Email { get; set; }

        [Required]
        [Column("password_hash")]
        public string PasswordHash { get; set; }

        // ==========================================
        // PROPIEDADES DE NAVEGACIÓN
        // ==========================================
        
        // 🚀 MODIFICACIÓN 2: La relación con la Empresa ahora es opcional (Company?)
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("DeptId")]
        public virtual Department? Department { get; set; }

        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; }

        public virtual ICollection<DocumentApproval> Approvals { get; set; }
    }
}