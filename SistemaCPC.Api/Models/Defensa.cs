using System.ComponentModel.DataAnnotations;

namespace SistemaCPC.Api.Models;

public class Defensa
{
    public int Id { get; set; }

    [Required]
    [StringLength(30)]
    public string NumeroOficio { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string Universidad { get; set; } = string.Empty;

    [StringLength(40)]
    public string Carta { get; set; } = string.Empty;

    public DateOnly Fecha { get; set; }

    public EstadoDefensa Estado { get; set; } = EstadoDefensa.PorPagar;

    public int AfiliadoId { get; set; }
    public Afiliado? Afiliado { get; set; }
}
