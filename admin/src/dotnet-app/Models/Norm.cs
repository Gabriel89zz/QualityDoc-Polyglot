using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityDoc.API.Models
{
    [Table("Norms")]
    public class Norm : BaseEntity
    {
        [Key]
        [Column("norm_id")]
        public int NormId { get; set; }

        [Required]
        [Column("norm_name")]
        [MaxLength(50)]
        public string NormName { get; set; }

        // 🚀 NUEVO: Año de la versión de la norma (Ej. "2015", "2016")
        [Column("release_year")]
        [MaxLength(4)]
        public string? ReleaseYear { get; set; }

        // Propiedad de navegación
        public ICollection<DocumentCategory> Categories { get; set; } = new List<DocumentCategory>();
    }
}