using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using QualityDoc.API.Data;
using QualityDoc.API.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http; // 🚀 Necesario para enviar datos a Python
using System.Text.Json; // 🚀 Necesario para armar el JSON
using System.Text;

namespace QualityDoc.API.Controllers
{
    [Authorize]
    public class ApprovalsController : Controller
    {
        private readonly QualityDocDbContext _context;
        private readonly IConfiguration _config;
        public ApprovalsController(QualityDocDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
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

            // LISTA 1: Mis Firmas Pendientes 
            var pendingApprovals = await _context.DocumentApprovals
                .Include(a => a.DocumentVersion)
                    .ThenInclude(v => v.Document)
                        .ThenInclude(d => d.Department) 
                .Where(a => a.ApproverId == userId && a.ApprovalStatus == "Pending")
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            // LISTA 2: Mis Documentos Rechazados 
            var rejectedDocs = await _context.DocumentVersions
                .Include(v => v.Document)
                .Where(v => v.CreatedBy == userId && v.StatusId == 1 && v.Approvals.Any())
                .OrderByDescending(v => v.UpdatedAt ?? v.CreatedAt)
                .ToListAsync();

            // LISTA 3: Mis Borradores Olvidados 
            var forgottenDrafts = await _context.DocumentVersions
                .Include(v => v.Document)
                .Where(v => v.CreatedBy == userId && v.StatusId == 1 && !v.Approvals.Any())
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

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
        // 3. SIGN: POST (Procesar la firma y mandar a Python)
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
                // 1. Ejecutamos el Stored Procedure de SQL Server
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_SignDocumentWorkflow @ApprovalID = {0}, @ApproverID = {1}, @Comments = {2}, @SignatureToken = {3}, @IsApproved = {4}",
                    approvalId, userId, comments ?? "", signatureToken, isApproved
                );

                // 🚀 2. VERIFICACIÓN Y ENVÍO A PYTHON (MONGODB)
                if (isApproved)
                {
                    // Revisamos cómo quedó el documento después de que SQL Server lo procesó
                    var updatedVersion = await _context.DocumentVersions
                        .Include(v => v.Document)
                            .ThenInclude(d => d.Category)
                        .Include(v => v.Document)
                            .ThenInclude(d => d.Department)
                        .FirstOrDefaultAsync(v => v.VersionId == approval.VersionId);

                    // Si el documento alcanzó el Estatus 3 (Aprobado Final), lo mandamos a Mongo
                    if (updatedVersion != null && updatedVersion.StatusId == 3)
                    {
                        var currentUser = await _context.Users.FindAsync(userId);

                        // Armamos el JSON exactamente como lo espera FastAPI
                        var payload = new
                        {
                            documento_id = updatedVersion.DocId,
                            codigo = updatedVersion.Document.DocCode,
                            titulo = updatedVersion.Document.DocName,
                            version = updatedVersion.VersionNum,
                            // Generamos etiquetas automáticas basadas en su categoría y departamento
                            etiquetas = new[] { updatedVersion.Document.Category.CategoryName, updatedVersion.Document.Department.DeptName, "Aprobado", "ISO" },
                            url_archivo = updatedVersion.FilePath,
                            aprobado_por = currentUser.FullName
                        };

                        using var httpClient = new HttpClient();
                        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                        try
                        {
                            // Leemos la URL base desde el appsettings.json
                            var pythonApiUrl = _config["Microservices:PythonSearchApi"];

                            // Concatenamos la ruta específica
                            var response = await httpClient.PostAsync($"{pythonApiUrl}/api/docs/index", content);
                            
                            if (response.IsSuccessStatusCode)
                            {
                                TempData["SuccessMessage"] = "¡Firma aplicada! El documento fue aprobado y ya está indexado en el portal operativo.";
                                return RedirectToAction(nameof(Index));
                            }
                            else
                            {
                                TempData["ErrorMessage"] = "Firma aplicada, pero ocurrió un error al indexar en el buscador (MongoDB).";
                            }
                        }
                        catch (Exception)
                        {
                            TempData["ErrorMessage"] = "Firma aplicada, pero el motor de búsqueda (Node/Python) está apagado o no responde.";
                        }
                    }
                    else
                    {
                        // Si se aprobó pero aún faltan firmas (ej. faltaba el Aprobador Final)
                        TempData["SuccessMessage"] = "¡Firma aplicada! El documento ha avanzado al siguiente nivel de revisión.";
                    }
                }
                else
                {
                    TempData["SuccessMessage"] = "Documento rechazado. Se ha regresado el estatus a Borrador.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al procesar la firma en la base de datos: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}