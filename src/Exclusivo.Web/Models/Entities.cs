using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Exclusivo.Web.Models;

public class Usuario
{
    public int Id { get; set; }
    [Required, StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "El usuario solo puede contener letras, números, punto, guion o guion bajo.")]
    public string NombreUsuario { get; set; } = string.Empty;
    [StringLength(100, MinimumLength = 2)] public string? Nombres { get; set; }
    [StringLength(100, MinimumLength = 2)] public string? Apellidos { get; set; }
    [Required] public string PasswordHash { get; set; } = string.Empty;
    [Required, StringLength(30)] public string Rol { get; set; } = "Vendedor";
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoAcceso { get; set; }
}

public class Proveedor
{
    public int Id { get; set; }
    [StringLength(100, MinimumLength = 2)] public string? Nombres { get; set; }
    [StringLength(100, MinimumLength = 2)] public string? Apellidos { get; set; }
    [Required, StringLength(120, MinimumLength = 2)] public string NombreOrganizacion { get; set; } = string.Empty;
    [StringLength(30)] public string? Ruc { get; set; }
    [DataType(DataType.PhoneNumber)]
    [RegularExpression(@"^(?:\+505[ -]?)?[0-9]{4}[ -]?[0-9]{4}$",
        ErrorMessage = "Ingrese 8 dígitos. Puede usar 8888-1111 o +505 8888-1111.")]
    [StringLength(30)]
    public string? Telefono { get; set; }
    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido."), StringLength(150)] public string? Correo { get; set; }
    [StringLength(250)] public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

/// <summary>Recomendaciones de control tomadas del catalogo Word (seccion 4.3).</summary>
public class RecomendacionCatalogo
{
    public int Id { get; set; }
    [Required, StringLength(40)] public string Ambito { get; set; } = "General";
    [Required, StringLength(500)] public string Texto { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
}

public class Categoria
{
    public int Id { get; set; }
    [Required, StringLength(80, MinimumLength = 2)] public string Nombre { get; set; } = string.Empty;
    [StringLength(250)] public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
}

public class Marca
{
    public int Id { get; set; }
    [Required, StringLength(80, MinimumLength = 2)] public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

public class Producto
{
    public int Id { get; set; }
    [Required, StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9-]+$", ErrorMessage = "El STOCK solo puede contener letras, números y guiones.")]
    [Display(Name = "STOCK")]
    public string Stock { get; set; } = string.Empty;

