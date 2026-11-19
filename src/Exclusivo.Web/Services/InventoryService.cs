using Exclusivo.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Exclusivo.Web.Services;

public class InventoryService
{
    private readonly AppDbContext _db;

    public InventoryService(AppDbContext db) => _db = db;

    public async Task<(bool Ok, string Mensaje)> RegistrarMovimientoAsync(
        int almacenId, int varianteProductoId, int cantidad, string tipo,
        string? motivo, string? documento, string? usuario)
    {
        if (cantidad == 0) return (false, "La cantidad debe ser diferente de cero.");

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var existencia = await _db.Existencias
            .FirstOrDefaultAsync(e => e.AlmacenId == almacenId && e.VarianteProductoId == varianteProductoId);

        if (existencia is null)
        {
            existencia = new Exclusivo.Web.Models.Existencia
            {
                AlmacenId = almacenId,
                VarianteProductoId = varianteProductoId,
                Cantidad = 0,
                Minimo = 0,
                Maximo = 1000
            };
            _db.Existencias.Add(existencia);
        }

        var delta = tipo switch
        {
            "Entrada" => Math.Abs(cantidad),
            "Salida" => -Math.Abs(cantidad),
            "Ajuste" => cantidad,
            _ => 0
        };

        if (delta == 0) return (false, "Tipo de movimiento no válido.");
        if (existencia.Cantidad + delta < 0)
            return (false, "No hay existencias suficientes para realizar la salida.");

        existencia.Cantidad += delta;

        _db.MovimientosInventario.Add(new Models.MovimientoInventario
        {
            Tipo = tipo,
            AlmacenId = almacenId,
            VarianteProductoId = varianteProductoId,
            Cantidad = Math.Abs(cantidad),
            Motivo = motivo,
            DocumentoReferencia = documento,
            UsuarioId = await _db.Usuarios.Where(u => u.NombreUsuario == usuario).Select(u => (int?)u.Id).FirstOrDefaultAsync()
        });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return (true, "Movimiento registrado correctamente.");
    }
}
