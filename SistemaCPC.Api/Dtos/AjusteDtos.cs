using System.ComponentModel.DataAnnotations;

namespace SistemaCPC.Api.Dtos;

public class AjusteSolicitud
{
    [Required(ErrorMessage = "El registro del afiliado es obligatorio.")]
    public string Registro { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de ajuste es obligatorio.")]
    [StringLength(40, ErrorMessage = "El tipo tiene como máximo 40 caracteres.")]
    public string Tipo { get; set; } = string.Empty;

    [Range(-100000, 100000, ErrorMessage = "El monto está fuera de rango.")]
    public decimal Monto { get; set; }

    [Required(ErrorMessage = "El motivo es obligatorio.")]
    [StringLength(300, ErrorMessage = "El motivo tiene como máximo 300 caracteres.")]
    public string Motivo { get; set; } = string.Empty;
}

public record AjusteRespuesta(string Tipo, decimal Monto, string Motivo, DateOnly Fecha);

public record RegistroBitacoraRespuesta(DateTime Fecha, string Usuario, string Motivo, decimal ValorAnterior, decimal ValorNuevo);
