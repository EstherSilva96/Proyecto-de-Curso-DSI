using ClosedXML.Excel;
using Exclusivo.Web.Data;
using Exclusivo.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Exclusivo.Web.Controllers;

[Authorize(Roles = "Administrador,Gerente")]
public class ReportsController : Controller
{
    private readonly AppDbContext _db;
    private static readonly TimeZoneInfo Zona =
        TimeZoneInfo.FindSystemTimeZoneById("America/Managua");

    public ReportsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta)
    {
        var (inicio, fin) = ObtenerFechas(desde, hasta);

        ViewBag.Desde = inicio.ToString("yyyy-MM-dd");
        ViewBag.Hasta = fin.ToString("yyyy-MM-dd");

        if (fin < inicio)
        {
            ModelState.AddModelError(
                string.Empty,
                "La fecha Hasta debe ser igual o posterior a Desde.");
        }

        var ventas = ModelState.IsValid
            ? await ConsultarVentasAsync(inicio, fin)
            : new List<Venta>();

        ViewBag.Total = ventas
            .Where(v => v.Estado == "Confirmada")
            .Sum(v => v.Total);

        ViewBag.Cantidad = ventas
            .Count(v => v.Estado == "Confirmada");

        return View(ventas);
    }

    [HttpGet]
    public async Task<IActionResult> ExportarExcel(
        DateTime? desde,
        DateTime? hasta)
    {
        var (inicio, fin) = ObtenerFechas(desde, hasta);

        if (!ModelState.IsValid || fin < inicio)
        {
            return BadRequest(
                "Revise las fechas: Hasta debe ser igual o posterior a Desde.");
        }

        var ventas = await ConsultarVentasAsync(inicio, fin);

        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Ventas");

        hoja.Range("A1:F1").Merge().Value =
            "EXCLUSIVO ERP - REPORTE DE VENTAS";

        hoja.Cell("A1").Style.Font.FontSize = 18;
        hoja.Cell("A1").Style.Font.Bold = true;
        hoja.Row(1).Height = 30;

        hoja.Range("A2:F2").Merge().Value =
            $"Período: {inicio:dd/MM/yyyy} al {fin:dd/MM/yyyy} | Hora de Nicaragua";

        hoja.Range("A3:F3").Merge().Value =
            "Las ventas anuladas se muestran, pero no se suman al total confirmado.";

        string[] encabezados =
        [
            "Factura",
            "Fecha",
            "Cliente",
            "Vendedor",
            "Total (C$)",
            "Estado"
        ];

        for (var i = 0; i < encabezados.Length; i++)
        {
            hoja.Cell(5, i + 1).Value = encabezados[i];
        }

        hoja.Range("A5:F5").Style.Font.Bold = true;
        hoja.Range("A5:F5").Style.Fill.BackgroundColor =
            XLColor.FromHtml("#212529");
        hoja.Range("A5:F5").Style.Font.FontColor = XLColor.White;

        var fila = 6;

        foreach (var venta in ventas)
        {
            hoja.Cell(fila, 1).Value = venta.NumeroFactura;

            hoja.Cell(fila, 2).Value =
                TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(venta.Fecha, DateTimeKind.Utc),
                    Zona);

            hoja.Cell(fila, 2).Style.DateFormat.Format =
                "dd/mm/yyyy hh:mm";

            hoja.Cell(fila, 3).Value =
                venta.Cliente?.Nombre ?? "Cliente general";

            hoja.Cell(fila, 4).Value =
                $"{venta.Usuario.Nombres} {venta.Usuario.Apellidos}".Trim();
            hoja.Cell(fila, 5).Value = venta.Total;

            hoja.Cell(fila, 5).Style.NumberFormat.Format =
                "\"C$\" #,##0.00";

            hoja.Cell(fila, 6).Value = venta.Estado;

            fila++;
        }

        if (ventas.Count > 0)
        {
            hoja.Range(5, 1, fila - 1, 6)
                .CreateTable("Ventas")
                .Theme = XLTableTheme.TableStyleMedium2;
        }
        else
        {
            hoja.Range("A6:F6").Merge().Value =
                "No hay ventas en el período seleccionado.";

            fila++;
        }

        hoja.Cell(fila + 1, 4).Value = "Total confirmado";

        hoja.Cell(fila + 1, 5).Value = ventas
            .Where(v => v.Estado == "Confirmada")
            .Sum(v => v.Total);

        hoja.Cell(fila + 1, 5).Style.NumberFormat.Format =
            "\"C$\" #,##0.00";

        hoja.Range(fila + 1, 4, fila + 1, 5)
            .Style.Font.Bold = true;

        hoja.Cell(fila + 2, 4).Value = "Ventas confirmadas";

        hoja.Cell(fila + 2, 5).Value =
            ventas.Count(v => v.Estado == "Confirmada");

        hoja.Column(1).Width = 26;
        hoja.Column(2).Width = 23;
        hoja.Column(3).Width = 42;
        hoja.Column(4).Width = 26;
        hoja.Column(5).Width = 20;
        hoja.Column(6).Width = 18;

        hoja.SheetView.FreezeRows(5);

        using var archivo = new MemoryStream();
        libro.SaveAs(archivo);

        return File(
            archivo.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"reporte-ventas-{inicio:yyyyMMdd}-{fin:yyyyMMdd}.xlsx");
    }

    private static (DateTime Inicio, DateTime Fin) ObtenerFechas(
        DateTime? desde,
        DateTime? hasta)
    {
        var hoy = TimeZoneInfo
            .ConvertTimeFromUtc(DateTime.UtcNow, Zona)
            .Date;

        return (
            (desde ?? hoy.AddDays(-30)).Date,
            (hasta ?? hoy).Date);
    }

    private async Task<List<Venta>> ConsultarVentasAsync(
        DateTime inicio,
        DateTime fin)
    {
        var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(inicio, DateTimeKind.Unspecified),
            Zona);

        var finUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(
                fin.AddDays(1),
                DateTimeKind.Unspecified),
            Zona);

        return await _db.Ventas
            .AsNoTracking()
            .Include(v => v.Usuario)
            .Include(v => v.Cliente)
            .Where(v => v.Fecha >= inicioUtc && v.Fecha < finUtc)
            .OrderByDescending(v => v.Fecha)
            .ThenByDescending(v => v.Id)
            .ToListAsync();
    }
}
