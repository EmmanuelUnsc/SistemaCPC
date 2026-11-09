using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Models;

namespace SistemaCPC.Api.Data;

// Datos de prueba para desarrollo. Son inventados: no usar datos del Excel del Colegio.
// Registro / contraseña:
//   admin / admin123   Superadministrador
//   dir01 / dir123     Directorio
//   0457  / 0457       Afiliado (tribunal habilitado)
//   0458  / 0458       Afiliado
public static class DatosIniciales
{
    public static void Cargar(CpcContext context)
    {
        context.Database.Migrate();

        var hasher = new PasswordHasher<Usuario>();

        if (!context.Usuarios.Any())
        {
            var usuarios = new List<(Usuario Usuario, string Contrasena)>
            {
                (new Superadministrador { Registro = "admin", Nombre = "Superadministrador de prueba", Cargo = "Presidencia", Rol = Rol.Superadministrador }, "admin123"),
                (new MiembroDirectorio { Registro = "dir01", Nombre = "Directorio de prueba", Cargo = "Vicepresidencia", Rol = Rol.Directorio }, "dir123"),
                (new Afiliado { Registro = "0457", Nombre = "Afiliado de prueba", Rol = Rol.Afiliado }, "0457")
            };

            foreach (var (usuario, contrasena) in usuarios)
            {
                usuario.ContrasenaCifrada = hasher.HashPassword(usuario, contrasena);
                context.Usuarios.Add(usuario);
            }

            context.SaveChanges();
        }

        // Parámetros y datos de ejemplo de afiliados, pagos y defensas.
        if (context.Parametros.Any())
        {
            return;
        }

        context.Parametros.Add(new Parametros { AporteMensual = 35, MontoPorDefensa = 208, UmbralInactividad = 4, TopeDefensasMes = 0 });

        var hoy = DateOnly.FromDateTime(DateTime.Today);

        var afiliado1 = context.Afiliados.First(a => a.Registro == "0457");
        afiliado1.Carnet = "1000001";
        afiliado1.FechaAfiliacion = new DateOnly(2020, 1, 15);
        afiliado1.FechaInicioCobro = new DateOnly(2020, 2, 1);
        afiliado1.EsTribunal = true;

        var afiliado2 = new Afiliado
        {
            Registro = "0458", Nombre = "Afiliado de prueba 2", Rol = Rol.Afiliado, Carnet = "1000002",
            FechaAfiliacion = new DateOnly(2025, 3, 10), FechaInicioCobro = new DateOnly(2025, 4, 1)
        };
        afiliado2.ContrasenaCifrada = hasher.HashPassword(afiliado2, "0458");
        context.Afiliados.Add(afiliado2);

        // El afiliado 0457 queda debiendo los últimos 3 meses.
        var meses = afiliado1.MesesTranscurridos(hoy) - 3;
        afiliado1.Pagos.Add(new Pago { NumeroRecibo = "R-0990", Fecha = hoy.AddMonths(-4), Monto = meses * 35, MesesCubiertos = meses, Origen = OrigenPago.Oficina });

        afiliado1.Defensas.Add(new Defensa { NumeroOficio = "OF-215/2026", Universidad = "Universidad B", Carta = "Carta N.º 90", Fecha = hoy.AddDays(-40), Estado = EstadoDefensa.Pagada });
        afiliado1.Defensas.Add(new Defensa { NumeroOficio = "OF-240/2026", Universidad = "Universidad A", Carta = "Carta N.º 98", Fecha = hoy.AddDays(-10) });

        context.SaveChanges();
    }
}
