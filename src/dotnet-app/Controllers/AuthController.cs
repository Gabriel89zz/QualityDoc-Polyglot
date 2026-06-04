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
        private readonly Services.IEmailService _emailService; 
        public AuthController(QualityDocDbContext context, IConfiguration config, Services.IEmailService emailService)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
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
                     role.Trim().Equals("Lector", StringComparison.OrdinalIgnoreCase) ||
                     role.Trim().Equals("Auditor", StringComparison.OrdinalIgnoreCase)))
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

            // =======================================================
            // 🚀 INTERCEPCIÓN PARA 2FA DE ADMINISTRADORES
            // =======================================================
            bool requires2FA = false;
            if (user.Role.RoleName == "Super Administrador" || user.Role.RoleName == "Super Admin" || user.Role.RoleName == "Admin de Empresa")
            {
                requires2FA = true; // Por defecto asumimos que sí lo necesita

                // 🧠 LÓGICA DE DISPOSITIVO DE CONFIANZA
                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(user.UserId.ToString() + user.PasswordHash);
                var expectedToken = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(plainTextBytes));
                var trustedCookie = Request.Cookies["QualityDoc2FA_Trusted_" + user.UserId];

                // Si la cookie existe en su navegador y coincide con la firma secreta, lo dejamos pasar
                if (trustedCookie == expectedToken)
                {
                    requires2FA = false; 
                }
            }

            if (requires2FA)
            {
                // 1. Generar código OTP de 6 dígitos
                string code = new Random().Next(100000, 999999).ToString();
                user.TwoFactorCode = code;
                user.TwoFactorExpiry = DateTime.UtcNow.AddMinutes(10);
                
                _context.Update(user);
                await _context.SaveChangesAsync();

                // 2. Disparar correo con el OTP
                try
                {
                    await _emailService.SendEmailAsync(
                        user.Email,
                        "🔒 Código de Seguridad 2FA - QualityDoc",
                        "Autenticación en Dos Pasos",
                        $"Hola <b>{user.FullName}</b>,<br><br>Se requiere verificación adicional para ingresar al panel de administración. Ingresa el siguiente código de seguridad (válido por 10 minutos):<br><br><div style='text-align:center; font-size:32px; font-weight:bold; letter-spacing:10px; background:#F1F5F9; color:#4F46E5; padding:20px; border-radius:12px; margin:20px 0;'>{code}</div>"
                    );
                }
                catch (Exception) { /* Ignoramos fallo smtp */ }

                // 3. Guardar en memoria temporal quién intentó entrar y mandarlo a la vista del PIN
                TempData["Pending2FAUserId"] = user.UserId;
                return RedirectToAction("Verify2FA");
            }
            // =======================================================

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
    user.Role.RoleName.Trim().Equals("Lector", StringComparison.OrdinalIgnoreCase) ||
    user.Role.RoleName.Trim().Equals("Auditor", StringComparison.OrdinalIgnoreCase))
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

                // =======================================================
                // 🚀 NUEVO: GENERAR 2FA PARA SU PRIMER INGRESO (ONBOARDING)
                // =======================================================
                string code = new Random().Next(100000, 999999).ToString();
                newUser.TwoFactorCode = code;
                newUser.TwoFactorExpiry = DateTime.UtcNow.AddMinutes(10);
                
                _context.Update(newUser);
                await _context.SaveChangesAsync();

                try
                {
                    await _emailService.SendEmailAsync(
                        newUser.Email,
                        "🎉 Bienvenido a QualityDoc - Confirma tu cuenta",
                        "Registro Exitoso",
                        $"Hola <b>{newUser.FullName}</b>,<br><br>Tu empresa <b>{newCompany.LegalName}</b> ha sido registrada con éxito en QualityDoc Polyglot. Para verificar tu correo e ingresar por primera vez, utiliza el siguiente código de seguridad:<br><br><div style='text-align:center; font-size:32px; font-weight:bold; letter-spacing:10px; background:#F1F5F9; color:#4F46E5; padding:20px; border-radius:12px; margin:20px 0;'>{code}</div>"
                    );
                }
                catch (Exception) { /* Ignoramos fallos del SMTP */ }

                await transaction.CommitAsync();

                // 🚀 REDIRIGIMOS A LA VISTA DE 2FA DIRECTAMENTE
                TempData["Pending2FAUserId"] = newUser.UserId;
                TempData["SuccessMessage"] = "¡Cuenta creada! Revisa tu correo electrónico para obtener el código de acceso inicial.";
                
                return RedirectToAction("Verify2FA");
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
            var user = await _context.Users.Include(u => u.Role).Include(u => u.Company).FirstOrDefaultAsync(u => u.UserId == int.Parse(userIdStr));
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
                new Claim("company_name", user.Company != null ? user.Company.LegalName : "Planta #" + user.CompanyId),
                new Claim("dept_id", user.DeptId.HasValue ? user.DeptId.Value.ToString() : "0"),
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

        // =======================================================
        // 5. ZONA DE SEGURIDAD Y CORREOS (2FA & RESET)
        // =======================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Verify2FA()
        {
            if (TempData["Pending2FAUserId"] == null) return RedirectToAction("Login");
            TempData.Keep("Pending2FAUserId"); // Mantenemos el ID vivo en memoria
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify2FA(string code, bool rememberDevice = false) // 🚀 Parámetro nuevo
        {
            var userIdStr = TempData["Pending2FAUserId"]?.ToString();
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");

            var user = await _context.Users.Include(u => u.Role).Include(u => u.Company).FirstOrDefaultAsync(u => u.UserId == int.Parse(userIdStr));
            
            if (user == null || user.TwoFactorCode != code || user.TwoFactorExpiry < DateTime.UtcNow)
            {
                TempData.Keep("Pending2FAUserId"); 
                ModelState.AddModelError(string.Empty, "El código es incorrecto o ha expirado.");
                return View();
            }

            // ÉXITO: Limpiamos el código 
            user.TwoFactorCode = null;
            user.TwoFactorExpiry = null;
            await _context.SaveChangesAsync();

            // =======================================================
            // 🚀 NUEVO: GUARDAR DISPOSITIVO DE CONFIANZA POR 30 DÍAS
            // =======================================================
            if (rememberDevice)
            {
                // Creamos una firma única encriptada usando su ID y el Hash de su contraseña
                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(user.UserId.ToString() + user.PasswordHash);
                var trustedToken = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(plainTextBytes));
                
                var cookieOptions = new CookieOptions {
                    Expires = DateTime.UtcNow.AddDays(30),
                    HttpOnly = true, // Evita hackeos por JavaScript (XSS)
                    Secure = true,   // Solo viaja por HTTPS
                    SameSite = SameSiteMode.Strict
                };
                Response.Cookies.Append("QualityDoc2FA_Trusted_" + user.UserId, trustedToken, cookieOptions);
            }
            // =======================================================

            // Firmamos las cookies (Igual que en el Login normal)
            var claims = new List<Claim> {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.RoleName),
                new Claim("CompanyId", user.CompanyId.HasValue ? user.CompanyId.Value.ToString() : "0"), 
                new Claim("CompanyName", user.Company != null ? user.Company.LegalName : "Sistema (Super Admin)")
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Status == "Active");
            if (user != null)
            {
                user.PasswordResetToken = Guid.NewGuid().ToString();
                user.ResetTokenExpiry = DateTime.UtcNow.AddHours(2);
                await _context.SaveChangesAsync();

                string resetLink = Url.Action("ResetPassword", "Auth", new { token = user.PasswordResetToken }, Request.Scheme);
                
                try {
                    await _emailService.SendEmailAsync(
                        user.Email,
                        "🔑 Restablecer tu contraseña - QualityDoc",
                        "Solicitud de Recuperación",
                        $"Hola <b>{user.FullName}</b>,<br><br>Recibimos una solicitud para restablecer tu contraseña. Si no la solicitaste, ignora este correo de forma segura.<br><br>Da clic en el siguiente botón para crear una nueva contraseña:",
                        resetLink,
                        "Crear Nueva Contraseña"
                    );
                } 
                catch(Exception ex) 
                {
                    throw new Exception("Error de Gmail: " + ex.Message);
                }
            }

            // ISO 27001: Siempre mostrar éxito para no revelar qué correos existen
            TempData["SuccessMessage"] = "Si el correo está registrado, recibirás las instrucciones en breve.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token && u.ResetTokenExpiry > DateTime.UtcNow);
            if (user == null)
            {
                TempData["ErrorMessage"] = "El enlace es inválido o ha expirado. Por favor, solicita uno nuevo.";
                return RedirectToAction("Login");
            }
            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token && u.ResetTokenExpiry > DateTime.UtcNow);
            if (user == null)
            {
                TempData["ErrorMessage"] = "El enlace ha expirado.";
                return RedirectToAction("Login");
            }

            // Actualizamos la contraseña y quemamos el token para que no se re-use
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.ResetTokenExpiry = null;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "¡Tu contraseña ha sido actualizada con éxito! Ya puedes iniciar sesión.";
            return RedirectToAction("Login");
        }
    }
}