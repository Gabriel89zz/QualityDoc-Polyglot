using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityDoc.API.Data;
using QualityDoc.API.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System;
using System.Linq;
using System.Threading.Tasks;
using QualityDoc.API.Helpers;

namespace QualityDoc.API.Controllers
{
    // 🛡️ ACCESO RESTRINGIDO: Solo el dueño del software entra aquí
    [Authorize(Roles = "Super Admin")]
    public class CompaniesController : Controller
    {
        private readonly QualityDocDbContext _context;

        public CompaniesController(QualityDocDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // HELPERS PARA OBTENER DATOS DEL SUPERADMIN
        // ==========================================
        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1");
        private int CurrentCompanyId => int.Parse(User.FindFirstValue("CompanyId") ?? "0");

      // 1. GET: /Companies (Paginado y Filtrado)
        public async Task<IActionResult> Index(string search, string status, int? pageNumber)
        {
            // Guardamos los valores actuales para que la vista no pierda el texto al recargar
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentStatus"] = status;

            int pageSize = 10;
            var query = _context.Companies.IgnoreQueryFilters().AsQueryable();

            // 🔍 Filtro por texto libre (Razón Social o RFC)
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.LegalName.Contains(search) || c.TaxId.Contains(search));
            }

            // 🏷️ Filtro por Estado de la empresa
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status == status);
            }

            query = query.OrderByDescending(c => c.CreatedAt);

            return View(await PaginatedList<Company>.CreateAsync(query, pageNumber ?? 1, pageSize));
        }

        // 2. GET: /Companies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // 👁️ CORRECCIÓN: Usamos "Filtered Include" para traer SOLO a los Administradores de Empresa[cite: 11]
            var company = await _context.Companies
                .IgnoreQueryFilters()
                .Include(c => c.CreatedByNavigation) 
                .Include(c => c.UpdatedByNavigation)
                .Include(c => c.DeletedByNavigation) // Para saber quién suspendió la cuenta[cite: 11]
                .Include(c => c.Users.Where(u => u.Role!.RoleName == "Admin de Empresa")) // 🚀 El filtro mágico
                    .ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(m => m.CompanyId == id);

            if (company == null) return NotFound();

            return View(company);
        }

        // 3. GET: /Companies/Create
        public IActionResult Create()
        {
            return View();
        }

        // 4. POST: /Companies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LegalName,TaxId")] Company company)
        {
            if (ModelState.IsValid)
            {
                // Validación: Evitar RFC/TaxId duplicados
                if (await _context.Companies.AnyAsync(c => c.TaxId == company.TaxId))
                {
                    ModelState.AddModelError("TaxId", "Este RFC / Tax ID ya está registrado en otra empresa.");
                    return View(company);
                }

                // 📝 RASTRO DE AUDITORÍA AUTOMÁTICO
                company.Status = "Active";
                company.CreatedAt = DateTime.UtcNow;
                company.CreatedBy = CurrentUserId;

                _context.Add(company);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(company);
        }

        // 5. GET: /Companies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Usamos IgnoreQueryFilters() para que encuentre a las inactivas
            var company = await _context.Companies
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.CompanyId == id);

            if (company == null) return NotFound();

            return View(company);
        }

        // 6. POST: /Companies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CompanyId,LegalName,TaxId,Status")] Company company)
        {
            if (id != company.CompanyId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Validación de RFC duplicado (excluyendo la empresa actual)
                    if (await _context.Companies.AnyAsync(c => c.TaxId == company.TaxId && c.CompanyId != id))
                    {
                        ModelState.AddModelError("TaxId", "Este RFC / Tax ID ya pertenece a otra empresa.");
                        return View(company);
                    }

                    // 📝 RASTRO DE AUDITORÍA AUTOMÁTICO
                    company.UpdatedAt = DateTime.UtcNow;
                    company.UpdatedBy = CurrentUserId;

                    _context.Update(company);
                    
                    // Protegemos los campos de creación para que EF no los sobreescriba con nulos
                    _context.Entry(company).Property(x => x.CreatedAt).IsModified = false;
                    _context.Entry(company).Property(x => x.CreatedBy).IsModified = false;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CompanyExists(company.CompanyId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(company);
        }

        // 7. GET: /Companies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            // 🛡️ ESCUDO INTELIGENTE: Si el Super Admin tiene una empresa asignada (diferente de 0), 
            // no lo deja borrar su propia casa. Quitamos la regla obsoleta del ID 1.
            if (CurrentCompanyId != 0 && id == CurrentCompanyId)
            {
                TempData["ErrorMessage"] = "🛑 Escudo de Seguridad: No puedes suspender tu propia empresa.";
                return RedirectToAction(nameof(Index));
            }

            // 👁️ Ignorar filtros por si le dan clic por error a una inactiva
            var company = await _context.Companies
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.CompanyId == id);

            if (company == null) return NotFound();

            return View(company);
        }

        // 8. POST: /Companies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // 🛡️ ESCUDO INTELIGENTE (Validación en el POST por seguridad)
            if (CurrentCompanyId != 0 && id == CurrentCompanyId)
            {
                TempData["ErrorMessage"] = "🛑 Escudo de Seguridad: No puedes suspender tu propia empresa.";
                return RedirectToAction(nameof(Index));
            }

            try 
            {
                // 🚀 EJECUCIÓN CON BLINDAJE
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_DisableCompanyComplete @CompanyID = {0}, @AdminUserID = {1}", 
                    id, CurrentUserId);
                
                TempData["SuccessMessage"] = "Empresa suspendida correctamente junto con todos sus usuarios y documentos.";
            }
            catch (Exception ex)
            {
                // Si llegara a fallar SQL por alguna llave foránea (FK), te lo dirá aquí con la alerta que configuramos
                string errorReal = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = "Error en base de datos al suspender: " + errorReal;
            }
            
            return RedirectToAction(nameof(Index));
        }

        // 9. POST: /Companies/Reactivate/5
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Reactivate(int id)
{
    // Buscamos la empresa incluso si está inactiva
    var company = await _context.Companies
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(c => c.CompanyId == id);

    if (company != null && company.Status != "Active")
    {
        // 🚀 Ejecutamos tu Stored Procedure anti-zombies
        await _context.Database.ExecuteSqlRawAsync(
            "EXEC sp_EnableCompanyComplete @CompanyID = {0}, @AdminUserID = {1}", 
            id, CurrentUserId);
    }
    
    return RedirectToAction(nameof(Index));
}

        private bool CompanyExists(int id)
        {
            // Agregamos IgnoreQueryFilters() aquí también
            return _context.Companies.IgnoreQueryFilters().Any(e => e.CompanyId == id);
        }
    }
}