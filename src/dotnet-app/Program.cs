using Microsoft.AspNetCore.Authentication.Cookies; // 1. Agregamos la librería de seguridad
using Microsoft.EntityFrameworkCore;
using QualityDoc.API.Data;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"/app/keys"))
    .SetApplicationName("QualityDocApp");

// Registrar el DbContext con la cadena de conexión
builder.Services.AddDbContext<QualityDocDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<QualityDoc.API.Services.IEmailService, QualityDoc.API.Services.EmailService>();

// ==============================================================
// 🚀 NUEVO: LÍMITE GLOBAL DE SUBIDA DE ARCHIVOS (Desde appsettings.json)
// Le sumamos 5MB extra de margen para los textos del formulario
// ==============================================================
var maxFileSizeMB = builder.Configuration.GetValue<long>("DocumentSettings:MaxFileSizeMB", 25);
var maxServerLimitBytes = (maxFileSizeMB + 5) * 1024 * 1024; 

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = maxServerLimitBytes;
});
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = maxServerLimitBytes;
});
// ==============================================================

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login"; 
        options.AccessDeniedPath = "/Auth/AccessDenied"; 
        
        options.Cookie.Name = "QualityDocAuthCookie";
        options.Cookie.Path = "/";
        options.Cookie.HttpOnly = true;
        
        // 🚀 CAMBIO CRÍTICO:
        // Si no tienes HTTPS instalado, 'Secure' debe ser 'SameAsRequest'
        // 'SameSite' debe ser 'Lax' para permitir navegación entre dominios
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; 

        options.ExpireTimeSpan = TimeSpan.FromHours(8); 
    });

// ==============================================================
// 🛡️ CONFIANZA EN NGINX (PROXY INVERSO)
// ==============================================================
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Limpiamos las redes conocidas para que acepte el tráfico de la red de Docker
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});


var app = builder.Build();

app.UseForwardedHeaders();
app.UsePathBase("/admin");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();


app.UseStaticFiles();
app.UseRouting();

// 3. ACTIVAR LOS GAFETES EN EL PIPELINE (¡EL ORDEN ES VITAL!)
app.UseAuthentication(); 

// 🚀 LOGGING DE AUTORIZACIÓN (Esto nos dirá qué está pasando)
app.Use((context, next) => {
    Console.WriteLine($"🔍 URL Visitada: {context.Request.Path}");
    Console.WriteLine($"🔍 ¿Está autenticado?: {context.User.Identity.IsAuthenticated}");
    return next();
});

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern:"{controller=Auth}/{action=Login}/{id?}");
    //.WithStaticAssets();

app.Run();