using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Data;
using SistemaCPC.Api.Models;
using SistemaCPC.Migracion;

if (args.Length < 2)
{
    Console.WriteLine("Uso: SistemaCPC.Migracion <ruta del Excel> <contraseña del superadministrador> [cadena de conexión] [carpeta de informes]");
    return 1;
}

var rutaExcel = args[0];
var contrasenaAdmin = args[1];
var conexion = args.Length > 2 ? args[2] : @"Server=(localdb)\MSSQLLocalDB;Database=SistemaCPC_Real;Trusted_Connection=True;TrustServerCertificate=True";
var carpetaInformes = args.Length > 3 ? args[3] : Path.Combine(Path.GetTempPath(), "SistemaCPC-Migracion");

if (new SqlConnectionStringBuilder(conexion).InitialCatalog != "SistemaCPC_Real")
{
    Console.WriteLine("La migración solo recrea la base SistemaCPC_Real.");
    return 1;
}

Console.WriteLine("Leyendo el Excel...");
var datos = LectorExcel.Leer(rutaExcel);
var corte = datos.FechaDeCorte;
Console.WriteLine($"Fecha de corte del Excel: {corte:dd/MM/yyyy}");

var opciones = new DbContextOptionsBuilder<CpcContext>().UseSqlServer(conexion).Options;

using (var context = new CpcContext(opciones))
{
    context.Database.EnsureDeleted();
    context.Database.Migrate();

    context.Parametros.Add(new Parametros { AporteMensual = 35, MontoPorDefensa = 208, UmbralInactividad = 4, TopeDefensasMes = 0 });
    var admin = new Superadministrador { Registro = "admin", Nombre = "Superadministrador", Cargo = "Presidencia", Rol = Rol.Superadministrador };
    admin.ContrasenaCifrada = new PasswordHasher<Usuario>().HashPassword(admin, contrasenaAdmin);
    context.Usuarios.Add(admin);

    var migrador = new Migrador(datos, admin);
    migrador.Convertir();
    context.Afiliados.AddRange(migrador.Afiliados.Values);
    context.SaveChanges();

    Directory.CreateDirectory(carpetaInformes);
    File.WriteAllLines(Path.Combine(carpetaInformes, "Migracion - observaciones.txt"),
        new[] { "Registro\tObservación" }.Concat(migrador.Observaciones), new UTF8Encoding(true));
    Console.WriteLine($"Meses cubiertos: {migrador.MesesDesdeDetalle} desde el detalle, {migrador.MesesDesdeMonto} desde el monto ÷ 35.");
    Console.WriteLine($"Observaciones: {migrador.Observaciones.Count}");
}

using var lectura = new CpcContext(opciones);
var afiliados = lectura.Afiliados.AsNoTracking().Include(a => a.Pagos).Include(a => a.Ajustes).Include(a => a.Defensas).ToList();
var porRegistro = afiliados.ToDictionary(a => int.Parse(a.Registro, CultureInfo.InvariantCulture));

var cargados = new StringBuilder();
cargados.AppendLine($"Afiliados: {afiliados.Count} de {datos.Clientes.Count} en RecordClientes");
cargados.AppendLine($"Pagos de aportes: {afiliados.Sum(a => a.Pagos.Count)} de {datos.Pagos.Count} en RecordFacturas");
cargados.AppendLine($"Ajustes del sistema antiguo: {afiliados.Sum(a => a.Ajustes.Count)} de {datos.Clientes.Count(c => c.SistemaAntiguo != 0)}");
cargados.AppendLine($"Defensas: {afiliados.Sum(a => a.Defensas.Count)} de {datos.Defensas.Count} con tribunal en la planilla");
cargados.AppendLine($"Defensas pagadas: {afiliados.Sum(a => a.Defensas.Count(d => d.Estado == EstadoDefensa.Pagada))} de {datos.PagosDeDefensa.Values.Sum()} pagos de defensa");
cargados.AppendLine($"Tribunales habilitados: {afiliados.Count(a => a.EsTribunal)} de {datos.TribunalesHabilitados.Count}");

static string Estado(EstadoAfiliado estado) => estado switch
{
    EstadoAfiliado.Fallecido => "Falleció",
    EstadoAfiliado.Renunciado => "Renunció",
    _ => estado.ToString()
};

var filasAfiliados = new List<string> { "Registro;Nombre;Saldo Excel;Deuda sistema;Diferencia;Estado Excel;Estado sistema;Causa" };
var causas = new Dictionary<string, List<int>>();
int comparados = 0, iguales = 0, estadosIguales = 0;

