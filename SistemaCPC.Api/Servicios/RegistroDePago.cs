using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Configuracion;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Models;

namespace SistemaCPC.Api.Servicios;

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
    protected abstract Task<Pago> CrearRecibo();

    // Template Method: operación gancho
    protected virtual DateOnly FechaDelPago() => DateOnly.FromDateTime(DateTime.Today);

    // Patrón Template Method: método plantilla
    public async Task<Pago> Registrar(Afiliado afiliado, int mesesCubiertos)
    {
        if (mesesCubiertos < 1)
        {
            throw new ArgumentException("Los meses cubiertos deben ser al menos 1.");
        }

        var pago = await CrearRecibo();
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

    protected override async Task<Pago> CrearRecibo()
    {
        var recibos = await Context.Pagos
     .Where(p => p.NumeroRecibo.StartsWith("P-"))
     .Select(p => p.NumeroRecibo)
     .ToListAsync();
        var mayor = recibos.Select(r => int.TryParse(r[2..], out var numero) ? numero : 0).DefaultIfEmpty(0).Max();
        return new Pago { NumeroRecibo = $"P-{mayor + 1:0000}", Origen = OrigenPago.Plataforma };
        
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

    protected override Task<Pago> CrearRecibo()
    {
        return Task.FromResult(new Pago { NumeroRecibo = _numeroRecibo, Origen = OrigenPago.Oficina });
    }

    protected override DateOnly FechaDelPago() => _fecha ?? base.FechaDelPago();
}
