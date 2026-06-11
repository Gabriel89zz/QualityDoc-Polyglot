using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityDoc.API.Models
{
    [Table("Departments")]
    public class Department : BaseEntity
    {
        [Key]
        [Column("dept_id")]
        public int DeptId { get; set; }

        [Required]
        [Column("company_id")]
        public int CompanyId { get; set; }

        [Required]
        [Column("dept_name")]
        [MaxLength(100)]
        public string DeptName { get; set; }

        // Propiedades de navegación
        [ForeignKey("CompanyId")]
        public Company Company { get; set; }
        
        public ICollection<User> Users { get; set; }
    }
}