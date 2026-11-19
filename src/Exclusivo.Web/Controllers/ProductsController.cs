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
            .Include(p => p.Categoria).Include(p => p.Marca).Include(p => p.ProveedorPrincipal)
            .OrderBy(p => p.Nombre).ToListAsync();
        return View(productos);
    }

    public async Task<IActionResult> Create() => await FormularioAsync(new Producto());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Producto producto)
    {
        if (await _db.Productos.AnyAsync(p => p.Sku == producto.Sku))
            ModelState.AddModelError("Sku", "Ya existe un producto con ese SKU.");
        if (producto.Precio <= 0) ModelState.AddModelError("Precio", "El precio debe ser mayor que cero.");
        if (producto.Costo < 0) ModelState.AddModelError("Costo", "El costo no puede ser negativo.");

        if (!ModelState.IsValid) return await FormularioAsync(producto);

        producto.FechaCreacion = DateTime.UtcNow;
        producto.Estado = "Activo";
        producto.HistorialPrecios.Add(new HistorialPrecio
        {
            CostoAnterior = 0, CostoNuevo = producto.Costo,
            PrecioAnterior = 0, PrecioNuevo = producto.Precio,
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
        if (id != producto.Id) return BadRequest();
        if (await _db.Productos.AnyAsync(p => p.Sku == producto.Sku && p.Id != id))
            ModelState.AddModelError("Sku", "Ya existe un producto con ese SKU.");
        if (!ModelState.IsValid) return await FormularioAsync(producto);

        var original = await _db.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (original is null) return NotFound();

        if (original.Precio != producto.Precio || original.Costo != producto.Costo)
        {
            producto.HistorialPrecios.Add(new HistorialPrecio
            {
                ProductoId = id,
                CostoAnterior = original.Costo, CostoNuevo = producto.Costo,
                PrecioAnterior = original.Precio, PrecioNuevo = producto.Precio,
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
        if (producto is null) return NotFound();
        producto.Estado = "Descontinuado";
        await _db.SaveChangesAsync();
        await _audit.RegistrarAsync(Usuario, "Descontinuar producto", "Producto", id, producto.Sku);
        TempData["Mensaje"] = "Producto descontinuado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> FormularioAsync(Producto producto)
    {
        ViewBag.Categorias = new SelectList(await _db.Categorias.Where(c => c.Activo).ToListAsync(), "Id", "Nombre", producto.CategoriaId);
        ViewBag.Marcas = new SelectList(await _db.Marcas.Where(m => m.Activo).ToListAsync(), "Id", "Nombre", producto.MarcaId);
        ViewBag.Proveedores = new SelectList(await _db.Proveedores.Where(p => p.Activo).ToListAsync(), "Id", "NombreOrganizacion", producto.ProveedorPrincipalId);
        return View("Form", producto);
    }
}
