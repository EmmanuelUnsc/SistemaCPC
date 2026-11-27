using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Dtos;
using SistemaCPC.Api.Models;
using SistemaCPC.Api.Servicios;

namespace SistemaCPC.Api.Controllers;

[Authorize(Roles = "Directorio")]
[ApiController]
[Route("api/afiliados")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class AfiliadosController : ControllerBase
{
    private static readonly string[] TiposDeFoto = { "image/jpeg", "image/png" };
    private const int TamanoMaximoFoto = 2 * 1024 * 1024;

    private readonly CpcContext _context;
    private readonly EstadoDeCuenta _estadoDeCuenta;
    private readonly ServicioBitacora _bitacora;

    public AfiliadosController(CpcContext context, EstadoDeCuenta estadoDeCuenta, ServicioBitacora bitacora)
    {
        _context = context;
        _estadoDeCuenta = estadoDeCuenta;
        _bitacora = bitacora;
    }

    [HttpGet]
    [EndpointSummary("CU-06: lista de afiliados con su estado y su deuda")]
    [ProducesResponseType<List<AfiliadoRespuesta>>(StatusCodes.Status200OK)]
    public async Task<List<AfiliadoRespuesta>> Listar()
    {
        var afiliados = await ConDatosDeCuenta().OrderBy(a => a.Registro).ToListAsync();
        var conFoto = (await _context.FotosAfiliados.Select(f => f.AfiliadoId).ToListAsync()).ToHashSet();
        return afiliados.Select(a => Respuesta(a, conFoto.Contains(a.Id))).ToList();
    }

    [HttpGet("{registro}")]
    [EndpointSummary("CU-06: un afiliado por su número de registro")]
    [ProducesResponseType<AfiliadoRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AfiliadoRespuesta>> Obtener(string registro)
    {
        var afiliado = await ConDatosDeCuenta().FirstOrDefaultAsync(a => a.Registro == registro);

        if (afiliado == null)
        {
            return NotFound(new ErrorRespuesta("No existe un afiliado con ese registro."));
        }

        return await RespuestaAsync(afiliado);
    }

    [HttpGet("{registro}/pagos")]
    [EndpointSummary("CU-05: pagos de un afiliado (recurso relacionado)")]
    [ProducesResponseType<List<PagoRespuesta>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PagoRespuesta>>> ListarPagos(string registro)
    {
        var afiliado = await _context.Afiliados.Include(a => a.Pagos).FirstOrDefaultAsync(a => a.Registro == registro);

        if (afiliado == null)
        {
            return NotFound(new ErrorRespuesta("No existe un afiliado con ese registro."));
        }

        return afiliado.Pagos
            .OrderByDescending(p => p.Fecha)
            .Select(p => new PagoRespuesta(p.NumeroRecibo, p.Fecha, p.Monto, p.MesesCubiertos, p.Origen.ToString()))
            .ToList();
    }

    [HttpPost]
    [EndpointSummary("CU-06: registrar un afiliado con su contraseña inicial")]
    [ProducesResponseType<AfiliadoRespuesta>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AfiliadoRespuesta>> Crear(AfiliadoSolicitud solicitud)
    {
        if (string.IsNullOrEmpty(solicitud.Contrasena))
        {
            return BadRequest(new ErrorRespuesta("La contraseña inicial es obligatoria al registrar un afiliado."));
        }

        var error = ValidarFechas(solicitud.FechaAfiliacion!.Value, solicitud.FechaInicioCobro!.Value);

        if (error != null)
        {
            return BadRequest(new ErrorRespuesta(error));
        }

        var registro = solicitud.Registro.Trim();

        if (await _context.Usuarios.AnyAsync(u => u.Registro == registro))
        {
            return Conflict(new ErrorRespuesta("Ya existe un usuario con ese número de registro."));
        }

        var afiliado = new Afiliado
        {
            Registro = registro,
            Rol = Rol.Afiliado,
            Nombre = solicitud.Nombre.Trim(),
            Carnet = (solicitud.Carnet ?? string.Empty).Trim(),
            FechaAfiliacion = solicitud.FechaAfiliacion.Value, FechaInicioCobro = solicitud.FechaInicioCobro.Value,
            EsTribunal = solicitud.EsTribunal
        };
        afiliado.ContrasenaCifrada = new PasswordHasher<Usuario>().HashPassword(afiliado, solicitud.Contrasena);

        _context.Afiliados.Add(afiliado);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(Obtener), new { registro = afiliado.Registro }, Respuesta(afiliado, false));
    }

    [HttpPut("{registro}")]
    [EndpointSummary("CU-06: actualizar los datos de un afiliado (la contraseña solo si se envía)")]
    [ProducesResponseType<AfiliadoRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AfiliadoRespuesta>> Actualizar(string registro, ActualizarAfiliadoSolicitud solicitud)
    {
        var afiliado = await ConDatosDeCuenta().FirstOrDefaultAsync(a => a.Registro == registro);

        if (afiliado == null)
        {
            return NotFound(new ErrorRespuesta("No existe un afiliado con ese registro."));
        }

        if (afiliado.FechaAfiliacion != solicitud.FechaAfiliacion || afiliado.FechaInicioCobro != solicitud.FechaInicioCobro)
        {
            var error = ValidarFechas(solicitud.FechaAfiliacion!.Value, solicitud.FechaInicioCobro!.Value);

            if (error != null)
            {
                return BadRequest(new ErrorRespuesta(error));
            }
        }

        var cobroAnterior = afiliado.FechaInicioCobro;
        var deudaAnterior = _estadoDeCuenta.Resumir(afiliado).Deuda;

        afiliado.Nombre = solicitud.Nombre.Trim();
        afiliado.Carnet = (solicitud.Carnet ?? string.Empty).Trim();
        afiliado.FechaAfiliacion = solicitud.FechaAfiliacion!.Value;
        afiliado.FechaInicioCobro = solicitud.FechaInicioCobro!.Value;
        afiliado.EsTribunal = solicitud.EsTribunal;

        if (!string.IsNullOrEmpty(solicitud.Contrasena))
        {
            afiliado.ContrasenaCifrada = new PasswordHasher<Usuario>().HashPassword(afiliado, solicitud.Contrasena);
        }

        if (afiliado.FechaInicioCobro != cobroAnterior)
        {
            _bitacora.Anotar(User.Identity!.Name!,
                $"Inicio de cobro ({afiliado.Registro}): {cobroAnterior:dd/MM/yyyy} a {afiliado.FechaInicioCobro:dd/MM/yyyy}",
                deudaAnterior, _estadoDeCuenta.Resumir(afiliado).Deuda);
        }

        await _context.SaveChangesAsync();
        return await RespuestaAsync(afiliado);
    }

    [HttpDelete("{registro}")]
    [EndpointSummary("CU-06: eliminar un afiliado cargado por error (solo si no tiene pagos, defensas ni ajustes)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(string registro)
    {
        var afiliado = await _context.Afiliados
            .Include(a => a.Pagos)
            .Include(a => a.Defensas)
            .Include(a => a.Ajustes)
            .FirstOrDefaultAsync(a => a.Registro == registro);

        if (afiliado == null)
        {
            return NotFound(new ErrorRespuesta("No existe un afiliado con ese registro."));
        }

        if (afiliado.Pagos.Count > 0 || afiliado.Defensas.Count > 0 || afiliado.Ajustes.Count > 0)
        {
            return Conflict(new ErrorRespuesta("El afiliado tiene pagos, defensas o ajustes: no se puede eliminar. Registre su baja con POST /api/afiliados/{registro}/baja."));
        }

        _context.Afiliados.Remove(afiliado);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{registro}/baja")]
    [EndpointSummary("CU-06, RF-25: registrar la baja por renuncia o fallecimiento; desde esa fecha no se suman aportes")]
    [ProducesResponseType<AfiliadoRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AfiliadoRespuesta>> RegistrarBaja(string registro, BajaSolicitud solicitud)
    {
        var afiliado = await ConDatosDeCuenta().FirstOrDefaultAsync(a => a.Registro == registro);

        if (afiliado == null)
        {
            return NotFound(new ErrorRespuesta("No existe un afiliado con ese registro."));
        }

        if (afiliado.FechaBaja != null)
        {
            return BadRequest(new ErrorRespuesta("El afiliado ya tiene una baja registrada."));
        }

        var fecha = solicitud.Fecha!.Value;

        if (fecha < afiliado.FechaAfiliacion || fecha > Hoy)
        {
            return BadRequest(new ErrorRespuesta("La fecha de baja debe estar entre la afiliación y hoy."));
        }

        var deudaAnterior = _estadoDeCuenta.Resumir(afiliado).Deuda;
        afiliado.FechaBaja = fecha;
        afiliado.MotivoBaja = solicitud.Motivo;
        _bitacora.Anotar(User.Identity!.Name!, $"Baja por {solicitud.Motivo} ({afiliado.Registro}) desde {fecha:dd/MM/yyyy}",
            deudaAnterior, _estadoDeCuenta.Resumir(afiliado).Deuda);
        await _context.SaveChangesAsync();

        return await RespuestaAsync(afiliado);
    }

    [HttpPut("{registro}/foto")]
    [EndpointSummary("CU-06: guardar la foto del afiliado (JPG o PNG, hasta 2 MB)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GuardarFoto(string registro, IFormFile foto)
    {
        if (!TiposDeFoto.Contains(foto.ContentType) || foto.Length > TamanoMaximoFoto)
        {
            return BadRequest(new ErrorRespuesta("La foto debe ser JPG o PNG de hasta 2 MB."));
        }

        var afiliado = await _context.Afiliados.FirstOrDefaultAsync(a => a.Registro == registro);

        if (afiliado == null)
        {
            return NotFound(new ErrorRespuesta("No existe un afiliado con ese registro."));
        }

        var guardada = await _context.FotosAfiliados.FindAsync(afiliado.Id);

        if (guardada == null)
        {
            guardada = new FotoAfiliado { AfiliadoId = afiliado.Id };
            _context.FotosAfiliados.Add(guardada);
        }

        using var memoria = new MemoryStream();
        await foto.CopyToAsync(memoria);
        guardada.Datos = memoria.ToArray();
        guardada.Tipo = foto.ContentType;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{registro}/foto")]
    [EndpointSummary("CU-06: ver la foto del afiliado")]
    [Produces("image/jpeg", "image/png", "application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerFoto(string registro)
    {
        var foto = await _context.FotosAfiliados.FirstOrDefaultAsync(f => f.Afiliado!.Registro == registro);

        if (foto == null)
        {
            return NotFound(new ErrorRespuesta("El afiliado no tiene foto."));
        }

        return File(foto.Datos, foto.Tipo);
    }

    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.Today);

    private static string? ValidarFechas(DateOnly afiliacion, DateOnly inicioCobro)
    {
        if (afiliacion > Hoy)
        {
            return "La fecha de afiliación no puede ser posterior a hoy.";
        }

        var mesDeAfiliacion = new DateOnly(afiliacion.Year, afiliacion.Month, 1);
        return inicioCobro < mesDeAfiliacion ? "El inicio de cobro no puede ser anterior a la fecha de afiliación." : null;
    }

    private IQueryable<Afiliado> ConDatosDeCuenta() =>
        _context.Afiliados.Include(a => a.Pagos).Include(a => a.Ajustes);

    private async Task<AfiliadoRespuesta> RespuestaAsync(Afiliado a) =>
        Respuesta(a, await _context.FotosAfiliados.AnyAsync(f => f.AfiliadoId == a.Id));

    private AfiliadoRespuesta Respuesta(Afiliado a, bool tieneFoto)
    {
        var resumen = _estadoDeCuenta.Resumir(a);
        return new AfiliadoRespuesta(a.Registro, a.Nombre, a.Carnet, a.FechaAfiliacion, a.FechaInicioCobro,
            a.EsTribunal, tieneFoto, resumen.Estado.ToString(), resumen.MesesAdeudados, resumen.Deuda,
            a.FechaBaja, a.MotivoBaja);
    }
}
