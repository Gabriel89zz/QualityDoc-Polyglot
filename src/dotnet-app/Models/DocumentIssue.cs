using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QualityDoc.API.Models
{
    [Table("DocumentIssues")]
    public class DocumentIssue : BaseEntity
    {
        [Key]
        [Column("issue_id")]
        public int IssueId { get; set; }

        [Column("company_id")]
        public int CompanyId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("doc_code")]
        public string DocCode { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("issue_type")]
        public string IssueType { get; set; }

        [Required]
        [Column("details")]
        public string Details { get; set; }

        [Column("reported_by")]
        public int ReportedBy { get; set; }

        [MaxLength(30)]
        [Column("issue_status")]
        public string IssueStatus { get; set; } = "Pending";

        // Navegaciones
        [ForeignKey("CompanyId")]
        public virtual Company Company { get; set; }

        [ForeignKey("ReportedBy")]
        public virtual User Reporter { get; set; }
    }
}