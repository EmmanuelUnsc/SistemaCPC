using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Patrones.Singleton;

namespace SistemaCPC.Api.Controllers;

public class ParametrosSolicitud
{
    public decimal AporteMensual { get; set; }
    public decimal MontoPorDefensa { get; set; }
    public int UmbralInactividad { get; set; }
    public int TopeDefensasMes { get; set; }
}

[Authorize]
[ApiController]
[Route("api/parametros")]
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
    public ParametrosSistema Obtener() => _parametros;

    [Authorize(Roles = "Directorio")]
    [HttpPut]
    public async Task<ActionResult<ParametrosSistema>> Actualizar(ParametrosSolicitud solicitud)
    {
        if (solicitud.AporteMensual <= 0 || solicitud.MontoPorDefensa <= 0 || solicitud.UmbralInactividad < 1
            || solicitud.TopeDefensasMes < 0)
        {
            return BadRequest(new { mensaje = "Los valores deben ser positivos; el tope puede ser 0 (sin tope)." });
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
