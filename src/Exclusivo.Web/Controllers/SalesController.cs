using Exclusivo.Web.Data;
using Exclusivo.Web.Models;
using Exclusivo.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Exclusivo.Web.Controllers;

[Authorize]
public class SalesController : Controller
{
    private readonly AppDbContext _db;
    private readonly SalesService _ventas;
    private readonly InventoryService _inventario;
    private readonly AuditService _audit;
    private string Usuario => AuthService.ObtenerUsuario(HttpContext) ?? "sistema";

    public SalesController(AppDbContext db, SalesService ventas, InventoryService inventario, AuditService audit)
    {
        _db = db;
        _ventas = ventas;
        _inventario = inventario;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var ventas = await _db.Ventas.Include(v => v.Usuario).Include(v => v.Cliente)
            .OrderByDescending(v => v.Fecha).ToListAsync();
        return View(ventas);
    }

    [Authorize(Roles = "Administrador,Gerente,Vendedor")]
    public async Task<IActionResult> Create() => await FormularioAsync(new VentaCreateViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Gerente,Vendedor")]
    public async Task<IActionResult> Create(VentaCreateViewModel model)
    {
        model.Items ??= [];

        if (model.Items.Any(i => i.Cantidad < 0))
        {
            ModelState.AddModelError(string.Empty, "La cantidad de los productos no puede ser negativa.");
        }

        var items = model.Items
            .Where(i => i.Cantidad > 0)
            .Select(i => new ItemVentaViewModel
            {
                VarianteProductoId = i.VarianteProductoId,
                Cantidad = i.Cantidad
            })
            .ToList();

        if (items.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Debe agregar al menos un producto al carrito.");
        }
        else if (items.Any(i => i.VarianteProductoId <= 0))
        {
            ModelState.AddModelError(string.Empty, "Seleccione una variante de producto válida para cada ítem.");
        }

        if (model.AlmacenId <= 0 || !await _db.Almacenes.AnyAsync(a => a.Id == model.AlmacenId && a.Activo))
        {
            ModelState.AddModelError(nameof(VentaCreateViewModel.AlmacenId), "Seleccione un almacén válido y activo.");
        }

        if (!ModelState.IsValid)
        {
            return await FormularioAsync(model);
        }

        model.Items = items;
        var resultado = await _ventas.CrearVentaAsync(model, Usuario);
        if (!resultado.Ok)
        {
            ModelState.AddModelError(string.Empty, resultado.Mensaje);
            return await FormularioAsync(model);
        }

        TempData["Mensaje"] = resultado.Mensaje;
        return RedirectToAction(nameof(Details), new { id = resultado.VentaId });
    }

    public async Task<IActionResult> Details(int id)
    {
        var venta = await _db.Ventas
            .Include(v => v.Detalles).ThenInclude(d => d.VarianteProducto).ThenInclude(vp => vp.Producto)
            .Include(v => v.Cliente).Include(v => v.Usuario)
            .FirstOrDefaultAsync(v => v.Id == id);
        return venta is null ? NotFound() : View(venta);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Gerente")]
    public async Task<IActionResult> Anular(int id, string motivo)
    {
        var motivoLimpio = motivo?.Trim();
        if (string.IsNullOrWhiteSpace(motivoLimpio))
        {
            TempData["Error"] = "La anulación requiere un motivo.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var venta = await _db.Ventas.Include(v => v.Detalles).FirstOrDefaultAsync(v => v.Id == id);
        if (venta is null) return NotFound();
        if (venta.Estado != "Confirmada")
        {
            TempData["Error"] = "La venta ya no está confirmada.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var almacenId = await _db.MovimientosInventario
            .Where(m => m.VentaId == id)
            .Select(m => m.AlmacenId)
            .FirstOrDefaultAsync();

        if (almacenId == 0)
            almacenId = await _db.Almacenes.Where(a => a.Activo).Select(a => a.Id).FirstOrDefaultAsync();

        if (almacenId == 0)
        {
            TempData["Error"] = "No hay un almacén activo disponible para reingresar el inventario.";
            return RedirectToAction(nameof(Details), new { id });
        }

        foreach (var detalle in venta.Detalles)
        {
            var resultado = await _inventario.RegistrarMovimientoAsync(
                almacenId, detalle.VarianteProductoId, detalle.Cantidad, "Entrada",
                $"Anulación: {motivoLimpio}", venta.NumeroFactura, Usuario);
            if (!resultado.Ok)
            {
                TempData["Error"] = resultado.Mensaje;
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        venta.Estado = "Anulada";
        venta.MotivoAnulacion = motivoLimpio;
        await _db.SaveChangesAsync();

        await _db.Auditorias.AddAsync(new Auditoria
        {
            Usuario = Usuario,
            Accion = "Anular venta",
            Entidad = "Venta",
            EntidadId = id,
            Detalles = $"{venta.NumeroFactura} · {motivoLimpio}"
        });
        await _db.SaveChangesAsync();

        TempData["Mensaje"] = "Venta anulada y stock restaurado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Gerente,Vendedor")]
    public async Task<IActionResult> RegistrarCliente(string? nombreCliente)
    {
        var nombre = nombreCliente?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            return BadRequest(new { mensaje = "Escriba el nombre del cliente." });
        if (nombre.Length > 150)
            return BadRequest(new { mensaje = "El nombre no puede superar 150 caracteres." });

        var existente = await _db.Clientes
            .FirstOrDefaultAsync(c => c.Nombre.ToLower() == nombre.ToLower());
        if (existente is not null)
        {
            if (!existente.Activo)
            {
                existente.Activo = true;
                await _db.SaveChangesAsync();
            }
            return Json(new { id = existente.Id, nombre = existente.Nombre });
        }

        var cliente = new Cliente { Nombre = nombre, Activo = true };
        _db.Clientes.Add(cliente);
        _db.Auditorias.Add(new Auditoria
        {
            Usuario = Usuario,
            Accion = "Crear cliente",
            Entidad = "Cliente",
            Detalles = nombre
        });
        await _db.SaveChangesAsync();

        return Json(new { id = cliente.Id, nombre = cliente.Nombre });
    }

    [HttpGet]
    public async Task<IActionResult> ProductoDetalle(int id)
    {
        var variante = await _db.Variantes.Include(v => v.Producto).FirstOrDefaultAsync(v => v.Id == id);
        if (variante is null) return NotFound();

        return Json(new
        {
            id = variante.Id,
            codigo = variante.Producto.Stock,
            nombre = variante.Producto.Nombre,
            costo = variante.Producto.Costo,
            precio = variante.Producto.Precio,
            talla = variante.Talla,
            color = variante.Color
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductoEditar(ProductoPosViewModel model)
    {
        var variante = await _db.Variantes.Include(v => v.Producto).FirstOrDefaultAsync(v => v.Id == model.VarianteId);
        if (variante is null) return Json(new { ok = false, mensaje = "El producto no existe." });

        if (string.IsNullOrWhiteSpace(model.Nombre))
            return Json(new { ok = false, mensaje = "El nombre es obligatorio." });
        if (model.Precio <= 0)
            return Json(new { ok = false, mensaje = "El precio debe ser mayor que cero." });
        if (model.Costo < 0)
            return Json(new { ok = false, mensaje = "El costo no puede ser negativo." });

        var producto = variante.Producto;
        if (producto.Precio != model.Precio || producto.Costo != model.Costo)
        {
            _db.HistorialPrecios.Add(new HistorialPrecio
            {
                ProductoId = producto.Id,
                CostoAnterior = producto.Costo, CostoNuevo = model.Costo,
                PrecioAnterior = producto.Precio, PrecioNuevo = model.Precio,
                Usuario = Usuario
            });
        }

        producto.Nombre = model.Nombre.Trim();
        producto.Costo = model.Costo;
        producto.Precio = model.Precio;
        variante.Talla = string.IsNullOrWhiteSpace(model.Talla) ? "Única" : model.Talla.Trim()[..Math.Min(10, model.Talla.Trim().Length)];
        variante.Color = string.IsNullOrWhiteSpace(model.Color) ? "Único" : model.Color.Trim();

        await _db.SaveChangesAsync();
        await _audit.RegistrarAsync(Usuario, "Editar producto desde POS", "Producto", producto.Id, producto.Nombre);
        return Json(new { ok = true, mensaje = "Producto actualizado.", nombre = producto.Nombre, talla = variante.Talla, color = variante.Color, precio = producto.Precio });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductoActualizar(ProductoPosViewModel model)
    {
        if (model.AlmacenId <= 0) return Json(new { ok = false, mensaje = "Seleccione un almacén." });
        if (model.Cantidad < 0) return Json(new { ok = false, mensaje = "La cantidad no puede ser negativa." });

        var actual = await _db.Existencias
            .Where(e => e.AlmacenId == model.AlmacenId && e.VarianteProductoId == model.VarianteId)
            .Select(e => e.Cantidad).FirstOrDefaultAsync();

        var delta = model.Cantidad - actual;
        if (delta == 0) return Json(new { ok = true, mensaje = "El stock no ha cambiado.", cantidad = actual });

        var resultado = await _inventario.RegistrarMovimientoAsync(
            model.AlmacenId, model.VarianteId, delta, "Ajuste",
            "Actualización de stock desde el punto de venta", null, Usuario);

        return Json(new { ok = resultado.Ok, mensaje = resultado.Mensaje, cantidad = model.Cantidad });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductoEliminar(int varianteId)
    {
        var variante = await _db.Variantes.Include(v => v.Producto).FirstOrDefaultAsync(v => v.Id == varianteId);
        if (variante is null) return Json(new { ok = false, mensaje = "El producto no existe." });

        variante.Activo = false;
        variante.Producto.Estado = "Descontinuado";
        await _db.SaveChangesAsync();
        await _audit.RegistrarAsync(Usuario, "Eliminar producto desde POS", "Producto", variante.ProductoId, variante.Producto.Nombre);
        return Json(new { ok = true, mensaje = "Producto eliminado del catálogo." });
    }

    private async Task<IActionResult> FormularioAsync(VentaCreateViewModel model)
    {
        ViewBag.Variantes = await _db.Variantes.Include(v => v.Producto).Include(v => v.Existencias)
            .Where(v => v.Activo && v.Producto.Estado == "Activo")
            .OrderBy(v => v.Producto.Nombre).ToListAsync();
        ViewBag.Clientes = new SelectList(await _db.Clientes.Where(c => c.Activo).ToListAsync(), "Id", "Nombre", model.ClienteId);
        ViewBag.Almacenes = new SelectList(await _db.Almacenes.Where(a => a.Activo).ToListAsync(), "Id", "Nombre", model.AlmacenId);
        if (model.AlmacenId == 0)
            model.AlmacenId = await _db.Almacenes.Where(a => a.Activo).Select(a => a.Id).FirstOrDefaultAsync();
        return View("Form", model);
    }
}
