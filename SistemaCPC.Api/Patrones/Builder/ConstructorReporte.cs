using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Patrones.Facade;
using SistemaCPC.Api.Patrones.Singleton;

namespace SistemaCPC.Api.Patrones.Builder;

public class Reporte
{
    public string Titulo { get; set; } = string.Empty;
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public List<string> Columnas { get; set; } = new();
    public List<List<string>> Filas { get; set; } = new();
    public Dictionary<string, int> Totales { get; set; } = new();
}

// Patrón Builder
public abstract class ConstructorReporte
{
    protected readonly CpcContext Context;
    protected readonly ParametrosSistema Parametros;
    protected readonly Reporte Reporte = new();
    protected readonly DateOnly Hoy = DateOnly.FromDateTime(DateTime.Today);

    protected ConstructorReporte(CpcContext context, ParametrosSistema parametros)
    {
        Context = context;
        Parametros = parametros;
    }

    public void DefinirPeriodo(DateOnly? desde, DateOnly? hasta)
    {
        Reporte.Desde = desde;
        Reporte.Hasta = hasta;
    }

    public abstract Task AgregarFilas();

    public abstract void AgregarTotales();

    public Reporte ObtenerReporte() => Reporte;
}

public class ReporteDefensas : ConstructorReporte
{
    private int _tribunales;

    public ReporteDefensas(CpcContext context, ParametrosSistema parametros) : base(context, parametros)
    {
        Reporte.Titulo = "Defensas del período";
        Reporte.Columnas = new() { "Oficio", "Tribunal", "Universidad", "Fecha", "Estado" };
    }

    public override async Task AgregarFilas()
    {
        var desde = Reporte.Desde ?? DateOnly.MinValue;
        var hasta = Reporte.Hasta ?? DateOnly.MaxValue;

        var defensas = await Context.Defensas
            .Include(d => d.Afiliado)
            .Where(d => d.Fecha >= desde && d.Fecha <= hasta)
            .OrderBy(d => d.Fecha)
            .ToListAsync();

        // Patrón Iterator: lo da C# con foreach y LINQ
        foreach (var d in defensas)
        {
            Reporte.Filas.Add(new() { d.NumeroOficio, $"{d.Afiliado!.Registro} - {d.Afiliado.Nombre}", d.Universidad,
                d.Fecha.ToString("dd/MM/yyyy"), d.Estado.ToString() });
        }

        _tribunales = defensas.Select(d => d.AfiliadoId).Distinct().Count();
    }

    public override void AgregarTotales()
    {
        Reporte.Totales["Defensas"] = Reporte.Filas.Count;
        Reporte.Totales["Tribunales distintos"] = _tribunales;
    }
}

public class ReporteAfiliadosPorEstado : ConstructorReporte
{
    private readonly EstadoDeCuenta _estadoDeCuenta;

    public ReporteAfiliadosPorEstado(CpcContext context, ParametrosSistema parametros, EstadoDeCuenta estadoDeCuenta)
        : base(context, parametros)
    {
        _estadoDeCuenta = estadoDeCuenta;
        Reporte.Titulo = "Afiliados por estado";
        Reporte.Columnas = new() { "Registro", "Nombre", "Estado", "Deuda (Bs)" };
    }

    public override async Task AgregarFilas()
    {
        var afiliados = await Context.Afiliados.Include(a => a.Pagos).Include(a => a.Ajustes).OrderBy(a => a.Registro).ToListAsync();

        foreach (var a in afiliados)
        {
            var resumen = _estadoDeCuenta.Resumir(a);
            Reporte.Filas.Add(new() { a.Registro, a.Nombre, resumen.Estado.ToString(), resumen.Deuda.ToString("0.00") });
        }
    }

    public override void AgregarTotales()
    {
        foreach (var estado in Enum.GetNames<Models.EstadoAfiliado>())
        {
            Reporte.Totales[estado] = Reporte.Filas.Count(f => f[2] == estado);
        }
    }
}

public class MatrizAfiliados : ConstructorReporte
{
    private readonly EstadoDeCuenta _estadoDeCuenta;

    public MatrizAfiliados(CpcContext context, ParametrosSistema parametros, EstadoDeCuenta estadoDeCuenta)
        : base(context, parametros)
    {
        _estadoDeCuenta = estadoDeCuenta;
        Reporte.Titulo = "Matriz de afiliados";
        Reporte.Columnas = new() { "Registro", "Nombre", "Afiliación", "Estado", "Deuda (Bs)", "Meses" };
    }

    public override async Task AgregarFilas()
    {
        var afiliados = await Context.Afiliados.Include(a => a.Pagos).Include(a => a.Ajustes).OrderBy(a => a.Registro).ToListAsync();

        foreach (var a in afiliados)
        {
            var resumen = _estadoDeCuenta.Resumir(a);
            Reporte.Filas.Add(new() { a.Registro, a.Nombre, a.FechaAfiliacion.ToString("dd/MM/yyyy"),
                resumen.Estado.ToString(), resumen.Deuda.ToString("0.00"), resumen.MesesAdeudados.ToString() });
        }
    }

    public override void AgregarTotales()
    {
        Reporte.Totales["Afiliados"] = Reporte.Filas.Count;
    }
}

// Patrón Builder: director
public class ServicioReportes
{
    public async Task<Reporte> Generar(ConstructorReporte constructor, DateOnly? desde, DateOnly? hasta)
    {
        constructor.DefinirPeriodo(desde, hasta);
        await constructor.AgregarFilas();
        constructor.AgregarTotales();
        return constructor.ObtenerReporte();
    }
}
