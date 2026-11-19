using System.ComponentModel.DataAnnotations;

namespace SistemaCPC.Api.Dtos;

public class AfiliadoSolicitud
{
    [Required(ErrorMessage = "El registro es obligatorio.")]
    [StringLength(20, ErrorMessage = "El registro tiene como máximo 20 caracteres.")]
    public string Registro { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120, ErrorMessage = "El nombre tiene como máximo 120 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El carnet es obligatorio.")]
    [StringLength(20, ErrorMessage = "El carnet tiene como máximo 20 caracteres.")]
    public string Carnet { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de afiliación es obligatoria.")]
    public DateOnly? FechaAfiliacion { get; set; }

    [Required(ErrorMessage = "La fecha de inicio de cobro es obligatoria.")]
    public DateOnly? FechaInicioCobro { get; set; }

    public bool EsTribunal { get; set; }

    [StringLength(100, MinimumLength = 4, ErrorMessage = "La contraseña debe tener entre 4 y 100 caracteres.")]
    public string? Contrasena { get; set; }
}

public class ActualizarAfiliadoSolicitud
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120, ErrorMessage = "El nombre tiene como máximo 120 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El carnet es obligatorio.")]
    [StringLength(20, ErrorMessage = "El carnet tiene como máximo 20 caracteres.")]
    public string Carnet { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de afiliación es obligatoria.")]
    public DateOnly? FechaAfiliacion { get; set; }

    [Required(ErrorMessage = "La fecha de inicio de cobro es obligatoria.")]
    public DateOnly? FechaInicioCobro { get; set; }

    public bool EsTribunal { get; set; }

    [StringLength(100, MinimumLength = 4, ErrorMessage = "La contraseña debe tener entre 4 y 100 caracteres.")]
    public string? Contrasena { get; set; }
}

public class BajaSolicitud
{
    [Required(ErrorMessage = "El motivo de la baja es obligatorio.")]
    [RegularExpression("^(Renuncia|Fallecimiento)$", ErrorMessage = "El motivo de la baja es Renuncia o Fallecimiento.")]
    public string Motivo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de la baja es obligatoria.")]
    public DateOnly? Fecha { get; set; }
}

public record AfiliadoRespuesta(string Registro, string Nombre, string Carnet, DateOnly FechaAfiliacion,
    DateOnly FechaInicioCobro, bool EsTribunal, bool TieneFoto, string Estado, int MesesAdeudados, decimal Deuda,
    DateOnly? FechaBaja, string? MotivoBaja);
