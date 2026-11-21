using Exclusivo.Web.Data;
using Exclusivo.Web.Models;
using Exclusivo.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Exclusivo.Web.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private string Usuario => AuthService.ObtenerUsuario(HttpContext) ?? "sistema";

    public CustomersController(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    /// <summary>Datos del cliente para precargar el modal cuando se selecciona uno guardado.</summary>
    [HttpGet]
    public async Task<IActionResult> Obtener(int id)
    {
        var cliente = await _db.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();

        return Json(new
        {
            id = cliente.Id,
            nombres = cliente.Nombres ?? cliente.Nombre,
            apellidos = cliente.Apellidos ?? string.Empty,
            cedula = cliente.Cedula ?? cliente.Documento,
            telefono = cliente.Telefono,
            direccion = cliente.Direccion
        });
    }

    /// <summary>Crea o actualiza un cliente y devuelve los datos para el selector del POS.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(ClienteFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { ok = false, errores });
        }

        var cedula = model.Cedula.Trim();
        var nombre = $"{model.Nombres.Trim()} {model.Apellidos.Trim()}".Trim();

        Cliente? cliente;
        if (model.Id > 0)
        {
            cliente = await _db.Clientes.FindAsync(model.Id);
            if (cliente is null) return Json(new { ok = false, errores = new[] { "El cliente no existe." } });
        }
        else
        {
            cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Cedula == cedula);
            if (cliente is not null)
                return Json(new { ok = true, id = cliente.Id, nombre = cliente.Nombre, mensaje = "El cliente ya estaba registrado." });

            cliente = new Cliente();
            _db.Clientes.Add(cliente);
        }

        cliente.Nombres = model.Nombres.Trim();
        cliente.Apellidos = model.Apellidos.Trim();
        cliente.Cedula = cedula;
        cliente.Documento = cedula;
        cliente.Nombre = nombre;
        cliente.Telefono = model.Telefono;
        cliente.Direccion = model.Direccion;
        cliente.Activo = true;

        await _db.SaveChangesAsync();
        await _audit.RegistrarAsync(Usuario, model.Id > 0 ? "Editar cliente" : "Crear cliente", "Cliente", cliente.Id, $"{nombre} · {cedula}");

        return Json(new { ok = true, id = cliente.Id, nombre });
    }
}
