using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using QualityDoc.API.Data;
using QualityDoc.API.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QualityDoc.API.Controllers
{
    [Authorize]
    public class ApprovalsController : Controller
    {
        private readonly QualityDocDbContext _context;

        public ApprovalsController(QualityDocDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // UTILIDADES DE SESIÓN
        // ==========================================
        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrEmpty(claim) ? 0 : int.Parse(claim);
        }

        // ==========================================
        // 1. INDEX: Centro de Tareas Unificado
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            // LISTA 1: Mis Firmas Pendientes (Lo que ya tenías)
            var pendingApprovals = await _context.DocumentApprovals
                .Include(a => a.DocumentVersion)
                    .ThenInclude(v => v.Document)
                        .ThenInclude(d => d.Department) 
                .Where(a => a.ApproverId == userId && a.ApprovalStatus == "Pending")
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            // 🚀 LISTA 2: Mis Documentos Rechazados (Borradores que SÍ tienen historial)
            var rejectedDocs = await _context.DocumentVersions
                .Include(v => v.Document)
                .Where(v => v.CreatedBy == userId && v.StatusId == 1 && v.Approvals.Any())
                .OrderByDescending(v => v.UpdatedAt ?? v.CreatedAt)
                .ToListAsync();

            // 🚀 LISTA 3: Mis Borradores Olvidados (Borradores que NO tienen historial)
            var forgottenDrafts = await _context.DocumentVersions
                .Include(v => v.Document)
                .Where(v => v.CreatedBy == userId && v.StatusId == 1 && !v.Approvals.Any())
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            // Enviamos las nuevas listas por ViewBag
            ViewBag.RejectedDocs = rejectedDocs;
            ViewBag.ForgottenDrafts = forgottenDrafts;

            return View(pendingApprovals);
        }

        // ==========================================
        // 2. REVIEW: GET (Pantalla para leer el PDF y Firmar)
        // ==========================================
        public async Task<IActionResult> Review(int? id)
        {
            if (id == null) return NotFound();

            var userId = GetCurrentUserId();

            var approval = await _context.DocumentApprovals
                .Include(a => a.DocumentVersion)
                    .ThenInclude(v => v.Document)
                        .ThenInclude(d => d.Category)
                .Include(a => a.DocumentVersion)
                    .ThenInclude(v => v.Document)
                        .ThenInclude(d => d.Department)
                .FirstOrDefaultAsync(a => a.ApprovalId == id && a.ApproverId == userId);

            if (approval == null || approval.ApprovalStatus != "Pending")
            {
                TempData["ErrorMessage"] = "Esta tarea ya fue procesada o no tienes permisos para verla.";
                return RedirectToAction(nameof(Index));
            }

            return View(approval);
        }

        // ==========================================
        // 3. SIGN: POST (Procesar la firma conectándose al Stored Procedure)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sign(int approvalId, string decision, string comments)
        {
            var userId = GetCurrentUserId();
            
            var approval = await _context.DocumentApprovals
                .FirstOrDefaultAsync(a => a.ApprovalId == approvalId && a.ApproverId == userId);

            if (approval == null || approval.ApprovalStatus != "Pending")
            {
                TempData["ErrorMessage"] = "Firma no válida o ya procesada.";
                return RedirectToAction(nameof(Index));
            }

            bool isApproved = (decision == "Approve");
            
            string signatureToken = Guid.NewGuid().ToString();

            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_SignDocumentWorkflow @ApprovalID = {0}, @ApproverID = {1}, @Comments = {2}, @SignatureToken = {3}, @IsApproved = {4}",
                    approvalId, userId, comments ?? "", signatureToken, isApproved
                );

                TempData["SuccessMessage"] = isApproved 
                    ? "¡Firma aplicada! El documento ha avanzado en el flujo." 
                    : "Documento rechazado. Se ha regresado el estatus a Borrador.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al procesar la firma en la base de datos: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}