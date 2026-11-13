using System.ComponentModel.DataAnnotations;

namespace SistemaCPC.Api.Models;

public class Ajuste
{
    public int Id { get; set; }

    [Required]
    [StringLength(40)]
    public string Tipo { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    [Required]
    [StringLength(300)]
    public string Motivo { get; set; } = string.Empty;

    public DateOnly Fecha { get; set; }

    public int AfiliadoId { get; set; }
    public Afiliado? Afiliado { get; set; }

    public int SuperadministradorId { get; set; }
    public Superadministrador? Superadministrador { get; set; }
}

public class RegistroBitacora
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; }

    [StringLength(20)]
    public string Usuario { get; set; } = string.Empty;

    [StringLength(300)]
    public string Motivo { get; set; } = string.Empty;

    public decimal ValorAnterior { get; set; }

    public decimal ValorNuevo { get; set; }
}
