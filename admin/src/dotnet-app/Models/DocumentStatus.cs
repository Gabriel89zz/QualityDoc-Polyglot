using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityDoc.API.Models
{
    [Table("DocumentStatus")]
    public class DocumentStatus : BaseEntity
    {
        [Key]
        [Column("status_id")]
        public int StatusId { get; set; }

        [Required]
        [Column("status_name")]
        [MaxLength(30)]
        public string StatusName { get; set; }

        // Propiedades de navegación
        public ICollection<DocumentVersion> DocumentVersions { get; set; }
    }
}