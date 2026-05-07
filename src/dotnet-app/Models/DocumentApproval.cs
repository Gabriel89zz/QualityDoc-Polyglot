using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityDoc.API.Models
{
    [Table("DocumentApprovals")]
    public class DocumentApproval : BaseEntity
    {
        [Key]
        [Column("approval_id")]
        public int ApprovalId { get; set; }

        [Required]
        [Column("version_id")]
        public int VersionId { get; set; }

        // 🚀 NUEVOS CAMPOS DEL FLUJO UNIVERSAL
        [Required]
        [Column("step_order")]
        public int StepOrder { get; set; } = 1;

        [Required]
        [Column("step_type")]
        [MaxLength(30)]
        public string StepType { get; set; } = "Revisó";

        [Required]
        [Column("approver_id")]
        public int ApproverId { get; set; }

        [Required]
        [Column("approval_status")]
        [MaxLength(20)]
        public string ApprovalStatus { get; set; } = "Pending";

        [Column("comments")]
        public string? Comments { get; set; }

        [Column("signature_token")]
        public string? SignatureToken { get; set; }

        [Column("signed_at")]
        public DateTime? SignedAt { get; set; }

        // ==========================================
        // PROPIEDADES DE NAVEGACIÓN (Relaciones)
        // ==========================================
        
        [ForeignKey("VersionId")]
        public virtual DocumentVersion? DocumentVersion { get; set; }

        [ForeignKey("ApproverId")]
        public virtual User? Approver { get; set; }
    }
}