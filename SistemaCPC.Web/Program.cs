using Microsoft.AspNetCore.Authentication.Cookies;
using SistemaCPC.Web.Patrones.Proxy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient("Api", cliente =>
    cliente.BaseAddress = new Uri(builder.Configuration["ApiUrl"]!));

// Patrón Proxy
builder.Services.AddScoped<IServicioAutenticacion, ProxyAutenticacion>();

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
