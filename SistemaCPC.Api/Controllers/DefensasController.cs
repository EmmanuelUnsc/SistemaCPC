using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Models;
using SistemaCPC.Api.Patrones.ChainOfResponsibility;
using SistemaCPC.Api.Patrones.Singleton;

namespace SistemaCPC.Api.Controllers;

public record DefensaRespuesta(string NumeroOficio, string Universidad, DateOnly Fecha, string Tribunal, string Estado);

[Authorize(Roles = "Directorio")]
[ApiController]
[Route("api/defensas")]
public class DefensasController : ControllerBase
{
    private readonly CpcContext _context;
    private readonly ParametrosSistema _parametros;

    public DefensasController(CpcContext context, ParametrosSistema parametros)
    {
        _context = context;
        _parametros = parametros;
    }

    [HttpGet]
    public async Task<List<DefensaRespuesta>> Listar()
    {
        return await _context.Defensas
            .OrderByDescending(d => d.Fecha)
            .Select(d => new DefensaRespuesta(d.NumeroOficio, d.Universidad, d.Fecha,
                d.Afiliado!.Registro + " - " + d.Afiliado.Nombre, d.Estado.ToString()))
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<DefensaRespuesta>> Registrar(SolicitudDefensa solicitud)
    {
        var cadena = new ValidadorDatosCompletos();
        cadena.Siguiente(new ValidadorOficioUnico(_context))
              .Siguiente(new ValidadorTribunalHabilitado(_context))
              .Siguiente(new ValidadorTopeMensual(_context, _parametros));

        var error = await cadena.Validar(solicitud);

        if (error != null)
        {
            return BadRequest(new { mensaje = error });
        }

        var tribunal = await _context.Afiliados.FirstAsync(a => a.Registro == solicitud.RegistroTribunal);
        var defensa = new Defensa
        {
            NumeroOficio = solicitud.NumeroOficio, Universidad = solicitud.Universidad, Carta = solicitud.Carta,
            Fecha = solicitud.Fecha, AfiliadoId = tribunal.Id, Estado = EstadoDefensa.PorPagar
        };
        _context.Defensas.Add(defensa);
        await _context.SaveChangesAsync();

        return Ok(new DefensaRespuesta(defensa.NumeroOficio, defensa.Universidad, defensa.Fecha,
            tribunal.Registro + " - " + tribunal.Nombre, defensa.Estado.ToString()));
    }
}
