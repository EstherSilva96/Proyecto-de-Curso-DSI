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
        View(await _db.Almacenes.OrderBy(a => a.Nombre).ToListAsync());

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
        var almacen = await _db.Almacenes.FindAsync(id);
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

    private static void NormalizarAlmacen(Almacen almacen)
    {
        almacen.Nombre = almacen.Nombre?.Trim() ?? string.Empty;
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
