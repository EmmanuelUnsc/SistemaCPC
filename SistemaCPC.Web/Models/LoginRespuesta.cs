namespace SistemaCPC.Web.Models;

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
