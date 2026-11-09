using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Models;
using SistemaCPC.Api.Patrones.Singleton;

namespace SistemaCPC.Api.Patrones.Facade;

public record ResumenAfiliado(EstadoAfiliado Estado, int MesesAdeudados, decimal Deuda);

public record PagoDelHistorial(string NumeroRecibo, DateOnly Fecha, decimal Monto, int MesesCubiertos);

public record EstadoCuentaRespuesta(string Registro, string Nombre, string Estado, int MesesAdeudados,
    decimal Deuda, List<PagoDelHistorial> Pagos);

public record DefensaDelTribunal(string NumeroOficio, string Universidad, DateOnly Fecha, string Estado);

public record SaldoTribunalRespuesta(string Registro, string Nombre, int DefensasPorPagar, decimal MontoPorCobrar,
    decimal DeudaAportes, decimal SaldoNeto, List<DefensaDelTribunal> Defensas);

// Patrón Facade: una sola puerta para todo lo que es "estado de cuenta".
// Por dentro reúne Afiliado, Pago, Defensa y los parámetros (Singleton); quien la usa no arma el cálculo.
public class EstadoDeCuenta
{
    private readonly CpcContext _context;
    private readonly ParametrosSistema _parametros;

    public EstadoDeCuenta(CpcContext context, ParametrosSistema parametros)
    {
        _context = context;
        _parametros = parametros;
    }

    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.Today);

    // Cálculo común: lo usan CU-02, CU-03 y los reportes de CU-09. El afiliado debe venir con sus pagos.
    public ResumenAfiliado Resumir(Afiliado afiliado)
    {
        return new ResumenAfiliado(
            afiliado.ObtenerEstado(_parametros.UmbralInactividad, Hoy),
            afiliado.MesesAdeudados(Hoy),
            afiliado.CalcularDeuda(_parametros.AporteMensual, Hoy));
    }

    // CU-02 Consultar estado de cuenta
    public async Task<EstadoCuentaRespuesta?> ObtenerEstado(string registro)
    {
        var afiliado = await _context.Afiliados
            .Include(a => a.Pagos)
            .FirstOrDefaultAsync(a => a.Registro == registro);

        if (afiliado == null)
        {
            return null;
        }

        var resumen = Resumir(afiliado);
        var pagos = afiliado.Pagos
            .OrderByDescending(p => p.Fecha)
            .Select(p => new PagoDelHistorial(p.NumeroRecibo, p.Fecha, p.Monto, p.MesesCubiertos))
            .ToList();

        return new EstadoCuentaRespuesta(afiliado.Registro, afiliado.Nombre, resumen.Estado.ToString(),
            resumen.MesesAdeudados, resumen.Deuda, pagos);
    }

    // CU-03 Consultar saldo de tribunal: lo que el Colegio le debe por defensas menos lo que debe de aportes.
    public async Task<SaldoTribunalRespuesta?> ObtenerSaldoTribunal(string registro)
    {
        var afiliado = await _context.Afiliados
            .Include(a => a.Pagos)
            .Include(a => a.Defensas)
            .FirstOrDefaultAsync(a => a.Registro == registro && a.EsTribunal);

        if (afiliado == null)
        {
            return null;
        }

        var porPagar = afiliado.Defensas.Count(d => d.Estado == EstadoDefensa.PorPagar);
        var porCobrar = porPagar * _parametros.MontoPorDefensa;
        var deuda = Resumir(afiliado).Deuda;
        var defensas = afiliado.Defensas
            .OrderByDescending(d => d.Fecha)
            .Select(d => new DefensaDelTribunal(d.NumeroOficio, d.Universidad, d.Fecha, d.Estado.ToString()))
            .ToList();

        return new SaldoTribunalRespuesta(afiliado.Registro, afiliado.Nombre, porPagar, porCobrar, deuda,
            porCobrar - deuda, defensas);
    }
}
