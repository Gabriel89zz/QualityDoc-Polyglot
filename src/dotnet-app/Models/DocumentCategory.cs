using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityDoc.API.Models
{
    [Table("DocumentCategories")]
    public class DocumentCategory : BaseEntity
    {
        [Key]
        [Column("category_id")]
        public int CategoryId { get; set; }

        [Required]
        [Column("company_id")]
        public int CompanyId { get; set; }

        // Lo puse opcional (?) por si una categoría aplica a varias normas
        [Column("norm_id")]
        public int? NormId { get; set; }

        [Required(ErrorMessage = "El nombre de la categoría es obligatorio")]
        [Column("category_name")]
        [MaxLength(100)]
        public string CategoryName { get; set; }

        [Required(ErrorMessage = "El prefijo es vital para la codificación ISO")]
        [Column("prefix")]
        [MaxLength(5)]
        public string Prefix { get; set; } // Ej: "MAN", "PRO", "FOR"

        [Column("description")]
        [MaxLength(255)]
        public string? Description { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "El nivel debe estar entre 1 y 10")]
        [Column("hierarchy_level")]
        public int HierarchyLevel { get; set; }

        // Propiedades de navegación
        [ForeignKey("CompanyId")]
        public Company? Company { get; set; }

        [ForeignKey("NormId")]
        public Norm? Norm { get; set; }

        public ICollection<Document>? Documents { get; set; }
    }
}