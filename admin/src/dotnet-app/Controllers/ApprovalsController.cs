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
                        // 🚀 Extraemos solo el nombre del archivo (ej. "ISO-9001.pdf") para evitar conflictos de rutas en Linux/Docker
                        // 🚀 FIX: Extraemos la ruta relativa exacta respetando subcarpetas para Python
                        string nombreArchivoFisico = "";
                        if (!string.IsNullOrEmpty(updatedVersion.FilePath))
                        {
                            // 1. Normalizamos las diagonales de Windows (\) a Linux (/)
                            nombreArchivoFisico = updatedVersion.FilePath.Replace("\\", "/");
                            
                            // 2. Cortamos todo lo que esté antes de "uploads/" para que empate con el volumen de Docker
                            int uploadsIndex = nombreArchivoFisico.IndexOf("uploads/", StringComparison.OrdinalIgnoreCase);
                            if (uploadsIndex >= 0)
                            {
                                // Tomamos lo que sigue después de "uploads/" (que son 8 caracteres)
                                nombreArchivoFisico = nombreArchivoFisico.Substring(uploadsIndex + 8); 
                            }
                            else
                            {
                                nombreArchivoFisico = Path.GetFileName(updatedVersion.FilePath);
                            }
                        }

                        var payload = new
                        {
                            documento_id = updatedVersion.DocId,
                            codigo = updatedVersion.Document.DocCode ?? "SIN-CODIGO",
                            titulo = updatedVersion.Document.DocName ?? "Sin Título",
                            version = updatedVersion.VersionNum ?? "1.0",
                            
                            etiquetas = new[] { 
                                normativaDinamica,        
                                updatedVersion.Document.Category?.CategoryName ?? "General",   
                                updatedVersion.Document.Department?.DeptName ?? "General",     
                                origenDocumento,                                               
                                "Vigente"                                                      
                            },
                            
                            url_archivo = updatedVersion.FilePath ?? "",
                            
                            // 🚀 NUEVO: Le mandamos el nombre físico a Python
                            archivo_fisico = nombreArchivoFisico,
                            
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
        // Si Python responde pero con un error (ej. 500 o 400), capturamos qué nos dijo
        string respuestaPython = await response.Content.ReadAsStringAsync();
        TempData["ErrorMessage"] = $"Firma aplicada, pero Python rechazó los datos (Código: {response.StatusCode}). Detalle: {respuestaPython}";
    }
}
catch (Exception ex)
{
    // Captura el error de red exacto (DNS, Connection Refused, etc.)
    string errorReal = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
    var pythonApiUrl = _config["Microservices:PythonSearchApi"];
    TempData["ErrorMessage"] = $"Firma aplicada, pero falló la conexión con FastAPI. Error: {errorReal} | URL intentada: {pythonApiUrl}/api/docs/index";
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


        [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RevertSignature(int versionId, int approvalId, int docId)
{
    var userId = GetCurrentUserId();
    bool isAdmin = User.IsInRole("Admin de Empresa"); 

    try
    {
        // 🚀 1. AsNoTracking() evita que C# sobreescriba accidentalmente lo que hará SQL Server
        var versionData = await _context.DocumentVersions
            .AsNoTracking()
            .Include(v => v.Document)
            .FirstOrDefaultAsync(v => v.VersionId == versionId);

        if (versionData == null) return NotFound();
        string newVersionNum = versionData.VersionNum;
        bool estabaVigente = versionData.StatusId == 3;

        // 🚀 2. LA DIVISIÓN MAESTRA: Evitamos que EF Core se confunda con valores nulos
        if (estabaVigente && isAdmin)
        {
            // BYPASS DE EMERGENCIA: Mandamos @ApprovalID = NULL directamente "quemado" en el query de texto.
            // Esto fuerza a SQL Server a entrar sí o sí al "CASO 1" (Regresar a Borrador)
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_RecallDocumentWorkflow @VersionID = {0}, @ApprovalID = NULL, @UserID = {1}, @NewVersionNum = {2}, @IsAdminBypass = {3}",
                versionId, userId, newVersionNum, isAdmin
            );
        }
        else
        {
            // RECALL NORMAL: Mandamos el ID de la firma para retroceder un solo paso
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_RecallDocumentWorkflow @VersionID = {0}, @ApprovalID = {1}, @UserID = {2}, @NewVersionNum = {3}, @IsAdminBypass = {4}",
                versionId, approvalId, userId, newVersionNum, isAdmin
            );
        }

        // 🚀 3. REGISTRO EN LA BITÁCORA INMUTABLE
        var auditLog = new DocumentAuditLog {
            CompanyId = versionData.Document.CompanyId, 
            DocId = docId,
            VersionId = versionId,
            ActionType = "SignatureRevoked",
            ActionDetails = estabaVigente ? "RECALL DE EMERGENCIA: El administrador retiró el documento vigente de producción y lo regresó a Borrador." : "El firmante canceló su dictamen previo.",
            VersionNum = newVersionNum,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
        _context.DocumentAuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        // 🚀 4. ELIMINACIÓN EN MONGODB
        if (estabaVigente)
        {
            try
            {
                using var httpClient = new HttpClient();
                var pythonApiUrl = _config["Microservices:PythonSearchApi"].TrimEnd('/');
                var response = await httpClient.DeleteAsync($"{pythonApiUrl}/api/docs/index/{docId}");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Recall ejecutado en BD, pero ocurrió un problema al intentar ocultar el documento en MongoDB (Portal Operativo).";
                    return RedirectToAction("Details", "Documents", new { id = docId });
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Recall ejecutado, pero FastAPI está fuera de línea. El documento podría seguir visible temporalmente.";
                return RedirectToAction("Details", "Documents", new { id = docId });
            }
        }

        TempData["SuccessMessage"] = $"¡Acción ejecutada con éxito! El documento (v{newVersionNum}) fue retirado y ha regresado a las etapas iniciales.";
    }
    catch (Exception ex)
    {
        string errorReal = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
        TempData["ErrorMessage"] = "Rechazado por el sistema: " + errorReal;
    }

    return RedirectToAction("Details", "Documents", new { id = docId });
}


        // ==========================================
        // 🚀 5. ENDPOINT: OBTENER HISTORIAL DE VERSIONES (Para la Línea de Tiempo)
        // ==========================================
        [HttpGet("api/documents/{docCode}/history")]
        [AllowAnonymous] // Permite que Laravel lo consulte fácilmente sin chocar con las cookies de sesión de C#
        public async Task<IActionResult> GetDocumentHistory(string docCode)
        {
            try
            {
                // Buscamos todas las versiones que coincidan con el código del documento
                var history = await _context.DocumentVersions
                    .Include(v => v.Document)
                    .Where(v => v.Document.DocCode == docCode)
                    .OrderByDescending(v => v.CreatedAt) // Ordenamos de la más reciente a la más antigua
                    .Select(v => new {
                        version = v.VersionNum,
                        descripcion = v.ChangeDescription ?? "Sin descripción de cambios",
                        fecha_aprobacion = v.ApprovedAt,
                        fecha_obsoleto = v.ObsoletedAt,
                        ruta_archivo = v.FilePath,
                        estado_id = v.StatusId // 3 = Vigente, 4 = Obsoleto
                    })
                    .ToListAsync();

                // Ok() automáticamente convierte el objeto anónimo a un JSON perfecto
                return Ok(history); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener historial", error = ex.Message });
            }
        }
    }
}