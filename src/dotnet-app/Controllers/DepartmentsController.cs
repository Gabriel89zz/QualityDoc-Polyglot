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
    // 🛡️ ACCESO RESTRINGIDO: Solo usuarios logueados (Cualquier rol operativo de empresa)
    [Authorize]
    public class DepartmentsController : Controller
    {
        private readonly QualityDocDbContext _context;

        public DepartmentsController(QualityDocDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // HELPERS MULTI-TENANT Y AUDITORÍA
        // ==========================================
        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        private int CurrentCompanyId => int.Parse(User.FindFirstValue("CompanyId") ?? "0");

        // 1. GET: /Departments
        public async Task<IActionResult> Index()
        {
            // 🔒 FILTRO MULTI-TENANT: Solo los departamentos de mi empresa
            var departments = await _context.Departments
                .IgnoreQueryFilters()
                .Where(d => d.CompanyId == CurrentCompanyId)
                .OrderBy(d => d.DeptName)
                .ToListAsync();

            return View(departments);
        }

        // 2. GET: /Departments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.Departments
                .IgnoreQueryFilters()
                .Include(d => d.CreatedByNavigation)
                .Include(d => d.UpdatedByNavigation)
                .FirstOrDefaultAsync(m => m.DeptId == id && m.CompanyId == CurrentCompanyId);

            if (department == null) return NotFound();

            return View(department);
        }

        // 3. GET: /Departments/Create
        public IActionResult Create()
        {
            return View();
        }

        // 4. POST: /Departments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DeptName")] Department department)
        {
            if (ModelState.IsValid)
            {
                // Validación para no repetir el nombre en la misma empresa
                if (await _context.Departments.AnyAsync(d => d.DeptName == department.DeptName && d.CompanyId == CurrentCompanyId))
                {
                    ModelState.AddModelError("DeptName", "Ya existe un departamento con este nombre en tu empresa.");
                    return View(department);
                }

                // 🔒 ASIGNACIÓN MULTI-TENANT AUTOMÁTICA
                department.CompanyId = CurrentCompanyId;
                
                // 📝 AUDITORÍA
                department.Status = "Active";
                department.CreatedAt = DateTime.UtcNow;
                department.CreatedBy = CurrentUserId;

                _context.Add(department);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        // 5. GET: /Departments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Validamos que sea de la empresa actual
            var department = await _context.Departments
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.DeptId == id && d.CompanyId == CurrentCompanyId);
                
            if (department == null) return NotFound();

            return View(department);
        }

        // 6. POST: /Departments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DeptId,DeptName,Status")] Department department)
        {
            if (id != department.DeptId) return NotFound();

            // Verificamos que el departamento pertenezca a la empresa actual antes de editar
            var existingDept = await _context.Departments.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(d => d.DeptId == id);
            if (existingDept == null || existingDept.CompanyId != CurrentCompanyId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Validación para no duplicar el nombre al editar
                    if (await _context.Departments.AnyAsync(d => d.DeptName == department.DeptName && d.CompanyId == CurrentCompanyId && d.DeptId != id))
                    {
                        ModelState.AddModelError("DeptName", "Otro departamento de tu empresa ya utiliza este nombre.");
                        return View(department);
                    }

                    // Forzamos el ID de la empresa para evitar inyecciones en el formulario
                    department.CompanyId = CurrentCompanyId;
                    
                    // 📝 AUDITORÍA
                    department.UpdatedAt = DateTime.UtcNow;
                    department.UpdatedBy = CurrentUserId;

                    _context.Update(department);
                    
                    // Protegemos datos inmutables
                    _context.Entry(department).Property(x => x.CreatedAt).IsModified = false;
                    _context.Entry(department).Property(x => x.CreatedBy).IsModified = false;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.DeptId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        // 7. POST: /Departments/Delete/5 (BOTÓN DIRECTO DESDE INDEX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // Usamos IgnoreQueryFilters por si tienes un filtro global que oculte inactivos
            var department = await _context.Departments.IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.DeptId == id && d.CompanyId == CurrentCompanyId);
                
            if (department != null)
            {
                // 🛡️ REGLA DE NEGOCIO: No borrar si hay usuarios activos adentro
                var hasActiveUsers = await _context.Users.AnyAsync(u => u.DeptId == id && u.Status == "Active");
                if (hasActiveUsers)
                {
                    TempData["ErrorMessage"] = "No puedes desactivar este departamento porque tiene empleados activos. Reasígnalos primero.";
                    return RedirectToAction(nameof(Index));
                }

                // Soft Delete
                department.Status = "Inactive";
                department.DeletedAt = DateTime.UtcNow;
                department.DeletedBy = CurrentUserId;
                
                _context.Update(department);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // 8. POST: /Departments/Reactivate/5 (NUEVO MÉTODO)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            var department = await _context.Departments.IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.DeptId == id && d.CompanyId == CurrentCompanyId);

            if (department != null && department.Status != "Active")
            {
                department.Status = "Active";
                department.DeletedAt = null;
                department.DeletedBy = null;
                department.UpdatedAt = DateTime.UtcNow;
                department.UpdatedBy = CurrentUserId;

                _context.Update(department);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool DepartmentExists(int id)
        {
            return _context.Departments.IgnoreQueryFilters().Any(e => e.DeptId == id && e.CompanyId == CurrentCompanyId);
        }
    }
}