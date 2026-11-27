using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Configuracion;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Dtos;
using SistemaCPC.Api.Models;
using SistemaCPC.Api.Seguridad;
using SistemaCPC.Api.Servicios;

namespace SistemaCPC.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/pagos")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
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
    [EndpointSummary("CU-05: corregir un pago (directorio; queda en la bitácora)")]
    [ProducesResponseType<PagoRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagoRespuesta>> Corregir(string numeroRecibo, CorreccionPagoSolicitud solicitud)
    {
        if (solicitud.MesesCubiertos < 1 || solicitud.Monto <= 0 || string.IsNullOrWhiteSpace(solicitud.Motivo))
        {
            return BadRequest(new ErrorRespuesta("Los meses, el monto y el motivo son obligatorios."));
        }

        var pago = await _context.Pagos.FirstOrDefaultAsync(p => p.NumeroRecibo == numeroRecibo);

        if (pago == null)
        {
            return NotFound(new ErrorRespuesta("No existe un pago con ese número de recibo."));
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
    [EndpointSummary("CU-05: pagos de un afiliado (el afiliado solo los suyos)")]
    [ProducesResponseType<List<PagoRespuesta>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PagoRespuesta>>> Listar(string registro)
    {
        if (!User.PuedeVer(registro))
        {
            return StatusCode(403, new ErrorRespuesta("Solo puede ver su propia información."));
        }

        var afiliado = await _context.Afiliados.Include(a => a.Pagos).FirstOrDefaultAsync(a => a.Registro == registro);

        if (afiliado == null)
        {
            return NotFound(new ErrorRespuesta("No existe un afiliado con ese registro."));
        }

        return afiliado.Pagos.OrderByDescending(p => p.Fecha).Select(Respuesta).ToList();
    }

    [Authorize(Roles = "Afiliado")]
    [HttpPost("plataforma")]
    [EndpointSummary("CU-04: pago simulado del afiliado que inició sesión")]
    [ProducesResponseType<PagoRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status404NotFound)]
    public Task<ActionResult<PagoRespuesta>> PagarEnPlataforma(PagoPlataformaSolicitud solicitud)
    {
        return Registrar(User.Identity!.Name!, solicitud.MesesCubiertos,
            new RegistroPagoPlataforma(_context, _parametros));
    }

    [Authorize(Roles = "Directorio")]
    [HttpPost("oficina")]
    [EndpointSummary("CU-05: registrar el pago de un recibo físico (directorio)")]
    [ProducesResponseType<PagoRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PagoRespuesta>> RegistrarEnOficina(PagoOficinaSolicitud solicitud)
    {
        if (string.IsNullOrWhiteSpace(solicitud.NumeroRecibo))
        {
            return BadRequest(new ErrorRespuesta("El número de recibo es obligatorio."));
        }

        if (solicitud.NumeroRecibo.Replace(" ", string.Empty).StartsWith("P-", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ErrorRespuesta("Los recibos que empiezan con «P-» son de la plataforma; escriba el número del recibo físico."));
        }


        if (await _context.Pagos.AnyAsync(p => p.NumeroRecibo == solicitud.NumeroRecibo))
        {
            return Conflict(new ErrorRespuesta("Ese número de recibo ya fue registrado."));
        }

        return await Registrar(solicitud.Registro, solicitud.MesesCubiertos,
            new RegistroPagoOficina(_context, _parametros, solicitud.NumeroRecibo, solicitud.Fecha));
    }

    private async Task<ActionResult<PagoRespuesta>> Registrar(string registro, int meses, RegistroDePago registroDePago)
    {
        var afiliado = await _context.Afiliados.FirstOrDefaultAsync(a => a.Registro == registro);

        if (afiliado == null)
        {
            return NotFound(new ErrorRespuesta("No existe un afiliado con ese registro."));
        }

        try
        {
            var pago = await registroDePago.Registrar(afiliado, meses);
            return Ok(Respuesta(pago));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorRespuesta(ex.Message));
        }
    }

    private static PagoRespuesta Respuesta(Pago p) =>
        new(p.NumeroRecibo, p.Fecha, p.Monto, p.MesesCubiertos, p.Origen.ToString());
}
