using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Models;
using SistemaCPC.Api.Patrones.Facade;

namespace SistemaCPC.Api.Controllers;

public class AfiliadoSolicitud
{
    public string Registro { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Carnet { get; set; } = string.Empty;
    public DateOnly FechaAfiliacion { get; set; }
    public DateOnly FechaInicioCobro { get; set; }
    public bool EsTribunal { get; set; }
    public string? Contrasena { get; set; }
}

public class BajaSolicitud
{
    public string Motivo { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
}

public record AfiliadoRespuesta(string Registro, string Nombre, string Carnet, DateOnly FechaAfiliacion,
    DateOnly FechaInicioCobro, bool EsTribunal, bool TieneFoto, string Estado, int MesesAdeudados, decimal Deuda,
    DateOnly? FechaBaja, string? MotivoBaja);

[Authorize(Roles = "Directorio")]
[ApiController]
[Route("api/afiliados")]
public class AfiliadosController : ControllerBase
{
    private static readonly string[] MotivosDeBaja = { "Renuncia", "Fallecimiento" };
    private static readonly string[] TiposDeFoto = { "image/jpeg", "image/png" };
    private const int TamanoMaximoFoto = 2 * 1024 * 1024;

    private readonly CpcContext _context;
    private readonly EstadoDeCuenta _estadoDeCuenta;

    public AfiliadosController(CpcContext context, EstadoDeCuenta estadoDeCuenta)
    {
        _context = context;
        _estadoDeCuenta = estadoDeCuenta;
    }

    [HttpGet]
    public async Task<List<AfiliadoRespuesta>> Listar()
    {
        var afiliados = await ConDatosDeCuenta().OrderBy(a => a.Registro).ToListAsync();
        var conFoto = (await _context.FotosAfiliados.Select(f => f.AfiliadoId).ToListAsync()).ToHashSet();
        return afiliados.Select(a => Respuesta(a, conFoto.Contains(a.Id))).ToList();
    }

    [HttpGet("{registro}")]
    public async Task<ActionResult<AfiliadoRespuesta>> Obtener(string registro)
    {
        var afiliado = await ConDatosDeCuenta().FirstOrDefaultAsync(a => a.Registro == registro);

        if (afiliado == null)
        {
            return NotFound(new { mensaje = "No existe un afiliado con ese registro." });
        }

        return Respuesta(afiliado);
    }

    [HttpPost]
    public async Task<ActionResult<AfiliadoRespuesta>> Crear(AfiliadoSolicitud solicitud)
    {
        var error = Validar(solicitud);

        if (error == null && (solicitud.Contrasena ?? string.Empty).Length < 4)
        {
            error = "La contraseña inicial debe tener al menos 4 caracteres.";
        }

        if (error != null)
        {
            return BadRequest(new { mensaje = error });
        }

        if (await _context.Usuarios.AnyAsync(u => u.Registro == solicitud.Registro))
        {
            return Conflict(new { mensaje = "Ya existe un usuario con ese número de registro." });
        }

        var afiliado = new Afiliado { Registro = solicitud.Registro.Trim(), Rol = Rol.Afiliado };
        Copiar(solicitud, afiliado);
        afiliado.ContrasenaCifrada = new PasswordHasher<Usuario>().HashPassword(afiliado, solicitud.Contrasena!);

        _context.Afiliados.Add(afiliado);
        await _context.SaveChangesAsync();

        return Ok(Respuesta(afiliado));
    }

    [HttpPut("{registro}")]
    public async Task<ActionResult<AfiliadoRespuesta>> Actualizar(string registro, AfiliadoSolicitud solicitud)
    {
        solicitud.Registro = registro;
        var error = Validar(solicitud);

        if (error == null && !string.IsNullOrEmpty(solicitud.Contrasena) && solicitud.Contrasena.Length < 4)
        {
            error = "La contraseña nueva debe tener al menos 4 caracteres.";
        }

        if (error != null)
        {
            return BadRequest(new { mensaje = error });
        }

        var afiliado = await ConDatosDeCuenta().FirstOrDefaultAsync(a => a.Registro == registro);

        if (afiliado == null)
        {
            return NotFound(new { mensaje = "No existe un afiliado con ese registro." });
        }

        Copiar(solicitud, afiliado);

        if (!string.IsNullOrEmpty(solicitud.Contrasena))
        {
            afiliado.ContrasenaCifrada = new PasswordHasher<Usuario>().HashPassword(afiliado, solicitud.Contrasena);
        }

        await _context.SaveChangesAsync();
        return Respuesta(afiliado);
    }

