using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Models;
using SistemaCPC.Api.Patrones.Decorator;
using SistemaCPC.Api.Patrones.FactoryMethod;
using SistemaCPC.Api.Patrones.Singleton;
using SistemaCPC.Api.Seguridad;

namespace SistemaCPC.Api.Controllers;

public class PagoPlataformaSolicitud
{
    public int MesesCubiertos { get; set; }
}

public class PagoOficinaSolicitud
{
    public string Registro { get; set; } = string.Empty;
    public string NumeroRecibo { get; set; } = string.Empty;
    public DateOnly? Fecha { get; set; }
    public int MesesCubiertos { get; set; }
}

public class CorreccionPagoSolicitud
{
    public int MesesCubiertos { get; set; }
    public decimal Monto { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

public record PagoRespuesta(string NumeroRecibo, DateOnly Fecha, decimal Monto, int MesesCubiertos, string Origen);

[Authorize]
[ApiController]
[Route("api/pagos")]
public class PagosController : ControllerBase
{
    private readonly CpcContext _context;
    private readonly ParametrosSistema _parametros;
    private readonly IServicioPagos _pagos;

    public PagosController(CpcContext context, ParametrosSistema parametros, IServicioPagos pagos)
    {
        _context = context;
        _parametros = parametros;
        _pagos = pagos;
    }

    [Authorize(Roles = "Directorio")]
    [HttpPut("{numeroRecibo}")]
    public async Task<ActionResult<PagoRespuesta>> Corregir(string numeroRecibo, CorreccionPagoSolicitud solicitud)
    {
        if (solicitud.MesesCubiertos < 1 || solicitud.Monto <= 0 || string.IsNullOrWhiteSpace(solicitud.Motivo))
        {
            return BadRequest(new { mensaje = "Los meses, el monto y el motivo son obligatorios." });
        }

        var pago = await _context.Pagos.FirstOrDefaultAsync(p => p.NumeroRecibo == numeroRecibo);

        if (pago == null)
        {
            return NotFound(new { mensaje = "No existe un pago con ese número de recibo." });
        }

        var afiliado = await _context.Afiliados
            .Include(a => a.Pagos)
            .Include(a => a.Ajustes)
            .FirstAsync(a => a.Id == pago.AfiliadoId);

        var corregido = await _pagos.Corregir(pago, afiliado, solicitud.MesesCubiertos, solicitud.Monto,
            solicitud.Motivo, User.Identity!.Name!);
        return Ok(Respuesta(corregido));
    }

    [HttpGet("{registro}")]
    public async Task<ActionResult<List<PagoRespuesta>>> Listar(string registro)
    {
        if (!User.PuedeVer(registro))
        {
            return StatusCode(403, new { mensaje = "Solo puede ver su propia información." });
        }

        var afiliado = await _context.Afiliados.Include(a => a.Pagos).FirstOrDefaultAsync(a => a.Registro == registro);

        if (afiliado == null)
        {
            return NotFound(new { mensaje = "No existe un afiliado con ese registro." });
        }

        return afiliado.Pagos.OrderByDescending(p => p.Fecha).Select(Respuesta).ToList();
    }

    [Authorize(Roles = "Afiliado")]
    [HttpPost("plataforma")]
    public Task<ActionResult<PagoRespuesta>> PagarEnPlataforma(PagoPlataformaSolicitud solicitud)
    {
        return Registrar(User.Identity!.Name!, solicitud.MesesCubiertos,
            new RegistroPagoPlataforma(_context, _parametros));
    }

    [Authorize(Roles = "Directorio")]
    [HttpPost("oficina")]
    public async Task<ActionResult<PagoRespuesta>> RegistrarEnOficina(PagoOficinaSolicitud solicitud)
    {
        if (string.IsNullOrWhiteSpace(solicitud.NumeroRecibo))
        {
            return BadRequest(new { mensaje = "El número de recibo es obligatorio." });
        }

        if (solicitud.Fecha > DateOnly.FromDateTime(DateTime.Today))
        {
            return BadRequest(new { mensaje = "La fecha del recibo no puede ser posterior a hoy." });
        }

        if (await _context.Pagos.AnyAsync(p => p.NumeroRecibo == solicitud.NumeroRecibo))
        {
            return Conflict(new { mensaje = "Ese número de recibo ya fue registrado." });
        }

        return await Registrar(solicitud.Registro, solicitud.MesesCubiertos,
            new RegistroPagoOficina(_context, _parametros, solicitud.NumeroRecibo, solicitud.Fecha));
    }

    private async Task<ActionResult<PagoRespuesta>> Registrar(string registro, int meses, RegistroDePago registroDePago)
    {
        var afiliado = await _context.Afiliados.FirstOrDefaultAsync(a => a.Registro == registro);

        if (afiliado == null)
        {
            return NotFound(new { mensaje = "No existe un afiliado con ese registro." });
        }

        try
        {
            var pago = await registroDePago.Registrar(afiliado, meses);
            return Ok(Respuesta(pago));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    private static PagoRespuesta Respuesta(Pago p) =>
        new(p.NumeroRecibo, p.Fecha, p.Monto, p.MesesCubiertos, p.Origen.ToString());
}
