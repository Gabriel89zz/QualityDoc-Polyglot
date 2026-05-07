using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityDoc.API.Models
{
    public abstract class BaseEntity
    {
        [Column("status")]
        public string Status { get; set; } = "Active";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("updated_by")]
        public int? UpdatedBy { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        [Column("deleted_by")]
        public int? DeletedBy { get; set; }


        // ==========================================
        // 🛡️ RELACIONES DE AUDITORÍA GLOBALES
        // ==========================================
        
        [ForeignKey("CreatedBy")]
        public virtual User CreatedByNavigation { get; set; }

        [ForeignKey("UpdatedBy")]
        public virtual User UpdatedByNavigation { get; set; }

        [ForeignKey("DeletedBy")]
        public virtual User DeletedByNavigation { get; set; }
    }
}