using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityDoc.API.Data;
using QualityDoc.API.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QualityDoc.API.Controllers
{
    // 🛡️ ACCESO RESTRINGIDO: Solo gerencia
    [Authorize(Roles = "Super Admin, Admin de Empresa")]
    public class QualityTicketsController : Controller
    {
        private readonly QualityDocDbContext _context;

        public QualityTicketsController(QualityDocDbContext context)
        {
            _context = context;
        }

        // Helpers de sesión
        private bool IsSuperAdmin => User.IsInRole("Super Admin");
        private int CurrentCompanyId => int.Parse(User.FindFirstValue("CompanyId") ?? "0");

        // 1. GET: /QualityTickets
        public async Task<IActionResult> Index()
        {
            var query = _context.DocumentIssues
                .Include(i => i.Reporter)
                .Include(i => i.Company)
                .OrderBy(i => i.IssueStatus == "Pending" ? 0 : 1) // Los pendientes arriba
                .ThenByDescending(i => i.CreatedAt)               // Los más recientes primero
                .AsQueryable();

            // 🕵️ LÓGICA MULTI-TENANT
            if (!IsSuperAdmin)
            {
                query = query.Where(i => i.CompanyId == CurrentCompanyId);
            }

            var tickets = await query.ToListAsync();
            return View(tickets);
        }

        // 2. POST: /QualityTickets/Resolve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id)
        {
            var ticket = await _context.DocumentIssues.FindAsync(id);
            if (ticket == null) return NotFound();

            // 🔒 Validar que el Admin de Empresa no resuelva tickets de otra empresa
            if (!IsSuperAdmin && ticket.CompanyId != CurrentCompanyId)
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            // Actualizamos el estado
            ticket.IssueStatus = "Resolved";
            ticket.UpdatedAt = DateTime.UtcNow;
            ticket.UpdatedBy = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1");

            _context.Update(ticket);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"El ticket del documento {ticket.DocCode} fue marcado como resuelto.";
            return RedirectToAction(nameof(Index));
        }
    }
}