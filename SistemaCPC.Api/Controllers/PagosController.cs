using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Models;
using SistemaCPC.Api.Patrones.FactoryMethod;
using SistemaCPC.Api.Patrones.Singleton;

namespace SistemaCPC.Api.Controllers;

public class PagoPlataformaSolicitud
{
    public string Registro { get; set; } = string.Empty;
    public int MesesCubiertos { get; set; }
}

public class PagoOficinaSolicitud
{
    public string Registro { get; set; } = string.Empty;
    public string NumeroRecibo { get; set; } = string.Empty;
    public int MesesCubiertos { get; set; }
}

public record PagoRespuesta(string NumeroRecibo, DateOnly Fecha, decimal Monto, int MesesCubiertos, string Origen);

[ApiController]
[Route("api/pagos")]
public class PagosController : ControllerBase
{
    private readonly CpcContext _context;
    private readonly ParametrosSistema _parametros;

    public PagosController(CpcContext context, ParametrosSistema parametros)
    {
        _context = context;
        _parametros = parametros;
    }

    // GET api/pagos/0457: pagos registrados del afiliado.
    [HttpGet("{registro}")]
    public async Task<ActionResult<List<PagoRespuesta>>> Listar(string registro)
    {
        var afiliado = await _context.Afiliados.Include(a => a.Pagos).FirstOrDefaultAsync(a => a.Registro == registro);

        if (afiliado == null)
        {
            return NotFound(new { mensaje = "No existe un afiliado con ese registro." });
        }

        return afiliado.Pagos.OrderByDescending(p => p.Fecha).Select(Respuesta).ToList();
    }

    // POST api/pagos/plataforma: CU-04 Pagar aportes (simulado).
    [HttpPost("plataforma")]
    public Task<ActionResult<PagoRespuesta>> PagarEnPlataforma(PagoPlataformaSolicitud solicitud)
    {
        return Registrar(solicitud.Registro, solicitud.MesesCubiertos,
            new RegistroPagoPlataforma(_context, _parametros));
    }

    // POST api/pagos/oficina: CU-05 Gestionar pagos (pago recibido en la oficina).
    [HttpPost("oficina")]
    public async Task<ActionResult<PagoRespuesta>> RegistrarEnOficina(PagoOficinaSolicitud solicitud)
    {
        if (string.IsNullOrWhiteSpace(solicitud.NumeroRecibo))
        {
            return BadRequest(new { mensaje = "El número de recibo es obligatorio." });
        }

        if (await _context.Pagos.AnyAsync(p => p.NumeroRecibo == solicitud.NumeroRecibo))
        {
            return Conflict(new { mensaje = "Ese número de recibo ya fue registrado." });
        }

        return await Registrar(solicitud.Registro, solicitud.MesesCubiertos,
            new RegistroPagoOficina(_context, _parametros, solicitud.NumeroRecibo));
    }

    // El controlador no sabe cómo se crea el recibo: lo decide la subclase de RegistroDePago.
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
