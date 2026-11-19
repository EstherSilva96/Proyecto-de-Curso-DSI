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

    public async Task<IActionResult> Index() => View(await _db.Almacenes.OrderBy(a => a.Nombre).ToListAsync());

    public IActionResult Create() => View("Form", new Almacen());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Almacen almacen)
    {
        if (await _db.Almacenes.AnyAsync(a => a.Nombre == almacen.Nombre))
            ModelState.AddModelError("Nombre", "Ya existe un almacén con ese nombre.");
        if (!ModelState.IsValid) return View("Form", almacen);
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
        if (await _db.Almacenes.AnyAsync(a => a.Nombre == almacen.Nombre && a.Id != id))
            ModelState.AddModelError("Nombre", "Ya existe un almacén con ese nombre.");
        if (!ModelState.IsValid) return View("Form", almacen);
        _db.Almacenes.Update(almacen);
        await _db.SaveChangesAsync();
        TempData["Mensaje"] = "Almacén actualizado.";
        return RedirectToAction(nameof(Index));
    }
}