    [HttpPost("{registro}/baja")]
    public async Task<ActionResult<AfiliadoRespuesta>> RegistrarBaja(string registro, BajaSolicitud solicitud)
    {
        if (!MotivosDeBaja.Contains(solicitud.Motivo))
        {
            return BadRequest(new { mensaje = "El motivo de la baja es Renuncia o Fallecimiento." });
        }

        var afiliado = await ConDatosDeCuenta().FirstOrDefaultAsync(a => a.Registro == registro);

        if (afiliado == null)
        {
            return NotFound(new { mensaje = "No existe un afiliado con ese registro." });
        }

        if (afiliado.FechaBaja != null)
        {
            return BadRequest(new { mensaje = "El afiliado ya tiene una baja registrada." });
        }

        if (solicitud.Fecha < afiliado.FechaAfiliacion || solicitud.Fecha > DateOnly.FromDateTime(DateTime.Today))
        {
            return BadRequest(new { mensaje = "La fecha de baja debe estar entre la afiliación y hoy." });
        }

        afiliado.FechaBaja = solicitud.Fecha;
        afiliado.MotivoBaja = solicitud.Motivo;
        await _context.SaveChangesAsync();

        return Respuesta(afiliado);
    }

    [HttpPut("{registro}/foto")]
    public async Task<IActionResult> GuardarFoto(string registro, IFormFile foto)
    {
        if (!TiposDeFoto.Contains(foto.ContentType) || foto.Length > TamanoMaximoFoto)
        {
            return BadRequest(new { mensaje = "La foto debe ser JPG o PNG de hasta 2 MB." });
        }

        var afiliado = await _context.Afiliados.FirstOrDefaultAsync(a => a.Registro == registro);

        if (afiliado == null)
        {
            return NotFound(new { mensaje = "No existe un afiliado con ese registro." });
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
    public async Task<IActionResult> ObtenerFoto(string registro)
    {
        var foto = await _context.FotosAfiliados.FirstOrDefaultAsync(f => f.Afiliado!.Registro == registro);

        if (foto == null)
        {
            return NotFound(new { mensaje = "El afiliado no tiene foto." });
        }

        return File(foto.Datos, foto.Tipo);
    }

    private IQueryable<Afiliado> ConDatosDeCuenta() =>
        _context.Afiliados.Include(a => a.Pagos).Include(a => a.Ajustes);

    private static string? Validar(AfiliadoSolicitud s)
    {
        if (string.IsNullOrWhiteSpace(s.Registro) || string.IsNullOrWhiteSpace(s.Nombre) || string.IsNullOrWhiteSpace(s.Carnet))
        {
            return "El registro, el nombre y el carnet son obligatorios.";
        }

        if (s.FechaAfiliacion == default || s.FechaInicioCobro == default)
        {
            return "La fecha de afiliación y la de inicio de cobro son obligatorias.";
        }

        return null;
    }

    private static void Copiar(AfiliadoSolicitud s, Afiliado a)
    {
        a.Nombre = s.Nombre.Trim();
        a.Carnet = s.Carnet.Trim();
        a.FechaAfiliacion = s.FechaAfiliacion;
        a.FechaInicioCobro = s.FechaInicioCobro;
        a.EsTribunal = s.EsTribunal;
    }

    private AfiliadoRespuesta Respuesta(Afiliado a) =>
        Respuesta(a, _context.FotosAfiliados.Any(f => f.AfiliadoId == a.Id));

    private AfiliadoRespuesta Respuesta(Afiliado a, bool tieneFoto)
    {
        var resumen = _estadoDeCuenta.Resumir(a);
        return new AfiliadoRespuesta(a.Registro, a.Nombre, a.Carnet, a.FechaAfiliacion, a.FechaInicioCobro,
            a.EsTribunal, tieneFoto, resumen.Estado.ToString(), resumen.MesesAdeudados, resumen.Deuda,
            a.FechaBaja, a.MotivoBaja);
    }
}
