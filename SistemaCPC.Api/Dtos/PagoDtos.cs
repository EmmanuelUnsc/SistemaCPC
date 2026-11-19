using System.ComponentModel.DataAnnotations;

namespace SistemaCPC.Api.Dtos;

public class PagoPlataformaSolicitud
{
    [Range(1, 120, ErrorMessage = "Los meses cubiertos van de 1 a 120.")]
    public int MesesCubiertos { get; set; }
}

public class PagoOficinaSolicitud
{
    [Required(ErrorMessage = "El registro del afiliado es obligatorio.")]
    public string Registro { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de recibo es obligatorio.")]
    [StringLength(20, ErrorMessage = "El número de recibo tiene como máximo 20 caracteres.")]
    public string NumeroRecibo { get; set; } = string.Empty;

    public DateOnly? Fecha { get; set; }

    [Range(1, 120, ErrorMessage = "Los meses cubiertos van de 1 a 120.")]
    public int MesesCubiertos { get; set; }
}

public class CorreccionPagoSolicitud
{
    [Range(1, 120, ErrorMessage = "Los meses cubiertos van de 1 a 120.")]
    public int MesesCubiertos { get; set; }

    [Range(0.01, 100000, ErrorMessage = "El monto debe ser mayor a 0.")]
    public decimal Monto { get; set; }

    [Required(ErrorMessage = "El motivo de la corrección es obligatorio.")]
    [StringLength(300, ErrorMessage = "El motivo tiene como máximo 300 caracteres.")]
    public string Motivo { get; set; } = string.Empty;
}

public record PagoRespuesta(string NumeroRecibo, DateOnly Fecha, decimal Monto, int MesesCubiertos, string Origen);
