using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Patrones.Builder;
using SistemaCPC.Api.Patrones.Singleton;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<CpcContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CpcConexion")));

// Singleton: una sola instancia de los parámetros para toda la API.
builder.Services.AddSingleton<ParametrosSistema>();

// Builder: el director que arma los reportes de CU-09.
builder.Services.AddScoped<ServicioReportes>();

var app = builder.Build();

// Crea las tablas, carga los datos de prueba si faltan y carga los parámetros en el Singleton.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CpcContext>();
    DatosIniciales.Cargar(context);
    app.Services.GetRequiredService<ParametrosSistema>().Cargar(context.Parametros.First());
}

if (app.Environment.IsDevelopment())
{
    // Documento OpenAPI en /openapi/v1.json y Swagger en /swagger para probar la API.
    app.MapOpenApi();
    app.UseSwaggerUI(opciones => opciones.SwaggerEndpoint("/openapi/v1.json", "SistemaCPC API"));
}

app.UseAuthorization();

app.MapControllers();

app.Run();
