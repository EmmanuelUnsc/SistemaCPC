using System.ComponentModel.DataAnnotations;

namespace SistemaCPC.Api.Dtos;

public class ParametrosSolicitud
{
    [Range(0.01, 10000, ErrorMessage = "El aporte mensual debe ser mayor a 0.")]
    public decimal AporteMensual { get; set; }

    [Range(0.01, 10000, ErrorMessage = "El monto por defensa debe ser mayor a 0.")]
    public decimal MontoPorDefensa { get; set; }

    [Range(1, 120, ErrorMessage = "El umbral de inactividad va de 1 a 120 cuotas.")]
    public int UmbralInactividad { get; set; }

    [Range(0, 100, ErrorMessage = "El tope de defensas por mes va de 0 (sin tope) a 100.")]
    public int TopeDefensasMes { get; set; }
}

public class SolicitudDefensa
{
    public string NumeroOficio { get; set; } = string.Empty;
    public string Universidad { get; set; } = string.Empty;
    public string Carta { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public string RegistroTribunal { get; set; } = string.Empty;
}

public record DefensaRespuesta(string NumeroOficio, string Universidad, DateOnly Fecha, string Tribunal, string Estado);

public record PagoDelHistorial(string NumeroRecibo, DateOnly Fecha, decimal Monto, int MesesCubiertos);

public record AjusteDelHistorial(string Tipo, DateOnly Fecha, decimal Monto, string Motivo);

public record AnioAdeudado(int Anio, List<string> Meses, decimal Monto);

public record EstadoCuentaRespuesta(string Registro, string Nombre, string Estado, int MesesAdeudados,
    decimal Deuda, decimal AporteMensual, List<AnioAdeudado> MesesPorAnio, List<PagoDelHistorial> Pagos,
    List<AjusteDelHistorial> Ajustes);

public record DefensaDelTribunal(string NumeroOficio, string Universidad, DateOnly Fecha, string Estado);

public record SaldoTribunalRespuesta(string Registro, string Nombre, int DefensasPorPagar, decimal MontoPorCobrar,
    decimal DeudaAportes, decimal SaldoNeto, List<DefensaDelTribunal> Defensas);

public record ErrorRespuesta(string Mensaje);
