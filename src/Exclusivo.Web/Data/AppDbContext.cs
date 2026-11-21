using Exclusivo.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Exclusivo.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<RecomendacionCatalogo> Recomendaciones => Set<RecomendacionCatalogo>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<VarianteProducto> Variantes => Set<VarianteProducto>();
    public DbSet<HistorialPrecio> HistorialPrecios => Set<HistorialPrecio>();
    public DbSet<Almacen> Almacenes => Set<Almacen>();
    public DbSet<Existencia> Existencias => Set<Existencia>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();
    public DbSet<OrdenCompra> OrdenesCompra => Set<OrdenCompra>();
    public DbSet<DetalleOrdenCompra> DetallesOrdenCompra => Set<DetalleOrdenCompra>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<ConfiguracionEmpresa> Configuraciones => Set<ConfiguracionEmpresa>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>().HasIndex(u => u.NombreUsuario).IsUnique();
        modelBuilder.Entity<Proveedor>().HasIndex(p => p.NombreOrganizacion).IsUnique();
        modelBuilder.Entity<Categoria>().HasIndex(c => c.Nombre).IsUnique();
        modelBuilder.Entity<Marca>().HasIndex(m => m.Nombre).IsUnique();
        modelBuilder.Entity<Producto>().HasIndex(p => p.Stock).IsUnique();
        modelBuilder.Entity<VarianteProducto>().HasIndex(v => v.CodigoBarras);
        modelBuilder.Entity<Cliente>().HasIndex(c => c.Documento);
        modelBuilder.Entity<Venta>().HasIndex(v => v.NumeroFactura).IsUnique();
        modelBuilder.Entity<OrdenCompra>().HasIndex(o => o.Numero).IsUnique();

        modelBuilder.Entity<Existencia>()
            .HasIndex(e => new { e.AlmacenId, e.VarianteProductoId }).IsUnique();

        modelBuilder.Entity<DetalleVenta>()
            .Property(d => d.Subtotal).HasComputedColumnSql("\"Cantidad\" * \"PrecioUnitario\"", stored: true);

        modelBuilder.Entity<MovimientoInventario>()
            .HasOne(m => m.Almacen).WithMany().HasForeignKey(m => m.AlmacenId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MovimientoInventario>()
            .HasOne(m => m.AlmacenDestino).WithMany().HasForeignKey(m => m.AlmacenDestinoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MovimientoInventario>()
            .HasOne(m => m.VarianteProducto).WithMany().HasForeignKey(m => m.VarianteProductoId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Venta>()
            .HasOne(v => v.Usuario).WithMany().HasForeignKey(v => v.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DetalleVenta>()
            .HasOne(d => d.VarianteProducto).WithMany().HasForeignKey(d => d.VarianteProductoId).OnDelete(DeleteBehavior.Restrict);
    }
}