    [Required, StringLength(150, MinimumLength = 3)] public string Nombre { get; set; } = string.Empty;
    [StringLength(500)] public string? Descripcion { get; set; }
    [Range(1, int.MaxValue)] public int? CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }
    [Range(1, int.MaxValue)] public int? MarcaId { get; set; }
    public Marca? Marca { get; set; }
    [Range(1, int.MaxValue)] public int? ProveedorPrincipalId { get; set; }
    public Proveedor? ProveedorPrincipal { get; set; }
    [Column(TypeName = "numeric(12,2)")]
    [Range(typeof(decimal), "0", "9999999999.99", ErrorMessage = "El costo no puede ser negativo.")]
    public decimal Costo { get; set; }
    [Column(TypeName = "numeric(12,2)")]
    [Range(typeof(decimal), "0.01", "9999999999.99", ErrorMessage = "El precio debe ser mayor que cero.")]
    public decimal Precio { get; set; }
    [NotMapped, StringLength(10)]
    [RegularExpression(@"^$|^(?:Única|S|M|L|XL|XXL|[0-9]{1,2})$",
        ErrorMessage = "Use Única, S, M, L, XL, XXL o una talla numérica.")]
    [Display(Name = "Talla")]
    public string? Talla { get; set; }
    [Required, StringLength(20)]
    [RegularExpression(@"^(?:Activo|Descontinuado)$")]
    public string Estado { get; set; } = "Activo";
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public ICollection<VarianteProducto> Variantes { get; set; } = new List<VarianteProducto>();
    public ICollection<HistorialPrecio> HistorialPrecios { get; set; } = new List<HistorialPrecio>();
}

public class VarianteProducto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    [StringLength(10)] public string? Talla { get; set; }
    [StringLength(40)] public string? Color { get; set; }
    [StringLength(80)] public string? Modelo { get; set; }
    [StringLength(80)] public string? CodigoBarras { get; set; }
    public bool Activo { get; set; } = true;
    public ICollection<Existencia> Existencias { get; set; } = new List<Existencia>();
}

public class HistorialPrecio
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    [Column(TypeName = "numeric(12,2)"), Range(typeof(decimal), "0", "9999999999.99")]
    public decimal CostoAnterior { get; set; }
    [Column(TypeName = "numeric(12,2)"), Range(typeof(decimal), "0", "9999999999.99")]
    public decimal CostoNuevo { get; set; }
    [Column(TypeName = "numeric(12,2)"), Range(typeof(decimal), "0", "9999999999.99")]
    public decimal PrecioAnterior { get; set; }
    [Column(TypeName = "numeric(12,2)"), Range(typeof(decimal), "0", "9999999999.99")]
    public decimal PrecioNuevo { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    [StringLength(100)] public string? Usuario { get; set; }
}

public class Almacen
{
    public int Id { get; set; }
    [Required, StringLength(100, MinimumLength = 2)] public string Nombre { get; set; } = string.Empty;
    [StringLength(250)] public string? Descripcion { get; set; }
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
    [Range(0, int.MaxValue)] public int Cantidad { get; set; }
    [Range(0, int.MaxValue)] public int Minimo { get; set; }
    [Range(0, int.MaxValue)] public int Maximo { get; set; } = 1000;
}

public class MovimientoInventario
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    [Required, StringLength(30)]
    [RegularExpression(@"^(?:Entrada|Salida|Ajuste)$")]
    public string Tipo { get; set; } = "Entrada";
    public int AlmacenId { get; set; }
    public Almacen Almacen { get; set; } = null!;
    public int? AlmacenDestinoId { get; set; }
    public Almacen? AlmacenDestino { get; set; }
    public int VarianteProductoId { get; set; }
    public VarianteProducto VarianteProducto { get; set; } = null!;
    [Range(1, int.MaxValue)] public int Cantidad { get; set; }
    [StringLength(250)] public string? Motivo { get; set; }
    [StringLength(100)] public string? DocumentoReferencia { get; set; }
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public int? OrdenCompraId { get; set; }
    public int? VentaId { get; set; }
}