foreach (var cliente in datos.Clientes)
{
    var afiliado = porRegistro[cliente.Registro];
    var deuda = afiliado.CalcularDeuda(35, corte);
    var estado = Estado(afiliado.ObtenerEstado(4, 35, corte));
    if (estado == cliente.Estado)
    {
        estadosIguales++;
    }

    string causa;
    if (cliente.Saldo == null)
    {
        causa = "El Excel no tiene saldo (#VALUE!)";
    }
    else
    {
        comparados++;
        if (cliente.Saldo == deuda)
        {
            iguales++;
            causa = string.Empty;
        }
        else if (afiliado.MotivoBaja == "Renuncia")
        {
            causa = "El Excel sigue cobrando después de la renuncia; el sistema cobra hasta la baja";
        }
        else
        {
            causa = "Sin explicar";
        }
    }

    if (causa.Length > 0)
    {
        causas.TryAdd(causa, new List<int>());
        causas[causa].Add(cliente.Registro);
    }

    filasAfiliados.Add(string.Join(';', cliente.Registro, cliente.Nombre, cliente.Saldo?.ToString(CultureInfo.InvariantCulture) ?? "",
        deuda.ToString(CultureInfo.InvariantCulture), ((cliente.Saldo ?? deuda) - deuda).ToString(CultureInfo.InvariantCulture),
        cliente.Estado, estado, causa));
}

var filasTribunales = new List<string> { "Registro;Nombre;Asistidas Excel;Asistidas sistema;Pagadas Excel;Pagadas sistema;Saldo neto Excel;Saldo neto sistema;Causa" };
var saldosExcel = datos.Clientes.ToDictionary(c => c.Registro, c => c.Saldo);
var causasTribunal = new Dictionary<string, List<int>>();
int tribunalesComparados = 0, tribunalesIguales = 0;

foreach (var saldo in datos.SaldosTribunal.Where(s => porRegistro.ContainsKey(s.Registro)))
{
    var afiliado = porRegistro[saldo.Registro];
    var asistidas = afiliado.Defensas.Count;
    var pagadas = afiliado.Defensas.Count(d => d.Estado == EstadoDefensa.Pagada);
    var saldoNeto = (asistidas - pagadas) * 208 - afiliado.CalcularDeuda(35, corte);

    tribunalesComparados++;
    string causa;
    if (saldoNeto == saldo.SaldoNeto)
    {
        tribunalesIguales++;
        causa = string.Empty;
    }
    else if (asistidas != saldo.Asistidas || pagadas != saldo.Pagadas)
    {
        causa = "Distinto número de defensas asistidas o pagadas";
    }
    else if (saldosExcel[saldo.Registro] == null)
    {
        causa = "Falleció sin saldo en el Excel; BasDatPorPagar toma la deuda como 0";
    }
    else if (saldo.DeudaAportes != saldosExcel[saldo.Registro])
    {
        causa = "BasDatPorPagar usa una deuda de aportes distinta del saldo de RecordClientes";
    }
    else if (afiliado.MotivoBaja == "Renuncia")
    {
        causa = "El Excel sigue cobrando después de la renuncia; el sistema cobra hasta la baja";
    }
    else
    {
        causa = "Sin explicar";
    }

    if (causa.Length > 0)
    {
        causasTribunal.TryAdd(causa, new List<int>());
        causasTribunal[causa].Add(saldo.Registro);
    }

    filasTribunales.Add(string.Join(';', saldo.Registro, afiliado.Nombre, saldo.Asistidas, asistidas, saldo.Pagadas, pagadas,
        saldo.SaldoNeto.ToString(CultureInfo.InvariantCulture), saldoNeto.ToString(CultureInfo.InvariantCulture), causa));
}

var bom = new UTF8Encoding(true);
File.WriteAllLines(Path.Combine(carpetaInformes, "Comparacion - afiliados.csv"), filasAfiliados, bom);
File.WriteAllLines(Path.Combine(carpetaInformes, "Comparacion - tribunales.csv"), filasTribunales, bom);

static string Porcentaje(int parte, int total) => total == 0 ? "0" : (100.0 * parte / total).ToString("0.0", CultureInfo.InvariantCulture);

var resumen = new StringBuilder();
resumen.AppendLine($"RNF-05: comparación de saldos del sistema con el Excel del Colegio al {corte:dd/MM/yyyy}");
resumen.AppendLine();
resumen.Append(cargados);
resumen.AppendLine();
resumen.AppendLine($"Saldo de aportes: {iguales} de {comparados} afiliados con saldo en el Excel coinciden ({Porcentaje(iguales, comparados)} %).");
resumen.AppendLine($"Estado (Activo, Inactivo, Falleció, Renunció): {estadosIguales} de {datos.Clientes.Count} coinciden ({Porcentaje(estadosIguales, datos.Clientes.Count)} %).");
resumen.AppendLine($"Saldo neto de tribunales (BasDatPorPagar): {tribunalesIguales} de {tribunalesComparados} coinciden ({Porcentaje(tribunalesIguales, tribunalesComparados)} %).");
resumen.AppendLine();
resumen.AppendLine("Afiliados que no coinciden o no se pueden comparar, por causa:");
foreach (var (causa, registros) in causas)
{
    resumen.AppendLine($"- {causa}: {registros.Count} (registros {string.Join(", ", registros)})");
}
resumen.AppendLine();
resumen.AppendLine("Tribunales con saldo neto distinto, por causa:");
foreach (var (causa, registros) in causasTribunal)
{
    resumen.AppendLine($"- {causa}: {registros.Count} (registros {string.Join(", ", registros)})");
}

File.WriteAllText(Path.Combine(carpetaInformes, "Resumen RNF-05.txt"), resumen.ToString(), bom);
Console.WriteLine();
Console.WriteLine(resumen);
Console.WriteLine($"Informes en {carpetaInformes}");
return 0;
