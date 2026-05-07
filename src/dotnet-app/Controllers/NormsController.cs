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
    // 🛡️ ACCESO RESTRINGIDO: Solo tú (Super Admin) gestionas el catálogo global de normas
    [Authorize(Roles = "Super Admin")]
    public class NormsController : Controller
    {
        private readonly QualityDocDbContext _context;

        public NormsController(QualityDocDbContext context)
        {
            _context = context;
        }

        // Helper para sacar tu ID de usuario logueado
        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1");

        // 1. GET: /Norms
        public async Task<IActionResult> Index()
        {
            // Traemos TODAS las normas (Activas e Inactivas)
            var norms = await _context.Norms
                .IgnoreQueryFilters()
                .OrderBy(n => n.NormName)
                .ThenByDescending(n => n.ReleaseYear) // Las más recientes primero
                .ToListAsync();

            return View(norms);
        }

        // 2. GET: /Norms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var norm = await _context.Norms
                .IgnoreQueryFilters()
                .Include(n => n.CreatedByNavigation)
                .Include(n => n.UpdatedByNavigation)
                .Include(n => n.DeletedByNavigation)
                .FirstOrDefaultAsync(m => m.NormId == id);

            if (norm == null) return NotFound();

            return View(norm);
        }

        // 3. GET: /Norms/Create
        public IActionResult Create()
        {
            return View();
        }

        // 4. POST: /Norms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // 🚀 INCLUIMOS EL ReleaseYear EN EL BIND
        public async Task<IActionResult> Create([Bind("NormName,ReleaseYear,Description")] Norm norm)
        {
            if (ModelState.IsValid)
            {
                // Validación Anti-Duplicados: No puedes registrar la misma norma y el mismo año dos veces
                if (await _context.Norms.AnyAsync(n => n.NormName == norm.NormName && n.ReleaseYear == norm.ReleaseYear))
                {
                    ModelState.AddModelError("NormName", "Esta normativa y versión ya están registradas en el catálogo.");
                    return View(norm);
                }

                // 📝 RASTRO DE AUDITORÍA
                norm.Status = "Active";
                norm.CreatedAt = DateTime.UtcNow;
                norm.CreatedBy = CurrentUserId;

                _context.Add(norm);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(norm);
        }

        // 5. GET: /Norms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var norm = await _context.Norms
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(n => n.NormId == id);

            if (norm == null) return NotFound();

            return View(norm);
        }

        // 6. POST: /Norms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("NormId,NormName,ReleaseYear,Description,Status")] Norm norm)
        {
            if (id != norm.NormId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Validación de duplicados excluyendo la norma actual
                    if (await _context.Norms.AnyAsync(n => n.NormName == norm.NormName && n.ReleaseYear == norm.ReleaseYear && n.NormId != id))
                    {
                        ModelState.AddModelError("NormName", "Otra normativa ya utiliza este nombre y versión.");
                        return View(norm);
                    }

                    var previousState = await _context.Norms.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(n => n.NormId == id);

                    // 📝 RASTRO DE AUDITORÍA Y PROTECCIÓN DE DATOS ORIGEN
                    norm.UpdatedAt = DateTime.UtcNow;
                    norm.UpdatedBy = CurrentUserId;
                    norm.CreatedAt = previousState!.CreatedAt;
                    norm.CreatedBy = previousState.CreatedBy;

                    _context.Update(norm);
                    _context.Entry(norm).Property(x => x.CreatedAt).IsModified = false;
                    _context.Entry(norm).Property(x => x.CreatedBy).IsModified = false;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NormExists(norm.NormId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(norm);
        }

        // 7. GET: /Norms/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var norm = await _context.Norms
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.NormId == id);

            if (norm == null) return NotFound();

            return View(norm);
        }

        // 8. POST: /Norms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var norm = await _context.Norms.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.NormId == id);
            
            if (norm != null)
            {
                // Soft Delete estándar: Apagamos el registro y guardamos quién lo mató
                norm.Status = "Inactive";
                norm.DeletedAt = DateTime.UtcNow;
                norm.DeletedBy = CurrentUserId;
                
                _context.Update(norm);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // 9. 🚀 NUEVO POST: /Norms/Reactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            var norm = await _context.Norms.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.NormId == id);

            if (norm != null && norm.Status != "Active")
            {
                // Limpiamos la basura del soft delete y lo volvemos a la vida
                norm.Status = "Active";
                norm.DeletedAt = null;
                norm.DeletedBy = null;
                norm.UpdatedAt = DateTime.UtcNow;
                norm.UpdatedBy = CurrentUserId;

                _context.Update(norm);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool NormExists(int id)
        {
            return _context.Norms.IgnoreQueryFilters().Any(e => e.NormId == id);
        }
    }
}