using SistemaCPC.Api.Data;
using SistemaCPC.Api.Models;
using SistemaCPC.Api.Patrones.Singleton;

namespace SistemaCPC.Api.Patrones.FactoryMethod;

// Patrón Factory Method
public abstract class RegistroDePago
{
    protected readonly CpcContext Context;
    private readonly ParametrosSistema _parametros;

    protected RegistroDePago(CpcContext context, ParametrosSistema parametros)
    {
        Context = context;
        _parametros = parametros;
    }

    // Factory Method: método de fábrica
    protected abstract Pago CrearRecibo();

    // Template Method: operación gancho
    protected virtual DateOnly FechaDelPago() => DateOnly.FromDateTime(DateTime.Today);

    // Patrón Template Method: método plantilla
    public async Task<Pago> Registrar(Afiliado afiliado, int mesesCubiertos)
    {
        if (mesesCubiertos < 1)
        {
            throw new ArgumentException("Los meses cubiertos deben ser al menos 1.");
        }

        var pago = CrearRecibo();
        pago.AfiliadoId = afiliado.Id;
        pago.Fecha = FechaDelPago();
        pago.MesesCubiertos = mesesCubiertos;
        pago.Monto = mesesCubiertos * _parametros.AporteMensual;

        Context.Pagos.Add(pago);
        await Context.SaveChangesAsync();
        return pago;
    }
}

public class RegistroPagoPlataforma : RegistroDePago
{
    public RegistroPagoPlataforma(CpcContext context, ParametrosSistema parametros)
        : base(context, parametros) { }

    protected override Pago CrearRecibo()
    {
        var numero = Context.Pagos.Count(p => p.Origen == OrigenPago.Plataforma) + 1;
        return new Pago { NumeroRecibo = $"P-{numero:0000}", Origen = OrigenPago.Plataforma };
    }
}

public class RegistroPagoOficina : RegistroDePago
{
    private readonly string _numeroRecibo;
    private readonly DateOnly? _fecha;

    public RegistroPagoOficina(CpcContext context, ParametrosSistema parametros, string numeroRecibo, DateOnly? fecha)
        : base(context, parametros)
    {
        _numeroRecibo = numeroRecibo;
        _fecha = fecha;
    }

    protected override Pago CrearRecibo()
    {
        return new Pago { NumeroRecibo = _numeroRecibo, Origen = OrigenPago.Oficina };
    }

    protected override DateOnly FechaDelPago() => _fecha ?? base.FechaDelPago();
}
