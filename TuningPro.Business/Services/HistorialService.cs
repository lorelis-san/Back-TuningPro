using TuningPro.Data.Entities;
using TuningPro.Data.Repositories;

namespace TuningPro.Business.Services;

public record HistorialPiezaDto(string Codigo, string Pieza, string Categoria, string Marca,
    int Cantidad, decimal PrecioUnitario, decimal Subtotal);

public record HistorialOrdenDto(int Id, string Codigo, DateTime FechaRegistro, string TecnicoNombre,
    decimal SubtotalRepuestos, decimal Igv, decimal ManoObra, decimal Total, string Estado);

public record HistorialProyectoDto(int Id, string Nombre, string Estado, DateTime FechaRegistro,
    List<HistorialPiezaDto> Piezas, decimal TotalPiezas, HistorialOrdenDto? Orden);

public record HistorialVehiculoDto(int Id, string Placa, string Marca, string Modelo, int Anio,
    string Cliente, int TotalProyectos, int TotalPiezasInstaladas, decimal InversionTotal,
    List<HistorialProyectoDto> Proyectos);

public interface IHistorialService
{
    Task<HistorialVehiculoDto> ObtenerAsync(int vehiculoId);
}

public class HistorialService : IHistorialService
{
    private readonly IRepository<Vehiculo> _vehiculos;
    private readonly IProyectoRepository _proyectos;
    private readonly IOrdenTrabajoRepository _ordenes;

    public HistorialService(IRepository<Vehiculo> vehiculos, IProyectoRepository proyectos,
        IOrdenTrabajoRepository ordenes)
    {
        _vehiculos = vehiculos; _proyectos = proyectos; _ordenes = ordenes;
    }

    public async Task<HistorialVehiculoDto> ObtenerAsync(int vehiculoId)
    {
        var vehiculo = await _vehiculos.GetByIdAsync(vehiculoId)
                       ?? throw new KeyNotFoundException("Vehículo no encontrado.");

        var proyectos = await _proyectos.GetByVehiculosAsync(new List<int> { vehiculoId });

        var ordenes = proyectos.Count == 0
            ? new List<OrdenTrabajo>()
            : await _ordenes.GetByProyectosAsync(proyectos.Select(p => p.Id).ToList());
        var ordenPorProyecto = ordenes.ToDictionary(o => o.ProyectoId);

        var proyectosDto = proyectos.Select(p =>
        {
            ordenPorProyecto.TryGetValue(p.Id, out var o);

            var detalle = o?.Detalles.Select(d => new HistorialPiezaDto(
                d.Pieza.Codigo, d.Pieza.Nombre, d.Pieza.Categoria, d.Pieza.Marca,
                d.Cantidad, d.PrecioUnitario, d.Cantidad * d.PrecioUnitario)
            ).ToList() ?? new List<HistorialPiezaDto>();

            var subtotalRepuestos = detalle.Sum(x => x.Subtotal);

            HistorialOrdenDto? ordenDto = null;
            if (o is not null)
            {
                var igv = Math.Round(subtotalRepuestos * 0.18m, 2);
                var total = subtotalRepuestos + igv + o.ManoObra;
                ordenDto = new HistorialOrdenDto(o.Id, o.Codigo, o.FechaRegistro, o.Tecnico.NombreCompleto,
                    subtotalRepuestos, igv, o.ManoObra, total, o.Estado);
            }

            return new HistorialProyectoDto(p.Id, p.Nombre, p.Estado, p.FechaRegistro, detalle, subtotalRepuestos, ordenDto);
        }).ToList();

        // Los proyectos cancelados o rechazados no cuentan: sus piezas volvieron al inventario
        var vigentes = proyectosDto.Where(p => p.Estado != "Cancelado" && p.Estado != "Rechazado").ToList();
        var inversion = vigentes.Sum(p => p.Orden?.Total ?? p.TotalPiezas);
        var piezasInstaladas = vigentes.Sum(p => p.Piezas.Sum(x => x.Cantidad));

        return new HistorialVehiculoDto(vehiculo.Id, vehiculo.Placa, vehiculo.Marca, vehiculo.Modelo,
            vehiculo.Anio, vehiculo.ClienteNombre, proyectosDto.Count, piezasInstaladas, inversion, proyectosDto);
    }
}