using System.ComponentModel.DataAnnotations;

namespace Exclusivo.Web.Models;

public class LoginViewModel
{
    [Required, Display(Name = "Usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Display(Name = "Contraseña")]
    public string Clave { get; set; } = string.Empty;
}

public class ClienteFormViewModel
{
    public int Id { get; set; }

    [Required, MaxLength(150), Display(Name = "Nombres")]
    public string Nombres { get; set; } = string.Empty;

    [Required, MaxLength(150), Display(Name = "Apellidos")]
    public string Apellidos { get; set; } = string.Empty;

    [Required, MaxLength(30), Display(Name = "Número de cédula")]
    public string Cedula { get; set; } = string.Empty;

    [MaxLength(30), Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [MaxLength(250), Display(Name = "Dirección")]
    public string? Direccion { get; set; }
}

public class ItemVentaViewModel
{
    public int VarianteProductoId { get; set; }
    public int Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public decimal CostoUnitario { get; set; }
}

public class VentaCreateViewModel
{
    public int? ClienteId { get; set; }
    public decimal Descuento { get; set; }
    public int AlmacenId { get; set; }
    public List<ItemVentaViewModel> Items { get; set; } = new();
}

public class ProductoPosViewModel
{
    public int VarianteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Costo { get; set; }
    public decimal Precio { get; set; }
    public string? Talla { get; set; }
    public string? Color { get; set; }
    public int AlmacenId { get; set; }
    public int Cantidad { get; set; }
}

public class MovimientoViewModel
{
    public int AlmacenId { get; set; }
    public int VarianteProductoId { get; set; }
    public int Cantidad { get; set; }
    [MaxLength(30)] public string Tipo { get; set; } = "Entrada";
    [MaxLength(250)] public string? Motivo { get; set; }
    [MaxLength(100)] public string? DocumentoReferencia { get; set; }
}

public class DashboardViewModel
{
    public int TotalProductos { get; set; }
    public int VentasHoy { get; set; }
    public decimal MontoHoy { get; set; }
    public decimal MontoMes { get; set; }
    public decimal GananciaBruta { get; set; }
    public int AlertasStock { get; set; }
    public int Agotados { get; set; }
    public decimal ValorInventario { get; set; }
    public List<Venta> UltimasVentas { get; set; } = new();
    public List<Existencia> StockBajo { get; set; } = new();
}
