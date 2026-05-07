using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityDoc.API.Models
{
    [Table("Companies")]
    public class Company : BaseEntity
    {
        [Key]
        [Column("company_id")]
        public int CompanyId { get; set; }

        [Required]
        [Column("legal_name")]
        [MaxLength(200)]
        public string LegalName { get; set; }

        [Required]
        [Column("tax_id")]
        [MaxLength(20)]
        public string TaxId { get; set; }

        // Propiedades de navegación (Datos y personal de la empresa)
        public ICollection<Department> Departments { get; set; }
        public ICollection<User> Users { get; set; }
        public ICollection<DocumentCategory> Categories { get; set; }
        public ICollection<Document> Documents { get; set; }

    }
}