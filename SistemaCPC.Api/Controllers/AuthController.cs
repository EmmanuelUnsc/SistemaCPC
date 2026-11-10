using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Models;

namespace SistemaCPC.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly CpcContext _context;

    public AuthController(CpcContext context)
    {
        _context = context;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginRespuesta>> Login(LoginSolicitud solicitud)
    {
        var usuario = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Registro == solicitud.Registro);

        if (usuario == null)
        {
            return Unauthorized(new { mensaje = "Registro o contraseña incorrectos." });
        }

        var resultado = new PasswordHasher<Usuario>()
            .VerifyHashedPassword(usuario, usuario.ContrasenaCifrada, solicitud.Contrasena);

        if (resultado == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { mensaje = "Registro o contraseña incorrectos." });
        }

        return Ok(new LoginRespuesta
        {
            Id = usuario.Id,
            Registro = usuario.Registro,
            Nombre = usuario switch
            {
                Afiliado a => a.Nombre,
                MiembroDirectorio m => m.Nombre,
                _ => usuario.Registro
            },
            Rol = usuario.Rol.ToString()
        });
    }
}
