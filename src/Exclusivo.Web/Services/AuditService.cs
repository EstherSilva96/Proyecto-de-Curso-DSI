using Exclusivo.Web.Data;
using Exclusivo.Web.Models;

namespace Exclusivo.Web.Services;

public class AuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db) => _db = db;

    public async Task RegistrarAsync(string? usuario, string accion, string? entidad, int? entidadId, string? detalles)
    {
        _db.Auditorias.Add(new Auditoria
        {
            Usuario = usuario,
            Accion = accion,
            Entidad = entidad,
            EntidadId = entidadId,
            Detalles = detalles
        });
        await _db.SaveChangesAsync();
    }
}
