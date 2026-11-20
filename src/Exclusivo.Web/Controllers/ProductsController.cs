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

    public async Task<IActionResult> Index()
    {
        var productos = await _db.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Marca)
            .Include(p => p.ProveedorPrincipal)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
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
        await _audit.RegistrarAsync(Usuario, "Crear producto", "Producto", producto.Id, producto.Sku);
        TempData["Mensaje"] = "Producto registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var producto = await _db.Productos.FindAsync(id);
        return producto is null ? NotFound() : await FormularioAsync(producto);
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
        await _audit.RegistrarAsync(Usuario, "Editar producto", "Producto", id, producto.Sku);
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
        await _audit.RegistrarAsync(Usuario, "Descontinuar producto", "Producto", id, producto.Sku);
        TempData["Mensaje"] = "Producto descontinuado.";
        return RedirectToAction(nameof(Index));
    }

    private static void NormalizarProducto(Producto producto)
    {
        producto.Sku = producto.Sku?.Trim() ?? string.Empty;
        producto.Nombre = producto.Nombre?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(producto.Estado))
            producto.Estado = producto.Estado.Trim();
    }

    private async Task ValidarProductoAsync(Producto producto, int? idActual)
    {
        if (string.IsNullOrWhiteSpace(producto.Sku))
        {
            ModelState.AddModelError(
                nameof(Producto.Sku),
                "Ingrese el SKU del producto; no puede quedar vacío ni contener solo espacios.");
        }
        else
        {
            var skuDuplicado = await _db.Productos.AnyAsync(p =>
                p.Sku == producto.Sku &&
                (!idActual.HasValue || p.Id != idActual.Value));

            if (skuDuplicado)
            {
                ModelState.AddModelError(
                    nameof(Producto.Sku),
                    "Ya existe un producto con ese SKU.");
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

    private async Task<IActionResult> FormularioAsync(Producto producto)
    {
        ViewBag.Categorias = new SelectList(
            await _db.Categorias.Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync(),
            "Id", "Nombre", producto.CategoriaId);
        ViewBag.Marcas = new SelectList(
            await _db.Marcas.Where(m => m.Activo).OrderBy(m => m.Nombre).ToListAsync(),
            "Id", "Nombre", producto.MarcaId);
        ViewBag.Proveedores = new SelectList(
            await _db.Proveedores.Where(p => p.Activo).OrderBy(p => p.NombreOrganizacion).ToListAsync(),
            "Id", "NombreOrganizacion", producto.ProveedorPrincipalId);
        return View("Form", producto);
    }
}
