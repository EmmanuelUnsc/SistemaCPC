using System.ComponentModel.DataAnnotations;

namespace SistemaCPC.Api.Dtos;

public class LoginSolicitud
{
    [Required(ErrorMessage = "El registro es obligatorio.")]
    public string Registro { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Contrasena { get; set; } = string.Empty;
}

public class LoginRespuesta
{
    public int Id { get; set; }
    public string Registro { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool EsTribunal { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime Vence { get; set; }
}
