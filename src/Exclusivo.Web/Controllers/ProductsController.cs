using Exclusivo.Web.Data;
using Exclusivo.Web.Models;
using Exclusivo.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Exclusivo.Web.Controllers;

[Authorize(Roles = "Administrador,Gerente,Inventario")]
public class ProductsController : Controller
{
    private static readonly string[] TiposTalla = ["Única", "S", "M", "L", "XL", "XXL"];

    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private string Usuario => AuthService.ObtenerUsuario(HttpContext) ?? "sistema";

    public ProductsController(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IActionResult> Index(string? buscar)
    {
        var termino = string.IsNullOrWhiteSpace(buscar) ? null : buscar.Trim();

        var consulta = _db.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Include(p => p.Marca)
            .Include(p => p.ProveedorPrincipal)
            .AsQueryable();

        if (termino is not null)
            consulta = consulta.Where(p => EF.Functions.ILike(p.Nombre, $"%{termino}%"));

        var productos = await consulta.OrderBy(p => p.Nombre).ToListAsync();
        ViewBag.Buscar = termino;
        return View(productos);
    }

    public async Task<IActionResult> Create() => await FormularioAsync(new Producto());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Producto producto)
    {
        NormalizarProducto(producto);
        await ValidarProductoAsync(producto, null);

        if (!ModelState.IsValid)
            return await FormularioAsync(producto);

        producto.FechaCreacion = DateTime.UtcNow;
        producto.Estado = "Activo";
        producto.HistorialPrecios.Add(new HistorialPrecio
        {
            CostoAnterior = 0,
            CostoNuevo = producto.Costo,
            PrecioAnterior = 0,
            PrecioNuevo = producto.Precio,
            Usuario = Usuario
        });

        _db.Productos.Add(producto);
        await _db.SaveChangesAsync();
        await AsegurarVariantePrincipalAsync(producto);
        await _audit.RegistrarAsync(Usuario, "Crear producto", "Producto", producto.Id, producto.Stock);
        TempData["Mensaje"] = "Producto registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var producto = await _db.Productos.FindAsync(id);
        if (producto is null) return NotFound();
        producto.Talla = (await _db.Variantes.FirstOrDefaultAsync(v => v.ProductoId == id))?.Talla;
        return await FormularioAsync(producto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Producto producto)
    {
        if (id != producto.Id)
            return BadRequest();

        var original = await _db.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (original is null)
            return NotFound();

        NormalizarProducto(producto);
        await ValidarProductoAsync(producto, id);

        if (!ModelState.IsValid)
            return await FormularioAsync(producto);

        producto.FechaCreacion = original.FechaCreacion;
        if (string.IsNullOrWhiteSpace(producto.Estado))
            producto.Estado = original.Estado;

        if (original.Precio != producto.Precio || original.Costo != producto.Costo)
        {
            producto.HistorialPrecios.Add(new HistorialPrecio
            {
                ProductoId = id,
                CostoAnterior = original.Costo,
                CostoNuevo = producto.Costo,
                PrecioAnterior = original.Precio,
                PrecioNuevo = producto.Precio,
                Usuario = Usuario
            });
        }

        _db.Productos.Update(producto);
        await _db.SaveChangesAsync();
        await AsegurarVariantePrincipalAsync(producto);
        await _audit.RegistrarAsync(Usuario, "Editar producto", "Producto", id, producto.Stock);
        TempData["Mensaje"] = "Producto actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Actualiza el estado del producto: Activo &lt;-&gt; Descontinuado.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var producto = await _db.Productos.FindAsync(id);
        if (producto is null)
            return NotFound();

        var estabaActivo = string.Equals(producto.Estado, "Activo", StringComparison.OrdinalIgnoreCase);
        producto.Estado = estabaActivo ? "Descontinuado" : "Activo";
        await _db.SaveChangesAsync();

        await _audit.RegistrarAsync(Usuario, "Actualizar estado de producto", "Producto", id, producto.Stock);
        TempData["Mensaje"] = estabaActivo ? "Producto descontinuado." : "Producto activado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var producto = await _db.Productos.FindAsync(id);
        if (producto is null)
            return NotFound();

        var variantesIds = await _db.Variantes
            .Where(v => v.ProductoId == id)
            .Select(v => v.Id)
            .ToListAsync();

        if (await _db.DetallesVenta.AnyAsync(d => variantesIds.Contains(d.VarianteProductoId)))
        {
            TempData["Error"] = "No se puede eliminar el producto porque tiene ventas registradas. Actualice su estado a Descontinuado.";
            return RedirectToAction(nameof(Index));
        }

        if (await _db.MovimientosInventario.AnyAsync(m => variantesIds.Contains(m.VarianteProductoId)))
        {
            TempData["Error"] = "No se puede eliminar el producto porque tiene movimientos de inventario registrados. Actualice su estado a Descontinuado.";
            return RedirectToAction(nameof(Index));
        }

        var existencias = await _db.Existencias
            .Where(e => variantesIds.Contains(e.VarianteProductoId))
            .ToListAsync();
        _db.Existencias.RemoveRange(existencias);

        var detallesOrden = await _db.DetallesOrdenCompra
            .Where(d => variantesIds.Contains(d.VarianteProductoId))
            .ToListAsync();
        _db.DetallesOrdenCompra.RemoveRange(detallesOrden);

        var historial = await _db.HistorialPrecios.Where(h => h.ProductoId == id).ToListAsync();
        _db.HistorialPrecios.RemoveRange(historial);

        var variantes = await _db.Variantes.Where(v => v.ProductoId == id).ToListAsync();
        _db.Variantes.RemoveRange(variantes);

        _db.Productos.Remove(producto);
        await _db.SaveChangesAsync();

        await _audit.RegistrarAsync(Usuario, "Eliminar producto", "Producto", id, producto.Stock);
        TempData["Mensaje"] = "Producto eliminado.";
        return RedirectToAction(nameof(Index));
    }

    private static void NormalizarProducto(Producto producto)
    {
        producto.Stock = producto.Stock?.Trim() ?? string.Empty;
        producto.Nombre = producto.Nombre?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(producto.Estado))
            producto.Estado = producto.Estado.Trim();
    }

