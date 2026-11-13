using TuningPro.Business.Config;
using TuningPro.Business.Strategies;
using TuningPro.Data.Entities;
using TuningPro.Data.Repositories;

namespace TuningPro.Business.Services;

// ---------- Compatibilidad ----------
public interface ICompatibilidadService
{
    Task<ResultadoValidacion> ValidarAsync(int vehiculoId, int piezaId);
    Task RegistrarAsync(int vehiculoId, int piezaId);
    Task<List<Pieza>> ListarPorVehiculoAsync(int vehiculoId);
    Task EliminarAsync(int vehiculoId, int piezaId);
}

public class CompatibilidadService : ICompatibilidadService
{
    private readonly IEnumerable<ICompatibilidadStrategy> _strategies;
    private readonly IRepository<Vehiculo> _vehiculos;
    private readonly IRepository<Pieza> _piezas;
    private readonly ICompatibilidadRepository _compat;

    public CompatibilidadService(IEnumerable<ICompatibilidadStrategy> strategies,
        IRepository<Vehiculo> vehiculos, IRepository<Pieza> piezas, ICompatibilidadRepository compat)
    {
        _strategies = strategies; _vehiculos = vehiculos; _piezas = piezas; _compat = compat;
    }

    public async Task<ResultadoValidacion> ValidarAsync(int vehiculoId, int piezaId)
    {
        var v = await _vehiculos.GetByIdAsync(vehiculoId) ?? throw new KeyNotFoundException("Vehículo no encontrado.");
        var p = await _piezas.GetByIdAsync(piezaId) ?? throw new KeyNotFoundException("Pieza no encontrada.");

        var strategy = _strategies.FirstOrDefault(s => s.Categoria == p.Categoria)
                       ?? _strategies.First(s => s.Categoria == "General");
        return await strategy.ValidarAsync(v, p);
    }

    // RF06: registrar compatibilidad vehículo-pieza
    public async Task RegistrarAsync(int vehiculoId, int piezaId)
    {
        if (await _vehiculos.GetByIdAsync(vehiculoId) is null)
            throw new KeyNotFoundException("Vehículo no encontrado.");
        if (await _piezas.GetByIdAsync(piezaId) is null)
            throw new KeyNotFoundException("Pieza no encontrada.");
        if (await _compat.ExisteAsync(vehiculoId, piezaId))
            throw new InvalidOperationException("La compatibilidad ya está registrada.");

        await _compat.AddAsync(new Compatibilidad { VehiculoId = vehiculoId, PiezaId = piezaId });
        await _compat.SaveAsync();
    }

    public async Task<List<Pieza>> ListarPorVehiculoAsync(int vehiculoId)
    {
        if (await _vehiculos.GetByIdAsync(vehiculoId) is null)
            throw new KeyNotFoundException("Vehículo no encontrado.");

        var relaciones = await _compat.GetByVehiculoAsync(vehiculoId);
        var ids = relaciones.Select(c => c.PiezaId).ToHashSet();

        return (await _piezas.GetAllAsync())
            .Where(p => ids.Contains(p.Id))
            .OrderBy(p => p.Categoria).ThenBy(p => p.Nombre)
            .ToList();
    }

    public async Task EliminarAsync(int vehiculoId, int piezaId)
    {
        var relacion = await _compat.GetAsync(vehiculoId, piezaId)
                       ?? throw new KeyNotFoundException("La compatibilidad no existe.");
        _compat.Remove(relacion);
        await _compat.SaveAsync();
    }
}

// ---------- Inventario ----------
public interface IInventarioService
{
    Task<Pieza> RegistrarMovimientoAsync(int piezaId, int cantidad, bool esEntrada);
    Task<List<Pieza>> ObtenerAlertasAsync();
}

public class InventarioService : IInventarioService
{
    private readonly IRepository<Pieza> _piezas;
    private readonly ConfiguracionTaller _config;

    public InventarioService(IRepository<Pieza> piezas, ConfiguracionTaller config)
    {
        _piezas = piezas; _config = config;
    }

    public async Task<Pieza> RegistrarMovimientoAsync(int piezaId, int cantidad, bool esEntrada)
    {
        var pieza = await _piezas.GetByIdAsync(piezaId) ?? throw new KeyNotFoundException("Pieza no encontrada.");
        if (cantidad <= 0) throw new InvalidOperationException("La cantidad debe ser mayor a 0.");
        if (!esEntrada && pieza.Stock < cantidad) throw new InvalidOperationException("Stock insuficiente.");

        pieza.Stock += esEntrada ? cantidad : -cantidad;
        _piezas.Update(pieza);
        await _piezas.SaveAsync();
        return pieza;
    }

    public async Task<List<Pieza>> ObtenerAlertasAsync() =>
        (await _piezas.GetAllAsync()).Where(p => p.Stock <= _config.StockMinimo).ToList();
}

// ---------- Proyectos ----------
public record ProyectoDetalleListadoDto(int Id, int ProyectoId, int PiezaId, string PiezaNombre,
    int Cantidad, decimal PrecioUnitario);

public record ProyectoListadoDto(int Id, string Nombre, int VehiculoId, string Estado,
    DateTime FechaRegistro, List<ProyectoDetalleListadoDto> Detalles);

