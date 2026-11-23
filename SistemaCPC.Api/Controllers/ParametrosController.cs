using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Configuracion;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Dtos;

namespace SistemaCPC.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/parametros")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class ParametrosController : ControllerBase
{
    private readonly ParametrosSistema _parametros;
    private readonly CpcContext _context;

    public ParametrosController(ParametrosSistema parametros, CpcContext context)
    {
        _parametros = parametros;
        _context = context;
    }

    [HttpGet]
    [EndpointSummary("Parámetros de los cálculos (aporte, monto por defensa, umbral y tope)")]
    [ProducesResponseType<ParametrosSistema>(StatusCodes.Status200OK)]
    public ParametrosSistema Obtener() => _parametros;

    [Authorize(Roles = "Directorio")]
    [HttpPut]
    [EndpointSummary("Cambiar los parámetros (directorio; la instancia única se recarga)")]
    [ProducesResponseType<ParametrosSistema>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ParametrosSistema>> Actualizar(ParametrosSolicitud solicitud)
    {
        if (solicitud.AporteMensual <= 0 || solicitud.MontoPorDefensa <= 0 || solicitud.UmbralInactividad < 1
            || solicitud.TopeDefensasMes < 0)
        {
            return BadRequest(new ErrorRespuesta("Los valores deben ser positivos; el tope puede ser 0 (sin tope)."));
        }

        var guardados = await _context.Parametros.FirstAsync();
        guardados.AporteMensual = solicitud.AporteMensual;
        guardados.MontoPorDefensa = solicitud.MontoPorDefensa;
        guardados.UmbralInactividad = solicitud.UmbralInactividad;
        guardados.TopeDefensasMes = solicitud.TopeDefensasMes;
        await _context.SaveChangesAsync();

        _parametros.Cargar(guardados);
        return _parametros;
    }
}
