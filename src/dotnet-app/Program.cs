using Microsoft.AspNetCore.Authentication.Cookies; // 1. Agregamos la librería de seguridad
using Microsoft.EntityFrameworkCore;
using QualityDoc.API.Data;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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

// 2. CONFIGURACIÓN DEL GUARDIA DE SEGURIDAD (Cookies)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Si alguien intenta entrar a un lugar prohibido, lo mandamos al Login
        options.LoginPath = "/Auth/Login"; 
        
        // Si alguien logueado intenta entrar a un lugar de Admin sin serlo:
        options.AccessDeniedPath = "/Auth/AccessDenied"; 
        
        // La sesión dura 8 horas (una jornada laboral estándar)
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UsePathBase("/admin");

app.UseStaticFiles();
app.UseRouting();

// 3. ACTIVAR LOS GAFETES EN EL PIPELINE (¡EL ORDEN ES VITAL!)
app.UseAuthentication(); // <- NUEVO: Primero lee la Cookie para saber QUIÉN eres
app.UseAuthorization();  // Después usa esa info para ver si tienes PERMISO (Roles)

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern:"{controller=Auth}/{action=Login}/{id?}");
    //.WithStaticAssets();

app.Run();