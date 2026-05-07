using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using QualityDoc.API.Data;
using QualityDoc.API.Models;

namespace QualityDoc.API.Controllers
{
    [Authorize] // 🛡️ Protección total
    public class DocumentCategoriesController : Controller
    {
        private readonly QualityDocDbContext _context;

        public DocumentCategoriesController(QualityDocDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // UTILIDADES (Obtener datos de sesión)
        // ==========================================
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
        // 1. INDEX: Listar las categorías de SU empresa
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var companyId = GetCurrentCompanyId();

            var categories = await _context.DocumentCategories
                .IgnoreQueryFilters()
                .Include(c => c.Norm) // Traemos la info de la norma (si aplica)
                .Where(c => c.CompanyId == companyId)
                .OrderBy(c => c.HierarchyLevel) // Ordenamos por la pirámide documental
                .ToListAsync();

            return View(categories);
        }

        // ==========================================
        // 2. CREATE: GET
        // ==========================================
        public IActionResult Create()
        {
            // Mandamos las normas activas a un ViewBag por si quieren enlazarla
            ViewBag.Norms = new SelectList(_context.Norms.Where(n => n.Status == "Active"), "NormId", "NormName");
            return View();
        }

        // ==========================================
        // 3. CREATE: POST
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DocumentCategory model)
        {
            if (ModelState.IsValid)
            {
                // Forzamos la auditoría y el multi-tenant
                model.CompanyId = GetCurrentCompanyId();
                model.Status = "Active";
                model.CreatedAt = DateTime.UtcNow;
                model.CreatedBy = GetCurrentUserId();

                // Aseguramos que los prefijos estén en mayúsculas (Ej: pro -> PRO)
                if (!string.IsNullOrEmpty(model.Prefix))
                {
                    model.Prefix = model.Prefix.ToUpper();
                }

                _context.DocumentCategories.Add(model);
                await _context.SaveChangesAsync();
                
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Norms = new SelectList(_context.Norms.Where(n => n.Status == "Active"), "NormId", "NormName", model.NormId);
            return View(model);
        }

        // ==========================================
        // 4. EDIT: GET
        // ==========================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var companyId = GetCurrentCompanyId();
            
            // 🛡️ Filtramos por ID y por CompanyId
            var category = await _context.DocumentCategories
                .IgnoreQueryFilters() // Agregado por si quieren editar una inactiva
                .FirstOrDefaultAsync(c => c.CategoryId == id && c.CompanyId == companyId);

            if (category == null) return NotFound();

            ViewBag.Norms = new SelectList(_context.Norms.Where(n => n.Status == "Active"), "NormId", "NormName", category.NormId);
            return View(category);
        }

        // ==========================================
        // 5. EDIT: POST
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DocumentCategory model)
        {
            if (id != model.CategoryId) return NotFound();

            var companyId = GetCurrentCompanyId();

            if (ModelState.IsValid)
            {
                try
                {
                    // 🛡️ Verificamos que realmente le pertenezca a la empresa
                    var existingCategory = await _context.DocumentCategories
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(c => c.CategoryId == id && c.CompanyId == companyId);

                    if (existingCategory == null) return NotFound();

                    // Actualizamos solo los campos permitidos
                    existingCategory.CategoryName = model.CategoryName;
                    existingCategory.Prefix = model.Prefix?.ToUpper();
                    existingCategory.Description = model.Description;
                    existingCategory.HierarchyLevel = model.HierarchyLevel;
                    existingCategory.NormId = model.NormId;
                    
                    existingCategory.UpdatedAt = DateTime.UtcNow;
                    existingCategory.UpdatedBy = GetCurrentUserId();

                    _context.Update(existingCategory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(model.CategoryId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Norms = new SelectList(_context.Norms.Where(n => n.Status == "Active"), "NormId", "NormName", model.NormId);
            return View(model);
        }

        // ==========================================
        // 6. DELETE (Soft Delete): POST (BOTÓN DIRECTO)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var companyId = GetCurrentCompanyId();
            
            var category = await _context.DocumentCategories
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.CategoryId == id && c.CompanyId == companyId);
            
            if (category != null)
            {
                // 🛡️ REGLA DE NEGOCIO: Evitar borrar si hay documentos activos amarrados a esta categoría
                var hasActiveDocuments = await _context.Documents.AnyAsync(d => d.CategoryId == id && d.Status == "Active");
                if (hasActiveDocuments)
                {
                    TempData["ErrorMessage"] = "No puedes suspender esta categoría porque tiene documentos activos vinculados.";
                    return RedirectToAction(nameof(Index));
                }

                // Soft Delete de acuerdo a las reglas ISO para mantener trazabilidad
                category.Status = "Inactive";
                category.DeletedAt = DateTime.UtcNow;
                category.DeletedBy = GetCurrentUserId();

                _context.Update(category);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 7. REACTIVATE: POST (NUEVO MÉTODO)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            var companyId = GetCurrentCompanyId();

            var category = await _context.DocumentCategories
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.CategoryId == id && c.CompanyId == companyId);

            if (category != null && category.Status != "Active")
            {
                category.Status = "Active";
                category.DeletedAt = null;
                category.DeletedBy = null;
                category.UpdatedAt = DateTime.UtcNow;
                category.UpdatedBy = GetCurrentUserId();

                _context.Update(category);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 8. DETAILS: GET
        // ==========================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var companyId = GetCurrentCompanyId();

            var category = await _context.DocumentCategories
                .IgnoreQueryFilters()
                .Include(c => c.Norm)
                .Include(c => c.CreatedByNavigation) 
                .Include(c => c.UpdatedByNavigation)
                .Include(c => c.DeletedByNavigation) // Agregado para ver quién la suspendió en la vista de detalles
                .FirstOrDefaultAsync(m => m.CategoryId == id && m.CompanyId == companyId);

            if (category == null) return NotFound();

            return View(category);
        }

        private bool CategoryExists(int id)
        {
            var companyId = GetCurrentCompanyId();
            return _context.DocumentCategories.IgnoreQueryFilters().Any(e => e.CategoryId == id && e.CompanyId == companyId);
        }
    }
}