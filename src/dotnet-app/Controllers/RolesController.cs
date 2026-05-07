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
    // 🛡️ ACCESO RESTRINGIDO: Panel global
    [Authorize(Roles = "Super Admin")]
    public class RolesController : Controller
    {
        private readonly QualityDocDbContext _context;

        // 🛡️ ESCUDO: Lista de roles críticos que no pueden ser borrados ni renombrados
        private readonly string[] _systemCriticalRoles = new[] 
        { 
            "Super Admin", 
            "Admin de Empresa", 
            "Creador de Doc", 
            "Revisor", 
            "Aprobador", 
            "Lector" 
        };

        public RolesController(QualityDocDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1");

        // 1. GET: /Roles
        public async Task<IActionResult> Index()
        {
            var roles = await _context.Roles
                .IgnoreQueryFilters()
                .OrderBy(r => r.RoleName)
                .ToListAsync();

            return View(roles);
        }

        // 2. GET: /Roles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var role = await _context.Roles
                .IgnoreQueryFilters()
                .Include(r => r.CreatedByNavigation)
                .Include(r => r.UpdatedByNavigation)
                .Include(r => r.DeletedByNavigation)
                .Include(r => r.Users) // Para ver cuántas personas tienen este rol
                .FirstOrDefaultAsync(m => m.RoleId == id);

            if (role == null) return NotFound();

            return View(role);
        }

        // 3. GET: /Roles/Create
        public IActionResult Create()
        {
            return View();
        }

        // 4. POST: /Roles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoleName,Description")] Role role)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Roles.AnyAsync(r => r.RoleName == role.RoleName))
                {
                    ModelState.AddModelError("RoleName", "Este nombre de rol ya está registrado.");
                    return View(role);
                }

                // 📝 RASTRO DE AUDITORÍA
                role.Status = "Active";
                role.CreatedAt = DateTime.UtcNow;
                role.CreatedBy = CurrentUserId;

                _context.Add(role);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(role);
        }

        // 5. GET: /Roles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var role = await _context.Roles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (role == null) return NotFound();

            // Pasamos un ViewBag a la vista para avisarle si es un rol protegido
            ViewBag.IsSystemRole = _systemCriticalRoles.Contains(role.RoleName);

            return View(role);
        }

        // 6. POST: /Roles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RoleId,RoleName,Description,Status")] Role role)
        {
            if (id != role.RoleId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var previousState = await _context.Roles.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(r => r.RoleId == id);
                    if (previousState == null) return NotFound();

                    // 🛡️ ESCUDO: Prevenir que le cambien el nombre a un rol maestro
                    if (_systemCriticalRoles.Contains(previousState.RoleName) && role.RoleName != previousState.RoleName)
                    {
                        ModelState.AddModelError("RoleName", "Protección del Sistema: No puedes modificar el nombre de un Rol Maestro.");
                        ViewBag.IsSystemRole = true;
                        return View(role);
                    }

                    if (await _context.Roles.AnyAsync(r => r.RoleName == role.RoleName && r.RoleId != id))
                    {
                        ModelState.AddModelError("RoleName", "Otro rol ya utiliza este nombre.");
                        ViewBag.IsSystemRole = _systemCriticalRoles.Contains(previousState.RoleName);
                        return View(role);
                    }

                    // 📝 RASTRO DE AUDITORÍA
                    role.UpdatedAt = DateTime.UtcNow;
                    role.UpdatedBy = CurrentUserId;
                    role.CreatedAt = previousState.CreatedAt;
                    role.CreatedBy = previousState.CreatedBy;

                    _context.Update(role);
                    _context.Entry(role).Property(x => x.CreatedAt).IsModified = false;
                    _context.Entry(role).Property(x => x.CreatedBy).IsModified = false;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoleExists(role.RoleId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(role);
        }

        // 7. GET: /Roles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var role = await _context.Roles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(m => m.RoleId == id);

            if (role == null) return NotFound();

            // 🛡️ ESCUDO: Bloquear la vista de eliminación si es un rol crítico
            if (_systemCriticalRoles.Contains(role.RoleName))
            {
                // Aquí en el futuro puedes usar Toastr o TempData para mostrar una alerta de error en la vista Index
                TempData["ErrorMessage"] = $"Protección: El rol '{role.RoleName}' es vital para el sistema y no puede ser suspendido.";
                return RedirectToAction(nameof(Index));
            }

            return View(role);
        }

        // 8. POST: /Roles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var role = await _context.Roles.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.RoleId == id);
            
            if (role != null)
            {
                // 🛡️ DOBLE VALIDACIÓN (Por si intentan hackear el POST)
                if (_systemCriticalRoles.Contains(role.RoleName))
                {
                    return RedirectToAction(nameof(Index));
                }

                // Soft Delete
                role.Status = "Inactive";
                role.DeletedAt = DateTime.UtcNow;
                role.DeletedBy = CurrentUserId;
                
                _context.Update(role);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // 9. POST: /Roles/Reactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            var role = await _context.Roles.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.RoleId == id);

            if (role != null && role.Status != "Active")
            {
                role.Status = "Active";
                role.DeletedAt = null;
                role.DeletedBy = null;
                role.UpdatedAt = DateTime.UtcNow;
                role.UpdatedBy = CurrentUserId;

                _context.Update(role);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool RoleExists(int id)
        {
            return _context.Roles.IgnoreQueryFilters().Any(e => e.RoleId == id);
        }
    }
}