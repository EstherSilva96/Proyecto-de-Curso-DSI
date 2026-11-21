using Exclusivo.Web.Controllers;
using Exclusivo.Web.Data;
using Exclusivo.Web.Models;
using Exclusivo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
}
