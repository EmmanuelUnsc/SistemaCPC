using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Configuracion;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Dtos;

namespace SistemaCPC.Api.Validaciones;

// Patrón Chain of Responsibility
public abstract class ValidadorDefensa
{
    private ValidadorDefensa? _siguiente;

    public ValidadorDefensa Siguiente(ValidadorDefensa validador)
    {
        _siguiente = validador;
        return validador;
    }

    public async Task<string?> Validar(SolicitudDefensa solicitud)
    {
        var error = await Revisar(solicitud);

        if (error != null || _siguiente == null)
        {
            return error;
        }

        return await _siguiente.Validar(solicitud);
    }

    protected abstract Task<string?> Revisar(SolicitudDefensa solicitud);
}

public class ValidadorDatosCompletos : ValidadorDefensa
{
    protected override Task<string?> Revisar(SolicitudDefensa s)
    {
        var falta = string.IsNullOrWhiteSpace(s.NumeroOficio) || string.IsNullOrWhiteSpace(s.Universidad)
                    || string.IsNullOrWhiteSpace(s.RegistroTribunal);
        return Task.FromResult(falta ? "El oficio, la universidad y el tribunal son obligatorios." : null);
    }
}

public class ValidadorOficioUnico : ValidadorDefensa
{
    private readonly CpcContext _context;

    public ValidadorOficioUnico(CpcContext context)
    {
        _context = context;
    }

    protected override async Task<string?> Revisar(SolicitudDefensa s)
    {
        var existe = await _context.Defensas.AnyAsync(d => d.NumeroOficio == s.NumeroOficio);
        return existe ? "Ese número de oficio ya fue registrado." : null;
    }
}

public class ValidadorTribunalHabilitado : ValidadorDefensa
{
    private readonly CpcContext _context;

    public ValidadorTribunalHabilitado(CpcContext context)
    {
        _context = context;
    }

    protected override async Task<string?> Revisar(SolicitudDefensa s)
    {
        var habilitado = await _context.Afiliados.AnyAsync(a =>
            a.Registro == s.RegistroTribunal && a.EsTribunal && a.FechaBaja == null);
        return habilitado ? null : "El tribunal no existe o no está habilitado.";
    }
}

public class ValidadorTopeMensual : ValidadorDefensa
{
    private readonly CpcContext _context;
    private readonly ParametrosSistema _parametros;

    public ValidadorTopeMensual(CpcContext context, ParametrosSistema parametros)
    {
        _context = context;
        _parametros = parametros;
    }

    protected override async Task<string?> Revisar(SolicitudDefensa s)
    {
        if (_parametros.TopeDefensasMes == 0)
        {
            return null;
        }

        var delMes = await _context.Defensas.CountAsync(d =>
            d.Afiliado!.Registro == s.RegistroTribunal &&
            d.Fecha.Year == s.Fecha.Year && d.Fecha.Month == s.Fecha.Month);

        return delMes >= _parametros.TopeDefensasMes ? "El tribunal ya llegó al tope de defensas del mes." : null;
    }
}
