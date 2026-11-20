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
    private static readonly string[] Roles = ["Administrador", "Gerente", "Inventario", "Vendedor", "Cajero"];
    private string Usuario => AuthService.ObtenerUsuario(HttpContext) ?? "sistema";

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

        if (string.IsNullOrWhiteSpace(usuario.NombreUsuario))
            ModelState.AddModelError(nameof(Usuario.NombreUsuario), "El nombre de usuario es obligatorio.");
        else if (await _db.Usuarios.AnyAsync(u => u.NombreUsuario.ToLower() == usuario.NombreUsuario.ToLower()))
            ModelState.AddModelError(nameof(Usuario.NombreUsuario), "Ese usuario ya existe.");

        if (!Roles.Contains(usuario.Rol))
            ModelState.AddModelError(nameof(Usuario.Rol), "Seleccione un rol válido.");

        if (string.IsNullOrWhiteSpace(clave) || clave.Trim().Length < 6)
            ModelState.AddModelError("Clave", "La contraseña debe tener al menos 6 caracteres.");

        if (!ModelState.IsValid)
            return Formulario(usuario);

        usuario.PasswordHash = PasswordHasher.Hash(clave!.Trim());
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

        NormalizarUsuario(usuario);
        ModelState.Remove("PasswordHash");

        var existente = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (existente is null) return NotFound();

        if (string.IsNullOrWhiteSpace(usuario.NombreUsuario))
            ModelState.AddModelError(nameof(Usuario.NombreUsuario), "El nombre de usuario es obligatorio.");
        else if (await _db.Usuarios.AnyAsync(u =>
                     u.NombreUsuario.ToLower() == usuario.NombreUsuario.ToLower() && u.Id != id))
            ModelState.AddModelError(nameof(Usuario.NombreUsuario), "Ese usuario ya existe.");

        if (!Roles.Contains(usuario.Rol))
            ModelState.AddModelError(nameof(Usuario.Rol), "Seleccione un rol válido.");

        if (!string.IsNullOrWhiteSpace(clave) && clave.Trim().Length < 6)
            ModelState.AddModelError("Clave", "La nueva contraseña debe tener al menos 6 caracteres.");

        if (!ModelState.IsValid)
            return Formulario(usuario);

        usuario.PasswordHash = string.IsNullOrWhiteSpace(clave)
            ? existente.PasswordHash
            : PasswordHasher.Hash(clave.Trim());
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
        await _audit.RegistrarAsync(
            Usuario,
            usuario.Activo ? "Activar usuario" : "Desactivar usuario",
            "Usuario",
            usuario.Id,
            usuario.NombreUsuario);
        TempData["Mensaje"] = usuario.Activo ? "Usuario activado." : "Usuario desactivado.";
        return RedirectToAction(nameof(Index));
    }

    private static void NormalizarUsuario(Usuario usuario)
    {
        usuario.NombreUsuario = usuario.NombreUsuario?.Trim() ?? string.Empty;
        usuario.Rol = usuario.Rol?.Trim() ?? string.Empty;
    }

    private IActionResult Formulario(Usuario usuario)
    {
        ViewBag.Roles = Roles
            .Select(r => new SelectListItem { Value = r, Text = r, Selected = r == usuario.Rol })
            .ToList();
        return View("Form", usuario);
    }
}
