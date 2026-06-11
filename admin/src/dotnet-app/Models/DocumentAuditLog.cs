using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityDoc.API.Models
{
    [Table("DocumentAuditLogs")]
    public class DocumentAuditLog
    {
        [Key]
        [Column("log_id")]
        public int LogId { get; set; }

        [Column("company_id")]
        public int CompanyId { get; set; }

        [Column("doc_id")]
        public int DocId { get; set; }

        [Column("version_id")]
        public int VersionId { get; set; }

        [Column("action_type")]
        public string ActionType { get; set; } // Ej: DraftCreated, DraftEdited, SentToReview, Approved, Rejected, Recalled, SignatureRevoked

        [Column("action_details")]
        public string ActionDetails { get; set; }
        
        [Column("version_num")]
        public string VersionNum { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Active";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("created_by")]
        public int CreatedBy { get; set; }

        // --- Propiedades de Navegación (Entity Framework) ---
        [ForeignKey("VersionId")]
        public virtual DocumentVersion DocumentVersion { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User User { get; set; }
    }
}