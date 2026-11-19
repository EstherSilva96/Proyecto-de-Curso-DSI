using System.Security.Claims;
using Exclusivo.Web.Data;
using Exclusivo.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Exclusivo.Web.Services;

public class AuthService
{
    private readonly AppDbContext _db;

    public AuthService(AppDbContext db) => _db = db;

    public async Task<Usuario?> ValidarAsync(string nombreUsuario, string clave)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario && u.Activo);
        if (usuario is null || !PasswordHasher.Verify(clave, usuario.PasswordHash)) return null;

        usuario.UltimoAcceso = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return usuario;
    }

    public async Task SignInAsync(HttpContext context, Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.NombreUsuario),
            new("Nombres", $"{usuario.Nombres} {usuario.Apellidos}".Trim()),
            new(ClaimTypes.Role, usuario.Rol)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false });
    }

    public static string? ObtenerUsuario(HttpContext context) =>
        context.User.FindFirstValue(ClaimTypes.Name);

    public static string? ObtenerRol(HttpContext context) =>
        context.User.FindFirstValue(ClaimTypes.Role);
}
