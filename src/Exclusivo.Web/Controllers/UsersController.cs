using Exclusivo.Web.Data;
using Exclusivo.Web.Models;
using Exclusivo.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Exclusivo.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private static readonly string[] Roles = ["Admin", "Gerente", "Inventario", "Vendedor", "Cajero"];
    private string UsuarioActual => AuthService.ObtenerUsuario(HttpContext) ?? "sys";

    public UsersController(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IActionResult> Index() =>
        View(await _db.Usuarios.OrderBy(u => u.NombreUsuario).ToListAsync());

    public IActionResult Create() => Formulario(new Usuario { Activo = true });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Usuario usuario, string? clave)
    {
        NormalizarUsuario(usuario);
        ModelState.Remove("PasswordHash");

        var nombreUsuarioLower = usuario.NombreUsuario.ToLower();

        if (string.IsNullOrWhiteSpace(usuario.NombreUsuario))
            ModelState.AddModelError(nameof(Usuario.NombreUsuario), "Usr req.");
        else if (await _db.Usuarios.AnyAsync(u => (u.NombreUsuario ?? string.Empty).ToLower() == nombreUsuarioLower))
            ModelState.AddModelError(nameof(Usuario.NombreUsuario), "Usr existe.");

        if (string.IsNullOrWhiteSpace(usuario.Rol) || !Roles.Contains(usuario.Rol))
            ModelState.AddModelError(nameof(Usuario.Rol), "Rol inval.");

        if (string.IsNullOrWhiteSpace(clave) || clave.Trim().Length < 6)
            ModelState.AddModelError("Clave", "Min 6 car.");

        if (!ModelState.IsValid)
            return Formulario(usuario);

        usuario.PasswordHash = PasswordHasher.Hash(clave!.Trim());
        usuario.FechaCreacion = DateTime.UtcNow;
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();
        await _audit.RegistrarAsync(UsuarioActual, "Crear usr", "Usr", usuario.Id, usuario.NombreUsuario);
        TempData["Mensaje"] = "Usr ok.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        return usuario is null ? NotFound() : Formulario(usuario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Usuario usuario, string? clave)
    {
        if (id != usuario.Id) return BadRequest();

        NormalizarUsuario(usuario);
        ModelState.Remove("PasswordHash");

        var existente = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (existente is null) return NotFound();

        var nombreUsuarioLower = usuario.NombreUsuario.ToLower();

        if (string.IsNullOrWhiteSpace(usuario.NombreUsuario))
            ModelState.AddModelError(nameof(Usuario.NombreUsuario), "Usr req.");
        else if (await _db.Usuarios.AnyAsync(u =>
                     (u.NombreUsuario ?? string.Empty).ToLower() == nombreUsuarioLower && u.Id != id))
            ModelState.AddModelError(nameof(Usuario.NombreUsuario), "Usr existe.");

        if (string.IsNullOrWhiteSpace(usuario.Rol) || !Roles.Contains(usuario.Rol))
            ModelState.AddModelError(nameof(Usuario.Rol), "Rol inval.");

        if (!string.IsNullOrWhiteSpace(clave) && clave.Trim().Length < 6)
            ModelState.AddModelError("Clave", "Min 6 car.");

        if (!ModelState.IsValid)
            return Formulario(usuario);

        usuario.PasswordHash = string.IsNullOrWhiteSpace(clave)
            ? existente.PasswordHash
            : PasswordHasher.Hash(clave.Trim());
        usuario.FechaCreacion = existente.FechaCreacion;
        usuario.UltimoAcceso = existente.UltimoAcceso;

        _db.Usuarios.Update(usuario);
        await _db.SaveChangesAsync();
        await _audit.RegistrarAsync(UsuarioActual, "Edit usr", "Usr", id, usuario.NombreUsuario);
        TempData["Mensaje"] = "Usr act.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();

        usuario.Activo = !usuario.Activo;
        await _db.SaveChangesAsync();
        await _audit.RegistrarAsync(
            UsuarioActual,
            usuario.Activo ? "Act usr" : "Desact usr",
            "Usr",
            usuario.Id,
            usuario.NombreUsuario);
        TempData["Mensaje"] = usuario.Activo ? "Usr act." : "Usr desact.";
        return RedirectToAction(nameof(Index));
    }

    private static void NormalizarUsuario(Usuario usuario)
    {
        usuario.NombreUsuario = (usuario.NombreUsuario ?? string.Empty).Trim();
        usuario.Rol = (usuario.Rol ?? string.Empty).Trim();
    }

    private IActionResult Formulario(Usuario usuario)
    {
        ViewBag.Roles = Roles
            .Select(r => new SelectListItem { Value = r, Text = r, Selected = r == usuario.Rol })
            .ToList();
        return View("Form", usuario);
    }
}
