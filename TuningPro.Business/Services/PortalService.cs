/*using TuningPro.Data.Entities;
using TuningPro.Data.Repositories;

namespace TuningPro.Business.Services;

public record PortalVehiculoDto(int Id, string Placa, string Marca, string Modelo, int Anio);
public record PortalDetalleDto(string Pieza, string Categoria, int Cantidad, decimal PrecioUnitario, decimal Subtotal);
public record PortalOrdenDto(DateTime FechaEmision, decimal ManoObra, decimal TotalPiezas, decimal Total);
public record PortalProyectoDto(int Id, string Nombre, string Vehiculo, string Estado,
    DateTime FechaRegistro, List<PortalDetalleDto> Piezas, decimal Total, PortalOrdenDto? Orden);
public record PortalResumenDto(string Cliente, List<PortalVehiculoDto> Vehiculos, List<PortalProyectoDto> Proyectos);

public interface IPortalService
{
    Task<PortalResumenDto> ObtenerResumenAsync(int usuarioId, string nombreCliente);
}

public class PortalService : IPortalService
{
    private readonly IVehiculoRepository _vehiculos;
    private readonly IProyectoRepository _proyectos;
    private readonly IRepository<Pieza> _piezas;
    private readonly IOrdenTrabajoRepository _ordenes;

    public PortalService(IVehiculoRepository vehiculos, IProyectoRepository proyectos,
        IRepository<Pieza> piezas, IOrdenTrabajoRepository ordenes)
    {
        _vehiculos = vehiculos; _proyectos = proyectos; _piezas = piezas; _ordenes = ordenes;
    }

    public async Task<PortalResumenDto> ObtenerResumenAsync(int usuarioId, string nombreCliente)
    {
        // Solo los vehículos que pertenecen a este usuario
        var vehiculos = await _vehiculos.GetByUsuarioAsync(usuarioId);
        var ids = vehiculos.Select(v => v.Id).ToList();

        var proyectos = ids.Count == 0
            ? new List<Proyectos>()
            : await _proyectos.GetByVehiculosAsync(ids);

        var ordenes = proyectos.Count == 0
            ? new List<OrdenTrabajo>()
            : await _ordenes.GetByProyectosAsync(proyectos.Select(p => p.Id).ToList());
        var ordenPorProyecto = ordenes.ToDictionary(o => o.ProyectoId);

        var piezas = (await _piezas.GetAllAsync()).ToDictionary(p => p.Id);
        var porVehiculo = vehiculos.ToDictionary(v => v.Id);

        var proyectosDto = proyectos.Select(p =>
        {
            var detalles = p.Detalles.Select(d =>
            {
                piezas.TryGetValue(d.PiezaId, out var pieza);
                return new PortalDetalleDto(
                    pieza?.Nombre ?? $"Pieza #{d.PiezaId}",
                    pieza?.Categoria ?? "",
                    d.Cantidad, d.PrecioUnitario, d.Cantidad * d.PrecioUnitario);
            }).ToList();

            PortalOrdenDto? orden = null;
            if (ordenPorProyecto.TryGetValue(p.Id, out var o))
                orden = new PortalOrdenDto(o.FechaEmision, o.ManoObra, o.TotalPiezas, o.Total);

            var v = porVehiculo[p.VehiculoId];
            return new PortalProyectoDto(
                p.Id, p.Nombre, $"{v.Marca} {v.Modelo} ({v.Placa})",
                p.Estado, p.FechaRegistro, detalles, detalles.Sum(x => x.Subtotal), orden);
        }).ToList();

        var vehiculosDto = vehiculos
            .Select(v => new PortalVehiculoDto(v.Id, v.Placa, v.Marca, v.Modelo, v.Anio))
            .ToList();

        return new PortalResumenDto(nombreCliente, vehiculosDto, proyectosDto);
    }
}*/