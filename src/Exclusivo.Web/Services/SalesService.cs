using Exclusivo.Web.Data;
using Exclusivo.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Exclusivo.Web.Services;

public class SalesService
{
    private readonly AppDbContext _db;

    public SalesService(AppDbContext db) => _db = db;

    public async Task<(bool Ok, string Mensaje, int VentaId)> CrearVentaAsync(VentaCreateViewModel model, string usuario)
    {
        if (model.Items.Count == 0) return (false, "El carrito está vacío.", 0);
        if (model.Descuento < 0) return (false, "El descuento no puede ser negativo.", 0);

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var config = await _db.Configuraciones.FirstOrDefaultAsync() ?? new ConfiguracionEmpresa();
        var almacenId = model.AlmacenId != 0
            ? model.AlmacenId
            : await _db.Almacenes.Where(a => a.Activo).Select(a => a.Id).FirstOrDefaultAsync();
        if (almacenId == 0) return (false, "No hay almacenes configurados.", 0);

        var usuarioDb = await _db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == usuario);
        if (usuarioDb is null) return (false, "Usuario no válido.", 0);

        var subtotal = 0m;
        var costoTotal = 0m;
        var existencias = new Dictionary<int, Existencia>();

        foreach (var item in model.Items)
        {
            if (item.Cantidad <= 0) return (false, "Las cantidades deben ser mayores que cero.", 0);
            var variante = await _db.Variantes.Include(v => v.Producto)
                .FirstOrDefaultAsync(v => v.Id == item.VarianteProductoId);
            if (variante is null) return (false, "Una de las variantes no existe.", 0);

            var existencia = await _db.Existencias
                .FirstOrDefaultAsync(e => e.AlmacenId == almacenId && e.VarianteProductoId == item.VarianteProductoId);
            if (existencia is null || existencia.Cantidad < item.Cantidad)
                return (false, "No hay existencias suficientes para una de las variantes.", 0);

            item.PrecioUnitario = variante.Producto.Precio;
            item.CostoUnitario = variante.Producto.Costo;
            existencias[item.VarianteProductoId] = existencia;
            subtotal += item.Cantidad * item.PrecioUnitario;
            costoTotal += item.Cantidad * item.CostoUnitario;
        }

        if (model.Descuento > subtotal) return (false, "El descuento supera el subtotal.", 0);

        var baseImponible = subtotal - model.Descuento;
        var impuesto = Math.Round(baseImponible * config.ImpuestoPorcentaje / 100m, 2);
        var total = baseImponible + impuesto;

        var numero = $"{config.PrefijoFactura}-{DateTime.UtcNow.Year}-{(await _db.Ventas.CountAsync() + 1):D6}";
        var venta = new Venta
        {
            NumeroFactura = numero,
            ClienteId = model.ClienteId,
            UsuarioId = usuarioDb.Id,
            Subtotal = subtotal,
            Descuento = model.Descuento,
            Impuesto = impuesto,
            Total = total,
            MetodoPago = model.MetodoPago
        };

        foreach (var item in model.Items)
        {
            venta.Detalles.Add(new DetalleVenta
            {
                VarianteProductoId = item.VarianteProductoId,
                Cantidad = item.Cantidad,
                PrecioUnitario = item.PrecioUnitario,
                CostoUnitario = item.CostoUnitario,
                Subtotal = item.Cantidad * item.PrecioUnitario
            });

            existencias[item.VarianteProductoId].Cantidad -= item.Cantidad;
            _db.MovimientosInventario.Add(new MovimientoInventario
            {
                Tipo = "Salida",
                AlmacenId = almacenId,
                VarianteProductoId = item.VarianteProductoId,
                Cantidad = item.Cantidad,
                Motivo = $"Venta {numero}",
                DocumentoReferencia = numero,
                UsuarioId = usuarioDb.Id
            });
        }

        venta.Pagos.Add(new Pago
        {
            Monto = total,
            Metodo = model.MetodoPago,
            Fecha = DateTime.UtcNow
        });

        _db.Ventas.Add(venta);
        _db.Auditorias.Add(new Auditoria
        {
            Usuario = usuario,
            Accion = "Crear venta",
            Entidad = "Venta",
            Detalles = $"{numero} · Total {total}"
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return (true, "Venta registrada correctamente.", venta.Id);
    }
}
