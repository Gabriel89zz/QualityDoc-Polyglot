using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    // 🛡️ ACTUALIZADO: Usamos los nombres exactos que tienes en tu Base de Datos
    [Authorize(Roles = "Super Admin, Admin de Empresa")]
    public class UsersController : Controller
    {
        private readonly QualityDocDbContext _context;

        public UsersController(QualityDocDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // HELPERS DE SEGURIDAD PARA EL CONTROLADOR
        // ==========================================
        private bool IsSuperAdmin => User.IsInRole("Super Admin");
        private int CurrentCompanyId => int.Parse(User.FindFirstValue("CompanyId") ?? "0");

       // ==========================================
        // 1. GET: /Users (Paginado y Filtrado)
        // ==========================================
        public async Task<IActionResult> Index(string search, int? roleId, int? deptId, string status, int? pageNumber)
        {
            // 🚀 Guardar filtros para la paginación
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentStatus"] = status;
            ViewBag.CurrentRole = roleId;
            ViewBag.CurrentDept = deptId;

            // 🚀 Definimos 10 usuarios por página
            int pageSize = 10;

            // 🚀 Combos para los filtros (Respetando Multi-tenant y Seguridad)
            var rolesQuery = _context.Roles.AsQueryable();
            var deptsQuery = _context.Departments.Where(d => d.Status == "Active").AsQueryable();

            if (IsSuperAdmin)
            {
                // El Super Admin no necesita filtrar por su propio rol, pero sí ve el resto
                rolesQuery = rolesQuery.Where(r => r.RoleName != "Super Administrador");
            }
            else
            {
                // 🔒 Candado de Seguridad: El Admin de Empresa NO verá los roles superiores en el filtro
                rolesQuery = rolesQuery.Where(r => r.RoleName != "Super Administrador" && r.RoleName != "Admin de Empresa");
                
                // Además, blindamos los departamentos a solo los de su empresa
                deptsQuery = deptsQuery.Where(d => d.CompanyId == CurrentCompanyId);
            }

            ViewBag.Roles = new SelectList(await rolesQuery.OrderBy(r => r.RoleName).ToListAsync(), "RoleId", "RoleName", roleId);
            ViewBag.Departments = new SelectList(await deptsQuery.OrderBy(d => d.DeptName).ToListAsync(), "DeptId", "DeptName", deptId);

            // 🚀 Iniciamos la consulta base
            var query = _context.Users
                .IgnoreQueryFilters()
                .Include(u => u.Role)
                .Include(u => u.Department)
                .AsQueryable();

            // 🔒 Filtro Multi-tenant de Seguridad
            if (!IsSuperAdmin)
            {
                query = query.Where(u => u.CompanyId == CurrentCompanyId);
            }

            // 🔍 Filtro por texto libre (Nombre o Correo)
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));
            }

            // 🎭 Filtro por Rol
            if (roleId.HasValue)
            {
                query = query.Where(u => u.RoleId == roleId.Value);
            }

            // 🏢 Filtro por Departamento
            if (deptId.HasValue)
            {
                query = query.Where(u => u.DeptId == deptId.Value);
            }

            // 🏷️ Filtro por Estado
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(u => u.Status == status);
            }

            query = query.OrderBy(u => u.FullName);

            return View(await PaginatedList<User>.CreateAsync(query, pageNumber ?? 1, pageSize));
        }

        // 2. GET: /Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .IgnoreQueryFilters()
                .Include(u => u.Role)
                .Include(u => u.Department)
                .Include(u => u.Company)
                .Include(u => u.CreatedByNavigation)
                .Include(u => u.UpdatedByNavigation)
                .Include(u => u.DeletedByNavigation) 
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (user == null) return NotFound();

            // 🔒 CANDADO: Evitar que un Admin vea detalles de usuarios de otra empresa
            if (!IsSuperAdmin && user.CompanyId != CurrentCompanyId)
            {
                return RedirectToAction("AccessDenied", "Auth"); 
            }

            return View(user);
        }

        // 3. GET: /Users/Create
        public IActionResult Create()
        {
            CargarCombos();
            return View();
        }

        // 4. POST: /Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,CompanyId,DeptId,RoleId,FullName,Email,PasswordHash")] User user)
        {
            // 🛡️ FORZADO DE DATOS: Si no es SuperAdmin, obligamos a que el CompanyId sea el suyo
            if (!IsSuperAdmin)
            {
                user.CompanyId = CurrentCompanyId;
            }

            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                {
                    ModelState.AddModelError("Email", "Este correo ya está registrado.");
                }
                else
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                    user.Status = "Active"; 
                    
                    user.CreatedBy = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1");

                    _context.Add(user);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            CargarCombos(user);
            return View(user);
        }

        // 5. GET: /Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // 🔒 CANDADO DE EDICIÓN
            if (!IsSuperAdmin && user.CompanyId != CurrentCompanyId)
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            CargarCombos(user);
            return View(user);
        }

        // 6. POST: /Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,CompanyId,DeptId,RoleId,FullName,Email,PasswordHash,Status")] User user)
        {
            if (id != user.UserId) return NotFound();

            // 🔒 RE-VALIDACIÓN: Evita que inyecten un CompanyId diferente por HTML
            if (!IsSuperAdmin)
            {
                user.CompanyId = CurrentCompanyId;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);

                    if (string.IsNullOrWhiteSpace(user.PasswordHash) || user.PasswordHash == "********")
                    {
                        user.PasswordHash = existingUser.PasswordHash;
                    }
                    else
                    {
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                    }

                    _context.Update(user);
                    _context.Entry(user).Property(x => x.CreatedAt).IsModified = false;
                    _context.Entry(user).Property(x => x.CreatedBy).IsModified = false;

                    user.UpdatedBy = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1");
                    user.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            CargarCombos(user);
            return View(user);
        }

        // 7. POST: /Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            
            // 🔒 CANDADO FINAL: Por si usan Postman o alteran el formulario
            if (!IsSuperAdmin && user?.CompanyId != CurrentCompanyId)
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            if (user != null)
            {
                string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                user.Status = "Inactive";
                user.DeletedAt = DateTime.UtcNow;
                user.DeletedBy = int.Parse(currentUserId ?? "1");

                _context.Update(user);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // 8. POST: /Users/Reactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.UserId == id);

            // 🔒 CANDADO FINAL
            if (!IsSuperAdmin && user?.CompanyId != CurrentCompanyId)
            {
                return RedirectToAction("AccessDenied", "Auth");
            }

            if (user != null && user.Status != "Active")
            {
                string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                user.Status = "Active";
                user.DeletedAt = null;
                user.DeletedBy = null;
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedBy = int.Parse(currentUserId ?? "1");

                _context.Update(user);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // 🧠 COMBOS INTELIGENTES
        // ==========================================
        private void CargarCombos(User user = null)
        {
            // 1. Combo de Empresas
            if (IsSuperAdmin)
            {
                ViewData["CompanyId"] = new SelectList(_context.Companies, "CompanyId", "LegalName", user?.CompanyId);
            }
            else
            {
                var myCompany = _context.Companies.Where(c => c.CompanyId == CurrentCompanyId);
                ViewData["CompanyId"] = new SelectList(myCompany, "CompanyId", "LegalName", CurrentCompanyId);
            }

           // ==========================================
            // 2. Combo de Roles (NIVELES DE SEGURIDAD APLICADOS)
            // ==========================================
            var rolesQuery = _context.Roles.AsQueryable();
            
            if (IsSuperAdmin)
            {
                // El Super Admin puede crear a los "Admin de Empresa", pero no a otro Super Admin
                rolesQuery = rolesQuery.Where(r => r.RoleName != "Super Admin");
            }
            else
            {
                // El Admin de Empresa NO puede crear ni Super Admins ni otros Admins de Empresa
                rolesQuery = rolesQuery.Where(r => r.RoleName != "Super Admin" && r.RoleName != "Admin de Empresa");
            }

            // Ordenamos alfabéticamente para que el menú se vea más limpio
            rolesQuery = rolesQuery.OrderBy(r => r.RoleName);
            
            ViewData["RoleId"] = new SelectList(rolesQuery, "RoleId", "RoleName", user?.RoleId);
            
            // 🚀 3. Combo de Departamentos (BLINDAJE MULTI-TENANT APLICADO)
            var deptsQuery = _context.Departments.AsQueryable();
            
            if (!IsSuperAdmin)
            {
                // Solo cargamos los departamentos de la empresa actual
                deptsQuery = deptsQuery.Where(d => d.CompanyId == CurrentCompanyId);
            }
            // Opcional para el SuperAdmin: Podríamos incluir el nombre de la empresa al lado del departamento 
            // para que no se confunda si hay dos departamentos llamados "Mantenimiento" en diferentes empresas.
            else
            {
                deptsQuery = deptsQuery.Include(d => d.Company);
            }

            // Ordenamos alfabéticamente para mejor experiencia de usuario
            deptsQuery = deptsQuery.OrderBy(d => d.DeptName);

            ViewData["DeptId"] = new SelectList(deptsQuery, "DeptId", "DeptName", user?.DeptId);
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}