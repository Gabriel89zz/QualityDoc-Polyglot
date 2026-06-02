using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using QualityDoc.API.Data;
using QualityDoc.API.Models;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using QualityDoc.API.Helpers;

namespace QualityDoc.API.Controllers
{
    [Authorize]
    public class DocumentsController : Controller
    {
        private readonly QualityDocDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DocumentsController(QualityDocDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private int GetCurrentCompanyId()
        {
            var claim = User.FindFirst("CompanyId")?.Value;
            return string.IsNullOrEmpty(claim) ? 0 : int.Parse(claim);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrEmpty(claim) ? 0 : int.Parse(claim);
        }

        // ==========================================
        // 1. INDEX: Listar Documentos
        // ==========================================
        public async Task<IActionResult> Index(int? pageNumber)
        {
            var companyId = GetCurrentCompanyId();
            var currentUserId = GetCurrentUserId();

            var currentUser = await _context.Users.FindAsync(currentUserId);
            ViewBag.UserDeptId = currentUser?.DeptId ?? 0;

            // 🚀 Definimos 10 registros por página
            int pageSize = 10;

            // 🚀 1. Iniciamos la consulta base (Solo filtramos por empresa)
            var query = _context.Documents
                .IgnoreQueryFilters()
                .Include(d => d.Category)
                .Include(d => d.Department)
                .Where(d => d.CompanyId == companyId);

            // 🚀 2. EL BLINDAJE: Si no es Admin, filtramos por su departamento
            if (!User.IsInRole("Admin de Empresa"))
            {
                query = query.Where(d => d.DeptId == currentUser.DeptId);
            }

            // Ordenamos para que los más nuevos salgan arriba
            query = query.OrderByDescending(d => d.CreatedAt);

            var latestStatuses = await _context.DocumentVersions
                .Where(v => v.Document.CompanyId == companyId)
                .GroupBy(v => v.DocId)
                .Select(g => new { DocId = g.Key, StatusId = g.OrderByDescending(v => v.VersionId).FirstOrDefault().StatusId })
                .ToDictionaryAsync(x => x.DocId, x => x.StatusId);
            ViewBag.LatestStatuses = latestStatuses;

            var hasAdvancedVersions = await _context.DocumentVersions
                .Where(v => v.Document.CompanyId == companyId && v.StatusId != 1)
                .Select(v => v.DocId)
                .Distinct()
                .ToListAsync();
            ViewBag.HasAdvancedVersions = hasAdvancedVersions;

            // 🚀 3. Pasamos la consulta a nuestra clase matemática (Sin el ToListAsync)
            return View(await PaginatedList<Document>.CreateAsync(query, pageNumber ?? 1, pageSize));
        }

        // ==========================================
        // 2. CREATE: GET
        // ==========================================
        public async Task<IActionResult> Create()
        {
            var isAdmin = User.IsInRole("Admin de Empresa");
            var isCreator = User.IsInRole("Creador de Doc");

            if (!isAdmin && !isCreator)
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            var companyId = GetCurrentCompanyId();
            var currentUser = await _context.Users.FindAsync(GetCurrentUserId());
            
           // 🚀 Enviamos la bandera a la vista para saber si dibujamos el select
            ViewBag.IsAdmin = isAdmin;

            // 🚀 NUEVO: Extraemos categorías, cruzamos con Norms y agrupamos
            var categoriasAgrupadas = await _context.DocumentCategories
                .Include(c => c.Norm)
                .Where(c => c.CompanyId == companyId && c.Status == "Active")
                .Select(c => new {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    NormGroupName = c.Norm != null ? c.Norm.NormName : "Otras Categorías"
                })
                .OrderBy(c => c.NormGroupName).ThenBy(c => c.CategoryName)
                .ToListAsync();

            // ==============================================================
            // 🚀 NUEVA LÓGICA DE CASCADA: NORMATIVAS Y CATEGORÍAS EN JSON
            // ==============================================================
            
            // 1. Cargamos las normativas para el filtro superior
            var norms = await _context.Norms.Where(n => n.Status == "Active").ToListAsync();
            ViewBag.Norms = new SelectList(norms, "NormId", "NormName");

            // 2. Extraemos las categorías y las empaquetamos (El ?? 0 es para las que no tienen norma)
            var rawCategories = await _context.DocumentCategories
                .Where(c => c.CompanyId == companyId && c.Status == "Active")
                .Select(c => new { 
                    CategoryId = c.CategoryId, 
                    CategoryName = c.CategoryName, 
                    NormId = c.NormId ?? 0 
                })
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            // 3. Lo enviamos como JSON puro a la vista
            ViewBag.CategoriasJson = System.Text.Json.JsonSerializer.Serialize(rawCategories);
            // ==============================================================
            ViewBag.Departments = new SelectList(_context.Departments.Where(d => d.CompanyId == companyId && d.Status == "Active"), "DeptId", "DeptName");
            
            return View();
        }

        // ==========================================
        // 3. CREATE: POST 
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Document model, IFormFile uploadedFile)
        {
            var isAdmin = User.IsInRole("Admin de Empresa");
            
            if (!isAdmin && !User.IsInRole("Creador de Doc"))
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            var companyId = GetCurrentCompanyId();
            var currentUser = await _context.Users.FindAsync(GetCurrentUserId());

           // EL BLINDAJE DE SEGURIDAD: Forzamos el DeptId si no es Admin
            if (!isAdmin && currentUser != null)
            {
                // Si el usuario tiene departamento lo asigna, si es null, asigna un 0 por defecto.
                model.DeptId = currentUser.DeptId ?? 0;
            }

            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                ModelState.AddModelError("uploadedFile", "Es obligatorio adjuntar el archivo PDF del documento.");
            }

