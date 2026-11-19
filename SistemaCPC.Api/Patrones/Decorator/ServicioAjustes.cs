using SistemaCPC.Api.Data;
using SistemaCPC.Api.Dtos;
using SistemaCPC.Api.Models;
using SistemaCPC.Api.Patrones.Facade;

namespace SistemaCPC.Api.Patrones.Decorator;

public interface IServicioAjustes
{
    Task<Ajuste> Registrar(Ajuste ajuste, Afiliado afiliado, string usuario);
}

public interface IServicioPagos
{
    Task<Pago> Corregir(Pago pago, Afiliado afiliado, int mesesCubiertos, decimal monto, string motivo, string usuario);
}

public class ServicioAjustes : IServicioAjustes
{
    private readonly CpcContext _context;

    public ServicioAjustes(CpcContext context)
    {
        _context = context;
    }

    public async Task<Ajuste> Registrar(Ajuste ajuste, Afiliado afiliado, string usuario)
    {
        ajuste.AfiliadoId = afiliado.Id;
        ajuste.Fecha = DateOnly.FromDateTime(DateTime.Today);
        afiliado.Ajustes.Add(ajuste);
        await _context.SaveChangesAsync();
        return ajuste;
    }
}

public class ServicioPagos : IServicioPagos
{
    private readonly CpcContext _context;

    public ServicioPagos(CpcContext context)
    {
        _context = context;
    }

    public async Task<Pago> Corregir(Pago pago, Afiliado afiliado, int mesesCubiertos, decimal monto, string motivo, string usuario)
    {
        pago.MesesCubiertos = mesesCubiertos;
        pago.Monto = monto;
        await _context.SaveChangesAsync();
        return pago;
    }
}

// Patrón Decorator
public class ServicioConBitacora : IServicioAjustes, IServicioPagos
{
    private readonly IServicioAjustes _ajustes;
    private readonly IServicioPagos _pagos;
    private readonly CpcContext _context;
    private readonly EstadoDeCuenta _estadoDeCuenta;

    public ServicioConBitacora(IServicioAjustes ajustes, IServicioPagos pagos, CpcContext context, EstadoDeCuenta estadoDeCuenta)
    {
        _ajustes = ajustes;
        _pagos = pagos;
        _context = context;
        _estadoDeCuenta = estadoDeCuenta;
    }

    public async Task<Ajuste> Registrar(Ajuste ajuste, Afiliado afiliado, string usuario)
    {
        var deudaAnterior = _estadoDeCuenta.Resumir(afiliado).Deuda;
        var resultado = await _ajustes.Registrar(ajuste, afiliado, usuario);
        await RegistrarEnBitacora(usuario, $"Ajuste {ajuste.Tipo} ({afiliado.Registro}): {ajuste.Motivo}",
            deudaAnterior, _estadoDeCuenta.Resumir(afiliado).Deuda);
        return resultado;
    }

    public async Task<Pago> Corregir(Pago pago, Afiliado afiliado, int mesesCubiertos, decimal monto, string motivo, string usuario)
    {
        var deudaAnterior = _estadoDeCuenta.Resumir(afiliado).Deuda;
        var resultado = await _pagos.Corregir(pago, afiliado, mesesCubiertos, monto, motivo, usuario);
        await RegistrarEnBitacora(usuario, $"Corrección del pago {pago.NumeroRecibo} ({afiliado.Registro}): {motivo}",
            deudaAnterior, _estadoDeCuenta.Resumir(afiliado).Deuda);
        return resultado;
    }

    private async Task RegistrarEnBitacora(string usuario, string motivo, decimal valorAnterior, decimal valorNuevo)
    {
        _context.Bitacora.Add(new RegistroBitacora
        {
            Fecha = DateTime.UtcNow,
            Usuario = usuario,
            Motivo = motivo,
            ValorAnterior = valorAnterior,
            ValorNuevo = valorNuevo
        });
        await _context.SaveChangesAsync();
    }
}
