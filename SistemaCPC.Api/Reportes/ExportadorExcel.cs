using ClosedXML.Excel;

namespace SistemaCPC.Api.Reportes;

public interface IExportador
{
    byte[] Exportar(Reporte reporte);
}

// Patrón Adapter
public class ExportadorExcel : IExportador
{
    public byte[] Exportar(Reporte reporte)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.AddWorksheet("Reporte");

        hoja.Cell(1, 1).Value = reporte.Titulo;
        hoja.Cell(1, 1).Style.Font.Bold = true;

        for (var c = 0; c < reporte.Columnas.Count; c++)
        {
            hoja.Cell(3, c + 1).Value = reporte.Columnas[c];
            hoja.Cell(3, c + 1).Style.Font.Bold = true;
        }

        for (var f = 0; f < reporte.Filas.Count; f++)
        {
            for (var c = 0; c < reporte.Filas[f].Count; c++)
            {
                hoja.Cell(f + 4, c + 1).Value = reporte.Filas[f][c];
            }
        }

        var fila = reporte.Filas.Count + 5;
        foreach (var total in reporte.Totales)
        {
            hoja.Cell(fila, 1).Value = total.Key;
            hoja.Cell(fila, 2).Value = total.Value;
            fila++;
        }

        hoja.Columns().AdjustToContents();

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }
}
