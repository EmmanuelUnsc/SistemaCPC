using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Dtos;
using SistemaCPC.Api.Models;
using SistemaCPC.Api.Patrones.Singleton;

namespace SistemaCPC.Api.Patrones.Facade;

public record ResumenAfiliado(EstadoAfiliado Estado, int MesesAdeudados, decimal Deuda);

// Patrón Facade
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

    public ResumenAfiliado Resumir(Afiliado afiliado)
    {
        var aporte = _parametros.AporteMensual;
        return new ResumenAfiliado(
            afiliado.ObtenerEstado(_parametros.UmbralInactividad, aporte, Hoy),
            afiliado.MesesAdeudados(aporte, Hoy),
            afiliado.CalcularDeuda(aporte, Hoy));
    }

    public async Task<EstadoCuentaRespuesta?> ObtenerEstado(string registro)
    {
        var afiliado = await _context.Afiliados
            .Include(a => a.Pagos)
            .Include(a => a.Ajustes)
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

        var aporte = _parametros.AporteMensual;
        var porAnio = afiliado.ListarMesesAdeudados(aporte, Hoy)
            .GroupBy(m => m.Mes.Year)
            .Select(g => new AnioAdeudado(g.Key, g.Select(m => NombreDelMes(m.Mes)).ToList(), g.Sum(m => m.Monto)))
            .ToList();

        return new EstadoCuentaRespuesta(afiliado.Registro, afiliado.Nombre, resumen.Estado.ToString(),
            resumen.MesesAdeudados, resumen.Deuda, aporte, porAnio, pagos);
    }

    private static string NombreDelMes(DateOnly mes)
    {
        var nombre = Espanol.DateTimeFormat.GetMonthName(mes.Month);
        return char.ToUpper(nombre[0]) + nombre[1..];
    }

    private static readonly System.Globalization.CultureInfo Espanol = new("es-BO");

    public async Task<SaldoTribunalRespuesta?> ObtenerSaldoTribunal(string registro)
    {
        var afiliado = await _context.Afiliados
            .Include(a => a.Pagos)
            .Include(a => a.Ajustes)
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
