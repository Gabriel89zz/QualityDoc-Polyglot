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
        public async Task<IActionResult> Index()
        {
            var companyId = GetCurrentCompanyId();
            var currentUserId = GetCurrentUserId();

            var currentUser = await _context.Users.FindAsync(currentUserId);
            ViewBag.UserDeptId = currentUser?.DeptId ?? 0;

            var documents = await _context.Documents
                .IgnoreQueryFilters()
                .Include(d => d.Category)
                .Include(d => d.Department)
                .Where(d => d.CompanyId == companyId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

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

            return View(documents);
        }

        // ==========================================
        // 2. CREATE: GET
        // ==========================================
        public IActionResult Create()
        {
            if (!User.IsInRole("Admin de Empresa") && !User.IsInRole("Creador de Doc"))
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            var companyId = GetCurrentCompanyId();

            ViewBag.Categories = new SelectList(_context.DocumentCategories.Where(c => c.CompanyId == companyId && c.Status == "Active"), "CategoryId", "CategoryName");
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
            if (!User.IsInRole("Admin de Empresa") && !User.IsInRole("Creador de Doc"))
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            var companyId = GetCurrentCompanyId();

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
                
                var docCount = await _context.Documents
                    .IgnoreQueryFilters()
                    .Where(d => d.CompanyId == companyId && d.CategoryId == model.CategoryId)
                    .CountAsync();
                
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
                string uniqueFileName = $"{folioCode}_v1.0_{Guid.NewGuid().ToString().Substring(0,8)}{fileExtension}";
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
                        VersionNum = "1.0",
                        FilePath = $"/uploads/documents/{uniqueFileName}",
                        Extension = fileExtension,
                        ChangeDescription = "Creación inicial del documento",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = GetCurrentUserId(),
                        Status = "Active"
                    };

                    _context.DocumentVersions.Add(docVersion);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    if (System.IO.File.Exists(filePathPhysical))
                    {
                        System.IO.File.Delete(filePathPhysical);
                    }
                    ModelState.AddModelError("", "Ocurrió un error al guardar en la base de datos.");
                }
            }

            ViewBag.Categories = new SelectList(_context.DocumentCategories.Where(c => c.CompanyId == companyId && c.Status == "Active"), "CategoryId", "CategoryName", model.CategoryId);
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
        // 🚀 Agregamos el parámetro newFile a la firma del método
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

                    // 1. Actualizamos los metadatos (ignoramos DeptId y CategoryId para proteger el folio)
                    existingDoc.DocName = model.DocName;
                    existingDoc.Description = model.Description;
                    existingDoc.IsExternal = model.IsExternal;
                    existingDoc.UpdatedAt = DateTime.UtcNow;
                    existingDoc.UpdatedBy = GetCurrentUserId();

                    _context.Update(existingDoc);

                    // 🚀 2. Si subieron un archivo nuevo, lo reemplazamos
                    if (newFile != null && newFile.Length > 0 && latestVersion != null)
                    {
                        string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "documents");
                        string fileExtension = Path.GetExtension(newFile.FileName).ToLower();
                        
                        string uniqueFileName = $"{existingDoc.DocCode}_v{latestVersion.VersionNum}_corregido_{Guid.NewGuid().ToString().Substring(0,8)}{fileExtension}";
                        string filePathPhysical = Path.Combine(uploadsFolder, uniqueFileName);

                        // Subimos el nuevo archivo
                        using (var fileStream = new FileStream(filePathPhysical, FileMode.Create))
                        {
                            await newFile.CopyToAsync(fileStream);
                        }

                        // Eliminamos el archivo PDF viejo
                        if (!string.IsNullOrEmpty(latestVersion.FilePath))
                        {
                            string oldPathPhysical = _env.WebRootPath + latestVersion.FilePath.Replace("/", "\\");
                            if (System.IO.File.Exists(oldPathPhysical))
                            {
                                System.IO.File.Delete(oldPathPhysical);
                            }
                        }

                        // Actualizamos la ruta en la base de datos
                        latestVersion.FilePath = $"/uploads/documents/{uniqueFileName}";
                        latestVersion.Extension = fileExtension;
                        latestVersion.UpdatedAt = DateTime.UtcNow;
                        latestVersion.UpdatedBy = GetCurrentUserId();

                        _context.Update(latestVersion);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    TempData["SuccessMessage"] = "El documento ha sido actualizado correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    await transaction.RollbackAsync();
                    if (!DocumentExists(model.DocId)) return NotFound();
                    else throw;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Ocurrió un error al procesar el archivo o la base de datos.";
                    return RedirectToAction(nameof(Edit), new { id = model.DocId });
                }
                
                // Redirigimos al expediente para que vea los cambios
                return RedirectToAction(nameof(Details), new { id = model.DocId });
            }

            // Si hay error de validación, volvemos a cargar la vista
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
                .Include(d => d.Department) 
                .FirstOrDefaultAsync(m => m.DocId == id && m.CompanyId == companyId);

            if (document == null) return NotFound();

            var currentUser = await _context.Users.FindAsync(GetCurrentUserId());
            ViewBag.UserDeptId = currentUser?.DeptId ?? 0;

            var versions = await _context.DocumentVersions
                .Include(v => v.DocumentStatus)
                .Include(v => v.Approvals) 
                    .ThenInclude(a => a.Approver) 
                .Where(v => v.DocId == document.DocId)
                .OrderByDescending(v => v.VersionId)
                .ToListAsync();

            ViewBag.Versions = versions;

            // 🚀 MODIFICACIÓN: Extraemos los nombres de los creadores para la bitácora
            // Convertimos a int? para evitar errores si CreatedBy es nulo o 0, y extraemos los IDs únicos
            var creatorIds = versions.Select(v => (int?)v.CreatedBy ?? 0).Where(uid => uid != 0).Distinct().ToList();
            
            var creatorsDict = await _context.Users
                .Where(u => creatorIds.Contains(u.UserId))
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

                string newVersionNum = "1.0"; 
                
                if (lastVersion != null && !string.IsNullOrEmpty(lastVersion.VersionNum))
                {
                    var parts = lastVersion.VersionNum.Split('.');
                    if (int.TryParse(parts[0], out int majorVersion))
                    {
                        newVersionNum = $"{majorVersion + 1}.0";
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

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "El documento fue enviado al Revisor de su departamento.";
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

            // Verificamos que sí sea un borrador
            if (version == null || version.StatusId != 1)
            {
                TempData["ErrorMessage"] = "Solo se pueden reemplazar archivos de versiones en estatus Borrador.";
                return RedirectToAction(nameof(Details), new { id = docId });
            }

            if (newFile != null && newFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "documents");
                string fileExtension = Path.GetExtension(newFile.FileName).ToLower();
                
                // Creamos un nuevo nombre para evitar problemas de caché
                string uniqueFileName = $"{version.Document.DocCode}_v{version.VersionNum}_corregido_{Guid.NewGuid().ToString().Substring(0,8)}{fileExtension}";
                string filePathPhysical = Path.Combine(uploadsFolder, uniqueFileName);

                // Subimos el nuevo archivo
                using (var fileStream = new FileStream(filePathPhysical, FileMode.Create))
                {
                    await newFile.CopyToAsync(fileStream);
                }

                // Eliminamos el archivo PDF viejo del servidor
                if (!string.IsNullOrEmpty(version.FilePath))
                {
                    string oldPathPhysical = _env.WebRootPath + version.FilePath.Replace("/", "\\");
                    if (System.IO.File.Exists(oldPathPhysical))
                    {
                        System.IO.File.Delete(oldPathPhysical);
                    }
                }

                // Actualizamos la base de datos
                version.FilePath = $"/uploads/documents/{uniqueFileName}";
                version.Extension = fileExtension;
                version.UpdatedAt = DateTime.UtcNow;
                version.UpdatedBy = GetCurrentUserId();

                _context.Update(version);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "El archivo PDF fue actualizado correctamente. No olvides dar clic en 'Solicitar Firmas' para reiniciar el flujo.";
            }
            else
            {
                TempData["ErrorMessage"] = "Debes seleccionar un archivo PDF válido.";
            }

            return RedirectToAction(nameof(Details), new { id = docId });
        }

        private bool DocumentExists(int id)
        {
            var companyId = GetCurrentCompanyId();
            return _context.Documents.IgnoreQueryFilters().Any(e => e.DocId == id && e.CompanyId == companyId);
        }
    }
}