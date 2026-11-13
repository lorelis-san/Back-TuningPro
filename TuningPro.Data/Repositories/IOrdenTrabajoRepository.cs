using Microsoft.EntityFrameworkCore;
using TuningPro.Data.Entities;

namespace TuningPro.Data.Repositories;

public interface IOrdenTrabajoRepository
{
    Task<int> ObtenerSiguienteNumeroAsync();
    Task<List<OrdenTrabajo>> ListarAsync();
    Task<OrdenTrabajo?> ObtenerPorIdAsync(int id);
    Task<OrdenTrabajo?> ObtenerPorProyectoIdAsync(int proyectoId);
    Task AgregarAsync(OrdenTrabajo orden);
    Task ActualizarEstadoAsync(OrdenTrabajo orden, string nuevoEstado);
    Task<List<OrdenTrabajo>> GetByProyectosAsync(IEnumerable<int> proyectoIds);
    // en IOrdenTrabajoRepository
    Task<List<OrdenTrabajo>> GetTodasAsync();
}

public class OrdenTrabajoRepository : IOrdenTrabajoRepository
{
    private readonly TuningProDbContext _context;
    public OrdenTrabajoRepository(TuningProDbContext context) => _context = context;

    public async Task<int> ObtenerSiguienteNumeroAsync()
    {
        var maximo = await _context.OrdenesTrabajo.MaxAsync(o => (int?)o.Numero) ?? 0;
        return maximo + 1;
    }

    public Task<List<OrdenTrabajo>> ListarAsync() =>
        _context.OrdenesTrabajo
            .Include(o => o.Proyecto).ThenInclude(p => p.Vehiculo)
            .Include(o => o.Tecnico)
            .Include(o => o.Detalles).ThenInclude(d => d.Pieza)
            .OrderByDescending(o => o.FechaRegistro)
            .ToListAsync();

    public Task<OrdenTrabajo?> ObtenerPorIdAsync(int id) =>
        _context.OrdenesTrabajo
            .Include(o => o.Proyecto).ThenInclude(p => p.Vehiculo)
            .Include(o => o.Proyecto).ThenInclude(p => p.Problemas).ThenInclude(pp => pp.ProblemaCatalogo)
            .Include(o => o.Tecnico)
            .Include(o => o.Detalles).ThenInclude(d => d.Pieza)
            .FirstOrDefaultAsync(o => o.Id == id);

    public Task<OrdenTrabajo?> ObtenerPorProyectoIdAsync(int proyectoId) =>
        _context.OrdenesTrabajo.FirstOrDefaultAsync(o => o.ProyectoId == proyectoId);

    public async Task AgregarAsync(OrdenTrabajo orden)
    {
        await _context.OrdenesTrabajo.AddAsync(orden);
        await _context.SaveChangesAsync();
    }

    public async Task ActualizarEstadoAsync(OrdenTrabajo orden, string nuevoEstado)
    {
        orden.Estado = nuevoEstado;
        _context.OrdenesTrabajo.Update(orden);
        await _context.SaveChangesAsync();
    }
    public Task<List<OrdenTrabajo>> GetByProyectosAsync(IEnumerable<int> proyectoIds) =>
    _context.OrdenesTrabajo
        .Include(o => o.Tecnico)
        .Include(o => o.Detalles).ThenInclude(d => d.Pieza)
        .Where(o => proyectoIds.Contains(o.ProyectoId))
        .ToListAsync();
    // en OrdenTrabajoRepository
    public Task<List<OrdenTrabajo>> GetTodasAsync() =>
        _context.OrdenesTrabajo
            .Include(o => o.Proyecto).ThenInclude(p => p.Vehiculo)
            .Include(o => o.Tecnico)
            .Include(o => o.Detalles).ThenInclude(d => d.Pieza)
            .ToListAsync();
}