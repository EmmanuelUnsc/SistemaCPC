using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace SistemaCPC.Migracion;

public record ClienteExcel(int Registro, string Nombre, DateOnly? Inscripcion, string InscripcionTexto, DateOnly? InicioCobro,
    DateOnly? Renuncia, string Estado, decimal? Saldo, decimal SistemaAntiguo);

public record FichaExcel(int Registro, DateOnly? AfiliadoEl, string Carnet);

public record PagoExcel(int Fila, int Cuenta, string Recibo, DateOnly? Fecha, string FechaTexto, decimal Monto, string Detalle);

public record DefensaExcel(int Fila, string Oficio, string Universidad, string FechaTexto, int Registro);

public record SaldoTribunalExcel(int Registro, int Asistidas, int Pagadas, decimal DeudaAportes, decimal SaldoNeto);

public class DatosExcel
{
    public List<ClienteExcel> Clientes { get; } = new();
    public Dictionary<int, FichaExcel> Fichas { get; } = new();
    public List<PagoExcel> Pagos { get; } = new();
    public Dictionary<int, int> PagosDeDefensa { get; } = new();
    public List<DefensaExcel> Defensas { get; } = new();
    public HashSet<int> TribunalesHabilitados { get; } = new();
    public List<SaldoTribunalExcel> SaldosTribunal { get; } = new();
    public DateOnly FechaDeCorte { get; set; }
}

public static class LectorExcel
{
    public static DatosExcel Leer(string ruta)
    {
        using var libro = new XLWorkbook(ruta);
        var datos = new DatosExcel();

        var clientes = libro.Worksheet("RecordClientes");
        foreach (var fila in clientes.RowsUsed().Where(f => f.RowNumber() >= 4))
        {
            if (Entero(fila.Cell(3)) is not { } registro)
            {
                continue;
            }

            datos.Clientes.Add(new ClienteExcel(registro, Texto(fila.Cell(8)), Fecha(fila.Cell(4)), Texto(fila.Cell(4)),
                Fecha(fila.Cell(5)), Fecha(fila.Cell(6)), Texto(fila.Cell(12)), Numero(fila.Cell(11)), Numero(fila.Cell(14)) ?? 0));
        }

        var fichas = libro.Worksheet("DATOS DE AFILIACIÓN");
        foreach (var fila in fichas.RowsUsed().Where(f => f.RowNumber() >= 2))
        {
            if (Entero(fila.Cell(1)) is { } registro)
            {
                datos.Fichas[registro] = new FichaExcel(registro, Fecha(fila.Cell(2)), Texto(fila.Cell(7)));
                datos.FechaDeCorte = Fecha(fila.Cell(22)) ?? datos.FechaDeCorte;
            }
        }

        var facturas = libro.Worksheet("RecordFacturas");
        foreach (var fila in facturas.RowsUsed().Where(f => f.RowNumber() >= 10))
        {
            if (Entero(fila.Cell(2)) is not { } cuenta)
            {
                continue;
            }

            datos.Pagos.Add(new PagoExcel(fila.RowNumber(), cuenta, Texto(fila.Cell(3)), Fecha(fila.Cell(4)), Texto(fila.Cell(4)),
                Numero(fila.Cell(5)) ?? 0, Texto(fila.Cell(6))));
        }

        var facturasDefensa = libro.Worksheet("RecordFacturas2");
        foreach (var fila in facturasDefensa.RowsUsed().Where(f => f.RowNumber() >= 7))
        {
            if (Entero(fila.Cell(2)) is { } cuenta && Numero(fila.Cell(5)) > 0)
            {
                datos.PagosDeDefensa[cuenta] = datos.PagosDeDefensa.GetValueOrDefault(cuenta) + 1;
            }
        }

        var planilla = libro.Worksheet("PLANILLA DE TRIBUNALES");
        foreach (var fila in planilla.RowsUsed().Where(f => f.RowNumber() >= 2))
        {
            if (Entero(fila.Cell(12)) is { } registro)
            {
                datos.Defensas.Add(new DefensaExcel(fila.RowNumber(), Texto(fila.Cell(1)), Texto(fila.Cell(2)), Texto(fila.Cell(8)), registro));
            }
        }

        var habilitados = libro.Worksheet("Tribunales habilitados");
        foreach (var fila in habilitados.RowsUsed().Where(f => f.RowNumber() >= 2))
        {
            if (Entero(fila.Cell(7)) is { } registro)
            {
                datos.TribunalesHabilitados.Add(registro);
            }
        }

        var porPagar = libro.Worksheet("BasDatPorPagar");
        foreach (var fila in porPagar.RowsUsed().Where(f => f.RowNumber() >= 4))
        {
            if (Entero(fila.Cell(3)) is { } registro && Numero(fila.Cell(14)) is { } saldoNeto)
            {
                datos.SaldosTribunal.Add(new SaldoTribunalExcel(registro, (int)(Numero(fila.Cell(9)) ?? 0),
                    (int)(Numero(fila.Cell(10)) ?? 0), Numero(fila.Cell(13)) ?? 0, saldoNeto));
            }
        }

        return datos;
    }

    private static XLCellValue Valor(IXLCell celda) => celda.HasFormula ? celda.CachedValue : celda.Value;

    private static string Texto(IXLCell celda)
    {
        var valor = Valor(celda);
        return valor.Type switch
        {
            XLDataType.Text => valor.GetText().Trim(),
            XLDataType.Number => valor.GetNumber().ToString(CultureInfo.InvariantCulture),
            XLDataType.DateTime => valor.GetDateTime().ToString("dd/MM/yyyy"),
            _ => string.Empty
        };
    }

    private static decimal? Numero(IXLCell celda)
    {
        var valor = Valor(celda);
        return valor.IsNumber ? (decimal)valor.GetNumber() : null;
    }

    private static int? Entero(IXLCell celda)
    {
        var numero = Numero(celda);
        return numero != null && numero == Math.Floor(numero.Value) ? (int)numero.Value : null;
    }

    private static DateOnly? Fecha(IXLCell celda)
    {
        var valor = Valor(celda);
        if (valor.IsDateTime)
        {
            return DateOnly.FromDateTime(valor.GetDateTime());
        }

        if (valor.IsText && DateTime.TryParseExact(Regex.Replace(valor.GetText(), @"\s", ""), "d/M/yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
        {
            return DateOnly.FromDateTime(fecha);
        }

        return null;
    }
}
