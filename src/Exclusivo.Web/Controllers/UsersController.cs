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
    private static readonly string[] Roles =
        ["Administrador", "Gerente", "Inventario", "Vendedor"];

    private string UsuarioActual =>
        AuthService.ObtenerUsuario(HttpContext) ?? "sys";

    public UsersController(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IActionResult> Index() =>
        View(await _db.Usuarios
            .OrderBy(u => u.NombreUsuario)
            .ToListAsync());

    public IActionResult Create() =>
        Formulario(new Usuario { Activo = true });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Usuario usuario, string? clave)
    {
        NormalizarUsuario(usuario);
        ModelState.Remove("PasswordHash");

        var nombreUsuarioLower = usuario.NombreUsuario.ToLower();

        if (string.IsNullOrWhiteSpace(usuario.NombreUsuario))
            ModelState.AddModelError(
                nameof(Usuario.NombreUsuario),
                "El nombre de usuario es obligatorio.");
        else if (await _db.Usuarios.AnyAsync(u =>
                     (u.NombreUsuario ?? string.Empty).ToLower()
                     == nombreUsuarioLower))
            ModelState.AddModelError(
                nameof(Usuario.NombreUsuario),
                "Ese usuario ya existe.");

        if (string.IsNullOrWhiteSpace(usuario.Rol)
            || !Roles.Contains(usuario.Rol))
            ModelState.AddModelError(
                nameof(Usuario.Rol),
                "Seleccione un rol válido.");

        if (string.IsNullOrWhiteSpace(clave) || clave.Length < 6)
            ModelState.AddModelError(
                "Clave",
                "La contraseña debe tener al menos 6 caracteres.");

        if (!ModelState.IsValid)
            return Formulario(usuario);

        usuario.PasswordHash = PasswordHasher.Hash(clave!);
        usuario.FechaCreacion = DateTime.UtcNow;

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        await _audit.RegistrarAsync(
            UsuarioActual,
            "Crear usuario",
            "Usuario",
            usuario.Id,
            usuario.NombreUsuario);

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
    public async Task<IActionResult> Edit(
        int id, Usuario usuario, string? clave)
    {
        if (id != usuario.Id)
            return BadRequest();

        NormalizarUsuario(usuario);
        ModelState.Remove("PasswordHash");

        var existente = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        if (existente is null)
            return NotFound();

        var esPropio = EsUsuarioActual(existente);
        if (esPropio)
        {
            usuario.Rol = existente.Rol;
            usuario.Activo = existente.Activo;
            ModelState.Remove(nameof(Usuario.Rol));
        }

        var nombreUsuarioLower = usuario.NombreUsuario.ToLower();

        if (string.IsNullOrWhiteSpace(usuario.NombreUsuario))
            ModelState.AddModelError(
                nameof(Usuario.NombreUsuario),
                "El nombre de usuario es obligatorio.");
        else if (await _db.Usuarios.AnyAsync(u =>
                     (u.NombreUsuario ?? string.Empty).ToLower()
                     == nombreUsuarioLower && u.Id != id))
            ModelState.AddModelError(
                nameof(Usuario.NombreUsuario),
                "Ese usuario ya existe.");

        if (string.IsNullOrWhiteSpace(usuario.Rol)
            || !Roles.Contains(usuario.Rol))
            ModelState.AddModelError(
                nameof(Usuario.Rol),
                "Seleccione un rol válido.");

        if (!string.IsNullOrWhiteSpace(clave) && clave.Length < 6)
            ModelState.AddModelError(
                "Clave",
                "La contraseña debe tener al menos 6 caracteres.");

        if (!ModelState.IsValid)
            return Formulario(usuario);

        existente.NombreUsuario = usuario.NombreUsuario;
        existente.Nombres = usuario.Nombres;
        existente.Apellidos = usuario.Apellidos;
        existente.Rol = usuario.Rol;
        existente.Activo = usuario.Activo;
        existente.PasswordHash = string.IsNullOrWhiteSpace(clave)
            ? existente.PasswordHash
            : PasswordHasher.Hash(clave);

        await _db.SaveChangesAsync();

        await _audit.RegistrarAsync(
            UsuarioActual,
            "Editar usuario",
            "Usuario",
            id,
            usuario.NombreUsuario);

        TempData["Mensaje"] = esPropio
            ? "Usuario actualizado. Su rol solo puede ser modificado por el ingeniero de sistemas."
            : "Usuario actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null)
            return NotFound();

        if (EsUsuarioActual(usuario))
        {
            TempData["Error"] = "No puede desactivar su propio usuario. Consulte con el ingeniero de sistemas para modificar este rol.";
            return RedirectToAction(nameof(Index));
        }

        usuario.Activo = !usuario.Activo;
        await _db.SaveChangesAsync();

        await _audit.RegistrarAsync(
            UsuarioActual,
            usuario.Activo ? "Activar usuario" : "Desactivar usuario",
            "Usuario",
            usuario.Id,
            usuario.NombreUsuario);

        TempData["Mensaje"] = usuario.Activo
            ? "Usuario activado."
            : "Usuario desactivado.";

        return RedirectToAction(nameof(Index));
    }

    private bool EsUsuarioActual(Usuario usuario) =>
        string.Equals(usuario.NombreUsuario, UsuarioActual, StringComparison.OrdinalIgnoreCase);

    private static void NormalizarUsuario(Usuario usuario)
    {
        usuario.NombreUsuario =
            (usuario.NombreUsuario ?? string.Empty).Trim();

        usuario.Rol = (usuario.Rol ?? string.Empty).Trim();
    }

    private IActionResult Formulario(Usuario usuario)
    {
        ViewBag.Roles = Roles
            .Select(r => new SelectListItem
            {
                Value = r,
                Text = r,
                Selected = r == usuario.Rol
            })
            .ToList();

        ViewBag.EsPropio = usuario.Id > 0 && EsUsuarioActual(usuario);
        return View("Form", usuario);
    }
}
