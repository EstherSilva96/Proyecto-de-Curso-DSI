using System.Security.Claims;
using Exclusivo.Web.Controllers;
using Exclusivo.Web.Data;
using Exclusivo.Web.Models;
using Exclusivo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace Exclusivo.Tests;

public class RolesTests
{
    private static AppDbContext CrearBase() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public void ElRolCajeroNoSeOfreceAlCrearUsuarios()
    {
        using var db = CrearBase();
        var controlador = new UsersController(db, new AuditService(db));

        var vista = Assert.IsType<ViewResult>(controlador.Create());
        var roles = Assert.IsType<List<SelectListItem>>(vista.ViewData["Roles"]);

        Assert.DoesNotContain(roles, r => r.Value == "Cajero");
        Assert.Contains(roles, r => r.Value == "Vendedor");
    }

    [Fact]
    public async Task UnPostConRolCajeroEsRechazado()
    {
        using var db = CrearBase();
        var controlador = new UsersController(db, new AuditService(db));

        var resultado = await controlador.Create(
            new Usuario
            {
                NombreUsuario = "intentocajero",
                Nombres = "Ana",
                Apellidos = "Ruiz",
                Rol = "Cajero"
            },
            "Cajero123*");

        Assert.IsType<ViewResult>(resultado);
        Assert.False(controlador.ModelState.IsValid);
        Assert.DoesNotContain(
            await db.Usuarios.ToListAsync(),
            u => u.NombreUsuario == "intentocajero");
    }

    [Fact]
    public async Task ElAdministradorNoPuedeDesactivarSuPropioUsuario()
    {
        using var db = CrearBase();
        var admin = new Usuario
        {
            NombreUsuario = "admin",
            Nombres = "Ana",
            Apellidos = "Silva",
            PasswordHash = "hash",
            Rol = "Administrador",
            Activo = true
        };
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();

        var controlador = CrearControlador(db, "admin");
        var resultado = await controlador.Toggle(admin.Id);

        Assert.IsType<RedirectToActionResult>(resultado);
        Assert.True((await db.Usuarios.FindAsync(admin.Id))!.Activo);
        Assert.Contains("ingeniero", controlador.TempData["Error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ElAdministradorNoPuedeCambiarSuPropioRol()
    {
        using var db = CrearBase();
        var admin = new Usuario
        {
            NombreUsuario = "admin",
            Nombres = "Ana",
            Apellidos = "Silva",
            PasswordHash = "hash",
            Rol = "Administrador",
            Activo = true
        };
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();

        var controlador = CrearControlador(db, "admin");
        var resultado = await controlador.Edit(admin.Id, new Usuario
        {
            Id = admin.Id,
            NombreUsuario = "admin",
            Nombres = "Ana",
            Apellidos = "Silva",
            PasswordHash = "hash",
            Rol = "Vendedor",
            Activo = true
        }, null);

        Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Administrador", (await db.Usuarios.FindAsync(admin.Id))!.Rol);
    }

    private static UsersController CrearControlador(AppDbContext db, string usuarioActual)
    {
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, usuarioActual),
            new Claim(ClaimTypes.Role, "Administrador")
        }, "Prueba"));

        var controlador = new UsersController(db, new AuditService(db))
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
        controlador.TempData = new TempDataDictionary(http, new ProveedorTempData());
        return controlador;
    }

    private sealed class ProveedorTempData : ITempDataProvider
    {
        private readonly Dictionary<string, object> _datos = new();

        public IDictionary<string, object> LoadTempData(HttpContext context) => _datos;

        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
