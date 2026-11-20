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
    private static readonly string[] TiposPermitidos = ["Entrada", "Salida", "Ajuste"];

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
        NormalizarMovimiento(model);
        await ValidarMovimientoAsync(model);

        if (!ModelState.IsValid)
            return await FormularioAsync(model);

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

    private static void NormalizarMovimiento(MovimientoViewModel model)
    {
        model.Tipo = model.Tipo?.Trim() ?? string.Empty;
        model.Motivo = string.IsNullOrWhiteSpace(model.Motivo) ? null : model.Motivo.Trim();
        model.DocumentoReferencia = string.IsNullOrWhiteSpace(model.DocumentoReferencia)
            ? null
            : model.DocumentoReferencia.Trim();
    }

    private async Task ValidarMovimientoAsync(MovimientoViewModel model)
    {
        if (model.AlmacenId <= 0 || !await _db.Almacenes.AnyAsync(a => a.Id == model.AlmacenId && a.Activo))
        {
            ModelState.AddModelError(
                nameof(MovimientoViewModel.AlmacenId),
                "Seleccione un almacén válido y activo.");
        }

        if (model.VarianteProductoId <= 0 || !await _db.Variantes.AnyAsync(v => v.Id == model.VarianteProductoId))
        {
            ModelState.AddModelError(
                nameof(MovimientoViewModel.VarianteProductoId),
                "Seleccione una variante de producto válida.");
        }

        if (string.IsNullOrWhiteSpace(model.Tipo) || !TiposPermitidos.Contains(model.Tipo))
        {
            ModelState.AddModelError(
                nameof(MovimientoViewModel.Tipo),
                "Seleccione un tipo de movimiento válido (Entrada, Salida o Ajuste).");
        }

        if (model.Cantidad <= 0)
        {
            ModelState.AddModelError(
                nameof(MovimientoViewModel.Cantidad),
                "La cantidad debe ser mayor que cero.");
        }
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
        ViewBag.Tipos = TiposPermitidos
            .Select(t => new SelectListItem { Value = t, Text = t, Selected = t == model.Tipo })
            .ToList();
        return View("Form", model);
    }
}
