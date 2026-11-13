using Microsoft.EntityFrameworkCore;
using TuningPro.Data.Entities;
namespace TuningPro.Data.Repositories;

public class ProyectoRepository : IProyectoRepository
{
    private readonly TuningProDbContext _db;
    public ProyectoRepository(TuningProDbContext db) => _db = db;

    public async Task<int> ObtenerSiguienteNumeroAsync() =>
        (await _db.Proyectos.MaxAsync(p => (int?)p.Numero) ?? 0) + 1;

    public async Task<string> ObtenerNombreUsuarioAsync(int usuarioId) =>
        await _db.Usuarios.Where(u => u.Id == usuarioId)
            .Select(u => u.NombreCompleto).FirstOrDefaultAsync() ?? string.Empty;

    private IQueryable<Proyectos> ConDetalle() => _db.Proyectos
        .Include(p => p.Vehiculo)
        .Include(p => p.Recepcionista)
        .Include(p => p.Detalles)
        .Include(p => p.Problemas).ThenInclude(x => x.ProblemaCatalogo);

    public Task<List<Proyectos>> ListarAsync() =>
        ConDetalle().AsNoTracking().OrderByDescending(p => p.Numero).ToListAsync();

    public Task<Proyectos?> ObtenerPorIdAsync(int id) =>
        ConDetalle().AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

    public Task<List<ProblemaCatalogo>> ListarProblemasAsync() =>
        _db.ProblemasCatalogo.AsNoTracking().Where(x => x.Activo).OrderBy(x => x.Id).ToListAsync();

    public Task<int> ContarProblemasValidosAsync(List<int> ids) =>
        _db.ProblemasCatalogo.CountAsync(x => x.Activo && ids.Contains(x.Id));

    public async Task AgregarAsync(Proyectos proyecto)
    {
        proyecto.Numero = await ObtenerSiguienteNumeroAsync();
        proyecto.Codigo = Proyectos.FormatearCodigo(proyecto.Numero);
        proyecto.Nombre = proyecto.Codigo;
        _db.Proyectos.Add(proyecto);
        await _db.SaveChangesAsync();
    }

    public async Task<List<Proyectos>> GetByVehiculosAsync(IEnumerable<int> vehiculoIds)
    {
        var ids = vehiculoIds.ToList();
        return await _db.Proyectos
            .Include(p => p.Detalles)
            .Where(p => ids.Contains(p.VehiculoId))
            .OrderByDescending(p => p.FechaRegistro)
            .ToListAsync();
    }

    // Con seguimiento de cambios: las órdenes de trabajo pueden modificar el proyecto
    public Task<Proyectos?> GetConDetallesAsync(int id) =>
        _db.Proyectos
            .Include(p => p.Detalles)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<Proyectos>> GetAllConDetallesAsync() =>
        await _db.Proyectos
            .Include(p => p.Detalles)
            .OrderByDescending(p => p.FechaRegistro)
            .ToListAsync();
    public async Task ActualizarEstadoAsync(int proyectoId, string estado)
    {
        var proyecto = await _db.Proyectos.FirstOrDefaultAsync(p => p.Id == proyectoId);
        if (proyecto is null) return;

        proyecto.Estado = estado;
        await _db.SaveChangesAsync();
    }
}