using Exclusivo.Web.Data;
using Exclusivo.Web.Models;
using Exclusivo.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Exclusivo.Web.Controllers;

[Authorize(Roles = "Administrador,Gerente,Inventario")]
public class WarehousesController : Controller
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private string Usuario => AuthService.ObtenerUsuario(HttpContext) ?? "sistema";

    public WarehousesController(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IActionResult> Index() =>
        View(await _db.Almacenes.AsNoTracking().OrderBy(a => a.Nombre).ToListAsync());

    public IActionResult Create() => View("Form", new Almacen { Activo = true });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Almacen almacen)
    {
        NormalizarAlmacen(almacen);
        await ValidarAlmacenAsync(almacen, idActual: null);

        if (!ModelState.IsValid)
            return View("Form", almacen);

        _db.Almacenes.Add(almacen);
        await _db.SaveChangesAsync();
        await _audit.RegistrarAsync(Usuario, "Crear almacén", "Almacen", almacen.Id, almacen.Nombre);
        TempData["Mensaje"] = "Almacén registrado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var almacen = await _db.Almacenes.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        return almacen is null ? NotFound() : View("Form", almacen);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Almacen almacen)
    {
        if (id != almacen.Id) return BadRequest();

        NormalizarAlmacen(almacen);
        await ValidarAlmacenAsync(almacen, idActual: id);

        if (!ModelState.IsValid)
            return View("Form", almacen);

        _db.Almacenes.Update(almacen);
        await _db.SaveChangesAsync();
        await _audit.RegistrarAsync(Usuario, "Editar almacén", "Almacen", almacen.Id, almacen.Nombre);
        TempData["Mensaje"] = "Almacén actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var almacen = await _db.Almacenes.FindAsync(id);
        if (almacen is null) return NotFound();

        if (almacen.Activo && await _db.Almacenes.CountAsync(a => a.Activo) <= 1)
        {
            TempData["Error"] = "Debe quedar al menos un almacén activo.";
            return RedirectToAction(nameof(Index));
        }

        almacen.Activo = !almacen.Activo;
        await _db.SaveChangesAsync();
        await _audit.RegistrarAsync(Usuario, almacen.Activo ? "Activar almacén" : "Desactivar almacén",
            "Almacen", almacen.Id, almacen.Nombre);
        TempData["Mensaje"] = almacen.Activo ? "Almacén activado." : "Almacén desactivado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var almacen = await _db.Almacenes.FindAsync(id);
        if (almacen is null) return NotFound();

        if (almacen.Activo && await _db.Almacenes.CountAsync(a => a.Activo) <= 1)
        {
            TempData["Error"] = "No se puede eliminar el único almacén activo.";
            return RedirectToAction(nameof(Index));
        }

        var tieneMovimientos = await _db.MovimientosInventario
            .AnyAsync(m => m.AlmacenId == id || m.AlmacenDestinoId == id);
        if (tieneMovimientos)
        {
            TempData["Error"] = "No se puede eliminar el almacén porque tiene movimientos registrados. Puede desactivarlo.";
            return RedirectToAction(nameof(Index));
        }

        var existencias = await _db.Existencias.Where(e => e.AlmacenId == id).ToListAsync();
        if (existencias.Any(e => e.Cantidad != 0))
        {
            TempData["Error"] = "No se puede eliminar el almacén porque aún tiene existencias. Transfiera el stock o desactívelo.";
            return RedirectToAction(nameof(Index));
        }

        if (existencias.Count > 0)
            _db.Existencias.RemoveRange(existencias);

        _db.Almacenes.Remove(almacen);
        await _db.SaveChangesAsync();
        await _audit.RegistrarAsync(Usuario, "Eliminar almacén", "Almacen", almacen.Id, almacen.Nombre);
        TempData["Mensaje"] = "Almacén eliminado.";
        return RedirectToAction(nameof(Index));
    }

    private static void NormalizarAlmacen(Almacen almacen)
    {
        almacen.Nombre = almacen.Nombre?.Trim() ?? string.Empty;
        almacen.Descripcion = string.IsNullOrWhiteSpace(almacen.Descripcion)
            ? null
            : almacen.Descripcion.Trim();
    }

    private async Task ValidarAlmacenAsync(Almacen almacen, int? idActual)
    {
        if (string.IsNullOrWhiteSpace(almacen.Nombre))
        {
            ModelState.AddModelError(nameof(Almacen.Nombre), "El nombre del almacén es obligatorio.");
            return;
        }

        if (almacen.Nombre.Length > 100)
        {
            ModelState.AddModelError(nameof(Almacen.Nombre), "El nombre no puede superar 100 caracteres.");
        }

        var existeDuplicado = await _db.Almacenes.AnyAsync(a =>
            a.Nombre.ToLower() == almacen.Nombre.ToLower() &&
            (!idActual.HasValue || a.Id != idActual.Value));

        if (existeDuplicado)
        {
            ModelState.AddModelError(nameof(Almacen.Nombre), "Ya existe un almacén con ese nombre.");
        }
    }
}