            ModelState.Remove("DocCode");
            ModelState.Remove("CompanyId");
            ModelState.Remove("Category");
            ModelState.Remove("Department");
            ModelState.Remove("Company");
            ModelState.Remove("Versions");

           if (ModelState.IsValid)
            {
                var category = await _context.DocumentCategories.FindAsync(model.CategoryId);

                // 🚀 Regresamos a contar por Categoría. 
                // Así, la categoría ISO tendrá su 001, y la categoría IATF tendrá su propio 001.
                var docCount = await _context.Documents
                    .IgnoreQueryFilters()
                    .Where(d => d.CompanyId == companyId && d.CategoryId == model.CategoryId)
                    .CountAsync();

                // El folio se armará con el prefijo exacto. Ej: "ISO-MAN-001"
                string folioCode = $"{category.Prefix}-{(docCount + 1):D3}";
                model.DocCode = folioCode;

                model.CompanyId = companyId;
                model.Status = "Active";
                model.CreatedAt = DateTime.UtcNow;
                model.CreatedBy = GetCurrentUserId();

                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "documents");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileExtension = Path.GetExtension(uploadedFile.FileName).ToLower();
                // 🔥 AQUÍ CAMBIAMOS EL v1.0 por v0.1 EN EL NOMBRE DEL ARCHIVO
                string uniqueFileName = $"{folioCode}_v0.1_{Guid.NewGuid().ToString().Substring(0,8)}{fileExtension}";
                string filePathPhysical = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePathPhysical, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(fileStream);
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Documents.Add(model);
                    await _context.SaveChangesAsync(); 

                    var docVersion = new DocumentVersion
                    {
                        DocId = model.DocId,
                        StatusId = 1,
                        // 🔥 AQUÍ NACE COMO 0.1
                        VersionNum = "0.1",
                        FilePath = $"/uploads/documents/{uniqueFileName}",
                        Extension = fileExtension,
                        ChangeDescription = "Creación inicial del documento",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = GetCurrentUserId(),
                        Status = "Active"
                    };

                    _context.DocumentVersions.Add(docVersion);
                    await _context.SaveChangesAsync(); // Genera el VersionId

                    // 🚀 NUEVO: Registro en la Bitácora Inmutable
                    var auditLog = new DocumentAuditLog {
                        CompanyId = companyId,
                        DocId = model.DocId,
                        VersionId = docVersion.VersionId, 
                        ActionType = "DraftCreated",
                        ActionDetails = "Creación inicial del documento en el sistema.",
                        VersionNum = "0.1",
                        CreatedBy = GetCurrentUserId(),
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.DocumentAuditLogs.Add(auditLog);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    if (System.IO.File.Exists(filePathPhysical))
                    {
                        System.IO.File.Delete(filePathPhysical);
                    }
                   // 🔥 EXTRAEMOS EL ERROR REAL QUE NOS MANDA SQL SERVER
                    string errorReal = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    ModelState.AddModelError("", "Error SQL: " + errorReal);
                }
            }

