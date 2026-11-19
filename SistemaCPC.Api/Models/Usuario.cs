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
        var fin = FechaBaja != null && FechaBaja < hoy ? FechaBaja.Value : hoy;

        if (fin < FechaInicioCobro)
        {
            return 0;
        }

        return (fin.Year - FechaInicioCobro.Year) * 12 + fin.Month - FechaInicioCobro.Month + 1;
    }

    public decimal CalcularDeuda(decimal aporteMensual, DateOnly hoy)
    {
        return MesesTranscurridos(hoy) * aporteMensual - Pagos.Sum(p => p.Monto) - Ajustes.Sum(a => a.Monto);
    }

    public int MesesAdeudados(decimal aporteMensual, DateOnly hoy)
    {
        var deuda = CalcularDeuda(aporteMensual, hoy);
        return deuda <= 0 || aporteMensual <= 0 ? 0 : (int)Math.Ceiling(deuda / aporteMensual);
    }

    public List<(DateOnly Mes, decimal Monto)> ListarMesesAdeudados(decimal aporteMensual, DateOnly hoy)
    {
        var deuda = CalcularDeuda(aporteMensual, hoy);
        var cantidad = MesesAdeudados(aporteMensual, hoy);
        var fin = FechaBaja != null && FechaBaja < hoy ? FechaBaja.Value : hoy;
        var ultimoMes = new DateOnly(fin.Year, fin.Month, 1);

        return Enumerable.Range(0, cantidad)
            .Select(i => (ultimoMes.AddMonths(i - cantidad + 1), i == 0 ? deuda - (cantidad - 1) * aporteMensual : aporteMensual))
            .ToList();
    }

    public EstadoAfiliado ObtenerEstado(int umbralInactividad, decimal aporteMensual, DateOnly hoy)
    {
        if (FechaBaja != null)
        {
            return MotivoBaja == "Fallecimiento" ? EstadoAfiliado.Fallecido : EstadoAfiliado.Renunciado;
        }

        return CalcularDeuda(aporteMensual, hoy) > umbralInactividad * aporteMensual
            ? EstadoAfiliado.Inactivo
            : EstadoAfiliado.Activo;
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
