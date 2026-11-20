using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using SistemaCPC.Api.Models;

namespace SistemaCPC.Migracion;

public class Migrador
{
    private static readonly string[] Meses =
        { "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" };

    private static readonly Dictionary<string, string> FechasCorregidas = new()
    {
        ["30/0872023"] = "30/08/2023",
        ["27/11//2023"] = "27/11/2023",
        ["03/02/205"] = "03/02/2025",
        ["06/02/205"] = "06/02/2025"
    };

    private static readonly Dictionary<string, string> MesesMalEscritos = new()
    {
        ["setiembre"] = "septiembre",
        ["seeptiembre"] = "septiembre",
        ["octurbre"] = "octubre",
        ["dieciembre"] = "diciembre"
    };

    private static readonly DateOnly InicioDeCobroMinimo = new(2018, 10, 1);

    private readonly DatosExcel _datos;
    private readonly Superadministrador _admin;

    public List<string> Observaciones { get; } = new();
    public Dictionary<int, Afiliado> Afiliados { get; } = new();
    public int MesesDesdeDetalle { get; private set; }
    public int MesesDesdeMonto { get; private set; }

    public Migrador(DatosExcel datos, Superadministrador admin)
    {
        _datos = datos;
        _admin = admin;
    }

    public void Convertir()
    {
        CargarAfiliados();
        CargarPagos();
        CargarSistemaAntiguo();
        CargarDefensas();
        AplicarBajasSinFecha();
        CifrarContrasenas();
    }

    private void CargarAfiliados()
    {
        var fundacion = _datos.Clientes.Where(c => c.Inscripcion != null).Min(c => c.Inscripcion!.Value);

        foreach (var cliente in _datos.Clientes)
        {
            var ficha = _datos.Fichas.GetValueOrDefault(cliente.Registro);
            var afiliacion = cliente.Inscripcion ?? ficha?.AfiliadoEl;
            if (afiliacion == null)
            {
                afiliacion = fundacion;
                Observaciones.Add($"{cliente.Registro}\tAfiliación «{cliente.InscripcionTexto}» sin fecha: se usa la fecha de fundación {fundacion:dd/MM/yyyy}.");
            }

            var inicio = cliente.InicioCobro;
            if (inicio == null)
            {
                inicio = afiliacion < InicioDeCobroMinimo ? InicioDeCobroMinimo : afiliacion;
                Observaciones.Add($"{cliente.Registro}\tSin inicio de cobro: se usa {inicio:dd/MM/yyyy}.");
            }

            var carnet = ficha?.Carnet ?? string.Empty;
            var afiliado = new Afiliado
            {
                Registro = cliente.Registro.ToString(CultureInfo.InvariantCulture),
                Nombre = cliente.Nombre,
                Rol = Rol.Afiliado,
                Carnet = carnet.Length > 20 ? carnet[..20] : carnet,
                FechaAfiliacion = afiliacion.Value,
                FechaInicioCobro = inicio.Value,
                EsTribunal = _datos.TribunalesHabilitados.Contains(cliente.Registro)
            };

            if (cliente.Estado == "Renunció")
            {
                afiliado.MotivoBaja = "Renuncia";
                afiliado.FechaBaja = cliente.Renuncia;
            }
            else if (cliente.Estado == "Falleció")
            {
                afiliado.MotivoBaja = "Fallecimiento";
                afiliado.FechaBaja = cliente.Renuncia;
            }

            Afiliados.Add(cliente.Registro, afiliado);
        }
    }