            ViewBag.IsAdmin = isAdmin;

            // 🚀 NUEVO: Mantenemos la agrupación si el formulario falla y recarga
            var categoriasAgrupadas = await _context.DocumentCategories
                .Include(c => c.Norm)
                .Where(c => c.CompanyId == companyId && c.Status == "Active")
                .Select(c => new {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    NormGroupName = c.Norm != null ? c.Norm.NormName : "Otras Categorías"
                })
                .OrderBy(c => c.NormGroupName).ThenBy(c => c.CategoryName)
                .ToListAsync();

           // ==============================================================
            // 🚀 NUEVA LÓGICA DE CASCADA: NORMATIVAS Y CATEGORÍAS EN JSON
            // ==============================================================
            
            // 1. Cargamos las normativas para el filtro superior
            var norms = await _context.Norms.Where(n => n.Status == "Active").ToListAsync();
            ViewBag.Norms = new SelectList(norms, "NormId", "NormName");

            // 2. Extraemos las categorías y las empaquetamos (El ?? 0 es para las que no tienen norma)
            var rawCategories = await _context.DocumentCategories
                .Where(c => c.CompanyId == companyId && c.Status == "Active")
                .Select(c => new { 
                    CategoryId = c.CategoryId, 
                    CategoryName = c.CategoryName, 
                    NormId = c.NormId ?? 0 
                })
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            // 3. Lo enviamos como JSON puro a la vista
            ViewBag.CategoriasJson = System.Text.Json.JsonSerializer.Serialize(rawCategories);
            // ==============================================================
            ViewBag.Departments = new SelectList(_context.Departments.Where(d => d.CompanyId == companyId && d.Status == "Active"), "DeptId", "DeptName", model.DeptId);
            
