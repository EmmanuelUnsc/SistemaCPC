using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using SistemaCPC.Web.Filtros;
using SistemaCPC.Web.Patrones.Proxy;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(opciones => opciones.Filters.Add<FiltroErroresApi>());

builder.Services.AddHttpClient("Api", cliente =>
    cliente.BaseAddress = new Uri(builder.Configuration["ApiUrl"]!));
builder.Services.AddHttpContextAccessor();

// Patrón Proxy
builder.Services.AddScoped<IServicioAutenticacion, ProxyAutenticacion>();
builder.Services.AddScoped<ProxyApi>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/Cuenta/Login";
        opciones.AccessDeniedPath = "/Cuenta/Login";
        opciones.ExpireTimeSpan = TimeSpan.FromMinutes(15);
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
