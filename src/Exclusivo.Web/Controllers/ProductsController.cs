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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Descontinuar(int id)
    {
        var producto = await _db.Productos.FindAsync(id);
        if (producto is null)
            return NotFound();

        producto.Estado = "Descontinuado";
        await _db.SaveChangesAsync();
        await _audit.RegistrarAsync(Usuario, "Descontinuar producto", "Producto", id, producto.Stock);
        TempData["Mensaje"] = "Producto descontinuado.";
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
