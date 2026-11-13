using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Patrones.Adapter;
using SistemaCPC.Api.Patrones.Builder;
using SistemaCPC.Api.Patrones.Decorator;
using SistemaCPC.Api.Patrones.Facade;
using SistemaCPC.Api.Patrones.Singleton;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<CpcContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CpcConexion")));

// Patrón Singleton
builder.Services.AddSingleton<ParametrosSistema>();

// Patrón Builder: director
builder.Services.AddScoped<ServicioReportes>();

// Patrón Facade
builder.Services.AddScoped<EstadoDeCuenta>();

// Patrón Decorator
builder.Services.AddScoped<ServicioAjustes>();
builder.Services.AddScoped<ServicioPagos>();
builder.Services.AddScoped(sp => new ServicioConBitacora(
    sp.GetRequiredService<ServicioAjustes>(), sp.GetRequiredService<ServicioPagos>(),
    sp.GetRequiredService<CpcContext>(), sp.GetRequiredService<EstadoDeCuenta>()));
builder.Services.AddScoped<IServicioAjustes>(sp => sp.GetRequiredService<ServicioConBitacora>());
builder.Services.AddScoped<IServicioPagos>(sp => sp.GetRequiredService<ServicioConBitacora>());

// Patrón Adapter
builder.Services.AddScoped<IExportador, ExportadorExcel>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CpcContext>();
    DatosIniciales.Cargar(context);
    app.Services.GetRequiredService<ParametrosSistema>().Cargar(context.Parametros.First());
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(opciones => opciones.SwaggerEndpoint("/openapi/v1.json", "SistemaCPC API"));
}

app.UseAuthorization();

app.MapControllers();

app.Run();
