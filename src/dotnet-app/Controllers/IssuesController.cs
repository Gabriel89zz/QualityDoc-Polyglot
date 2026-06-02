using Microsoft.AspNetCore.Mvc;
using QualityDoc.API.Data;
using QualityDoc.API.Models;
using Microsoft.AspNetCore.Authorization; // 🚀 Asegúrate de tener este using
using System;
using System.Threading.Tasks;

namespace QualityDoc.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous] // 🚀 FIX 1: Permite que Laravel (PHP) le envíe paquetes internamente sin cookies de sesión
    public class IssuesController : ControllerBase
    {
        private readonly QualityDocDbContext _context;

        public IssuesController(QualityDocDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateIssue([FromBody] CreateIssueDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var newIssue = new DocumentIssue
                {
                    CompanyId = request.CompanyId,
                    DocCode = request.DocCode,
                    IssueType = request.IssueType,
                    Details = request.Details,
                    ReportedBy = request.UserId,
                    IssueStatus = "Pending",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = request.UserId
                };

                _context.DocumentIssues.Add(newIssue);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Reporte guardado en SQL Server exitosamente." });
            }
            catch (Exception ex)
            {
                // 🚀 Mejoramos el mensaje de error para capturar detalles profundos de SQL
                string realError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { success = false, message = "Error de BD: " + realError });
            }
        }
    }

    public class CreateIssueDto
    {
        public int CompanyId { get; set; }
        public int UserId { get; set; }
        public string DocCode { get; set; }
        public string IssueType { get; set; }
        public string Details { get; set; }
    }
}