    private async Task ValidarProductoAsync(Producto producto, int? idActual)
    {
        if (string.IsNullOrWhiteSpace(producto.Stock))
        {
            ModelState.AddModelError(
                nameof(Producto.Stock),
                "Ingrese el STOCK del producto; no puede quedar vacío ni contener solo espacios.");
        }
        else
        {
            var stockDuplicado = await _db.Productos.AnyAsync(p =>
                p.Stock == producto.Stock &&
                (!idActual.HasValue || p.Id != idActual.Value));

            if (stockDuplicado)
            {
                ModelState.AddModelError(
                    nameof(Producto.Stock),
                    "Ya existe un producto con ese STOCK.");
            }
        }

        if (string.IsNullOrWhiteSpace(producto.Nombre))
        {
            ModelState.AddModelError(
                nameof(Producto.Nombre),
                "Ingrese el nombre del producto; no puede quedar vacío ni contener solo espacios.");
        }

        if (producto.Precio <= 0)
        {
            ModelState.AddModelError(
                nameof(Producto.Precio),
                "El precio debe ser mayor que cero.");
        }

        if (producto.Costo < 0)
        {
            ModelState.AddModelError(
                nameof(Producto.Costo),
                "El costo no puede ser negativo.");
        }
        else if (producto.Precio > 0 && producto.Precio < producto.Costo)
        {
            ModelState.AddModelError(
                nameof(Producto.Precio),
                "El precio no puede ser menor que el costo; la venta generaría pérdidas.");
        }

        if (!string.IsNullOrWhiteSpace(producto.Talla) &&
            !TiposTalla.Contains(producto.Talla.Trim(), StringComparer.OrdinalIgnoreCase) &&
            !producto.Talla.Trim().All(char.IsDigit))
        {
            ModelState.AddModelError(
                nameof(Producto.Talla),
                "Use Única, S, M, L, XL, XXL o una talla numérica.");
        }

        if (producto.CategoriaId <= 0 || !await _db.Categorias.AnyAsync(c => c.Id == producto.CategoriaId && c.Activo))
        {
            ModelState.AddModelError(
                nameof(Producto.CategoriaId),
                "Seleccione una categoría válida.");
        }

        if (producto.MarcaId <= 0 || !await _db.Marcas.AnyAsync(m => m.Id == producto.MarcaId && m.Activo))
        {
            ModelState.AddModelError(
                nameof(Producto.MarcaId),
                "Seleccione una marca válida.");
        }

        if (producto.ProveedorPrincipalId <= 0 || !await _db.Proveedores.AnyAsync(p => p.Id == producto.ProveedorPrincipalId && p.Activo))
        {
            ModelState.AddModelError(
                nameof(Producto.ProveedorPrincipalId),
                "Seleccione un proveedor principal válido.");
        }
    }

