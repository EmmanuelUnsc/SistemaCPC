using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Dtos;
using SistemaCPC.Api.Models;
using SistemaCPC.Api.Seguridad;

namespace SistemaCPC.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly CpcContext _context;
    private readonly EmisorToken _emisor;

    public AuthController(CpcContext context, EmisorToken emisor)
    {
        _context = context;
        _emisor = emisor;
    }

    [HttpPost("login")]
    [EndpointSummary("CU-01: iniciar sesión y obtener el token (dura 15 minutos)")]
    [ProducesResponseType<LoginRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginRespuesta>> Login(LoginSolicitud solicitud)
    {
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Registro == solicitud.Registro);

        if (usuario == null)
        {
            return Unauthorized(new ErrorRespuesta("Registro o contraseña incorrectos."));
        }

        var resultado = new PasswordHasher<Usuario>()
            .VerifyHashedPassword(usuario, usuario.ContrasenaCifrada, solicitud.Contrasena);

        if (resultado == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new ErrorRespuesta("Registro o contraseña incorrectos."));
        }

        return Ok(Respuesta(usuario));
    }

    [Authorize]
    [HttpPost("renovar")]
    [EndpointSummary("Renovar el token antes de que venza")]
    [ProducesResponseType<LoginRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorRespuesta>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginRespuesta>> Renovar()
    {
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Registro == User.Identity!.Name);

        if (usuario == null)
        {
            return Unauthorized(new ErrorRespuesta("El usuario ya no existe."));
        }

        return Ok(Respuesta(usuario));
    }

    private LoginRespuesta Respuesta(Usuario usuario)
    {
        var token = _emisor.Emitir(usuario);

        return new LoginRespuesta
        {
            Id = usuario.Id,
            Registro = usuario.Registro,
            Nombre = usuario switch
            {
                Afiliado a => a.Nombre,
                MiembroDirectorio m => m.Nombre,
                _ => usuario.Registro
            },
            Rol = usuario.Rol.ToString(),
            Token = token.Token,
            Vence = token.Vence
        };
    }
}
