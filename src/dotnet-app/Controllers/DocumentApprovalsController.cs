using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using QualityDoc.API.Data;
using System.Linq;
using System.Threading.Tasks;

namespace QualityDoc.API.Controllers
{
    // 🔒 CANDADO: Solo los gerentes y super admins pueden ver el radar global
    [Authorize(Roles = "Super Admin, Admin de Empresa")]
    public class DocumentApprovalsController : Controller
    {
        private readonly QualityDocDbContext _context;

        public DocumentApprovalsController(QualityDocDbContext context)
        {
            _context = context;
        }

        private int GetCurrentCompanyId()
        {
            var claim = User.FindFirst("CompanyId")?.Value;
            return string.IsNullOrEmpty(claim) ? 0 : int.Parse(claim);
        }

        // ==========================================
        // 1. INDEX: Flujos Activos (Torre de Control)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var companyId = GetCurrentCompanyId();

            // TABLA 1: Flujos pendientes de firma (En Revisión - Status 2)
            var activeFlows = await _context.DocumentApprovals
                .Include(a => a.DocumentVersion)
                    .ThenInclude(v => v.Document)
                        .ThenInclude(d => d.Department)
                .Include(a => a.Approver) 
                .Where(a => a.DocumentVersion.Document.CompanyId == companyId 
                         && a.ApprovalStatus == "Pending"
                         && a.DocumentVersion.StatusId == 2) 
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            // TABLA 2: Documentos en Borrador (Status 1)
            var drafts = await _context.DocumentVersions
                .Include(v => v.Document)
                    .ThenInclude(d => d.Department)
                .Where(v => v.Document.CompanyId == companyId && v.StatusId == 1) 
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            // 🚀 CORRECCIÓN: Filtramos los valores nulos con .HasValue y extraemos el valor con .Value
            var creatorIds = drafts.Where(d => d.CreatedBy.HasValue).Select(d => d.CreatedBy.Value).Distinct().ToList();
            var creators = await _context.Users
                .Where(u => creatorIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.FullName);

            ViewBag.Drafts = drafts;
            ViewBag.Creators = creators;

            return View(activeFlows);
        }
    }
}