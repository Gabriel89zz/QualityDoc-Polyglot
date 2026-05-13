using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using QualityDoc.API.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace QualityDoc.API.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly QualityDocDbContext _context;

        public HomeController(QualityDocDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Extraemos datos de la sesión actual
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // 🚀 1. LA TRAMPA: Si tiene sesión viva y es Operario o Lector, lo expulsamos hacia el controlador Auth
            if (role != null && (role.Trim().Equals("Operario", StringComparison.OrdinalIgnoreCase) || 
                                 role.Trim().Equals("Lector", StringComparison.OrdinalIgnoreCase)))
            {
                return RedirectToAction("GoToPhpPortal", "Auth");
            }

            var companyIdClaim = User.FindFirst("CompanyId")?.Value;
            int currentCompanyId = string.IsNullOrEmpty(companyIdClaim) ? 0 : int.Parse(companyIdClaim);
            
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int currentUserId = string.IsNullOrEmpty(userIdClaim) ? 0 : int.Parse(userIdClaim);

            // 🧠 LÓGICA DE KPIS POR PERFIL
            if (role == "Super Admin")
            {
                ViewBag.TotalEmpresas = await _context.Companies.CountAsync(c => c.Status == "Active");
                ViewBag.TotalUsuariosGlobales = await _context.Users.CountAsync(u => u.Status == "Active");
                ViewBag.TotalNormas = await _context.Norms.CountAsync(n => n.Status == "Active");
            }
            else if (role == "Admin de Empresa")
            {
                ViewBag.DocsAprobados = await _context.DocumentVersions.CountAsync(v => v.Document.CompanyId == currentCompanyId && v.StatusId == 3);
                ViewBag.FlujosActivos = await _context.DocumentApprovals.CountAsync(a => a.DocumentVersion.Document.CompanyId == currentCompanyId && a.ApprovalStatus == "Pending" && a.DocumentVersion.StatusId == 2);
                ViewBag.Borradores = await _context.DocumentVersions.CountAsync(v => v.Document.CompanyId == currentCompanyId && v.StatusId == 1);
                ViewBag.FirmasRecientes = await _context.DocumentApprovals
                    .Include(a => a.DocumentVersion)
                        .ThenInclude(v => v.Document)
                    .Include(a => a.Approver)
                    .Where(a => a.DocumentVersion.Document.CompanyId == currentCompanyId && a.ApprovalStatus == "Pending" && a.DocumentVersion.StatusId == 2)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(5) 
                    .ToListAsync();
            }
            else if (role == "Creador de Doc" || role == "Revisor" || role == "Aprobador")
            {
                // 1. Tareas de firma pendientes
                ViewBag.MisFirmasPendientes = await _context.DocumentApprovals
                    .CountAsync(a => a.ApproverId == currentUserId && a.ApprovalStatus == "Pending");

                // 2. 🚀 NUEVO: Alerta de Rechazos/Observaciones
                // Buscamos versiones creadas por este usuario que estén en Borrador (StatusId = 1) 
                // PERO que ya tengan un historial en la tabla de Aprobaciones (lo que significa que fue devuelto/rechazado)
                ViewBag.MisDocsRechazados = await _context.DocumentVersions
                    .Where(v => v.CreatedBy == currentUserId 
                             && v.StatusId == 1 
                             && v.Approvals.Any())
                    .CountAsync();
            }

            return View();
        }
    }
}