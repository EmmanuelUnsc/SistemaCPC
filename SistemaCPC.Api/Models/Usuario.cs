using System.ComponentModel.DataAnnotations;

namespace SistemaCPC.Api.Models;

// Roles del sistema (enumeración Rol del diagrama de clases).
public enum Rol
{
    Afiliado,
    Directorio,
    Superadministrador
}

// Clase base de Afiliado y MiembroDirectorio (una sola tabla "Usuarios").
// Sprint 1: solo los datos que necesita CU-01 Iniciar sesión.
public abstract class Usuario
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string Registro { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string ContrasenaCifrada { get; set; } = string.Empty;

    public Rol Rol { get; set; }
}

public class Afiliado : Usuario
{
    [Required]
    [StringLength(120)]
    public string Nombre { get; set; } = string.Empty;
}

public class MiembroDirectorio : Usuario
{
    [Required]
    [StringLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(60)]
    public string Cargo { get; set; } = string.Empty;
}

// Miembro del directorio que además registra ajustes (CU-10).
public class Superadministrador : MiembroDirectorio
{
}