/*
public interface IProyectoService
{
    IReadOnlyDictionary<string, string[]> Transiciones { get; }
    Task<Proyecto> CrearAsync(string nombre, int vehiculoId);
    Task<Proyecto> AsignarPiezaAsync(int proyectoId, int piezaId, int cantidad);
    Task<Proyecto?> ObtenerAsync(int id);
    Task<List<Proyecto>> ListarAsync();
    Task<List<ProyectoListadoDto>> ListarConDetalleAsync();
    Task<Proyecto> CambiarEstadoAsync(int id, string estado);
}
/*
public class ProyectoService : IProyectoService
{
    // RF09: estados del proyecto y cambios permitidos entre ellos
    private static readonly Dictionary<string, string[]> TransicionesPermitidas = new()
    {
        ["Registrado"] = new[] { "En proceso", "Rechazado", "Cancelado" },
        ["En proceso"] = new[] { "En pausa", "Finalizado", "Cancelado" },
        ["En pausa"] = new[] { "En proceso", "Cancelado" },
        ["Finalizado"] = Array.Empty<string>(),
        ["Cancelado"] = Array.Empty<string>(),
        ["Rechazado"] = Array.Empty<string>()
    };

    public IReadOnlyDictionary<string, string[]> Transiciones => TransicionesPermitidas;

    private readonly IProyectoRepository _proyectos;
    private readonly IRepository<Pieza> _piezas;
    private readonly ICompatibilidadService _compat;
    private readonly IInventarioService _inventario;
    private readonly IOrdenTrabajoRepository _ordenes;

    public ProyectoService(IProyectoRepository proyectos, IRepository<Pieza> piezas,
        ICompatibilidadService compat, IInventarioService inventario, IOrdenTrabajoRepository ordenes)
    {
        _proyectos = proyectos; _piezas = piezas; _compat = compat; _inventario = inventario;
        _ordenes = ordenes;
    }
    
    public async Task<Proyecto> CrearAsync(string nombre, int vehiculoId)
    {
        var proyecto = new Proyecto { Nombre = nombre, VehiculoId = vehiculoId };
        await _proyectos.AddAsync(proyecto);
        await _proyectos.SaveAsync();
        return proyecto;
    }

    public async Task<Proyecto> AsignarPiezaAsync(int proyectoId, int piezaId, int cantidad)
    {
        var proyecto = await _proyectos.GetConDetallesAsync(proyectoId)
                       ?? throw new KeyNotFoundException("Proyecto no encontrado.");
        var pieza = await _piezas.GetByIdAsync(piezaId)
                    ?? throw new KeyNotFoundException("Pieza no encontrada.");

        if (proyecto.Estado is "Finalizado" or "Cancelado" or "Rechazado")
            throw new InvalidOperationException($"El proyecto está {proyecto.Estado.ToLower()}, no admite más piezas.");

        if (proyecto.Estado == "En pausa")
            throw new InvalidOperationException("El proyecto está en pausa. Reanúdalo para asignar piezas.");

        if (await _ordenes.ExisteParaProyectoAsync(proyectoId))
            throw new InvalidOperationException("El proyecto ya tiene una orden de trabajo emitida, no admite más piezas.");

        // 1) Validar compatibilidad (Strategy)
        var resultado = await _compat.ValidarAsync(proyecto.VehiculoId, piezaId);
        if (!resultado.EsCompatible) throw new InvalidOperationException(resultado.Mensaje);

        // 2) Descontar stock
        await _inventario.RegistrarMovimientoAsync(piezaId, cantidad, esEntrada: false);

        // 3) Registrar en el proyecto (trazabilidad)
        proyecto.Detalles.Add(new DetalleProyecto
        {
            PiezaId = piezaId,
            Cantidad = cantidad,
            PrecioUnitario = pieza.PrecioUnitario
        });
        proyecto.Estado = "En proceso";
        await _proyectos.SaveAsync();
        return proyecto;
    }

    public Task<Proyecto?> ObtenerAsync(int id) => _proyectos.GetConDetallesAsync(id);

    public Task<List<Proyecto>> ListarAsync() => _proyectos.GetAllConDetallesAsync();

    public async Task<List<ProyectoListadoDto>> ListarConDetalleAsync()
    {
        var proyectos = await _proyectos.GetAllConDetallesAsync();
        var piezas = (await _piezas.GetAllAsync()).ToDictionary(p => p.Id, p => p.Nombre);

        return proyectos.Select(p => new ProyectoListadoDto(
            p.Id, p.Nombre, p.VehiculoId, p.Estado, p.FechaRegistro,
            p.Detalles.Select(d => new ProyectoDetalleListadoDto(
                d.Id, d.ProyectoId, d.PiezaId,
                piezas.GetValueOrDefault(d.PiezaId, $"Pieza #{d.PiezaId}"),
                d.Cantidad, d.PrecioUnitario)).ToList()
        )).ToList();
    }

    public async Task<Proyecto> CambiarEstadoAsync(int id, string estado)
    {
        estado = (estado ?? string.Empty).Trim();
        if (!TransicionesPermitidas.ContainsKey(estado))
            throw new InvalidOperationException("Estado no válido.");

        var proyecto = await _proyectos.GetConDetallesAsync(id)
                       ?? throw new KeyNotFoundException("Proyecto no encontrado.");

        if (!TransicionesPermitidas.TryGetValue(proyecto.Estado, out var permitidos) || !permitidos.Contains(estado))
            throw new InvalidOperationException($"No se puede pasar de \"{proyecto.Estado}\" a \"{estado}\".");

        if (estado is "Cancelado" or "Rechazado")
        {
            if (await _ordenes.ExisteParaProyectoAsync(id))
                throw new InvalidOperationException(
                    "El proyecto ya tiene una orden de trabajo emitida; no se puede cancelar ni rechazar.");

            // Las piezas asignadas vuelven al inventario (borra este bloque si el equipo decide no reponer el stock)
            foreach (var d in proyecto.Detalles)
                await _inventario.RegistrarMovimientoAsync(d.PiezaId, d.Cantidad, esEntrada: true);
        }

        proyecto.Estado = estado;
        await _proyectos.SaveAsync();
        return proyecto;
    }*/
