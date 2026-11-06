using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<CpcContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CpcConexion")));

var app = builder.Build();

// Crea las tablas y carga los usuarios de prueba si la base está vacía.
using (var scope = app.Services.CreateScope())
{
    DatosIniciales.Cargar(scope.ServiceProvider.GetRequiredService<CpcContext>());
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
