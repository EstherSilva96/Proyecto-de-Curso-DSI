using Exclusivo.Web.Data;
using Exclusivo.Web.Models;
using Exclusivo.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Exclusivo.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class UsersController : Controller
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private static readonly string[] Roles = { "Administrador", "Gerente", "Inventario", "Vendedor", "Cajero" };
    private string Usuario => AuthService.ObtenerUsuario(HttpContext) ?? "sistema";

    public UsersController(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IActionResult> Index() => View(await _db.Usuarios.OrderBy(u => u.NombreUsuario).ToListAsync());

    public IActionResult Create() => Formulario(new Usuario());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Usuario usuario, string clave)
    {
        if (await _db.Usuarios.AnyAsync(u => u.NombreUsuario == usuario.NombreUsuario))
            ModelState.AddModelError("NombreUsuario", "Ese usuario ya existe.");
        if (string.IsNullOrWhiteSpace(clave) || clave.Length < 6)
            ModelState.AddModelError("Clave", "La contraseña debe tener al menos 6 caracteres.");
        if (!ModelState.IsValid) return Formulario(usuario);

        usuario.PasswordHash = PasswordHasher.Hash(clave);
        usuario.FechaCreacion = DateTime.UtcNow;
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();
        await _audit.RegistrarAsync(Usuario, "Crear usuario", "Usuario", usuario.Id, usuario.NombreUsuario);
        TempData["Mensaje"] = "Usuario registrado.";
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
        if (await _db.Usuarios.AnyAsync(u => u.NombreUsuario == usuario.NombreUsuario && u.Id != id))
            ModelState.AddModelError("NombreUsuario", "Ese usuario ya existe.");
        if (!ModelState.IsValid) return Formulario(usuario);

        var existente = await _db.Usuarios.AsNoTracking().FirstAsync(u => u.Id == id);
        usuario.PasswordHash = string.IsNullOrWhiteSpace(clave) ? existente.PasswordHash : PasswordHasher.Hash(clave);
        usuario.FechaCreacion = existente.FechaCreacion;
        usuario.UltimoAcceso = existente.UltimoAcceso;
        _db.Usuarios.Update(usuario);
        await _db.SaveChangesAsync();
        await _audit.RegistrarAsync(Usuario, "Editar usuario", "Usuario", id, usuario.NombreUsuario);
        TempData["Mensaje"] = "Usuario actualizado.";
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
        TempData["Mensaje"] = usuario.Activo ? "Usuario activado." : "Usuario desactivado.";
        return RedirectToAction(nameof(Index));
    }

    private IActionResult Formulario(Usuario usuario)
    {
        ViewBag.Roles = Roles
            .Select(r => new SelectListItem { Value = r, Text = r, Selected = r == usuario.Rol })
            .ToList();
        return View("Form", usuario);
    }
}
