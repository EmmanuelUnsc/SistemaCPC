using Microsoft.AspNetCore.Mvc;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Patrones.Builder;
using SistemaCPC.Api.Patrones.Singleton;

namespace SistemaCPC.Api.Controllers;

// CU-09 Generar reportes
[ApiController]
[Route("api/reportes")]
public class ReportesController : ControllerBase
{
    private readonly CpcContext _context;
    private readonly ParametrosSistema _parametros;
    private readonly ServicioReportes _servicio;

    public ReportesController(CpcContext context, ParametrosSistema parametros, ServicioReportes servicio)
    {
        _context = context;
        _parametros = parametros;
        _servicio = servicio;
    }

    // GET api/reportes/defensas?desde=2026-01-01&hasta=2026-12-31
    [HttpGet("defensas")]
    public Task<Reporte> Defensas(DateOnly? desde, DateOnly? hasta) =>
        _servicio.Generar(new ReporteDefensas(_context, _parametros), desde, hasta);

    // GET api/reportes/estados
    [HttpGet("estados")]
    public Task<Reporte> AfiliadosPorEstado() =>
        _servicio.Generar(new ReporteAfiliadosPorEstado(_context, _parametros), null, null);

    // GET api/reportes/matriz
    [HttpGet("matriz")]
    public Task<Reporte> Matriz() =>
        _servicio.Generar(new MatrizAfiliados(_context, _parametros), null, null);
}
