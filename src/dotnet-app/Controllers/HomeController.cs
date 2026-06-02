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

                ViewBag.TotalEmpresas = await _context.Companies.CountAsync(c => c.Status == "Active");
                ViewBag.TotalUsuariosGlobales = await _context.Users.CountAsync(u => u.Status == "Active");
                ViewBag.TotalNormas = await _context.Norms.CountAsync(n => n.Status == "Active");

                // 📊 CONSULTA REAL: Traer el Top 3 de empresas activas con su respectiva cantidad de usuarios activos
                var empresasConUsuarios = await _context.Companies
                    .Where(c => c.Status == "Active")
                    .Select(c => new {
                        Nombre = c.LegalName,
                        Cantidad = _context.Users.Count(u => u.CompanyId == c.CompanyId && u.Status == "Active")
                    })
                    .OrderByDescending(x => x.Cantidad)
                    .Take(3) // Tomamos las 3 principales para mantener limpia la leyenda de la gráfica
                    .ToListAsync();

                // Lo convertimos a un Diccionario para que sea ultra fácil de leer en la vista con Razor
                ViewBag.DatosGrafica = empresasConUsuarios.ToDictionary(x => x.Nombre, x => x.Cantidad);
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

                // 2. Alerta de Rechazos/Observaciones
                ViewBag.MisDocsRechazados = await _context.DocumentVersions
                    .Where(v => v.CreatedBy == currentUserId 
                             && v.StatusId == 1 
                             && v.Approvals.Any())
                    .CountAsync();

                // 🚀 3. Borradores olvidados (Sin iniciar flujo)
                ViewBag.MisBorradoresSinIniciar = await _context.DocumentVersions
                    .Where(v => v.CreatedBy == currentUserId 
                             && v.StatusId == 1 
                             && !v.Approvals.Any()) // La clave: NO hay historial de firmas
                    .CountAsync();

                // ==========================================
                // 📊 NUEVOS KPIS PARA EL DASHBOARD OPERATIVO
                // ==========================================

                // KPI 1: Docs. Creados (Todos los documentos donde él es el autor)
                ViewBag.MisDocsCreados = await _context.Documents
                    .CountAsync(d => d.CreatedBy == currentUserId); 

                // KPI 2: Docs. Aprobados (Documentos vigentes / StatusId == 3)
                ViewBag.MisDocsAprobados = await _context.DocumentVersions
                    .CountAsync(v => v.Document.CreatedBy == currentUserId && v.StatusId == 3);

                // KPI 3: En Revisión (Documentos atorados en aprobación / StatusId == 2)
                ViewBag.MisFlujosActivos = await _context.DocumentVersions
                    .CountAsync(v => v.Document.CreatedBy == currentUserId && v.StatusId == 2);

                // Cuenta cuántos flujos ya aprobó o revisó este usuario en el pasado (Estatus != Pending)
                ViewBag.MisFirmasRealizadas = await _context.DocumentApprovals
                    .CountAsync(a => a.ApproverId == currentUserId && a.ApprovalStatus != "Pending");

                // KPI 3 para Revisores: Documentos que ellos han rechazado/devuelto al creador por contener errores
                // Nota: Ajusta "Rejected" por la palabra o número que utilices en tu BD para los rechazos.
                ViewBag.MisDocsRechazadosPorMi = await _context.DocumentApprovals
                    .CountAsync(a => a.ApproverId == currentUserId && a.ApprovalStatus == "Rejected");
            }

            // =====================================================================
            // 🚀 LÓGICA GLOBAL PARA EL WIDGET "MIS TAREAS" (Barra Lateral Derecha)
            // =====================================================================

            // 🔴 1. Tareas de Máxima Prioridad: Documentos Rechazados (Borradores con historial de firmas)
            ViewBag.ListaRechazados = await _context.DocumentVersions
                .Include(v => v.Document)
                .Where(v => v.CreatedBy == currentUserId && v.StatusId == 1 && v.Approvals.Any())
                .OrderByDescending(v => v.VersionId) 
                .Take(3)
                .ToListAsync();

            // 🟠 2. Firmas Pendientes del Usuario (Como Revisor o Aprobador)
            ViewBag.ListaFirmas = await _context.DocumentApprovals
                .Include(a => a.DocumentVersion)
                    .ThenInclude(v => v.Document)
                .Where(a => a.ApproverId == currentUserId && a.ApprovalStatus == "Pending")
                .OrderByDescending(a => a.CreatedAt)
                .Take(3)
                .ToListAsync();

            // 🔵 3. Mis Borradores (Documentos sin enviar a flujo)
            ViewBag.ListaBorradores = await _context.DocumentVersions
                .Include(v => v.Document)
                .Where(v => v.CreatedBy == currentUserId && v.StatusId == 1 && !v.Approvals.Any())
                .OrderByDescending(v => v.VersionId) 
                .Take(3)
                .ToListAsync();

            return View();
        }
    }
}