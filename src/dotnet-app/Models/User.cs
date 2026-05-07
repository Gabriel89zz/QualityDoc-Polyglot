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

        [Required]
        [Column("company_id")]
        public int CompanyId { get; set; }

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

        // Propiedades de navegación
        [ForeignKey("CompanyId")]
        public Company Company { get; set; }

        [ForeignKey("DeptId")]
        public Department Department { get; set; }

        [ForeignKey("RoleId")]
        public Role Role { get; set; }

        public ICollection<DocumentApproval> Approvals { get; set; }
    }
}