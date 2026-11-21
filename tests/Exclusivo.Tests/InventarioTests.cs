using System.ComponentModel.DataAnnotations;
using Exclusivo.Web.Controllers;
using Exclusivo.Web.Data;
using Exclusivo.Web.Models;
using Exclusivo.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Exclusivo.Tests;

public class InventarioTests
{
    // Comprueba reglas y registros en memoria,
    // no transacciones reales de PostgreSQL.
    private static async Task<AppDbContext> CrearBaseAsync()
    {
        var opciones = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var db = new AppDbContext(opciones);

        db.Existencias.Add(new Existencia
        {
            Almacen = new Almacen
            {
                Nombre = "Almacén de prueba",
                Activo = true
            },
            VarianteProducto = new VarianteProducto
            {
                Producto = new Producto
                {
                    Stock = "INV-PRUEBA",
                    Nombre = "Producto de prueba",
                    Precio = 100,
                    Costo = 50
                }
            },
            Cantidad = 10
        });

        await db.SaveChangesAsync();
        return db;
    }

    private static InventoryController CrearControlador(AppDbContext db)
    {
        var controlador = new InventoryController(
            db,
            new InventoryService(db),
            new AuditService(db))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        controlador.TempData = new TempDataDictionary(
            controlador.HttpContext,
            new TempDataPrueba());

        return controlador;
    }

    private static MovimientoViewModel Movimiento(
        Existencia existencia, string tipo, int cantidad) => new()
    {
        AlmacenId = existencia.AlmacenId,
        VarianteProductoId = existencia.VarianteProductoId,
        Tipo = tipo,
        Cantidad = cantidad,
        Motivo = "Comprobación de inventario"
    };

    [Theory]
    [InlineData("Ajuste", -2, 8, -2)]
    [InlineData("Ajuste", 2, 12, 2)]
    [InlineData("Entrada", 2, 12, 2)]
    [InlineData("Salida", 2, 8, 2)]
    public async Task LosMovimientosActualizanElStockYConservanLaCantidadRegistrada(
        string tipo, int cantidad, int stockEsperado, int cantidadEsperada)
    {
        using var db = await CrearBaseAsync();
        var controlador = CrearControlador(db);

        var modelo = Movimiento(
            await db.Existencias.SingleAsync(), tipo, cantidad);

        Assert.True(Validator.TryValidateObject(
            modelo,
            new ValidationContext(modelo),
            new List<ValidationResult>(),
            true));

        Assert.IsType<RedirectToActionResult>(
            await controlador.Create(modelo));

        Assert.Equal(
            stockEsperado,
            (await db.Existencias.AsNoTracking().SingleAsync()).Cantidad);

        Assert.Equal(
            cantidadEsperada,
            (await db.MovimientosInventario
                .AsNoTracking().SingleAsync()).Cantidad);
    }

    [Theory]
    [InlineData("Entrada", -2)]
    [InlineData("Salida", -2)]
    [InlineData("Entrada", 0)]
    [InlineData("Salida", 0)]
    [InlineData("Ajuste", 0)]
    public async Task UnaCantidadInvalidaNoModificaElStockNiCreaMovimientos(
        string tipo, int cantidad)
    {
        using var db = await CrearBaseAsync();
        var controlador = CrearControlador(db);

        var modelo = Movimiento(
            await db.Existencias.SingleAsync(), tipo, cantidad);

        Assert.IsType<ViewResult>(await controlador.Create(modelo));
        Assert.False(controlador.ModelState.IsValid);

        Assert.Equal(
            10,
            (await db.Existencias.AsNoTracking().SingleAsync()).Cantidad);

        Assert.Empty(
            await db.MovimientosInventario.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task UnAjusteNoPuedeDejarExistenciasNegativas()
    {
        using var db = await CrearBaseAsync();
        var controlador = CrearControlador(db);

        var modelo = Movimiento(
            await db.Existencias.SingleAsync(), "Ajuste", -11);

        Assert.IsType<ViewResult>(await controlador.Create(modelo));
        Assert.False(controlador.ModelState.IsValid);

        Assert.Equal(
            10,
            (await db.Existencias.AsNoTracking().SingleAsync()).Cantidad);

        Assert.Empty(
            await db.MovimientosInventario.AsNoTracking().ToListAsync());
    }

    private sealed class TempDataPrueba : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(
            HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object> values)
        {
        }
    }
}
