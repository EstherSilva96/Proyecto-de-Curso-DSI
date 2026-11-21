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

    /// <summary>Listado administrativo de clientes con búsqueda por nombre, cédula o teléfono.</summary>
    [Authorize(Roles = "Administrador,Gerente,Vendedor")]
    public async Task<IActionResult> Index(string? buscar)
    {
        var termino = buscar?.Trim();
        IQueryable<Cliente> consulta = _db.Clientes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(termino))
        {
            consulta = consulta.Where(c =>
                EF.Functions.ILike(c.Nombre, $"%{termino}%") ||
                EF.Functions.ILike(c.Nombres ?? string.Empty, $"%{termino}%") ||
                EF.Functions.ILike(c.Apellidos ?? string.Empty, $"%{termino}%") ||
                EF.Functions.ILike(c.Cedula ?? string.Empty, $"%{termino}%") ||
                EF.Functions.ILike(c.Documento ?? string.Empty, $"%{termino}%") ||
                EF.Functions.ILike(c.Telefono ?? string.Empty, $"%{termino}%"));
        }

        ViewBag.Buscar = termino;
        return View(await consulta.OrderBy(c => c.Nombre).ToListAsync());
    }

    [Authorize(Roles = "Administrador,Gerente,Vendedor")]
    public IActionResult Create() => Formulario(new ClienteFormViewModel());

    [Authorize(Roles = "Administrador,Gerente,Vendedor")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClienteFormViewModel modelo)
    {
        Normalizar(modelo);
        await ValidarAsync(modelo);

        if (!ModelState.IsValid)
            return Formulario(modelo);

        var cliente = new Cliente { FechaCreacion = DateTime.UtcNow };
        Aplicar(cliente, modelo);
        _db.Clientes.Add(cliente);
        await _db.SaveChangesAsync();

        await _audit.RegistrarAsync(Usuario, "Crear cliente", "Cliente", cliente.Id, cliente.Nombre);
        TempData["Mensaje"] = "Cliente registrado.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador,Gerente,Vendedor")]
    public async Task<IActionResult> Edit(int id)
    {
        var cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (cliente is null) return NotFound();

        return Formulario(DesdeCliente(cliente));
    }

    [Authorize(Roles = "Administrador,Gerente,Vendedor")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ClienteFormViewModel modelo)
    {
        if (id != modelo.Id) return BadRequest();

        Normalizar(modelo);
        await ValidarAsync(modelo, idActual: id);

        if (!ModelState.IsValid)
            return Formulario(modelo);

        var cliente = await _db.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();

        Aplicar(cliente, modelo);
        await _db.SaveChangesAsync();

        await _audit.RegistrarAsync(Usuario, "Editar cliente", "Cliente", cliente.Id, cliente.Nombre);
        TempData["Mensaje"] = "Cliente actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador,Gerente,Vendedor")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var cliente = await _db.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();

        cliente.Activo = !cliente.Activo;
        await _db.SaveChangesAsync();

        await _audit.RegistrarAsync(Usuario, cliente.Activo ? "Activar cliente" : "Desactivar cliente",
            "Cliente", cliente.Id, cliente.Nombre);
        TempData["Mensaje"] = cliente.Activo ? "Cliente activado." : "Cliente desactivado.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Administrador,Gerente,Vendedor")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var cliente = await _db.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();

        if (await _db.Ventas.AnyAsync(v => v.ClienteId == id))
        {
            TempData["Error"] = "No se puede eliminar el cliente porque tiene ventas registradas. Puede desactivarlo.";
            return RedirectToAction(nameof(Index));
        }

        _db.Clientes.Remove(cliente);
        await _db.SaveChangesAsync();

        await _audit.RegistrarAsync(Usuario, "Eliminar cliente", "Cliente", cliente.Id, cliente.Nombre);
        TempData["Mensaje"] = "Cliente eliminado.";
        return RedirectToAction(nameof(Index));
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

    private IActionResult Formulario(ClienteFormViewModel modelo) => View("Form", modelo);

    private static ClienteFormViewModel DesdeCliente(Cliente cliente)
    {
        var nombres = cliente.Nombres;
        var apellidos = cliente.Apellidos;

        if (string.IsNullOrWhiteSpace(nombres) || string.IsNullOrWhiteSpace(apellidos))
        {
            var partes = (cliente.Nombre ?? string.Empty).Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            nombres ??= partes.Length > 0 ? partes[0] : cliente.Nombre;
            apellidos ??= partes.Length > 1 ? partes[1] : cliente.Nombre;
        }

        return new ClienteFormViewModel
        {
            Id = cliente.Id,
            Nombres = nombres ?? string.Empty,
            Apellidos = apellidos ?? string.Empty,
            Cedula = cliente.Cedula ?? cliente.Documento,
            Telefono = cliente.Telefono,
            Direccion = cliente.Direccion,
            Correo = cliente.Correo,
            Activo = cliente.Activo
        };
    }

    private static void Normalizar(ClienteFormViewModel modelo)
    {
        modelo.Nombres = modelo.Nombres?.Trim() ?? string.Empty;
        modelo.Apellidos = modelo.Apellidos?.Trim() ?? string.Empty;
        modelo.Cedula = modelo.Cedula?.Trim() ?? string.Empty;
        modelo.Telefono = string.IsNullOrWhiteSpace(modelo.Telefono) ? null : modelo.Telefono.Trim();
        modelo.Direccion = string.IsNullOrWhiteSpace(modelo.Direccion) ? null : modelo.Direccion.Trim();
        modelo.Correo = string.IsNullOrWhiteSpace(modelo.Correo) ? null : modelo.Correo.Trim();
    }

    private async Task ValidarAsync(ClienteFormViewModel modelo, int? idActual = null)
    {
        if (!string.IsNullOrWhiteSpace(modelo.Cedula) && await _db.Clientes.AnyAsync(c =>
                c.Id != idActual &&
                ((c.Cedula ?? string.Empty) == modelo.Cedula ||
                 (c.Documento ?? string.Empty) == modelo.Cedula)))
        {
            ModelState.AddModelError(nameof(ClienteFormViewModel.Cedula), "Ya existe un cliente con esa cédula.");
        }
    }

    private static void Aplicar(Cliente cliente, ClienteFormViewModel modelo)
    {
        cliente.Nombres = modelo.Nombres;
        cliente.Apellidos = modelo.Apellidos;
        cliente.Nombre = $"{modelo.Nombres} {modelo.Apellidos}".Trim();
        cliente.Cedula = modelo.Cedula;
        cliente.Documento = modelo.Cedula;
        cliente.Telefono = modelo.Telefono;
        cliente.Direccion = modelo.Direccion;
        cliente.Correo = modelo.Correo;
        cliente.Activo = modelo.Activo;
    }
}
