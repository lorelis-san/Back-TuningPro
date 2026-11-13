using Microsoft.EntityFrameworkCore;
using TuningPro.Data.Entities;
namespace TuningPro.Data.Repositories;

public interface IProyectoRepository
{
    Task<int> ObtenerSiguienteNumeroAsync();
    Task<string> ObtenerNombreUsuarioAsync(int usuarioId);
    Task<List<Proyectos>> ListarAsync();
    Task<Proyectos?> ObtenerPorIdAsync(int id);
    Task<List<ProblemaCatalogo>> ListarProblemasAsync();
    Task<int> ContarProblemasValidosAsync(List<int> ids);
    Task AgregarAsync(Proyectos proyecto);

    // Métodos que ya usaban Historial, OrdenTrabajo, Portal y Reporte
    Task<List<Proyectos>> GetByVehiculosAsync(IEnumerable<int> vehiculoIds);
    Task<Proyectos?> GetConDetallesAsync(int id);
    Task<List<Proyectos>> GetAllConDetallesAsync();
    Task ActualizarEstadoAsync(int proyectoId, string estado);
}
