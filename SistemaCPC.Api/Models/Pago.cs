using System.ComponentModel.DataAnnotations;

namespace SistemaCPC.Api.Models;

public class Pago
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string NumeroRecibo { get; set; } = string.Empty;

    public DateOnly Fecha { get; set; }

    public decimal Monto { get; set; }

    public int MesesCubiertos { get; set; }

    public OrigenPago Origen { get; set; }

    public int AfiliadoId { get; set; }
    public Afiliado? Afiliado { get; set; }
}