            return View(model);
        }

        // ==========================================
        // 4. EDIT: GET
        // ==========================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var isAdmin = User.IsInRole("Admin de Empresa");
            var isCreator = User.IsInRole("Creador de Doc");

            if (!isAdmin && !isCreator) return RedirectToAction("AccessDenied", "Auth");

            var companyId = GetCurrentCompanyId();
            
            var document = await _context.Documents
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.DocId == id && d.CompanyId == companyId);

            if (document == null) return NotFound();

            var latestVersion = await _context.DocumentVersions
                .Where(v => v.DocId == id)
                .OrderByDescending(v => v.VersionId)
                .FirstOrDefaultAsync();

            if (latestVersion != null && latestVersion.StatusId != 1)
            {
                TempData["ErrorMessage"] = "Trazabilidad ISO: No se pueden editar los metadatos porque el documento ya está en revisión o vigente.";
                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _context.Users.FindAsync(GetCurrentUserId());
            if (!isAdmin && isCreator && document.DeptId != currentUser?.DeptId)
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            ViewBag.Categories = new SelectList(_context.DocumentCategories.Where(c => c.CompanyId == companyId && c.Status == "Active"), "CategoryId", "CategoryName", document.CategoryId);
            ViewBag.Departments = new SelectList(_context.Departments.Where(d => d.CompanyId == companyId && d.Status == "Active"), "DeptId", "DeptName", document.DeptId);
            
            return View(document);
        }

        // ==========================================
        // 5. EDIT: POST
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Document model, IFormFile newFile)
        {
            if (id != model.DocId) return NotFound();

            var isAdmin = User.IsInRole("Admin de Empresa");
            var isCreator = User.IsInRole("Creador de Doc");

            if (!isAdmin && !isCreator) return RedirectToAction("AccessDenied", "Auth");

            var latestVersion = await _context.DocumentVersions
                .Where(v => v.DocId == id)
                .OrderByDescending(v => v.VersionId)
                .FirstOrDefaultAsync();

            if (latestVersion != null && latestVersion.StatusId != 1)
            {
                TempData["ErrorMessage"] = "Trazabilidad ISO: Operación bloqueada. El documento no está en estatus Borrador.";
                return RedirectToAction(nameof(Index));
            }

            var companyId = GetCurrentCompanyId();

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var existingDoc = await _context.Documents
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(d => d.DocId == id && d.CompanyId == companyId);

                    if (existingDoc == null) return NotFound();

                    var currentUser = await _context.Users.FindAsync(GetCurrentUserId());

                    if (!isAdmin && isCreator && existingDoc.DeptId != currentUser?.DeptId)
                    {
                        return RedirectToAction("AccessDenied", "Auth");
                    }

                    // 1. Actualizamos metadatos del maestro
                    existingDoc.DocName = model.DocName;
                    existingDoc.Description = model.Description;
                    existingDoc.IsExternal = model.IsExternal;
                    existingDoc.UpdatedAt = DateTime.UtcNow;
                    existingDoc.UpdatedBy = GetCurrentUserId();

                    _context.Update(existingDoc);

                    // 🚀 2. LÓGICA DE SUMAR DECIMAL AL EDITAR
                    if (latestVersion != null)
                    {
                        decimal versionActual = 0.0m;
                        decimal.TryParse(latestVersion.VersionNum, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out versionActual);
                        
                        // Sumamos 0.1 porque el creador lo modificó
                        latestVersion.VersionNum = (versionActual + 0.1m).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                        latestVersion.UpdatedAt = DateTime.UtcNow;
                        latestVersion.UpdatedBy = GetCurrentUserId();

                        if (newFile != null && newFile.Length > 0)
                        {
                            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "documents");
                            string fileExtension = Path.GetExtension(newFile.FileName).ToLower();
                            
                            string uniqueFileName = $"{existingDoc.DocCode}_v{latestVersion.VersionNum}_corregido_{Guid.NewGuid().ToString().Substring(0,8)}{fileExtension}";
                            string filePathPhysical = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePathPhysical, FileMode.Create))
                            {
                                await newFile.CopyToAsync(fileStream);
                            }

                            if (!string.IsNullOrEmpty(latestVersion.FilePath))
                            {
                                string oldPathPhysical = _env.WebRootPath + latestVersion.FilePath.Replace("/", "\\");
                                if (System.IO.File.Exists(oldPathPhysical))
                                {
                                    System.IO.File.Delete(oldPathPhysical);
                                }
                            }

                            latestVersion.FilePath = $"/uploads/documents/{uniqueFileName}";
                            latestVersion.Extension = fileExtension;
                        }

                        _context.Update(latestVersion);

                        // 🚀 3. NUEVO: REGISTRO EN LA BITÁCORA INMUTABLE
                        var auditLog = new DocumentAuditLog {
                            CompanyId = companyId,
                            DocId = existingDoc.DocId,
                            VersionId = latestVersion.VersionId,
                            ActionType = "DraftEdited",
                            ActionDetails = "El usuario editó los metadatos estructurales o reemplazó el archivo PDF del borrador.",
                            VersionNum = latestVersion.VersionNum, // Guarda la foto exacta: Ej. 0.2
                            CreatedBy = GetCurrentUserId(),
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.DocumentAuditLogs.Add(auditLog);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    TempData["SuccessMessage"] = "El documento ha sido actualizado y su versión ha incrementado.";
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Ocurrió un error al procesar el archivo o la base de datos.";
                    return RedirectToAction(nameof(Edit), new { id = model.DocId });
                }
                
                return RedirectToAction(nameof(Details), new { id = model.DocId });
            }

            ViewBag.Categories = new SelectList(_context.DocumentCategories.Where(c => c.CompanyId == companyId && c.Status == "Active"), "CategoryId", "CategoryName", model.CategoryId);
            ViewBag.Departments = new SelectList(_context.Departments.Where(d => d.CompanyId == companyId && d.Status == "Active"), "DeptId", "DeptName", model.DeptId);
            return View(model);
        }

        
        // ==========================================
        // 6. DELETE (Suspender)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!User.IsInRole("Admin de Empresa")) return RedirectToAction("AccessDenied", "Auth");

            var hasAdvanced = await _context.DocumentVersions.AnyAsync(v => v.DocId == id && v.StatusId != 1);
            if (hasAdvanced)
            {
                TempData["ErrorMessage"] = "Trazabilidad ISO: No se puede eliminar el registro maestro. El documento ya tiene historial de versiones. Por favor proceda a crear una nueva versión para hacerlo obsoleto.";
                return RedirectToAction(nameof(Index));
            }

            var companyId = GetCurrentCompanyId();
            var document = await _context.Documents
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.DocId == id && d.CompanyId == companyId);
            
            if (document != null)
            {
                document.Status = "Inactive";
                document.DeletedAt = DateTime.UtcNow;
                document.DeletedBy = GetCurrentUserId();

                _context.Update(document);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 7. REACTIVATE
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            if (!User.IsInRole("Admin de Empresa")) return RedirectToAction("AccessDenied", "Auth");

            var companyId = GetCurrentCompanyId();
            var document = await _context.Documents
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.DocId == id && d.CompanyId == companyId);

            if (document != null && document.Status != "Active")
            {
                document.Status = "Active";
                document.DeletedAt = null;
                document.DeletedBy = null;
                document.UpdatedAt = DateTime.UtcNow;
                document.UpdatedBy = GetCurrentUserId();

                _context.Update(document);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 8. DETAILS
        // ==========================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var companyId = GetCurrentCompanyId();

            var document = await _context.Documents
                .IgnoreQueryFilters()
                .Include(d => d.Category)
                    .ThenInclude(c => c.Norm)
                .Include(d => d.Department) 
                .FirstOrDefaultAsync(m => m.DocId == id && m.CompanyId == companyId);

            if (document == null) return NotFound();

            var currentUser = await _context.Users.FindAsync(GetCurrentUserId());
            ViewBag.UserDeptId = currentUser?.DeptId ?? 0;

            var versions = await _context.DocumentVersions
                .Include(v => v.DocumentStatus)
                .Include(v => v.Approvals) 
                    .ThenInclude(a => a.Approver) 
                // 🚀 NUEVO: Cargamos la bitácora inmutable y la ordenamos por fecha
                .Include(v => v.AuditLogs.OrderBy(log => log.CreatedAt))
                .Where(v => v.DocId == document.DocId)
                .OrderByDescending(v => v.VersionId)
                .ToListAsync();

            ViewBag.Versions = versions;

           // 🚀 MODIFICACIÓN: Extraemos los nombres de TODOS los involucrados (Creadores, Firmantes y Actores de Bitácora)
            var userIds = versions.Select(v => (int?)v.CreatedBy ?? 0)
                .Concat(versions.SelectMany(v => v.AuditLogs.Select(al => (int?)al.CreatedBy ?? 0)))
                .Concat(versions.SelectMany(v => v.Approvals.Select(a => (int?)a.ApproverId ?? 0)))
                .Where(uid => uid != 0).Distinct().ToList();

            var creatorsDict = await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.FullName);
            ViewBag.Creators = creatorsDict;

            return View(document);
        }

        // ==========================================
        // 9. NUEVA VERSIÓN: GET
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> NewVersion(int? id)
        {
            if (id == null) return NotFound();

            var isAdmin = User.IsInRole("Admin de Empresa");
            var isCreator = User.IsInRole("Creador de Doc");

            if (!isAdmin && !isCreator) return RedirectToAction("AccessDenied", "Auth");

            var companyId = GetCurrentCompanyId();
            
            var document = await _context.Documents
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.DocId == id && d.CompanyId == companyId);

            if (document == null) return NotFound();

            var currentUser = await _context.Users.FindAsync(GetCurrentUserId());
            if (!isAdmin && isCreator && document.DeptId != currentUser?.DeptId)
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            var lastVersion = await _context.DocumentVersions
                .Where(v => v.DocId == id)
                .OrderByDescending(v => v.VersionId)
                .FirstOrDefaultAsync();

            if (lastVersion != null && lastVersion.StatusId == 2)
            {
                TempData["ErrorMessage"] = "Bloqueo ISO: No se puede crear una nueva versión mientras el documento se encuentre 'En Revisión'. Por favor espere a su aprobación o solicite que sea rechazado para continuar.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var viewModel = new NewDocumentVersionViewModel
            {
                DocId = document.DocId,
                DocCode = document.DocCode,
                DocName = document.DocName
            };

            return View(viewModel);
        }

        // ==========================================
        // 10. NUEVA VERSIÓN: POST
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewVersion(NewDocumentVersionViewModel model)
        {
            var isAdmin = User.IsInRole("Admin de Empresa");
            var isCreator = User.IsInRole("Creador de Doc");

            if (!isAdmin && !isCreator) return RedirectToAction("AccessDenied", "Auth");

            var companyId = GetCurrentCompanyId();

            if (ModelState.IsValid)
            {
                var document = await _context.Documents
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(d => d.DocId == model.DocId && d.CompanyId == companyId);

                if (document == null) return NotFound();

                var currentUser = await _context.Users.FindAsync(GetCurrentUserId());
                if (!isAdmin && isCreator && document.DeptId != currentUser?.DeptId)
                {
                    return RedirectToAction("AccessDenied", "Auth");
                }

                var lastVersion = await _context.DocumentVersions
                    .Where(v => v.DocId == model.DocId)
                    .OrderByDescending(v => v.VersionId)
                    .FirstOrDefaultAsync();

                if (lastVersion != null && lastVersion.StatusId == 2)
                {
                    TempData["ErrorMessage"] = "Bloqueo ISO: Operación cancelada. El documento se encuentra 'En Revisión'.";
                    return RedirectToAction(nameof(Details), new { id = model.DocId });
                }

                string newVersionNum = "0.1"; // Por si no hubiera versión anterior
                if (lastVersion != null && !string.IsNullOrEmpty(lastVersion.VersionNum))
                {
                    if (decimal.TryParse(lastVersion.VersionNum, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal versionActual))
                    {
                        // LÓGICA DE VERSIONAMIENTO MAYOR PROFESIONAL (ISO ÁGIL)
                        // Extraemos el entero (Ej: 1.5 -> 1) y le sumamos 1 entero (1 + 1.0 = 2.0)
                        // El documento nacerá como un borrador de la siguiente versión mayor "2.0"
                        decimal nextMajorVersion = Math.Floor(versionActual) + 1.0m;
                        newVersionNum = nextMajorVersion.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "documents");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileExtension = Path.GetExtension(model.NewFile.FileName).ToLower();
                string uniqueFileName = $"{document.DocCode}_v{newVersionNum}_{Guid.NewGuid().ToString().Substring(0,8)}{fileExtension}";
                string filePathPhysical = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePathPhysical, FileMode.Create))
                {
                    await model.NewFile.CopyToAsync(fileStream);
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var newDocVersion = new DocumentVersion
                    {
                        DocId = model.DocId,
                        StatusId = 1, 
                        VersionNum = newVersionNum,
                        FilePath = $"/uploads/documents/{uniqueFileName}",
                        Extension = fileExtension,
                        ChangeDescription = model.ChangeDescription,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = GetCurrentUserId(),
                        Status = "Active"
                    };

                    _context.DocumentVersions.Add(newDocVersion);

                    document.UpdatedAt = DateTime.UtcNow;
                    document.UpdatedBy = GetCurrentUserId();
                    _context.Update(document);

                    await _context.SaveChangesAsync(); // Genera el VersionId

                    // 🚀 NUEVO: Registro en la Bitácora Inmutable
                    var auditLog = new DocumentAuditLog {
                        CompanyId = companyId,
                        DocId = model.DocId,
                        VersionId = newDocVersion.VersionId,
                        ActionType = "NewVersionCreated", // 🚀 FIX: TIPO DE ACCIÓN CORRECTO
                        ActionDetails = "Se ha subido una nueva versión del documento para iniciar un nuevo ciclo.",
                        VersionNum = newVersionNum, 
                        CreatedBy = GetCurrentUserId(),
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.DocumentAuditLogs.Add(auditLog);
                    await _context.SaveChangesAsync();
                    
                    await transaction.CommitAsync();

                    return RedirectToAction(nameof(Details), new { id = model.DocId });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();

                    if (System.IO.File.Exists(filePathPhysical))
                    {
                        System.IO.File.Delete(filePathPhysical);
                    }

                    ModelState.AddModelError("", "Ocurrió un error al guardar la nueva versión.");
                }
            }

            return View(model);
        }

        // ==========================================
        // 11. ENVIAR A REVISIÓN
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendToReview(int versionId, int docId)
        {
            var isAdmin = User.IsInRole("Admin de Empresa");
            var isCreator = User.IsInRole("Creador de Doc");

            if (!isAdmin && !isCreator) return RedirectToAction("AccessDenied", "Auth");

            var companyId = GetCurrentCompanyId();
            var version = await _context.DocumentVersions
                .Include(v => v.Document) 
                .FirstOrDefaultAsync(v => v.VersionId == versionId && v.DocId == docId);

            if (version == null || version.StatusId != 1 || version.Document == null)
            {
                TempData["ErrorMessage"] = "La versión no es válida o ya se encuentra en revisión.";
                return RedirectToAction(nameof(Details), new { id = docId });
            }

            var currentUser = await _context.Users.FindAsync(GetCurrentUserId());
            if (!isAdmin && isCreator && version.Document.DeptId != currentUser?.DeptId)
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            var revisorRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "Revisor" && r.Status == "Active");

            if (revisorRole == null)
            {
                TempData["ErrorMessage"] = "Error de sistema: No existe el rol 'Revisor'.";
                return RedirectToAction(nameof(Details), new { id = docId });
            }

            var assignedUser = await _context.Users
                .FirstOrDefaultAsync(u => u.RoleId == revisorRole.RoleId 
                                       && u.Status == "Active" 
                                       && u.CompanyId == companyId
                                       && u.DeptId == version.Document.DeptId);

            if (assignedUser == null)
            {
                TempData["ErrorMessage"] = "No hay ningún usuario con el rol 'Revisor' asignado a este departamento.";
                return RedirectToAction(nameof(Details), new { id = docId });
            }

            // 🚀 Creación de la aprobación explícita alineada a los nuevos constraints
            var approval = new DocumentApproval
            {
                VersionId = version.VersionId,
                StepOrder = 1,
                StepType = "Revisó", 
                ApproverId = assignedUser.UserId, 
                ApprovalStatus = "Pending", 
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = GetCurrentUserId()
            };
            
            _context.DocumentApprovals.Add(approval);
            version.StatusId = 2;
            version.UpdatedAt = DateTime.UtcNow;
            version.UpdatedBy = GetCurrentUserId();
            _context.Update(version);

            // 🚀 NUEVO: Registro en la Bitácora Inmutable
            var auditLog = new DocumentAuditLog {
                CompanyId = companyId,
                DocId = docId,
                VersionId = versionId,
                ActionType = "SentToReview",
                ActionDetails = "El creador finalizó el borrador y solicitó revisión formal en sistema.",
                VersionNum = version.VersionNum,
                CreatedBy = GetCurrentUserId(),
                CreatedAt = DateTime.UtcNow
            };
            _context.DocumentAuditLogs.Add(auditLog);

            // 🛡️ MODIFICACIÓN: Bloque Try-Catch para atrapar violaciones...
            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "El documento fue enviado al Revisor de su departamento.";
            }
            catch (DbUpdateException)
            {
                // Si SQL rechaza el StepType o ApprovalStatus, EF Core lanza esta excepción
                TempData["ErrorMessage"] = "Error de integridad referencial o de validación estricta en la base de datos. Contacte al administrador.";
            }

            return RedirectToAction(nameof(Details), new { id = docId });
        }

        // ==========================================
        // 12. REEMPLAZAR ARCHIVO DE UN BORRADOR (NUEVO)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplaceDraftFile(int versionId, int docId, IFormFile newFile)
        {
            var isAdmin = User.IsInRole("Admin de Empresa");
            var isCreator = User.IsInRole("Creador de Doc");

            if (!isAdmin && !isCreator) return RedirectToAction("AccessDenied", "Auth");

            var companyId = GetCurrentCompanyId();
            var version = await _context.DocumentVersions
                .Include(v => v.Document)
                .FirstOrDefaultAsync(v => v.VersionId == versionId && v.DocId == docId && v.Document.CompanyId == companyId);
            
            if (version == null || version.StatusId != 1)
            {
                TempData["ErrorMessage"] = "Solo se pueden reemplazar archivos de versiones en estatus Borrador.";
                return RedirectToAction(nameof(Details), new { id = docId });
            }

            if (newFile != null && newFile.Length > 0)
            {
                // 🚀 LÓGICA DE SUMAR DECIMAL AL REEMPLAZAR ARCHIVO
                decimal versionActual = 0.0m;
                decimal.TryParse(version.VersionNum, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out versionActual);
                
                // Sumamos 0.1
                version.VersionNum = (versionActual + 0.1m).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "documents");
                string fileExtension = Path.GetExtension(newFile.FileName).ToLower();
                
                string uniqueFileName = $"{version.Document.DocCode}_v{version.VersionNum}_corregido_{Guid.NewGuid().ToString().Substring(0,8)}{fileExtension}";
                string filePathPhysical = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePathPhysical, FileMode.Create))
                {
                    await newFile.CopyToAsync(fileStream);
                }

                if (!string.IsNullOrEmpty(version.FilePath))
                {
                    string oldPathPhysical = _env.WebRootPath + version.FilePath.Replace("/", "\\");
                    if (System.IO.File.Exists(oldPathPhysical))
                    {
                        System.IO.File.Delete(oldPathPhysical);
                    }
                }

                version.FilePath = $"/uploads/documents/{uniqueFileName}";
                version.Extension = fileExtension;
                version.UpdatedAt = DateTime.UtcNow;
                version.UpdatedBy = GetCurrentUserId();

                _context.Update(version);

                // 🚀 NUEVO: Registro en la Bitácora Inmutable
                var auditLog = new DocumentAuditLog {
                    CompanyId = companyId,
                    DocId = docId,
                    VersionId = versionId,
                    ActionType = "DraftEdited",
                    ActionDetails = "El usuario modificó los archivos PDF del borrador o actualizó sus metadatos estructurales.",
                    VersionNum = version.VersionNum,
                    CreatedBy = GetCurrentUserId(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.DocumentAuditLogs.Add(auditLog);

                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "El archivo PDF fue actualizado y la versión aumentó. No olvides dar clic en 'Solicitar Firmas'.";
            }
            else
            {
                TempData["ErrorMessage"] = "Debes seleccionar un archivo PDF válido.";
            }

            return RedirectToAction(nameof(Details), new { id = docId });
        }

        // ==========================================
        // 13. DESHACER ENVÍO / RECALL WORKFLOW (NUEVO)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecallWorkflow(int versionId, int docId)
        {
            var userId = GetCurrentUserId();

            try
            {
                // 1. OBTENEMOS LA VERSIÓN ANTES DE DESHACER (Con Include)
                var versionData = await _context.DocumentVersions
                    .Include(v => v.Document) // 🚀 NUEVO: Necesario para sacar el CompanyId
                    .FirstOrDefaultAsync(v => v.VersionId == versionId);

                if (versionData == null) return NotFound();

                // // 2. LÓGICA DE VERSIONAMIENTO (CEREBRO EN C#)
                // // Al deshacer el envío, sumamos 0.1 para que quede rastro en el historial
                // decimal versionActual = 0.0m;
                // decimal.TryParse(versionData.VersionNum, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out versionActual);
                
                // string newVersionNum = (versionActual + 0.1m).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                string newVersionNum = versionData.VersionNum;
                // 3. EJECUTAMOS EL SP PASANDO EL NUEVO NÚMERO DE VERSIÓN
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_RecallDocumentWorkflow @VersionID = {0}, @ApprovalID = NULL, @UserID = {1}, @NewVersionNum = {2}",
                    versionId, userId, newVersionNum
                );

                // 🚀 NUEVO: Registro en la Bitácora Inmutable
                var auditLog = new DocumentAuditLog {
                    CompanyId = versionData.Document.CompanyId, 
                    DocId = docId,
                    VersionId = versionId,
                    ActionType = "Recalled",
                    ActionDetails = "El creador canceló el proceso de revisión activa. El documento regresó a Borrador.",
                    VersionNum = newVersionNum,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.DocumentAuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Flujo cancelado con éxito. El documento regresó a Borrador bajo la versión {newVersionNum}.";
            }
            catch (Exception ex)
            {
                // Extraemos el mensaje exacto enviado por el THROW de SQL Server (Ej: Error 50005 de Revisor ya firmó)
                string errorReal = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = "Operación rechazada por el sistema: " + errorReal;
            }

            // Redirigimos al expediente del documento para refrescar la vista
            return RedirectToAction(nameof(Details), new { id = docId });
        }

        private bool DocumentExists(int id)
        {
            var companyId = GetCurrentCompanyId();
            return _context.Documents.IgnoreQueryFilters().Any(e => e.DocId == id && e.CompanyId == companyId);
        }
    }
}