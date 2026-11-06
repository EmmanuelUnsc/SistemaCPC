namespace SistemaCPC.Web.Models;

// Lo que devuelve POST /api/auth/login de SistemaCPC.Api.
public class LoginRespuesta
{
    public int Id { get; set; }
    public string Registro { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}
