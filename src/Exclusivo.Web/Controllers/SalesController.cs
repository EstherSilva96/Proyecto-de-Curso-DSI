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
    private string Usuario => AuthService.ObtenerUsuario(HttpContext) ?? "sistema";

    public SalesController(AppDbContext db, SalesService ventas, InventoryService inventario)
    {
        _db = db;
        _ventas = ventas;
        _inventario = inventario;
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
        var items = model.Items.Where(i => i.Cantidad > 0).ToList();
        if (items.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Debe agregar al menos un producto al carrito.");
            return await FormularioAsync(model);
        }

        model.Items = items.Select(i => new ItemVentaViewModel { VarianteProductoId = i.VarianteProductoId, Cantidad = i.Cantidad }).ToList();
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
        if (string.IsNullOrWhiteSpace(motivo))
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
            .Where(m => m.VentaId == id).Select(m => m.AlmacenId).FirstOrDefaultAsync();
        if (almacenId == 0) almacenId = await _db.Almacenes.Where(a => a.Activo).Select(a => a.Id).FirstOrDefaultAsync();

        foreach (var detalle in venta.Detalles)
        {
            var resultado = await _inventario.RegistrarMovimientoAsync(
                almacenId, detalle.VarianteProductoId, detalle.Cantidad, "Ajuste",
                $"Anulación: {motivo}", venta.NumeroFactura, Usuario);
            if (!resultado.Ok)
            {
                TempData["Error"] = resultado.Mensaje;
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        venta.Estado = "Anulada";
        venta.MotivoAnulacion = motivo;
        await _db.SaveChangesAsync();
        await _db.Auditorias.AddAsync(new Auditoria
        {
            Usuario = Usuario, Accion = "Anular venta", Entidad = "Venta", EntidadId = id,
            Detalles = $"{venta.NumeroFactura} · {motivo}"
        });
        await _db.SaveChangesAsync();
        TempData["Mensaje"] = "Venta anulada y stock restaurado.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<IActionResult> FormularioAsync(VentaCreateViewModel model)
    {
        ViewBag.Variantes = await _db.Variantes.Include(v => v.Producto).Include(v => v.Existencias)
            .OrderBy(v => v.Producto.Nombre).ToListAsync();
        ViewBag.Clientes = new SelectList(await _db.Clientes.Where(c => c.Activo).ToListAsync(), "Id", "Nombre", model.ClienteId);
        ViewBag.Almacenes = new SelectList(await _db.Almacenes.Where(a => a.Activo).ToListAsync(), "Id", "Nombre", model.AlmacenId);
        if (model.AlmacenId == 0)
            model.AlmacenId = await _db.Almacenes.Where(a => a.Activo).Select(a => a.Id).FirstOrDefaultAsync();
        return View("Form", model);
    }
}
