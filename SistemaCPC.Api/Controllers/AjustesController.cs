using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Models;
using SistemaCPC.Api.Patrones.Decorator;

namespace SistemaCPC.Api.Controllers;

public class AjusteSolicitud
{
    public string Registro { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

public record AjusteRespuesta(string Tipo, decimal Monto, string Motivo, DateOnly Fecha);

public record RegistroBitacoraRespuesta(DateTime Fecha, string Usuario, string Motivo, decimal ValorAnterior, decimal ValorNuevo);

[Authorize(Roles = "Directorio")]
[ApiController]
[Route("api/ajustes")]
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
    public async Task<ActionResult<AjusteRespuesta>> Registrar(AjusteSolicitud solicitud)
    {
        if (string.IsNullOrWhiteSpace(solicitud.Tipo) || string.IsNullOrWhiteSpace(solicitud.Motivo) || solicitud.Monto == 0)
        {
            return BadRequest(new { mensaje = "El tipo, el monto y el motivo son obligatorios." });
        }

        var superadministrador = await _context.Usuarios.OfType<Superadministrador>()
            .FirstAsync(u => u.Registro == User.Identity!.Name);

        var afiliado = await _context.Afiliados
            .Include(a => a.Pagos)
            .Include(a => a.Ajustes)
            .FirstOrDefaultAsync(a => a.Registro == solicitud.Registro);

        if (afiliado == null)
        {
            return NotFound(new { mensaje = "No existe un afiliado con ese registro." });
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
    public async Task<List<RegistroBitacoraRespuesta>> Bitacora()
    {
        return await _context.Bitacora
            .OrderByDescending(b => b.Fecha)
            .Select(b => new RegistroBitacoraRespuesta(b.Fecha, b.Usuario, b.Motivo, b.ValorAnterior, b.ValorNuevo))
            .ToListAsync();
    }
}
