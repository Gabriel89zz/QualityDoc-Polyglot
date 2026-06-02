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
                // 🚀 LÓGICA DE VERSIONAMIENTO PROFESIONAL (ISO ÁGIL)
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
                        // 🟢 ES LA FIRMA DEL APROBADOR FINAL
                        if (versionActual < 1.0m)
                        {
                            // Si viene de borradores iniciales (Ej: 0.3), lo "graduamos" a 1.0
                            versionActual = 1.0m;
                        }
                        // 🧠 SI YA ERA MAYOR A 1.0 (Ej: Venía de un Recall y estaba en 1.2):
                        // NO HACEMOS NADA. Se queda en su decimal actual y se publica como 1.2
                        // Esto evita que salte a 2.0 por un simple error de dedo.
                    }
                    else
                    {
                        // 🟠 MIENTRAS ESTÉ EN BORRADOR, REVISIÓN O RECHAZO:
                        // Siempre sumamos 0.1 al historial (Ej: 0.1 -> 0.2, o 1.1 -> 1.2)
                        versionActual += 0.1m;
                    }

                    updatedVersion.VersionNum = versionActual.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                    _context.DocumentVersions.Update(updatedVersion);

                    // 🚀 NUEVO: Registro en la Bitácora Inmutable
                    var auditLog = new DocumentAuditLog {
                        CompanyId = updatedVersion.Document.CompanyId,
                        DocId = updatedVersion.DocId,
                        VersionId = updatedVersion.VersionId,
                        ActionType = isApproved ? "Approved" : "Rejected",
                        ActionDetails = isApproved ? "Firma Autorizada exitosamente." : "El documento fue rechazado y devuelto al autor.",
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
                    TempData["SuccessMessage"] = $"Documento devuelto al creador con observaciones. La versión aumentó a {updatedVersion?.VersionNum}.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al procesar la firma en la base de datos: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // 4. DESHACER FIRMA / REVERTIR DECISIÓN (ACTUALIZADO)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevertSignature(int versionId, int approvalId, int docId)
        {
            var userId = GetCurrentUserId();

            try
            {
                // 1. OBTENEMOS LOS DATOS DE LA VERSIÓN ANTES DE RETROCEDER
                var versionData = await _context.DocumentVersions
                    .Include(v => v.Document) // 🚀 NUEVO: Necesario para el CompanyId
                    .FirstOrDefaultAsync(v => v.VersionId == versionId);

                if (versionData == null) return NotFound();

                // Guardamos si estaba aprobado para saber si hay que borrarlo de MongoDB
                bool estabaAprobado = (versionData.StatusId == 3);

                // // 2. LÓGICA DE VERSIONAMIENTO (CEREBRO EN C#)
                // // Siempre sumamos 0.1 al deshacer para dejar evidencia en el historial ISO/IATF
                // decimal versionActual = 0.0m;
                // decimal.TryParse(versionData.VersionNum, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out versionActual);
                
                // string newVersionNum = (versionActual + 0.1m).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                string newVersionNum = versionData.VersionNum;
                // 3. EJECUTAMOS EL SP
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_RecallDocumentWorkflow @VersionID = {0}, @ApprovalID = {1}, @UserID = {2}, @NewVersionNum = {3}",
                    versionId, approvalId, userId, newVersionNum
                );

                // 🚀 NUEVO: Registro en la Bitácora Inmutable
                var auditLog = new DocumentAuditLog {
                    CompanyId = versionData.Document.CompanyId, 
                    DocId = docId,
                    VersionId = versionId,
                    ActionType = "SignatureRevoked",
                    ActionDetails = "El firmante canceló su dictamen previo. El flujo operativo se ha pausado y retrocedido.",
                    VersionNum = newVersionNum,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.DocumentAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                // 4. 🔥 SINCRO DE MICROSERVICIOS: BAJAR DOCUMENTO DE INTERNET (MONGO / PHP)
                // Si el documento ya estaba publicado (Estatus 3), le ordenamos a Python que lo elimine
                if (estabaAprobado)
                {
                    using var httpClient = new HttpClient();
                    try
                    {
                        var pythonApiUrl = _config["Microservices:PythonSearchApi"];
                        
                        // Enviamos un DELETE al endpoint de indexación en FastAPI con el ID del documento
                        var response = await httpClient.DeleteAsync($"{pythonApiUrl}/api/docs/index/{docId}");
                        
                        if (!response.IsSuccessStatusCode)
                        {
                            TempData["ErrorMessage"] = "Firma revocada en SQL, pero ocurrió un problema al intentar ocultar el documento en el portal operativo (MongoDB).";
                            return RedirectToAction("Details", "Documents", new { id = docId });
                        }
                    }
                    catch (Exception)
                    {
                        TempData["ErrorMessage"] = "Firma revocada, pero el motor de búsqueda (FastAPI) está fuera de línea. El documento podría seguir visible en el portal de PHP.";
                        return RedirectToAction("Details", "Documents", new { id = docId });
                    }
                }

                TempData["SuccessMessage"] = $"¡Firma revocada con éxito! El documento ha regresado a revisión bajo la versión {newVersionNum}.";
            }
            catch (Exception ex)
            {
                // Extraemos el mensaje exacto enviado por el THROW de SQL Server
                string errorReal = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = "No se pudo revertir el flujo: " + errorReal;
            }

            // Redirigimos de vuelta al expediente de Detalles para ver los cambios en la bitácora
            return RedirectToAction("Details", "Documents", new { id = docId });
        }
    }
}