using SistemaCPC.Api.Data;
using SistemaCPC.Api.Models;
using SistemaCPC.Api.Patrones.Singleton;

namespace SistemaCPC.Api.Patrones.FactoryMethod;

// Patrón Factory Method: el proceso de registrar un pago es el mismo en CU-04 y CU-05
// (validar, guardar y actualizar el saldo); solo cambia cómo se crea el recibo,
// y eso lo decide cada subclase en CrearRecibo().
public abstract class RegistroDePago
{
    protected readonly CpcContext Context;
    private readonly ParametrosSistema _parametros;

    protected RegistroDePago(CpcContext context, ParametrosSistema parametros)
    {
        Context = context;
        _parametros = parametros;
    }

    // Método de fábrica.
    protected abstract Pago CrearRecibo();

    public async Task<Pago> Registrar(Afiliado afiliado, int mesesCubiertos)
    {
        if (mesesCubiertos < 1)
        {
            throw new ArgumentException("Los meses cubiertos deben ser al menos 1.");
        }

        var pago = CrearRecibo();
        pago.AfiliadoId = afiliado.Id;
        pago.Fecha = DateOnly.FromDateTime(DateTime.Today);
        pago.MesesCubiertos = mesesCubiertos;
        pago.Monto = mesesCubiertos * _parametros.AporteMensual;

        // El saldo se calcula con los pagos guardados, así que al guardar el pago se actualiza.
        Context.Pagos.Add(pago);
        await Context.SaveChangesAsync();
        return pago;
    }
}

// CU-04 Pagar aportes (simulado): el sistema numera el recibo.
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

// CU-05 Gestionar pagos: el directorio ingresa el número del recibo físico de la oficina.
public class RegistroPagoOficina : RegistroDePago
{
    private readonly string _numeroRecibo;

    public RegistroPagoOficina(CpcContext context, ParametrosSistema parametros, string numeroRecibo)
        : base(context, parametros)
    {
        _numeroRecibo = numeroRecibo;
    }

    protected override Pago CrearRecibo()
    {
        return new Pago { NumeroRecibo = _numeroRecibo, Origen = OrigenPago.Oficina };
    }
}
