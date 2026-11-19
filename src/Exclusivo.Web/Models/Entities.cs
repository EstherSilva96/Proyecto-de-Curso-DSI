using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Exclusivo.Web.Models;

public class Usuario
{
    public int Id { get; set; }
    [Required, MaxLength(50)] public string NombreUsuario { get; set; } = string.Empty;
    [MaxLength(100)] public string? Nombres { get; set; }
    [MaxLength(100)] public string? Apellidos { get; set; }
    [Required] public string PasswordHash { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string Rol { get; set; } = "Vendedor";
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoAcceso { get; set; }
}

public class Proveedor
{
    public int Id { get; set; }
    [MaxLength(100)] public string? Nombres { get; set; }
    [MaxLength(100)] public string? Apellidos { get; set; }
    [Required, MaxLength(120)] public string NombreOrganizacion { get; set; } = string.Empty;
    [MaxLength(30)] public string? Ruc { get; set; }
    [MaxLength(30)] public string? Telefono { get; set; }
    [MaxLength(150)] public string? Correo { get; set; }
    [MaxLength(250)] public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

public class Categoria
{
    public int Id { get; set; }
    [Required, MaxLength(80)] public string Nombre { get; set; } = string.Empty;
    [MaxLength(250)] public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
}

public class Marca
{
    public int Id { get; set; }
    [Required, MaxLength(80)] public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

public class Producto
{
    public int Id { get; set; }
    [Required, MaxLength(50)] public string Sku { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string Nombre { get; set; } = string.Empty;
    [MaxLength(500)] public string? Descripcion { get; set; }
    public int? CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }
    public int? MarcaId { get; set; }
    public Marca? Marca { get; set; }
    public int? ProveedorPrincipalId { get; set; }
    public Proveedor? ProveedorPrincipal { get; set; }
    [Column(TypeName = "numeric(12,2)")] public decimal Costo { get; set; }
    [Column(TypeName = "numeric(12,2)")] public decimal Precio { get; set; }
    [MaxLength(300)] public string? ImagenUrl { get; set; }
    [MaxLength(20)] public string Estado { get; set; } = "Activo";
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public ICollection<VarianteProducto> Variantes { get; set; } = new List<VarianteProducto>();
    public ICollection<HistorialPrecio> HistorialPrecios { get; set; } = new List<HistorialPrecio>();
}

public class VarianteProducto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    [MaxLength(10)] public string? Talla { get; set; }
    [MaxLength(40)] public string? Color { get; set; }
    [MaxLength(80)] public string? Modelo { get; set; }
    [MaxLength(80)] public string? CodigoBarras { get; set; }
    public bool Activo { get; set; } = true;
    public ICollection<Existencia> Existencias { get; set; } = new List<Existencia>();
}

public class HistorialPrecio
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    [Column(TypeName = "numeric(12,2)")] public decimal CostoAnterior { get; set; }
    [Column(TypeName = "numeric(12,2)")] public decimal CostoNuevo { get; set; }
    [Column(TypeName = "numeric(12,2)")] public decimal PrecioAnterior { get; set; }
    [Column(TypeName = "numeric(12,2)")] public decimal PrecioNuevo { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    [MaxLength(100)] public string? Usuario { get; set; }
}

public class Almacen
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Nombre { get; set; } = string.Empty;
    [MaxLength(250)] public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public ICollection<Existencia> Existencias { get; set; } = new List<Existencia>();
}

public class Existencia
{
    public int Id { get; set; }
    public int AlmacenId { get; set; }
    public Almacen Almacen { get; set; } = null!;
    public int VarianteProductoId { get; set; }
    public VarianteProducto VarianteProducto { get; set; } = null!;
    public int Cantidad { get; set; }
    public int Minimo { get; set; }
    public int Maximo { get; set; } = 1000;
}

public class MovimientoInventario
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    [MaxLength(30)] public string Tipo { get; set; } = "Entrada";
    public int AlmacenId { get; set; }
    public Almacen Almacen { get; set; } = null!;
    public int? AlmacenDestinoId { get; set; }
    public Almacen? AlmacenDestino { get; set; }
    public int VarianteProductoId { get; set; }
    public VarianteProducto VarianteProducto { get; set; } = null!;
    public int Cantidad { get; set; }
    [MaxLength(250)] public string? Motivo { get; set; }
    [MaxLength(100)] public string? DocumentoReferencia { get; set; }
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public int? OrdenCompraId { get; set; }
    public int? VentaId { get; set; }
}

