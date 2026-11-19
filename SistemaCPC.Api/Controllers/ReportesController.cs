using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Patrones.Adapter;
using SistemaCPC.Api.Patrones.Builder;
using SistemaCPC.Api.Patrones.Facade;
using SistemaCPC.Api.Patrones.Singleton;

namespace SistemaCPC.Api.Controllers;

[Authorize(Roles = "Directorio")]
[ApiController]
[Route("api/reportes")]
public class ReportesController : ControllerBase
{
    private readonly CpcContext _context;
    private readonly ParametrosSistema _parametros;
    private readonly ServicioReportes _servicio;
    private readonly EstadoDeCuenta _estadoDeCuenta;
    private readonly IExportador _exportador;

    public ReportesController(CpcContext context, ParametrosSistema parametros, ServicioReportes servicio,
        EstadoDeCuenta estadoDeCuenta, IExportador exportador)
    {
        _context = context;
        _parametros = parametros;
        _servicio = servicio;
        _estadoDeCuenta = estadoDeCuenta;
        _exportador = exportador;
    }

    [HttpGet("defensas")]
    public Task<Reporte> Defensas(DateOnly? desde, DateOnly? hasta) => GenerarReporte("defensas", desde, hasta)!;

    [HttpGet("estados")]
    public Task<Reporte> AfiliadosPorEstado() => GenerarReporte("estados", null, null)!;

    [HttpGet("matriz")]
    public Task<Reporte> Matriz() => GenerarReporte("matriz", null, null)!;

    [HttpGet("{tipo}/excel")]
    public async Task<IActionResult> DescargarExcel(string tipo, DateOnly? desde, DateOnly? hasta)
    {
        var generando = GenerarReporte(tipo, desde, hasta);

        if (generando == null)
        {
            return NotFound(new { mensaje = "Los reportes son: defensas, estados y matriz." });
        }

        var archivo = _exportador.Exportar(await generando);
        return File(archivo, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Reporte {tipo}.xlsx");
    }

    private Task<Reporte>? GenerarReporte(string tipo, DateOnly? desde, DateOnly? hasta)
    {
        ConstructorReporte? constructor = tipo switch
        {
            "defensas" => new ReporteDefensas(_context, _parametros),
            "estados" => new ReporteAfiliadosPorEstado(_context, _parametros, _estadoDeCuenta),
            "matriz" => new MatrizAfiliados(_context, _parametros, _estadoDeCuenta),
            _ => null
        };

        return constructor == null ? null : _servicio.Generar(constructor, desde, hasta);
    }
}
