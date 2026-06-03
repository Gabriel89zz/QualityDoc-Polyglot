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

                // ====================================================================
                // 🚀 LÓGICA DE VERSIONAMIENTO: REGLA DEL ENTERO SAGRADO
                // ====================================================================
                var updatedVersion = await _context.DocumentVersions
                    .Include(v => v.Document).ThenInclude(d => d.Category)
                    .Include(v => v.Document).ThenInclude(d => d.Department)
                    .FirstOrDefaultAsync(v => v.VersionId == approval.VersionId);

                if (updatedVersion != null)
                {
                    decimal versionActual = 0.0m;
                    decimal.TryParse(updatedVersion.VersionNum, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out versionActual);

                    if (isApproved && updatedVersion.StatusId == 3)
                    {
                        // 🟢 FIRMA FINAL: EL DOCUMENTO SE GRADÚA
                        // Usamos Math.Floor para sacar la base (Ej: 0.3 -> 0) y le sumamos 1.0 = 1.0
                        // O si venía de (1.2 -> 1) + 1.0 = 2.0
                        versionActual = Math.Floor(versionActual) + 1.0m;
                    }
                    // 🔴 SI FUE RECHAZADO, O SOLO ES EL REVISOR (Status 2): 
                    // NO SE SUMAN DECIMALES. La versión se queda intacta porque el archivo físico no se modificó.

                    updatedVersion.VersionNum = versionActual.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                    _context.DocumentVersions.Update(updatedVersion);

                    // 🚀 Registro en la Bitácora Inmutable
                    var auditLog = new DocumentAuditLog {
                        CompanyId = updatedVersion.Document.CompanyId,
                        DocId = updatedVersion.DocId,
                        VersionId = updatedVersion.VersionId,
                        ActionType = isApproved ? "Approved" : "Rejected",
                        ActionDetails = isApproved ? "Firma Autorizada exitosamente." : "El documento fue rechazado y devuelto al autor. No hubo salto de versión.",
                        VersionNum = versionActual.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                        CreatedBy = userId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.DocumentAuditLogs.Add(auditLog);

                    await _context.SaveChangesAsync();
                }
                
                // 🚀 2. VERIFICACIÓN Y ENVÍO A PYTHON (MONGODB)
                if (isApproved)
                {
                    if (updatedVersion != null && updatedVersion.StatusId == 3)
                    {
                        var currentUser = await _context.Users.FindAsync(userId);

                        // ============================================================
                        // 🧠 ESTRATEGIA DE METADATOS PROFESIONALES (ISO / IATF)
                        // ============================================================
                        // 1. Extraemos la normativa dinámicamente del prefijo (Ej: "ISO-MAN" -> "ISO")
                        string normativaDinamica = "General";
                        if (updatedVersion.Document.Category != null && !string.IsNullOrEmpty(updatedVersion.Document.Category.Prefix))
                        {
                            var partesPrefijo = updatedVersion.Document.Category.Prefix.Split('-');
                            if (partesPrefijo.Length > 0)
                            {
                                normativaDinamica = partesPrefijo[0]; // Producirá "ISO" o "IATF" de forma limpia
                            }
                        }

                        // 2. Identificamos el origen del documento (Requisito regulatorio estricto)
                        string origenDocumento = updatedVersion.Document.IsExternal ? "Externo" : "Interno";

                        // Armamos el JSON con la taxonomía optimizada para MongoDB
                        var payload = new
                        {
                            documento_id = updatedVersion.DocId,
                            codigo = updatedVersion.Document.DocCode ?? "SIN-CODIGO",
                            titulo = updatedVersion.Document.DocName ?? "Sin Título",
                            version = updatedVersion.VersionNum ?? "1.0",
                            
                            etiquetas = new[] { 
                                normativaDinamica,                                             // "ISO" o "IATF" dinámico
                                updatedVersion.Document.Category?.CategoryName ?? "General",   // "Manual de Calidad", "Procedimientos", etc.
                                updatedVersion.Document.Department?.DeptName ?? "General",     // "Calidad", "Producción", etc.
                                origenDocumento,                                               // "Interno" o "Externo"
                                "Vigente"                                                      // Estatus del ciclo de vida ISO
                            },
                            
                            url_archivo = updatedVersion.FilePath ?? "",
                            aprobado_por = currentUser.FullName ?? "Sistema",
                            empresa_id = updatedVersion.Document.Department?.CompanyId ?? 0, 
                            departamento_id = updatedVersion.Document.Department?.DeptId ?? 0 
                        };

                        using var httpClient = new HttpClient();
                        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                        try
                        {
                            var pythonApiUrl = _config["Microservices:PythonSearchApi"];
                            var response = await httpClient.PostAsync($"{pythonApiUrl}/api/docs/index", content);
                            
                            if (response.IsSuccessStatusCode)
                            {
                                TempData["SuccessMessage"] = $"¡Firma aplicada! El documento (v{updatedVersion.VersionNum}) fue aprobado y ya está disponible en el portal operativo.";
                                return RedirectToAction(nameof(Index));
                            }
                            else
                            {
                                TempData["ErrorMessage"] = "Firma aplicada, pero ocurrió un error al indexar en MongoDB.";
                            }
                        }
                        catch (Exception)
                        {
                            TempData["ErrorMessage"] = "Firma aplicada, pero el motor de búsqueda (FastAPI/Python) está fuera de línea.";
                        }
                    }
                    else
                    {
                        TempData["SuccessMessage"] = $"¡Firma aplicada! El documento ha avanzado a la versión {updatedVersion?.VersionNum}.";
                    }
                }
                else
                {
                    TempData["SuccessMessage"] = $"Documento devuelto al creador con observaciones. Se mantuvo la versión {updatedVersion?.VersionNum} para correcciones.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al procesar la firma en la base de datos: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // 4. DESHACER FIRMA / REVERTIR DECISIÓN (BLINDADO ISO 9001)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevertSignature(int versionId, int approvalId, int docId)
        {
            var userId = GetCurrentUserId();
            try
            {
                // 1. OBTENEMOS LOS DATOS DE LA VERSIÓN
                var versionData = await _context.DocumentVersions
                    .Include(v => v.Document)
                    .FirstOrDefaultAsync(v => v.VersionId == versionId);

                if (versionData == null) return NotFound();

                // 🚀 BLINDAJE ISO 9001: REGLA DE INMUTABILIDAD FINAL
                // Si el documento ya alcanzó la versión oficial (Status 3 = Aprobado), 
                // el flujo ya no se puede deshacer.
                if (versionData.StatusId == 3)
                {
                    TempData["ErrorMessage"] = "Bloqueo de Calidad: No puedes revocar la firma de un documento que ya fue publicado (v" + versionData.VersionNum + "). Si detectaste un error, debes crear una 'Nueva Versión'.";
                    return RedirectToAction("Details", "Documents", new { id = docId });
                }

                // Si aún está en revisión, mantenemos el número actual (Ej. 0.3)
                string newVersionNum = versionData.VersionNum;

                // 2. EJECUTAMOS EL SP PARA RETROCEDER EL FLUJO
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_RecallDocumentWorkflow @VersionID = {0}, @ApprovalID = {1}, @UserID = {2}, @NewVersionNum = {3}",
                    versionId, approvalId, userId, newVersionNum
                );

                // 3. REGISTRO EN LA BITÁCORA INMUTABLE
                var auditLog = new DocumentAuditLog {
                    CompanyId = versionData.Document.CompanyId, 
                    DocId = docId,
                    VersionId = versionId,
                    ActionType = "SignatureRevoked",
                    ActionDetails = "El firmante canceló su dictamen previo. El flujo operativo ha retrocedido a revisión.",
                    VersionNum = newVersionNum,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.DocumentAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"¡Firma revocada con éxito! El documento ha regresado a revisión bajo la versión {newVersionNum}.";
            }
            catch (Exception ex)
            {
                string errorReal = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = "No se pudo revertir el flujo: " + errorReal;
            }

            return RedirectToAction("Details", "Documents", new { id = docId });
        }
    }
}