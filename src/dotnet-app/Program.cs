using Microsoft.AspNetCore.Authentication.Cookies; // 1. Agregamos la librería de seguridad
using Microsoft.EntityFrameworkCore;
using QualityDoc.API.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Registrar el DbContext con la cadena de conexión
builder.Services.AddDbContext<QualityDocDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
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