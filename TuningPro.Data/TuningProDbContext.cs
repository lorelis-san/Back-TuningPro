using Microsoft.EntityFrameworkCore;
using TuningPro.Data.Entities;

namespace TuningPro.Data;

public class TuningProDbContext : DbContext
{
    public TuningProDbContext(DbContextOptions<TuningProDbContext> options) : base(options) { }

    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Pieza> Piezas => Set<Pieza>();
    public DbSet<Compatibilidad> Compatibilidades => Set<Compatibilidad>();
    public DbSet<DetalleProyecto> DetallesProyecto => Set<DetalleProyecto>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<OrdenTrabajo> OrdenesTrabajo => Set<OrdenTrabajo>();

    public DbSet<Proyectos> Proyectos => Set<Proyectos>();
    public DbSet<ProblemaCatalogo> ProblemasCatalogo => Set<ProblemaCatalogo>();
    public DbSet<ProyectoProblema> ProyectosProblemas => Set<ProyectoProblema>();
    public DbSet<DetalleOrdenTrabajo> DetallesOrdenTrabajo => Set<DetalleOrdenTrabajo>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Vehiculo>().HasIndex(v => v.Placa).IsUnique();
        mb.Entity<Pieza>().Property(p => p.PrecioUnitario).HasPrecision(18, 2);
        mb.Entity<DetalleProyecto>().Property(d => d.PrecioUnitario).HasPrecision(18, 2);

        mb.Entity<Pieza>().HasOne<Proveedor>().WithMany().HasForeignKey(p => p.ProveedorId);
        mb.Entity<Compatibilidad>().HasOne<Vehiculo>().WithMany().HasForeignKey(c => c.VehiculoId);
        mb.Entity<Compatibilidad>().HasOne<Pieza>().WithMany().HasForeignKey(c => c.PiezaId);
        mb.Entity<Compatibilidad>().HasIndex(c => new { c.VehiculoId, c.PiezaId }).IsUnique();
        mb.Entity<DetalleProyecto>().HasOne<Pieza>().WithMany().HasForeignKey(d => d.PiezaId);
        mb.Entity<Usuario>().HasIndex(u => u.NombreUsuario).IsUnique();
        mb.Entity<Vehiculo>().HasOne<Usuario>().WithMany().HasForeignKey(v => v.UsuarioId);

        // ---------- Orden de trabajo ----------
        mb.Entity<OrdenTrabajo>(e =>
        {
            e.Property(o => o.ManoObra).HasColumnType("decimal(10,2)");

            // Una sola OT por proyecto
            e.HasIndex(o => o.ProyectoId).IsUnique();

            e.HasOne(o => o.Proyecto).WithMany()
             .HasForeignKey(o => o.ProyectoId).OnDelete(DeleteBehavior.Restrict);

            e.HasOne(o => o.Tecnico).WithMany()
             .HasForeignKey(o => o.TecnicoId).OnDelete(DeleteBehavior.Restrict);

            e.HasMany(o => o.Detalles).WithOne(d => d.OrdenTrabajo)
             .HasForeignKey(d => d.OrdenTrabajoId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<DetalleOrdenTrabajo>(e =>
        {
            e.Property(d => d.PrecioUnitario).HasColumnType("decimal(10,2)");

            e.HasOne(d => d.Pieza).WithMany()
             .HasForeignKey(d => d.PiezaId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Datos de ejemplo para la demo ----------
        mb.Entity<Proveedor>().HasData(
            new Proveedor { Id = 1, Nombre = "Brembo", Contacto = "ventas@brembo.com" },
            new Proveedor { Id = 2, Nombre = "Bilstein", Contacto = "ventas@bilstein.com" },
            new Proveedor { Id = 3, Nombre = "HKS", Contacto = "ventas@hks.com" });

        mb.Entity<Vehiculo>().HasData(
            new Vehiculo { Id = 1, Placa = "ABC-123", Marca = "Toyota", Modelo = "Supra", Anio = 1998, ClienteNombre = "Juan Pérez" },
            new Vehiculo { Id = 2, Placa = "XYZ-789", Marca = "Honda", Modelo = "Civic", Anio = 1993, ClienteNombre = "María Torres" });

        mb.Entity<Pieza>().HasData(
            new Pieza { Id = 1, Codigo = "BRM-001", Nombre = "Discos de freno Brembo", Marca = "Brembo", Categoria = "Frenos", Stock = 8, PrecioUnitario = 1200m, ProveedorId = 1 },
            new Pieza { Id = 2, Codigo = "BIL-001", Nombre = "Amortiguadores Bilstein B8", Marca = "Bilstein", Categoria = "Suspensión", Stock = 4, PrecioUnitario = 2100m, ProveedorId = 2 },
            new Pieza { Id = 3, Codigo = "HKS-001", Nombre = "Turbo HKS GT", Marca = "HKS", Categoria = "Turbo", Stock = 1, PrecioUnitario = 5400m, ProveedorId = 3 });

        mb.Entity<Compatibilidad>().HasData(
            new Compatibilidad { Id = 1, VehiculoId = 1, PiezaId = 1 },
            new Compatibilidad { Id = 2, VehiculoId = 1, PiezaId = 2 },
            new Compatibilidad { Id = 3, VehiculoId = 1, PiezaId = 3 },
            new Compatibilidad { Id = 4, VehiculoId = 2, PiezaId = 1 },
            new Compatibilidad { Id = 5, VehiculoId = 2, PiezaId = 3 });

        // ---------- Proyectos ----------
        mb.Entity<Proyectos>(e =>
        {
            e.HasIndex(x => x.Numero).IsUnique();
            e.Property(x => x.Codigo).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Codigo).IsUnique();
            e.Property(x => x.Nombre).HasMaxLength(150);
            e.Property(x => x.OtrosProblema).HasMaxLength(200);
            e.Property(x => x.NotasRecepcion).HasMaxLength(1000);

            e.HasOne(x => x.Vehiculo).WithMany()
             .HasForeignKey(x => x.VehiculoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Recepcionista).WithMany()
             .HasForeignKey(x => x.RecepcionistaId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Detalles).WithOne()
             .HasForeignKey(d => d.ProyectoId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<ProyectoProblema>(e =>
        {
            e.HasKey(x => new { x.ProyectoId, x.ProblemaCatalogoId });
            e.HasOne(x => x.Proyecto).WithMany(p => p.Problemas)
             .HasForeignKey(x => x.ProyectoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ProblemaCatalogo).WithMany()
             .HasForeignKey(x => x.ProblemaCatalogoId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<ProblemaCatalogo>().HasData(
            new ProblemaCatalogo { Id = 1, Nombre = "Motor" },
            new ProblemaCatalogo { Id = 2, Nombre = "Frenos" },
            new ProblemaCatalogo { Id = 3, Nombre = "Suspensión" },
            new ProblemaCatalogo { Id = 4, Nombre = "Sistema eléctrico" },
            new ProblemaCatalogo { Id = 5, Nombre = "Transmisión" },
            new ProblemaCatalogo { Id = 6, Nombre = "Mantenimiento preventivo" },
            new ProblemaCatalogo { Id = 7, Nombre = "Aire acondicionado" });
    }
}