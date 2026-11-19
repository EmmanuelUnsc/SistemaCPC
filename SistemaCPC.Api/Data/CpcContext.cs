using Microsoft.EntityFrameworkCore;
using SistemaCPC.Api.Models;

namespace SistemaCPC.Api.Data;

public class CpcContext : DbContext
{
    public CpcContext(DbContextOptions<CpcContext> options)
        : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Afiliado> Afiliados => Set<Afiliado>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Defensa> Defensas => Set<Defensa>();
    public DbSet<Parametros> Parametros => Set<Parametros>();
    public DbSet<Ajuste> Ajustes => Set<Ajuste>();
    public DbSet<RegistroBitacora> Bitacora => Set<RegistroBitacora>();
    public DbSet<FotoAfiliado> FotosAfiliados => Set<FotoAfiliado>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configuracion)
    {
        configuracion.Properties<decimal>().HavePrecision(12, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>()
            .HasDiscriminator<string>("Tipo")
            .HasValue<Afiliado>("Afiliado")
            .HasValue<MiembroDirectorio>("MiembroDirectorio")
            .HasValue<Superadministrador>("Superadministrador");

        modelBuilder.Entity<Usuario>().HasIndex(u => u.Registro).IsUnique();
        modelBuilder.Entity<Usuario>().Property(u => u.Rol).HasConversion<string>();

        modelBuilder.Entity<Afiliado>().Property(a => a.Nombre).HasColumnName("Nombre");
        modelBuilder.Entity<MiembroDirectorio>().Property(m => m.Nombre).HasColumnName("Nombre");

        modelBuilder.Entity<Pago>().HasIndex(p => p.NumeroRecibo).IsUnique();
        modelBuilder.Entity<Pago>().Property(p => p.Origen).HasConversion<string>();
        modelBuilder.Entity<Defensa>().Property(d => d.Estado).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Pago>().Property(p => p.Origen).HasMaxLength(20);
        modelBuilder.Entity<Usuario>().Property(u => u.Rol).HasMaxLength(30);

        modelBuilder.Entity<Ajuste>()
            .HasOne(a => a.Superadministrador)
            .WithMany()
            .HasForeignKey(a => a.SuperadministradorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FotoAfiliado>()
            .HasOne(f => f.Afiliado)
            .WithOne()
            .HasForeignKey<FotoAfiliado>(f => f.AfiliadoId);
    }
}
