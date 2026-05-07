using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http; // 🚀 NUEVO: Necesario para leer el archivo subido (IFormFile)

namespace QualityDoc.API.Models
{
    [Table("DocumentVersions")]
    public class DocumentVersion : BaseEntity
    {
        [Key]
        [Column("version_id")]
        public int VersionId { get; set; }

        [Required]
        [Column("doc_id")]
        public int DocId { get; set; }

        [Required]
        [Column("status_id")]
        public int StatusId { get; set; }

        [Required]
        [Column("version_num")]
        [MaxLength(10)]
        public string VersionNum { get; set; } = null!;

        [Required]
        [Column("file_path")]
        public string FilePath { get; set; } = null!;

        [Required]
        [Column("extension")]
        [MaxLength(10)]
        public string Extension { get; set; } = null!;

        [Column("change_description")]
        public string? ChangeDescription { get; set; }

        // 🚀 CAMPOS: Trazabilidad de tiempos exactos para ISO 9001
        [Column("approved_at")]
        public DateTime? ApprovedAt { get; set; }

        [Column("obsoleted_at")]
        public DateTime? ObsoletedAt { get; set; }

        // ==========================================
        // 🚀 PROPIEDAD VIRTUAL PARA SUBIR EL ARCHIVO
        // ==========================================
        // [NotMapped] evita que EF Core busque esto en la base de datos
        // Le ponemos "?" porque al leer de la BD, no habrá archivo cargado en memoria.
        [NotMapped]
        public IFormFile? UploadedFile { get; set; }

        // ==========================================
        // PROPIEDADES DE NAVEGACIÓN (Relaciones)
        // ==========================================
        
        [ForeignKey("DocId")]
        public virtual Document? Document { get; set; }

        [ForeignKey("StatusId")]
        public virtual DocumentStatus? DocumentStatus { get; set; }

        // Inicializamos la lista para evitar NullReferenceException
        public virtual ICollection<DocumentApproval> Approvals { get; set; } = new List<DocumentApproval>();
    }
}