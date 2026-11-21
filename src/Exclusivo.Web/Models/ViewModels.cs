using System.ComponentModel.DataAnnotations;

namespace Exclusivo.Web.Models;

public class LoginViewModel
{
    [Required, StringLength(50, MinimumLength = 3), Display(Name = "Usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password), Display(Name = "Contraseña")]
    public string Clave { get; set; } = string.Empty;
}

public class ClienteFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(150, MinimumLength = 2, ErrorMessage = "Los nombres deben tener entre 2 y 150 caracteres."), Display(Name = "Nombres")]
    public string Nombres { get; set; } = string.Empty;

    [Required, StringLength(150, MinimumLength = 2, ErrorMessage = "Los apellidos deben tener entre 2 y 150 caracteres."), Display(Name = "Apellidos")]
    public string Apellidos { get; set; } = string.Empty;

    [Required, StringLength(30)]
    [RegularExpression(@"^[0-9]{3}-[0-9]{6}-[0-9]{4}$", ErrorMessage = "Use el formato 000-000000-0000.")]
    [Display(Name = "Número de cédula")]
    public string Cedula { get; set; } = string.Empty;

    [StringLength(30)]
    [RegularExpression(@"^$|^(?:\+505[ -]?)?[0-9]{4}[ -]?[0-9]{4}$",
        ErrorMessage = "Ingrese 8 dígitos. Puede usar 8888-1111 o +505 8888-1111.")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [StringLength(250, ErrorMessage = "La dirección no puede superar 250 caracteres.")]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
    [StringLength(150)]
    [Display(Name = "Correo electrónico")]
    public string? Correo { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;
}

/// <summary>
/// Representa una fila del carrito del POS. No lleva [Range] en VarianteProductoId
/// porque el formulario publica todas las variantes, incluidas las de cantidad 0;
/// el controlador filtra esas filas antes de validar.
/// </summary>
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

    [Range(typeof(decimal), "0", "9999999999.99", ErrorMessage = "El descuento no puede ser negativo.")]
    public decimal Descuento { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un almacén válido.")]
    public int AlmacenId { get; set; }

    public List<ItemVentaViewModel> Items { get; set; } = new();
}

public class ProductoPosViewModel
{
    [Range(1, int.MaxValue)] public int VarianteId { get; set; }

    [Required, StringLength(150, MinimumLength = 3)] public string Nombre { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999.99")] public decimal Costo { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999.99")] public decimal Precio { get; set; }

    [StringLength(10)] public string? Talla { get; set; }

    [StringLength(40)] public string? Color { get; set; }

    [Range(1, int.MaxValue)] public int AlmacenId { get; set; }

    [Range(0, int.MaxValue)] public int Cantidad { get; set; }
}

public class MovimientoViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un almacén válido.")]
    public int AlmacenId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una variante válida.")]
    public int VarianteProductoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor que cero.")]
    public int Cantidad { get; set; }

    [Required, StringLength(30)]
    [RegularExpression(@"^(?:Entrada|Salida|Ajuste)$", ErrorMessage = "Seleccione Entrada, Salida o Ajuste.")]
    public string Tipo { get; set; } = "Entrada";

    [StringLength(250)] public string? Motivo { get; set; }
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
