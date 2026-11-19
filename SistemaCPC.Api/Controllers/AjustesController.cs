using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Dtos;
using SistemaCPC.Api.Models;
using SistemaCPC.Api.Patrones.Decorator;

namespace SistemaCPC.Api.Controllers;

[Authorize(Roles = "Directorio")]
[ApiController]
[Route("api/ajustes")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class AjustesController : ControllerBase
{
    private readonly CpcContext _context;
    private readonly IServicioAjustes _ajustes;

    public AjustesController(CpcContext context, IServicioAjustes ajustes)
    {
        _context = context;
        _ajustes = ajustes;
    }

    [Authorize(Roles = "Superadministrador")]
    [HttpPost]
    [EndpointSummary("CU-10: registrar un ajuste a la deuda (superadministrador; queda en la bitácora)")]
    [ProducesResponseType<AjusteRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AjusteRespuesta>> Registrar(AjusteSolicitud solicitud)
    {
        if (string.IsNullOrWhiteSpace(solicitud.Tipo) || string.IsNullOrWhiteSpace(solicitud.Motivo) || solicitud.Monto == 0)
        {
            return BadRequest(new ErrorRespuesta("El tipo, el monto y el motivo son obligatorios."));
        }

        var superadministrador = await _context.Usuarios.OfType<Superadministrador>()
            .FirstAsync(u => u.Registro == User.Identity!.Name);

        var afiliado = await _context.Afiliados
            .Include(a => a.Pagos)
            .Include(a => a.Ajustes)
            .FirstOrDefaultAsync(a => a.Registro == solicitud.Registro);

        if (afiliado == null)
        {
            return NotFound(new ErrorRespuesta("No existe un afiliado con ese registro."));
        }

        var ajuste = new Ajuste
        {
            Tipo = solicitud.Tipo, Monto = solicitud.Monto, Motivo = solicitud.Motivo,
            SuperadministradorId = superadministrador.Id
        };

        var registrado = await _ajustes.Registrar(ajuste, afiliado, superadministrador.Registro);
        return Ok(new AjusteRespuesta(registrado.Tipo, registrado.Monto, registrado.Motivo, registrado.Fecha));
    }

    [HttpGet("bitacora")]
    [EndpointSummary("Bitácora de ajustes y correcciones de pagos")]
    [ProducesResponseType<List<RegistroBitacoraRespuesta>>(StatusCodes.Status200OK)]
    public async Task<List<RegistroBitacoraRespuesta>> Bitacora()
    {
        return await _context.Bitacora
            .OrderByDescending(b => b.Fecha)
            .Select(b => new RegistroBitacoraRespuesta(b.Fecha, b.Usuario, b.Motivo, b.ValorAnterior, b.ValorNuevo))
            .ToListAsync();
    }
}
