using SistemaCPC.Api.Data;
using SistemaCPC.Api.Models;
using SistemaCPC.Api.Patrones.Facade;

namespace SistemaCPC.Api.Patrones.Decorator;

public interface IServicioAjustes
{
    Task<Ajuste> Registrar(Ajuste ajuste, Afiliado afiliado, string usuario);
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

// Patrón Decorator
public class ServicioConBitacora : IServicioAjustes
{
    private readonly IServicioAjustes _servicio;
    private readonly CpcContext _context;
    private readonly EstadoDeCuenta _estadoDeCuenta;

    public ServicioConBitacora(IServicioAjustes servicio, CpcContext context, EstadoDeCuenta estadoDeCuenta)
    {
        _servicio = servicio;
        _context = context;
        _estadoDeCuenta = estadoDeCuenta;
    }

    public async Task<Ajuste> Registrar(Ajuste ajuste, Afiliado afiliado, string usuario)
    {
        var deudaAnterior = _estadoDeCuenta.Resumir(afiliado).Deuda;

        var resultado = await _servicio.Registrar(ajuste, afiliado, usuario);

        _context.Bitacora.Add(new RegistroBitacora
        {
            Fecha = DateTime.UtcNow,
            Usuario = usuario,
            Motivo = $"{ajuste.Tipo} ({afiliado.Registro}): {ajuste.Motivo}",
            ValorAnterior = deudaAnterior,
            ValorNuevo = _estadoDeCuenta.Resumir(afiliado).Deuda
        });
        await _context.SaveChangesAsync();
        return resultado;
    }
}