    private void CargarPagos()
    {
        var recibos = new HashSet<string>();

        foreach (var pago in _datos.Pagos)
        {
            if (!Afiliados.TryGetValue(pago.Cuenta, out var afiliado))
            {
                Observaciones.Add($"{pago.Cuenta}\tPago «{pago.Recibo}» (fila {pago.Fila}, Bs {pago.Monto}) de una cuenta que no existe en RecordClientes: no se carga.");
                continue;
            }

            var fecha = pago.Fecha;
            if (fecha == null && FechasCorregidas.TryGetValue(pago.FechaTexto, out var corregida))
            {
                fecha = DateOnly.ParseExact(corregida, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                Observaciones.Add($"{pago.Cuenta}\tFecha del recibo «{pago.Recibo}» mal escrita «{pago.FechaTexto}»: se corrige a {corregida}.");
            }

            if (fecha == null)
            {
                Observaciones.Add($"{pago.Cuenta}\tRecibo «{pago.Recibo}» (fila {pago.Fila}) con fecha ilegible «{pago.FechaTexto}»: no se carga.");
                continue;
            }

            var recibo = NormalizarRecibo(pago);
            if (!recibos.Add(recibo))
            {
                Observaciones.Add($"{pago.Cuenta}\tRecibo repetido «{recibo}» (fila {pago.Fila}): se carga como «{recibo}-{pago.Fila}».");
                recibo = $"{recibo}-{pago.Fila}";
                recibos.Add(recibo);
            }

            afiliado.Pagos.Add(new Pago
            {
                NumeroRecibo = recibo,
                Fecha = fecha.Value,
                Monto = pago.Monto,
                MesesCubiertos = MesesCubiertos(pago),
                Origen = OrigenPago.Oficina
            });
        }
    }

    private void CargarSistemaAntiguo()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);

