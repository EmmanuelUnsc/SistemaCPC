using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SistemaCPC.Api.Models;

namespace SistemaCPC.Api.Seguridad;

public class OpcionesToken
{
    public string Clave { get; set; } = string.Empty;
    public string Emisor { get; set; } = string.Empty;
    public int MinutosDeVida { get; set; } = 15;

    public SymmetricSecurityKey Llave() => new(Encoding.UTF8.GetBytes(Clave));
}

public record TokenEmitido(string Token, DateTime Vence);

public class EmisorToken
{
    private readonly OpcionesToken _opciones;

    public EmisorToken(OpcionesToken opciones)
    {
        _opciones = opciones;
    }

    public TokenEmitido Emitir(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new("name", usuario.Registro),
            new("role", usuario.Rol.ToString())
        };

        if (usuario.Rol == Rol.Superadministrador)
        {
            claims.Add(new Claim("role", Rol.Directorio.ToString()));
        }

        var vence = DateTime.UtcNow.AddMinutes(_opciones.MinutosDeVida);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _opciones.Emisor,
            Audience = _opciones.Emisor,
            Expires = vence,
            SigningCredentials = new SigningCredentials(_opciones.Llave(), SecurityAlgorithms.HmacSha256)
        });

        return new TokenEmitido(token, vence);
    }
}

public static class Permisos
{
    public static bool PuedeVer(this ClaimsPrincipal usuario, string registro) =>
        usuario.IsInRole(Rol.Directorio.ToString()) || usuario.Identity?.Name == registro;
}