public class Cliente
{
    public int Id { get; set; }
    [Required, StringLength(150, MinimumLength = 2)] public string Nombre { get; set; } = string.Empty;
    [StringLength(150, MinimumLength = 2)] public string? Nombres { get; set; }
    [StringLength(150, MinimumLength = 2)] public string? Apellidos { get; set; }
    [StringLength(30)] public string Documento { get; set; } = string.Empty;
    [StringLength(30)]
    [RegularExpression(@"^$|^[0-9]{3}-[0-9]{6}-[0-9]{4}$", ErrorMessage = "Use el formato 000-000000-0000.")]
    public string? Cedula { get; set; }
    [DataType(DataType.PhoneNumber)]
    [RegularExpression(@"^$|^(?:\+505[ -]?)?[0-9]{4}[ -]?[0-9]{4}$",
        ErrorMessage = "Ingrese 8 dígitos. Puede usar 8888-1111 o +505 8888-1111.")]
    [StringLength(30)]
    public string? Telefono { get; set; }
    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido."), StringLength(150)] public string? Correo { get; set; }
    [StringLength(250)] public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

public class Venta
{
    public int Id { get; set; }
    [Required, StringLength(30, MinimumLength = 5)] public string NumeroFactura { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    [Column(TypeName = "numeric(12,2)"), Range(typeof(decimal), "0", "9999999999.99")]
    public decimal Subtotal { get; set; }
    [Column(TypeName = "numeric(12,2)"), Range(typeof(decimal), "0", "9999999999.99")]
    public decimal Descuento { get; set; }
    [Column(TypeName = "numeric(12,2)"), Range(typeof(decimal), "0", "9999999999.99")]
    public decimal Impuesto { get; set; }
    [Column(TypeName = "numeric(12,2)"), Range(typeof(decimal), "0", "9999999999.99")]
    public decimal Total { get; set; }
    [Required, StringLength(20)]
    [RegularExpression(@"^(?:Confirmada|Anulada)$")]
    public string Estado { get; set; } = "Confirmada";
    [StringLength(250)] public string? MotivoAnulacion { get; set; }
    public ICollection<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();
}

public class DetalleVenta
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public Venta Venta { get; set; } = null!;
    public int VarianteProductoId { get; set; }
    public VarianteProducto VarianteProducto { get; set; } = null!;
    [Range(1, int.MaxValue)] public int Cantidad { get; set; }
    [Column(TypeName = "numeric(12,2)"), Range(typeof(decimal), "0", "9999999999.99")]
    public decimal PrecioUnitario { get; set; }
    [Column(TypeName = "numeric(12,2)"), Range(typeof(decimal), "0", "9999999999.99")]
    public decimal CostoUnitario { get; set; }
    [Column(TypeName = "numeric(12,2)"), Range(typeof(decimal), "0", "9999999999.99")]
    public decimal Subtotal { get; set; }
}

public class OrdenCompra
{
    public int Id { get; set; }
    [Required, StringLength(30, MinimumLength = 3)] public string Numero { get; set; } = string.Empty;
    public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    [StringLength(30)] public string Estado { get; set; } = "Borrador";
    [Column(TypeName = "numeric(12,2)"), Range(typeof(decimal), "0", "9999999999.99")]
    public decimal Subtotal { get; set; }
    [Column(TypeName = "numeric(12,2)"), Range(typeof(decimal), "0", "9999999999.99")]
    public decimal Impuesto { get; set; }
    [Column(TypeName = "numeric(12,2)"), Range(typeof(decimal), "0", "9999999999.99")]
    public decimal Total { get; set; }
    public ICollection<DetalleOrdenCompra> Detalles { get; set; } = new List<DetalleOrdenCompra>();
}

public class DetalleOrdenCompra
{
    public int Id { get; set; }
    public int OrdenCompraId { get; set; }
    public OrdenCompra OrdenCompra { get; set; } = null!;
    public int VarianteProductoId { get; set; }
    public VarianteProducto VarianteProducto { get; set; } = null!;
    [Range(1, int.MaxValue)] public int Cantidad { get; set; }
    [Range(0, int.MaxValue)] public int CantidadRecibida { get; set; }
    [Column(TypeName = "numeric(12,2)"), Range(typeof(decimal), "0", "9999999999.99")]
    public decimal CostoUnitario { get; set; }
}

public class Auditoria
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    [StringLength(100)] public string? Usuario { get; set; }
    [Required, StringLength(100)] public string Accion { get; set; } = string.Empty;
    [StringLength(80)] public string? Entidad { get; set; }
    public int? EntidadId { get; set; }
    [StringLength(1000)] public string? Detalles { get; set; }
}

public class ConfiguracionEmpresa
{
    public int Id { get; set; }
    [Required, StringLength(150, MinimumLength = 2)] public string Nombre { get; set; } = "Exclusivo";
    [Required, StringLength(10)] public string Moneda { get; set; } = "C$";
    [Column(TypeName = "numeric(5,2)"), Range(typeof(decimal), "0", "100")]
    public decimal ImpuestoPorcentaje { get; set; } = 15;
    [Column(TypeName = "numeric(12,2)"), Range(typeof(decimal), "0", "100")]
    public decimal DescuentoMaximoAutorizado { get; set; } = 10;
    [Required, StringLength(10)] public string PrefijoFactura { get; set; } = "FAC";
}