public class Cliente
{
    public int Id { get; set; }
    [Required, MaxLength(150)] public string Nombre { get; set; } = string.Empty;
    [MaxLength(30)] public string? Documento { get; set; }
    [MaxLength(30)] public string? Telefono { get; set; }
    [MaxLength(150)] public string? Correo { get; set; }
    [MaxLength(250)] public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

public class Venta
{
    public int Id { get; set; }
    [Required, MaxLength(30)] public string NumeroFactura { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    [Column(TypeName = "numeric(12,2)")] public decimal Subtotal { get; set; }
    [Column(TypeName = "numeric(12,2)")] public decimal Descuento { get; set; }
    [Column(TypeName = "numeric(12,2)")] public decimal Impuesto { get; set; }
    [Column(TypeName = "numeric(12,2)")] public decimal Total { get; set; }
    [MaxLength(20)] public string Estado { get; set; } = "Confirmada";
    [MaxLength(30)] public string MetodoPago { get; set; } = "Efectivo";
    [MaxLength(250)] public string? MotivoAnulacion { get; set; }
    public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}

public class DetalleVenta
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public Venta Venta { get; set; } = null!;
    public int VarianteProductoId { get; set; }
    public VarianteProducto VarianteProducto { get; set; } = null!;
    public int Cantidad { get; set; }
    [Column(TypeName = "numeric(12,2)")] public decimal PrecioUnitario { get; set; }
    [Column(TypeName = "numeric(12,2)")] public decimal CostoUnitario { get; set; }
    [Column(TypeName = "numeric(12,2)")] public decimal Subtotal { get; set; }
}

public class Pago
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public Venta Venta { get; set; } = null!;
    [Column(TypeName = "numeric(12,2)")] public decimal Monto { get; set; }
    [MaxLength(30)] public string Metodo { get; set; } = "Efectivo";
    [MaxLength(100)] public string? Referencia { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}

public class OrdenCompra
{
    public int Id { get; set; }
    [Required, MaxLength(30)] public string Numero { get; set; } = string.Empty;
    public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    [MaxLength(30)] public string Estado { get; set; } = "Borrador";
    [Column(TypeName = "numeric(12,2)")] public decimal Subtotal { get; set; }
    [Column(TypeName = "numeric(12,2)")] public decimal Impuesto { get; set; }
    [Column(TypeName = "numeric(12,2)")] public decimal Total { get; set; }
    public ICollection<DetalleOrdenCompra> Detalles { get; set; } = new List<DetalleOrdenCompra>();
}

public class DetalleOrdenCompra
{
    public int Id { get; set; }
    public int OrdenCompraId { get; set; }
    public OrdenCompra OrdenCompra { get; set; } = null!;
    public int VarianteProductoId { get; set; }
    public VarianteProducto VarianteProducto { get; set; } = null!;
    public int Cantidad { get; set; }
    public int CantidadRecibida { get; set; }
    [Column(TypeName = "numeric(12,2)")] public decimal CostoUnitario { get; set; }
}

public class Auditoria
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    [MaxLength(100)] public string? Usuario { get; set; }
    [Required, MaxLength(100)] public string Accion { get; set; } = string.Empty;
    [MaxLength(80)] public string? Entidad { get; set; }
    public int? EntidadId { get; set; }
    [MaxLength(1000)] public string? Detalles { get; set; }
}

public class ConfiguracionEmpresa
{
    public int Id { get; set; }
    [Required, MaxLength(150)] public string Nombre { get; set; } = "Exclusivo";
    [MaxLength(10)] public string Moneda { get; set; } = "C$";
    [Column(TypeName = "numeric(5,2)")] public decimal ImpuestoPorcentaje { get; set; } = 15;
    [Column(TypeName = "numeric(12,2)")] public decimal DescuentoMaximoAutorizado { get; set; } = 10;
    public string PrefijoFactura { get; set; } = "FAC";
}
