using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityDoc.API.Models
{
    [Table("Documents")]
    public class Document : BaseEntity
    {
        [Key]
        [Column("doc_id")]
        public int DocId { get; set; }

        [Required]
        [Column("company_id")]
        public int CompanyId { get; set; }

        [Required]
        [Column("category_id")]
        public int CategoryId { get; set; }

        // 🚀 NUEVA PROPIEDAD: Relación con el Departamento
        [Required]
        [Column("dept_id")]
        public int DeptId { get; set; }

        [Required]
        [Column("doc_code")]
        [MaxLength(50)]
        public string DocCode { get; set; } = null!;

        [Required]
        [Column("doc_name")]
        [MaxLength(255)]
        public string DocName { get; set; } = null!;

        // 🚀 NUEVA PROPIEDAD: Descripción opcional
        [Column("description")]
        public string? Description { get; set; }

        [Column("is_external")]
        public bool IsExternal { get; set; } = false;

        // ==========================================
        // PROPIEDADES DE NAVEGACIÓN (Relaciones)
        // ==========================================
        
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("CategoryId")]
        public virtual DocumentCategory? Category { get; set; }

        // 🚀 NUEVA NAVEGACIÓN: Para poder acceder a model.Department.DeptName
        [ForeignKey("DeptId")]
        public virtual Department? Department { get; set; }

        // Inicializamos la colección de versiones para evitar NullReferenceException
        public virtual ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();

    }
}