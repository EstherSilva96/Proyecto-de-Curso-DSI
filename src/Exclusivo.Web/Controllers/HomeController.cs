using Exclusivo.Web.Data;
using Exclusivo.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Exclusivo.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var hoy = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var inicioMes = DateTime.SpecifyKind(new DateTime(hoy.Year, hoy.Month, 1), DateTimeKind.Utc);

        var ventasHoy = await _db.Ventas
            .Where(v => v.Estado == "Confirmada" && v.Fecha >= hoy)
            .ToListAsync();
        var ventasMes = await _db.Ventas
            .Where(v => v.Estado == "Confirmada" && v.Fecha >= inicioMes)
            .ToListAsync();

        var existencias = await _db.Existencias
            .Include(e => e.VarianteProducto).ThenInclude(v => v.Producto)
            .ToListAsync();

        var stockBajo = existencias.Where(e => e.Cantidad <= e.Minimo).ToList();

        var detallesVendidos = await _db.DetallesVenta
            .Include(d => d.Venta)
            .Include(d => d.VarianteProducto).ThenInclude(v => v.Producto)
            .Where(d => d.Venta.Estado == "Confirmada")
            .ToListAsync();

        var productosMasVendidos = detallesVendidos
            .GroupBy(d => new
            {
                d.VarianteProducto.ProductoId,
                Nombre = d.VarianteProducto.Producto != null
                    ? d.VarianteProducto.Producto.Nombre
                    : "Producto"
            })
            .Select(g => new ProductoMasVendidoViewModel
            {
                ProductoId = g.Key.ProductoId,
                Nombre = g.Key.Nombre,
                Unidades = g.Sum(d => d.Cantidad),
                Total = g.Sum(d => d.Subtotal)
            })
            .OrderByDescending(p => p.Unidades)
            .ThenByDescending(p => p.Total)
            .Take(5)
            .ToList();

        var modelo = new DashboardViewModel
        {
            TotalProductos = await _db.Productos.CountAsync(p => p.Estado == "Activo"),
            VentasHoy = ventasHoy.Count,
            MontoHoy = ventasHoy.Sum(v => v.Total),
            MontoMes = ventasMes.Sum(v => v.Total),
            GananciaBruta = await _db.DetallesVenta
                .Include(d => d.Venta)
                .Where(d => d.Venta.Estado == "Confirmada" && d.Venta.Fecha >= inicioMes)
                .SumAsync(d => (d.PrecioUnitario - d.CostoUnitario) * d.Cantidad),
            AlertasStock = stockBajo.Count,
            Agotados = stockBajo.Count(e => e.Cantidad == 0),
            ValorInventario = existencias.Sum(e => e.Cantidad * e.VarianteProducto.Producto.Costo),
            StockBajo = stockBajo.Take(8).ToList(),
            UltimasVentas = await _db.Ventas
                .Include(v => v.Usuario)
                .OrderByDescending(v => v.Fecha)
                .Take(8)
                .ToListAsync(),
            ProductosMasVendidos = productosMasVendidos
        };

        return View(modelo);
    }

    public IActionResult Error() => View();
}
