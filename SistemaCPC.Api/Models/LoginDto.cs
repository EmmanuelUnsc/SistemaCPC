using System.ComponentModel.DataAnnotations;

namespace SistemaCPC.Api.Models;

// Lo que recibe POST /api/auth/login.
public class LoginSolicitud
{
    [Required(ErrorMessage = "El registro es obligatorio.")]
    public string Registro { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Contrasena { get; set; } = string.Empty;
}

// Lo que devuelve la API cuando el inicio de sesión es correcto.
public class LoginRespuesta
{
    public int Id { get; set; }
    public string Registro { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}
