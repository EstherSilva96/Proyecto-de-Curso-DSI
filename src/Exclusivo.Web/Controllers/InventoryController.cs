using Exclusivo.Web.Data;
using Exclusivo.Web.Models;
using Exclusivo.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Exclusivo.Web.Controllers;

[Authorize]
public class InventoryController : Controller
{
    private readonly AppDbContext _db;
    private readonly InventoryService _inventario;
    private readonly AuditService _audit;
    private string Usuario => AuthService.ObtenerUsuario(HttpContext) ?? "sistema";

    public InventoryController(AppDbContext db, InventoryService inventario, AuditService audit)
    {
        _db = db;
        _inventario = inventario;
        _audit = audit;
    }

    public async Task<IActionResult> Index()
    {
        var existencias = await _db.Existencias
            .Include(e => e.Almacen)
            .Include(e => e.VarianteProducto).ThenInclude(v => v.Producto)
            .OrderBy(e => e.VarianteProducto.Producto.Nombre)
            .ToListAsync();
        ViewBag.Movimientos = await _db.MovimientosInventario
            .Include(m => m.VarianteProducto).ThenInclude(v => v.Producto)
            .Include(m => m.Almacen)
            .OrderByDescending(m => m.Fecha).Take(20).ToListAsync();
        return View(existencias);
    }

    [Authorize(Roles = "Administrador,Gerente,Inventario")]
    public async Task<IActionResult> Create() => await FormularioAsync(new MovimientoViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrador,Gerente,Inventario")]
    public async Task<IActionResult> Create(MovimientoViewModel model)
    {
        if (!ModelState.IsValid) return await FormularioAsync(model);

        var resultado = await _inventario.RegistrarMovimientoAsync(
            model.AlmacenId, model.VarianteProductoId, model.Cantidad,
            model.Tipo, model.Motivo, model.DocumentoReferencia, Usuario);

        if (!resultado.Ok)
        {
            ModelState.AddModelError(string.Empty, resultado.Mensaje);
            return await FormularioAsync(model);
        }

        await _audit.RegistrarAsync(Usuario, "Movimiento de inventario", "MovimientoInventario", null,
            $"{model.Tipo} · Variante {model.VarianteProductoId} · {model.Cantidad}");
        TempData["Mensaje"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> FormularioAsync(MovimientoViewModel model)
    {
        ViewBag.Almacenes = (await _db.Almacenes.Where(a => a.Activo).OrderBy(a => a.Nombre).ToListAsync())
            .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Nombre, Selected = a.Id == model.AlmacenId })
            .ToList();

        var variantes = await _db.Variantes.Include(v => v.Producto)
            .OrderBy(v => v.Producto.Nombre).ToListAsync();
        ViewBag.Variantes = variantes
            .Select(v => new SelectListItem
            {
                Value = v.Id.ToString(),
                Text = $"{v.Producto.Nombre} · {v.Color} / {v.Talla}",
                Selected = v.Id == model.VarianteProductoId
            })
            .ToList();
        ViewBag.Tipos = new[] { "Entrada", "Salida", "Ajuste" }
            .Select(t => new SelectListItem { Value = t, Text = t, Selected = t == model.Tipo })
            .ToList();
        return View("Form", model);
    }
}
