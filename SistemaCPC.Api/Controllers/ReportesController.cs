using Microsoft.AspNetCore.Mvc;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Patrones.Builder;
using SistemaCPC.Api.Patrones.Facade;
using SistemaCPC.Api.Patrones.Singleton;

namespace SistemaCPC.Api.Controllers;

[ApiController]
[Route("api/reportes")]
public class ReportesController : ControllerBase
{
    private readonly CpcContext _context;
    private readonly ParametrosSistema _parametros;
    private readonly ServicioReportes _servicio;
    private readonly EstadoDeCuenta _estadoDeCuenta;

    public ReportesController(CpcContext context, ParametrosSistema parametros, ServicioReportes servicio,
        EstadoDeCuenta estadoDeCuenta)
    {
        _context = context;
        _parametros = parametros;
        _servicio = servicio;
        _estadoDeCuenta = estadoDeCuenta;
    }

    [HttpGet("defensas")]
    public Task<Reporte> Defensas(DateOnly? desde, DateOnly? hasta) =>
        _servicio.Generar(new ReporteDefensas(_context, _parametros), desde, hasta);

    [HttpGet("estados")]
    public Task<Reporte> AfiliadosPorEstado() =>
        _servicio.Generar(new ReporteAfiliadosPorEstado(_context, _parametros, _estadoDeCuenta), null, null);

    [HttpGet("matriz")]
    public Task<Reporte> Matriz() =>
        _servicio.Generar(new MatrizAfiliados(_context, _parametros, _estadoDeCuenta), null, null);
}