    /// <summary>
    /// Crea (o actualiza) la variante principal del producto con la talla indicada y
    /// genera una fila de existencia por almacen activo para que aparezca en inventario y POS.
    /// </summary>
    private async Task AsegurarVariantePrincipalAsync(Producto producto)
    {
        var talla = string.IsNullOrWhiteSpace(producto.Talla) ? "Única" : producto.Talla.Trim();
        if (talla.Length > 10) talla = talla[..10];

        var variante = await _db.Variantes.FirstOrDefaultAsync(v => v.ProductoId == producto.Id);
        if (variante is null)
        {
            variante = new VarianteProducto
            {
                ProductoId = producto.Id,
                Talla = talla,
                Color = "Único",
                CodigoBarras = $"{producto.Stock}-{talla}"
            };
            _db.Variantes.Add(variante);
            await _db.SaveChangesAsync();

            var almacenesIds = await _db.Almacenes.Where(a => a.Activo).Select(a => a.Id).ToListAsync();
            foreach (var almacenId in almacenesIds)
            {
                _db.Existencias.Add(new Existencia
                {
                    AlmacenId = almacenId,
                    VarianteProductoId = variante.Id,
                    Cantidad = 0,
                    Minimo = 0,
                    Maximo = 1000
                });
            }
            await _db.SaveChangesAsync();
        }
        else if (variante.Talla != talla)
        {
            variante.Talla = talla;
            await _db.SaveChangesAsync();
        }
    }

    private async Task<IActionResult> FormularioAsync(Producto producto)
    {
        var categorias = await _db.Categorias.Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync();
        var marcas = await _db.Marcas.Where(m => m.Activo).OrderBy(m => m.Nombre).ToListAsync();

        static string EtiquetaMarca(Marca m) =>
            m.Nombre.Equals(CatalogoHelper.MarcaPropia, StringComparison.OrdinalIgnoreCase)
                ? $"{CatalogoHelper.MarcaPropia} (marca propia)"
                : m.Nombre;

        ViewBag.Categorias = new SelectList(categorias, "Id", "Nombre", producto.CategoriaId);
        ViewBag.Marcas = new SelectList(
            marcas.Select(m => new SelectListItem { Value = m.Id.ToString(), Text = EtiquetaMarca(m) }),
            "Value", "Text", producto.MarcaId?.ToString());
        ViewBag.MarcasPorCategoria = categorias.ToDictionary(
            c => c.Id,
            c => CatalogoHelper.MarcasPara(c.Nombre)
                .Select(n => marcas.FirstOrDefault(m => m.Nombre.Equals(n, StringComparison.OrdinalIgnoreCase)))
                .Where(m => m is not null)
                .Select(m => new { id = m!.Id, nombre = EtiquetaMarca(m!) })
                .ToList());
        ViewBag.NombresSugeridos = categorias.ToDictionary(
            c => c.Id,
            c => marcas.ToDictionary(
                m => m.Id,
                m => CatalogoHelper.NombresPara(c.Nombre, m.Nombre)));
        ViewBag.Proveedores = new SelectList(
            await _db.Proveedores.Where(p => p.Activo).OrderBy(p => p.NombreOrganizacion).ToListAsync(),
            "Id", "NombreOrganizacion", producto.ProveedorPrincipalId);
        return View("Form", producto);
    }
}
