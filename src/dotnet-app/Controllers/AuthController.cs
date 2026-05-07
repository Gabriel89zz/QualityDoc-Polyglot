using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using QualityDoc.API.Data;
using QualityDoc.API.ViewModels;
using QualityDoc.API.Models; 

namespace QualityDoc.API.Controllers
{
    public class AuthController : Controller
    {
        private readonly QualityDocDbContext _context;

        public AuthController(QualityDocDbContext context)
        {
            _context = context;
        }

        // =======================================================
        // 1. ZONA DE LOGIN 
        // =======================================================
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || user.Status != "Active" || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Credenciales incorrectas o usuario dado de baja.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.RoleName), 
                new Claim("CompanyId", user.CompanyId.ToString()), 
                new Claim("CompanyName", user.Company.LegalName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Home");
        }

        // =======================================================
        // 2. ZONA DE REGISTRO PÚBLICO (SaaS Autoregistro)
        // =======================================================
        
        [HttpGet]
        public IActionResult Register()
        {
            // Si ya está logueado, no tiene sentido que se registre de nuevo
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 🛡️ Validación extra: Verificar que el correo no esté repetido en todo el sistema
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Este correo electrónico ya está registrado en el sistema.");
                return View(model);
            }

            // ⚡ INICIAMOS LA TRANSACCIÓN ATÓMICA
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // ==========================================
                // PASO 1: Crear la Empresa (El Padre)
                // ==========================================
                var newCompany = new Company
                {
                    LegalName = model.LegalName,
                    TaxId = model.TaxId,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 1 // 1 = El sistema/SuperAdmin
                };
                _context.Companies.Add(newCompany);
                await _context.SaveChangesAsync(); // SQL genera el CompanyId

                // ==========================================
                // PASO 2: Crear el departamento "Dirección" exclusivo
                // ==========================================
                var defaultDept = new Department
                {
                    DeptName = "Dirección",
                    CompanyId = newCompany.CompanyId, // 👈 Lo atamos a su propia empresa
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 1 
                };
                _context.Departments.Add(defaultDept);
                await _context.SaveChangesAsync(); // 👈 SQL genera el DeptId

                // ==========================================
                // PASO 3: Crear el Usuario Administrador (El Hijo)
                // ==========================================
                var newUser = new User
                {
                    CompanyId = newCompany.CompanyId, 
                    RoleId = 2,       // 2 = Rol de Administrador General
                    DeptId = defaultDept.DeptId, // 👈 MAGIA: Usamos el ID del departamento recién nacido
                    FullName = model.AdminFullName,
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password), // 🔐 Encriptación automática de BCrypt
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 1
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // PASO 4: Si llegamos hasta aquí sin errores, guardamos TODO definitivamente
                await transaction.CommitAsync();

                // Redirigimos al Login para que estrene sus nuevas credenciales
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                // 💥 SI ALGO FALLA: Deshacemos todo (ni empresa, ni depto, ni usuario)
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Ocurrió un error crítico durante el registro: " + ex.Message);
                return View(model);
            }
        }

        // =======================================================
        // 3. ZONA DE UTILIDADES 
        // =======================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}