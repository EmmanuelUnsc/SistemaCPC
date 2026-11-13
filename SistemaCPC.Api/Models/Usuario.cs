using System.ComponentModel.DataAnnotations;

namespace SistemaCPC.Api.Models;

public enum Rol
{
    Afiliado,
    Directorio,
    Superadministrador
}

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

    [StringLength(20)]
    public string Carnet { get; set; } = string.Empty;

    public DateOnly FechaAfiliacion { get; set; }

    public DateOnly FechaInicioCobro { get; set; }

    public bool EsTribunal { get; set; }

    public DateOnly? FechaBaja { get; set; }

    [StringLength(20)]
    public string? MotivoBaja { get; set; }

    public List<Pago> Pagos { get; set; } = new();

    public List<Defensa> Defensas { get; set; } = new();

    public List<Ajuste> Ajustes { get; set; } = new();

    public int MesesTranscurridos(DateOnly hoy)
    {
        if (hoy < FechaInicioCobro)
        {
            return 0;
        }

        return (hoy.Year - FechaInicioCobro.Year) * 12 + hoy.Month - FechaInicioCobro.Month + 1;
    }

    public int MesesAdeudados(DateOnly hoy)
    {
        return Math.Max(0, MesesTranscurridos(hoy) - Pagos.Sum(p => p.MesesCubiertos));
    }

    public decimal CalcularDeuda(decimal aporteMensual, DateOnly hoy)
    {
        return Math.Max(0, MesesAdeudados(hoy) * aporteMensual - Ajustes.Sum(a => a.Monto));
    }

    public EstadoAfiliado ObtenerEstado(int umbralInactividad, DateOnly hoy)
    {
        if (FechaBaja != null)
        {
            return MotivoBaja == "Fallecimiento" ? EstadoAfiliado.Fallecido : EstadoAfiliado.Renunciado;
        }

        return MesesAdeudados(hoy) >= umbralInactividad ? EstadoAfiliado.Inactivo : EstadoAfiliado.Activo;
    }
}

public class MiembroDirectorio : Usuario
{
    [Required]
    [StringLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(60)]
    public string Cargo { get; set; } = string.Empty;
}

public class Superadministrador : MiembroDirectorio
{
}
