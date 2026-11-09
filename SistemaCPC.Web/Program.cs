using Microsoft.AspNetCore.Authentication.Cookies;
using SistemaCPC.Web.Patrones.Proxy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Cliente HTTP para llamar a SistemaCPC.Api (la dirección está en appsettings.json).
builder.Services.AddHttpClient("Api", cliente =>
    cliente.BaseAddress = new Uri(builder.Configuration["ApiUrl"]!));

// Proxy: los controladores usan las interfaces de servicio; el proxy hace la llamada HTTP a la API.
builder.Services.AddScoped<IServicioAutenticacion, ProxyAutenticacion>();

// CU-01 Iniciar sesión: después de que la API valida al usuario, la Web guarda la sesión en una cookie
// que vence a los 30 minutos sin uso.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/Cuenta/Login";
        opciones.AccessDeniedPath = "/Cuenta/Login";
        opciones.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        opciones.SlidingExpiration = true;
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
