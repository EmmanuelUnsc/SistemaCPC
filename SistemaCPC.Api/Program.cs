using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Patrones.Adapter;
using SistemaCPC.Api.Patrones.Builder;
using SistemaCPC.Api.Patrones.Decorator;
using SistemaCPC.Api.Patrones.Facade;
using SistemaCPC.Api.Patrones.Singleton;
using SistemaCPC.Api.Seguridad;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(opciones => opciones.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
builder.Services.AddOpenApi(opciones => opciones.AddDocumentTransformer((documento, contexto, cancelacion) =>
{
    documento.Components ??= new OpenApiComponents();
    documento.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    documento.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Token que devuelve POST /api/auth/login"
    };
    documento.Security ??= new List<OpenApiSecurityRequirement>();
    documento.Security.Add(new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", documento)] = new List<string>()
    });
    return Task.CompletedTask;
}));

var opcionesToken = builder.Configuration.GetSection("Token").Get<OpcionesToken>()!;
builder.Services.AddSingleton(opcionesToken);
builder.Services.AddSingleton<EmisorToken>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.MapInboundClaims = false;
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = opcionesToken.Emisor,
            ValidAudience = opcionesToken.Emisor,
            IssuerSigningKey = opcionesToken.Llave(),
            NameClaimType = "name",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddDbContext<CpcContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CpcConexion")));

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