        foreach (var cliente in _datos.Clientes.Where(c => c.SistemaAntiguo != 0))
        {
            Afiliados[cliente.Registro].Ajustes.Add(new Ajuste
            {
                Tipo = "Saldo inicial sin recibo",
                Monto = cliente.SistemaAntiguo,
                Motivo = "Sistema antiguo (migración del Excel del Colegio)",
                Fecha = hoy,
                Superadministrador = _admin
            });
        }
    }

    private void CargarDefensas()
    {
        int? anioAnterior = null;

        foreach (var defensa in _datos.Defensas)
        {
            if (!Afiliados.TryGetValue(defensa.Registro, out var afiliado))
            {
                Observaciones.Add($"{defensa.Registro}\tDefensa de la fila {defensa.Fila} con un tribunal que no existe en RecordClientes: no se carga.");
                continue;
            }

            var fecha = FechaDeDefensa(defensa.FechaTexto, anioAnterior, out var anioDeducido);
            if (fecha == null)
            {
                Observaciones.Add($"{defensa.Registro}\tDefensa de la fila {defensa.Fila} con fecha ilegible «{defensa.FechaTexto}»: no se carga.");
                continue;
            }

            if (anioDeducido)
            {
                Observaciones.Add($"{defensa.Registro}\tDefensa de la fila {defensa.Fila} sin año «{defensa.FechaTexto}»: se usa el año de la fila anterior ({fecha:dd/MM/yyyy}).");
            }

            anioAnterior = fecha.Value.Year;
            afiliado.Defensas.Add(new Defensa
            {
                NumeroOficio = string.IsNullOrEmpty(defensa.Oficio) ? "S/N" : defensa.Oficio,
                Universidad = string.IsNullOrEmpty(defensa.Universidad) ? "Sin dato" : defensa.Universidad,
                Fecha = fecha.Value
            });
        }

        foreach (var (registro, pagadas) in _datos.PagosDeDefensa)
        {
            if (!Afiliados.TryGetValue(registro, out var afiliado))
            {
                Observaciones.Add($"{registro}\t{pagadas} pagos de defensa de una cuenta que no existe en RecordClientes: no se cargan.");
                continue;
            }

            var ordenadas = afiliado.Defensas.OrderBy(d => d.Fecha).ToList();
            if (pagadas > ordenadas.Count)
            {
                Observaciones.Add($"{registro}\tTiene {pagadas} pagos de defensa y {ordenadas.Count} defensas: se marcan todas como pagadas.");
            }

            foreach (var defensa in ordenadas.Take(pagadas))
            {
                defensa.Estado = EstadoDefensa.Pagada;
            }
        }
    }

    private void AplicarBajasSinFecha()
    {
        foreach (var (registro, afiliado) in Afiliados.Where(a => a.Value.MotivoBaja != null && a.Value.FechaBaja == null))
        {
            var ultimoPago = afiliado.Pagos.Select(p => (DateOnly?)p.Fecha).Max();
            afiliado.FechaBaja = ultimoPago ?? afiliado.FechaInicioCobro;
            Observaciones.Add($"{registro}\t{afiliado.MotivoBaja} sin fecha: se usa {(ultimoPago != null ? "la del último pago" : "el inicio de cobro")}, {afiliado.FechaBaja:dd/MM/yyyy}.");
        }
    }

    private void CifrarContrasenas()
    {
        var sinCarnet = new List<string>();
        var claves = Afiliados.Values.Select(a =>
        {
            var numero = Regex.Match(a.Carnet, @"^\d+").Value;
            if (numero.Length == 0)
            {
                sinCarnet.Add(a.Registro);
                numero = a.Registro;
            }
            return (Afiliado: a, Clave: numero);
        }).ToList();

        Parallel.ForEach(claves, c => c.Afiliado.ContrasenaCifrada = new PasswordHasher<Usuario>().HashPassword(c.Afiliado, c.Clave));

        foreach (var registro in sinCarnet)
        {
            Observaciones.Add($"{registro}\tSin carnet en DATOS DE AFILIACIÓN: la contraseña inicial es el número de registro.");
        }
    }

    private static string NormalizarRecibo(PagoExcel pago)
    {
        var texto = Regex.Replace(pago.Recibo, @"\s+", " ").Trim();
        if (texto.StartsWith("Dir", StringComparison.OrdinalIgnoreCase))
        {
            return $"{texto} / {pago.Cuenta}";
        }

        var am = Regex.Match(texto, @"^AM\s*(\d+)$", RegexOptions.IgnoreCase);
        return am.Success ? $"AM {am.Groups[1].Value}" : texto;
    }

    private int MesesCubiertos(PagoExcel pago)
    {
        var detalle = Regex.Match(QuitarTildes(pago.Detalle.ToLowerInvariant()),
            $@"({string.Join('|', Meses)})\s*(?:de\s*|del\s*)?(\d{{4}})?\s+(?:a|al|hasta)\s+(?:\w+\s+)?({string.Join('|', Meses)})\s*(?:de\s*|del\s*)?(\d{{4}})");
        if (detalle.Success)
        {
            var mesInicio = Array.IndexOf(Meses, detalle.Groups[1].Value) + 1;
            var mesFin = Array.IndexOf(Meses, detalle.Groups[3].Value) + 1;
            var anioFin = int.Parse(detalle.Groups[4].Value, CultureInfo.InvariantCulture);
            var anioInicio = detalle.Groups[2].Success
                ? int.Parse(detalle.Groups[2].Value, CultureInfo.InvariantCulture)
                : mesInicio > mesFin ? anioFin - 1 : anioFin;
            var cantidad = (anioFin - anioInicio) * 12 + mesFin - mesInicio + 1;
            if (cantidad is >= 1 and <= 120)
            {
                MesesDesdeDetalle++;
                return cantidad;
            }
        }

        MesesDesdeMonto++;
        return Math.Max(1, (int)Math.Round(pago.Monto / 35, MidpointRounding.AwayFromZero));
    }

    private static DateOnly? FechaDeDefensa(string texto, int? anioAnterior, out bool anioDeducido)
    {
        anioDeducido = false;
        var limpio = QuitarTildes(texto.ToLowerInvariant());
        foreach (var (mal, bien) in MesesMalEscritos)
        {
            limpio = limpio.Replace(mal, bien);
        }
        var partes = Regex.Match(limpio, $@"(\d{{1,2}})\s*(?:de\s+)?({string.Join('|', Meses)})[\s,\-]*(?:de\s+|del\s+)?(\d{{4}})?");
        if (!partes.Success)
        {
            return null;
        }

        var dia = int.Parse(partes.Groups[1].Value, CultureInfo.InvariantCulture);
        var mes = Array.IndexOf(Meses, partes.Groups[2].Value) + 1;
        int anio;
        if (partes.Groups[3].Success)
        {
            anio = int.Parse(partes.Groups[3].Value, CultureInfo.InvariantCulture);
        }
        else if (anioAnterior != null)
        {
            anio = anioAnterior.Value;
            anioDeducido = true;
        }
        else
        {
            return null;
        }

        return dia <= DateTime.DaysInMonth(anio, mes) ? new DateOnly(anio, mes, dia) : null;
    }

    private static string QuitarTildes(string texto)
    {
        var descompuesto = texto.Normalize(NormalizationForm.FormD);
        return new string(descompuesto.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray())
            .Normalize(NormalizationForm.FormC);
    }
}
