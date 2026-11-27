namespace SistemaCPC.Web.Models;

public record AnioAdeudado(int Anio, List<string> Meses, decimal Monto);

public record PagoDelHistorial(string NumeroRecibo, DateOnly Fecha, decimal Monto, int MesesCubiertos);

public record EstadoCuenta(string Registro, string Nombre, string Estado, int MesesAdeudados, decimal Deuda,
    decimal AporteMensual, List<AnioAdeudado> MesesPorAnio, List<PagoDelHistorial> Pagos);

public record PagoRegistrado(string NumeroRecibo, DateOnly Fecha, decimal Monto, int MesesCubiertos, string Origen);

public record AfiliadoDatos(string Registro, string Nombre, string Carnet, DateOnly FechaAfiliacion,
    DateOnly FechaInicioCobro, bool EsTribunal, bool TieneFoto, string Estado, int MesesAdeudados, decimal Deuda,
    DateOnly? FechaBaja, string? MotivoBaja);

public record AjusteRegistrado(string Tipo, decimal Monto, string Motivo, DateOnly Fecha);

public record RegistroBitacora(DateTime Fecha, string Usuario, string Motivo, decimal ValorAnterior, decimal ValorNuevo);

public class PantallaPagos
{
    public string? Registro { get; set; }
    public EstadoCuenta? Afiliado { get; set; }
    public List<PagoRegistrado> Pagos { get; set; } = new();
}

public class PantallaAjustes
{
    public string? Registro { get; set; }
    public EstadoCuenta? Afiliado { get; set; }
    public List<RegistroBitacora> Bitacora { get; set; } = new();
}

public class FormularioAfiliado
{
    public string Modo { get; set; } = string.Empty;
    public string Registro { get; set; } = string.Empty;
    public string RegistroOriginal { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Carnet { get; set; } = string.Empty;
    public DateOnly? FechaAfiliacion { get; set; }
    public DateOnly? FechaInicioCobro { get; set; }
    public bool EsTribunal { get; set; }
    public string? Contrasena { get; set; }
    public IFormFile? Foto { get; set; }
    public string? Baja { get; set; }
    public DateOnly? FechaBaja { get; set; }
}

public static class Formato
{
    public static string Bs(decimal monto) => $"Bs {monto:0.##}";
    public static string Fecha(DateOnly fecha) => fecha.ToString("dd/MM/yyyy");
    public static string Fecha(DateTime fecha) => fecha.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    public static string ColorEstado(string estado) => estado switch
    {
        "Activo" => "text-bg-success",
        "Inactivo" => "text-bg-danger",
        _ => "text-bg-secondary"
    };
}
