using Exclusivo.Web.Data;
using Exclusivo.Web.Models;
using Exclusivo.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Exclusivo.Web.Controllers;

[Authorize(Roles = "Administrador,Gerente")]
public class SuppliersController : Controller
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private string Usuario => AuthService.ObtenerUsuario(HttpContext) ?? "sistema";

    public SuppliersController(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IActionResult> Index() =>
        View(await _db.Proveedores.OrderBy(p => p.NombreOrganizacion).ToListAsync());

    public IActionResult Create() => View("Form", new Proveedor());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Proveedor proveedor)
    {
        NormalizarCampos(proveedor);
        await ValidarProveedorAsync(proveedor, null);

        if (!ModelState.IsValid)
            return View("Form", proveedor);

        proveedor.FechaCreacion = DateTime.UtcNow;
        _db.Proveedores.Add(proveedor);
        await _db.SaveChangesAsync();
        await _audit.RegistrarAsync(Usuario, "Crear proveedor", "Proveedor", proveedor.Id, proveedor.NombreOrganizacion);
        TempData["Mensaje"] = "Proveedor registrado.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var proveedor = await _db.Proveedores.FindAsync(id);
        return proveedor is null ? NotFound() : View("Form", proveedor);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Proveedor proveedor)
    {
        if (id != proveedor.Id)
            return BadRequest();

        var existente = await _db.Proveedores.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (existente is null)
            return NotFound();

        NormalizarCampos(proveedor);
        await ValidarProveedorAsync(proveedor, id);

        if (!ModelState.IsValid)
            return View("Form", proveedor);

        proveedor.FechaCreacion = existente.FechaCreacion;
        _db.Proveedores.Update(proveedor);
        await _db.SaveChangesAsync();
        await _audit.RegistrarAsync(Usuario, "Editar proveedor", "Proveedor", id, proveedor.NombreOrganizacion);
        TempData["Mensaje"] = "Proveedor actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var proveedor = await _db.Proveedores.FindAsync(id);
        if (proveedor is null)
            return NotFound();

        proveedor.Activo = !proveedor.Activo;
        await _db.SaveChangesAsync();
        TempData["Mensaje"] = proveedor.Activo ? "Proveedor activado." : "Proveedor desactivado.";
        return RedirectToAction(nameof(Index));
    }

    private static void NormalizarCampos(Proveedor proveedor)
    {
        proveedor.NombreOrganizacion = proveedor.NombreOrganizacion?.Trim() ?? string.Empty;
        proveedor.Apellidos = string.IsNullOrWhiteSpace(proveedor.Apellidos) ? null : proveedor.Apellidos.Trim();
        proveedor.Ruc = string.IsNullOrWhiteSpace(proveedor.Ruc) ? null : proveedor.Ruc.Trim();
        proveedor.Telefono = string.IsNullOrWhiteSpace(proveedor.Telefono) ? null : proveedor.Telefono.Trim();
        proveedor.Correo = string.IsNullOrWhiteSpace(proveedor.Correo) ? null : proveedor.Correo.Trim();
        proveedor.Direccion = string.IsNullOrWhiteSpace(proveedor.Direccion) ? null : proveedor.Direccion.Trim();
    }

    private async Task ValidarProveedorAsync(Proveedor proveedor, int? idActual)
    {
        if (string.IsNullOrWhiteSpace(proveedor.NombreOrganizacion))
        {
            ModelState.AddModelError(
                nameof(Proveedor.NombreOrganizacion),
                "Ingrese el nombre de la organización; no puede quedar vacío ni contener solo espacios.");
        }
        else
        {
            var duplicado = await _db.Proveedores.AnyAsync(p =>
                p.NombreOrganizacion == proveedor.NombreOrganizacion &&
                (!idActual.HasValue || p.Id != idActual.Value));

            if (duplicado)
            {
                ModelState.AddModelError(
                    nameof(Proveedor.NombreOrganizacion),
                    "Ya existe un proveedor con ese nombre de organización.");
            }
        }
    }
}
