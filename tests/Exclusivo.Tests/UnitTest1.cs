using ClosedXML.Excel;
using Exclusivo.Web.Controllers;
using Exclusivo.Web.Data;
using Exclusivo.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Exclusivo.Tests;

public class ReportesTests
{
    private static readonly DateTime Dia = new(2026, 11, 20);

    private static AppDbContext CrearBase()
    {
        var opciones = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(opciones);
    }

    private static Venta CrearVenta(
        string factura,
        decimal total,
        string estado,
        DateTime fecha)
    {
        return new Venta
        {
            NumeroFactura = factura,
            Total = total,
            Estado = estado,
            Fecha = fecha,
            Cliente = new Cliente
            {
                Nombre = "Cliente de prueba"
            },
            Usuario = new Usuario
            {
                NombreUsuario = "vendedor.prueba",
                Nombres = "Ana",
                Apellidos = "Prueba",
                PasswordHash = "dato-solo-para-pruebas",
                Rol = "Vendedor"
            }
        };
    }

    [Fact]
    public async Task LosTotalesExcluyenLasVentasAnuladas()
    {
        using var db = CrearBase();

        db.Ventas.AddRange(
            CrearVenta("PRUEBA-1", 125.50m, "Confirmada",
                new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc)),
            CrearVenta("PRUEBA-2", 900m, "Anulada",
                new DateTime(2026, 11, 20, 13, 0, 0, DateTimeKind.Utc)));

        await db.SaveChangesAsync();

        var controlador = new ReportsController(db);

        var pantalla = Assert.IsType<ViewResult>(
            await controlador.Index(Dia, Dia));

        Assert.Equal(
            125.50m,
            Assert.IsType<decimal>(pantalla.ViewData["Total"]));

        Assert.Equal(
            1,
            Assert.IsType<int>(pantalla.ViewData["Cantidad"]));

        Assert.Equal(
            2,
            Assert.IsType<List<Venta>>(pantalla.Model).Count);

        var archivo = Assert.IsType<FileContentResult>(
            await controlador.ExportarExcel(Dia, Dia));

        Assert.Equal(
            "reporte-ventas-20261120-20261120.xlsx",
            archivo.FileDownloadName);

        using var libro = new XLWorkbook(
            new MemoryStream(archivo.FileContents));

        var hoja = libro.Worksheet("Ventas");

        Assert.Equal("Cliente de prueba", hoja.Cell("C6").GetString());
        Assert.Equal("Ana Prueba", hoja.Cell("D6").GetString());
        Assert.Equal("Anulada", hoja.Cell("F6").GetString());
        Assert.Equal(XLDataType.Number, hoja.Cell("E6").DataType);
        Assert.Equal(125.50m, hoja.Cell("E9").GetValue<decimal>());
        Assert.Equal(1, hoja.Cell("E10").GetValue<int>());
        Assert.Single(hoja.Tables);
    }

    [Fact]
    public async Task ElPeriodoUsaElDiaCompletoDeNicaragua()
    {
        using var db = CrearBase();

        db.Ventas.AddRange(
            CrearVenta("ANTES", 10m, "Confirmada",
                new DateTime(2026, 11, 20, 5, 59, 0, DateTimeKind.Utc)),
            CrearVenta("INICIO", 20m, "Confirmada",
                new DateTime(2026, 11, 20, 6, 0, 0, DateTimeKind.Utc)),
            CrearVenta("FINAL", 30m, "Confirmada",
                new DateTime(2026, 11, 21, 5, 59, 0, DateTimeKind.Utc)),
            CrearVenta("DESPUES", 40m, "Confirmada",
                new DateTime(2026, 11, 21, 6, 0, 0, DateTimeKind.Utc)));

        await db.SaveChangesAsync();

        var pantalla = Assert.IsType<ViewResult>(
            await new ReportsController(db).Index(Dia, Dia));

        var ventas = Assert.IsType<List<Venta>>(pantalla.Model);

        Assert.Equal(
            new[] { "FINAL", "INICIO" },
            ventas.Select(v => v.NumeroFactura));

        Assert.Equal(
            50m,
            Assert.IsType<decimal>(pantalla.ViewData["Total"]));
    }

    [Fact]
    public async Task UnPeriodoSinVentasGeneraUnExcelValido()
    {
        using var db = CrearBase();

        var archivo = Assert.IsType<FileContentResult>(
            await new ReportsController(db).ExportarExcel(Dia, Dia));

        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            archivo.ContentType);

        using var libro = new XLWorkbook(
            new MemoryStream(archivo.FileContents));

        var hoja = libro.Worksheet("Ventas");

        Assert.Contains("No hay ventas", hoja.Cell("A6").GetString());
        Assert.Equal(0m, hoja.Cell("E8").GetValue<decimal>());
        Assert.Empty(hoja.Tables);
    }

    [Fact]
    public async Task LasFechasInvertidasSeRechazan()
    {
        using var db = CrearBase();
        var controlador = new ReportsController(db);

        Assert.IsType<BadRequestObjectResult>(
            await controlador.ExportarExcel(Dia.AddDays(1), Dia));

        var pantalla = Assert.IsType<ViewResult>(
            await controlador.Index(Dia.AddDays(1), Dia));

        Assert.False(controlador.ModelState.IsValid);
        Assert.Empty(Assert.IsType<List<Venta>>(pantalla.Model));
    }
}
