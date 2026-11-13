using TuningPro.Data.Entities;
using TuningPro.Data.Repositories;

namespace TuningPro.Business.Services;

public record PortalVehiculoDto(int Id, string Placa, string Marca, string Modelo, int Anio);
public record PortalDetalleDto(string Pieza, string Categoria, int Cantidad, decimal PrecioUnitario, decimal Subtotal);
public record PortalOrdenDto(string Codigo, string Estado, DateTime FechaRegistro, string Tecnico,
    decimal SubtotalRepuestos, decimal Igv, decimal ManoObra, decimal Total);
public record PortalProyectoDto(int Id, string Codigo, string Vehiculo, string Estado,
    DateTime FechaRegistro, List<PortalDetalleDto> Piezas, decimal Total, PortalOrdenDto? Orden);
public record PortalResumenDto(string Cliente, List<PortalVehiculoDto> Vehiculos, List<PortalProyectoDto> Proyectos);

public interface IPortalService
{
    Task<PortalResumenDto> ObtenerResumenAsync(int usuarioId, string nombreCliente);
}

public class PortalService : IPortalService
{
    private const decimal TasaIgv = 0.18m;

    private readonly IVehiculoRepository _vehiculos;
    private readonly IProyectoRepository _proyectos;
    private readonly IOrdenTrabajoRepository _ordenes;

    public PortalService(IVehiculoRepository vehiculos, IProyectoRepository proyectos,
        IOrdenTrabajoRepository ordenes)
    {
        _vehiculos = vehiculos; _proyectos = proyectos; _ordenes = ordenes;
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

        var ordenPorProyecto = ordenes
            .GroupBy(o => o.ProyectoId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(o => o.FechaRegistro).First());

        var porVehiculo = vehiculos.ToDictionary(v => v.Id);

        var proyectosDto = proyectos.Select(p =>
        {
            var piezas = new List<PortalDetalleDto>();
            PortalOrdenDto? ordenDto = null;
            decimal total = 0;

            if (ordenPorProyecto.TryGetValue(p.Id, out var o))
            {
                piezas = o.Detalles.Select(d => new PortalDetalleDto(
                    d.Pieza.Nombre, d.Pieza.Categoria,
                    d.Cantidad, d.PrecioUnitario, d.Cantidad * d.PrecioUnitario)).ToList();

                var subtotal = piezas.Sum(x => x.Subtotal);
                var igv = Math.Round(subtotal * TasaIgv, 2);
                total = subtotal + igv + o.ManoObra;

                ordenDto = new PortalOrdenDto(o.Codigo, o.Estado, o.FechaRegistro,
                    o.Tecnico.NombreCompleto, subtotal, igv, o.ManoObra, total);
            }

            var vehiculoTxt = porVehiculo.TryGetValue(p.VehiculoId, out var v)
                ? $"{v.Marca} {v.Modelo} ({v.Placa})"
                : $"Vehículo #{p.VehiculoId}";

            return new PortalProyectoDto(p.Id, p.Codigo, vehiculoTxt, p.Estado,
                p.FechaRegistro, piezas, total, ordenDto);
        }).ToList();

        var vehiculosDto = vehiculos
            .Select(v => new PortalVehiculoDto(v.Id, v.Placa, v.Marca, v.Modelo, v.Anio))
            .ToList();

        return new PortalResumenDto(nombreCliente, vehiculosDto, proyectosDto);
    }
}