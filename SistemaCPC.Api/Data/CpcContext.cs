using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Models;

namespace SistemaCPC.Api.Data;

public class CpcContext : DbContext
{
    public CpcContext(DbContextOptions<CpcContext> options)
        : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Herencia del diagrama de clases: una sola tabla con una columna que dice el tipo.
        modelBuilder.Entity<Usuario>()
            .HasDiscriminator<string>("Tipo")
            .HasValue<Afiliado>("Afiliado")
            .HasValue<MiembroDirectorio>("MiembroDirectorio")
            .HasValue<Superadministrador>("Superadministrador");

        modelBuilder.Entity<Usuario>().HasIndex(u => u.Registro).IsUnique();
        modelBuilder.Entity<Usuario>().Property(u => u.Rol).HasConversion<string>();

        // Afiliado y MiembroDirectorio comparten la columna Nombre.
        modelBuilder.Entity<Afiliado>().Property(a => a.Nombre).HasColumnName("Nombre");
        modelBuilder.Entity<MiembroDirectorio>().Property(m => m.Nombre).HasColumnName("Nombre");
    }
}
