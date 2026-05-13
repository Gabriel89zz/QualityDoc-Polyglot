using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization; // 🚀 Agregado para el método GoToPhpPortal
using QualityDoc.API.Data;
using QualityDoc.API.ViewModels;
using QualityDoc.API.Models;
using System; // 🚀 Agregado para StringComparison

// 🚀 NUEVOS USINGS PARA JWT
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace QualityDoc.API.Controllers
{
    public class AuthController : Controller
    {
        private readonly QualityDocDbContext _context;
        private readonly IConfiguration _config; 

        public AuthController(QualityDocDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // =======================================================
        // 1. ZONA DE LOGIN 
        // =======================================================
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;
                
                // 🚀 LA TRAMPA EN EL GET: Si ya está logueado y es operario, mandarlo a PHP
                if (role != null && (role.Trim().Equals("Operario", StringComparison.OrdinalIgnoreCase) || 
                                     role.Trim().Equals("Lector", StringComparison.OrdinalIgnoreCase)))
                {
                    return RedirectToAction("GoToPhpPortal", "Auth");
                }

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
                
                new Claim("CompanyId", user.CompanyId.HasValue ? user.CompanyId.Value.ToString() : "0"), 
                new Claim("CompanyName", user.Company != null ? user.Company.LegalName : "Sistema (Super Admin)")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity));

            // 🚀 EL TRUCO DE LA REDIRECCIÓN BLINDADO
            if (user.Role.RoleName.Trim().Equals("Operario", StringComparison.OrdinalIgnoreCase) || 
                user.Role.RoleName.Trim().Equals("Lector", StringComparison.OrdinalIgnoreCase))
            {
                var jwtToken = GenerarTokenParaPhp(user);
                return Redirect($"http://127.0.0.1/auth/token?token={jwtToken}");
            }

            return RedirectToAction("Index", "Home");
        }

        // =======================================================
        // 2. ZONA DE REGISTRO PÚBLICO (SaaS Autoregistro)
        // =======================================================
        
        [HttpGet]
        public IActionResult Register()
        {
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

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Este correo electrónico ya está registrado en el sistema.");
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var newCompany = new Company
                {
                    LegalName = model.LegalName,
                    TaxId = model.TaxId,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 1
                };

                _context.Companies.Add(newCompany);
                await _context.SaveChangesAsync(); 

                var defaultDept = new Department
                {
                    DeptName = "Dirección",
                    CompanyId = newCompany.CompanyId, 
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 1 
                };

                _context.Departments.Add(defaultDept);
                await _context.SaveChangesAsync(); 

                var newUser = new User
                {
                    CompanyId = newCompany.CompanyId, 
                    RoleId = 2,       
                    DeptId = defaultDept.DeptId, 
                    FullName = model.AdminFullName,
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password), 
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 1
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Ocurrió un error crítico durante el registro: " + ex.Message);
                return View(model);
            }
        }

        // =======================================================
        // 3. ZONA DE UTILIDADES 
        // =======================================================

       [HttpGet]
       [HttpPost] 
        public async Task<IActionResult> Logout()
        {
            // Mata la cookie de C#
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    
            // Ahora sí, te manda a la pantalla de login limpio
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // =======================================================
        // 4. MÉTODOS PARA EL PUENTE C# -> PHP (JWT)
        // =======================================================

        // 🚀 NUEVO MÉTODO PARA ATRAPAR SESIONES VIVAS Y SALTAR A PHP
        [HttpGet]
        [Authorize] 
        public async Task<IActionResult> GoToPhpPortal()
        {
            // Obtenemos el ID del usuario desde su cookie activa
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");

            // Buscamos sus datos en la BD para armarle su Token
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == int.Parse(userIdStr));
            if (user == null) return RedirectToAction("Logout");

            // Generamos el Token y saltamos a Nginx / Laravel
            var jwtToken = GenerarTokenParaPhp(user);
            return Redirect($"http://127.0.0.1/auth/token?token={jwtToken}");
        }

        private string GenerarTokenParaPhp(User user)
        {
            var secretKey = _config["JwtConfig:SecretKey"];
            if (string.IsNullOrEmpty(secretKey)) 
                throw new Exception("La clave JWT no está configurada en appsettings.json");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim("role", user.Role.RoleName),
                new Claim("name", user.FullName),
                new Claim("company_id", user.CompanyId.HasValue ? user.CompanyId.Value.ToString() : "0"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["JwtConfig:Issuer"] ?? "QualityDoc-CSharp",
                audience: _config["JwtConfig:Audience"] ?? "QualityDoc-PHP",
                claims: claims,
                expires: DateTime.UtcNow.AddSeconds(30), 
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}