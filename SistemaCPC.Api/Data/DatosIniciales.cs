using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Models;

namespace SistemaCPC.Api.Data;

// Usuarios de prueba para desarrollo. Son inventados: no usar datos del Excel del Colegio.
// Registro / contraseña:
//   admin / admin123   Superadministrador
//   dir01 / dir123     Directorio
//   0457  / 0457       Afiliado
public static class DatosIniciales
{
    public static void Cargar(CpcContext context)
    {
        context.Database.Migrate();

        if (context.Usuarios.Any())
        {
            return;
        }

        var hasher = new PasswordHasher<Usuario>();

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
}
