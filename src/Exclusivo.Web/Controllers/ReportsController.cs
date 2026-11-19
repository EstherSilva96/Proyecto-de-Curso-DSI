using System.Globalization;
using System.Text;
using Exclusivo.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Exclusivo.Web.Controllers;

[Authorize(Roles = "Administrador,Gerente")]
public class ReportsController : Controller
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta)
    {
        var inicio = DateTime.SpecifyKind((desde ?? DateTime.UtcNow.AddDays(-30)).Date, DateTimeKind.Utc);
        var fin = DateTime.SpecifyKind((hasta ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        var ventas = await _db.Ventas
            .Include(v => v.Usuario).Include(v => v.Cliente)
            .Where(v => v.Fecha >= inicio && v.Fecha <= fin)
            .OrderByDescending(v => v.Fecha).ToListAsync();

        ViewBag.Desde = inicio.ToString("yyyy-MM-dd");
        ViewBag.Hasta = fin.ToString("yyyy-MM-dd");
        ViewBag.Total = ventas.Sum(v => v.Total);
        ViewBag.Cantidad = ventas.Count;
        return View(ventas);
    }

    [HttpGet]
    public async Task<IActionResult> ExportarCsv(DateTime? desde, DateTime? hasta)
    {
        var inicio = DateTime.SpecifyKind((desde ?? DateTime.UtcNow.AddDays(-30)).Date, DateTimeKind.Utc);
        var fin = DateTime.SpecifyKind((hasta ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
        var ventas = await _db.Ventas.Where(v => v.Fecha >= inicio && v.Fecha <= fin).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Factura;Fecha;Cliente;Total;Estado");
        foreach (var v in ventas)
            sb.AppendLine($"{v.NumeroFactura};{v.Fecha:yyyy-MM-dd HH:mm};{v.ClienteId};{v.Total.ToString(CultureInfo.InvariantCulture)};{v.Estado}");

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "reporte-ventas.csv");
    }
}
