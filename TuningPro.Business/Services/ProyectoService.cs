using TuningPro.Business.Dtos;
using TuningPro.Data.Entities;
using TuningPro.Data.Repositories;
namespace TuningPro.Business.Services;

public class ProyectoService : IProyectoService
{
    private const decimal TasaIgv = 0.18m;

    private readonly IProyectoRepository _repo;
    private readonly IClienteRepository _clientes;
    private readonly IOrdenTrabajoRepository _ordenes;
    private readonly IOrdenTrabajoService _ordenesService;

    public ProyectoService(IProyectoRepository repo, IClienteRepository clientes,
        IOrdenTrabajoRepository ordenes, IOrdenTrabajoService ordenesService)
    {
        _repo = repo;
        _clientes = clientes;
        _ordenes = ordenes;
        _ordenesService = ordenesService;
    }

    private static ProyectoDto Mapear(Proyectos p, OrdenTrabajo? o)
    {
        decimal total = 0;
        if (o is not null && o.Estado != "Cancelado")
        {
            var subtotal = o.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
            total = subtotal + Math.Round(subtotal * TasaIgv, 2) + o.ManoObra;
        }

        return new ProyectoDto(
            p.Id, p.Codigo, p.FechaRegistro,
            p.Vehiculo.UsuarioId, p.Vehiculo.ClienteNombre,
            p.VehiculoId, $"{p.Vehiculo.Marca} {p.Vehiculo.Modelo}", p.Vehiculo.Placa,
            p.Recepcionista?.NombreCompleto ?? string.Empty,
            p.Problemas.Select(x => x.ProblemaCatalogo.Nombre).ToList(),
            p.OtrosProblema, p.NotasRecepcion,
            p.Estado,
            total,
            o?.Id, o?.Codigo, o?.Estado);
    }

    public async Task<ProyectoNuevoInfoDto> ObtenerInfoNuevoAsync(int usuarioId)
    {
        var numero = await _repo.ObtenerSiguienteNumeroAsync();
        var nombre = await _repo.ObtenerNombreUsuarioAsync(usuarioId);
        return new ProyectoNuevoInfoDto(numero, Proyectos.FormatearCodigo(numero), DateTime.Now, nombre);
    }

    public async Task<List<ProblemaDto>> ListarProblemasAsync() =>
        (await _repo.ListarProblemasAsync()).Select(x => new ProblemaDto(x.Id, x.Nombre)).ToList();

    public async Task<List<ProyectoDto>> ListarAsync()
    {
        var proyectos = await _repo.ListarAsync();
        var ordenes = (await _ordenes.GetTodasAsync())
            .GroupBy(o => o.ProyectoId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(o => o.FechaRegistro).First());

        return proyectos
            .Select(p => Mapear(p, ordenes.GetValueOrDefault(p.Id)))
            .ToList();
    }

    public async Task<ProyectoDto?> ObtenerAsync(int id)
    {
        var p = await _repo.ObtenerPorIdAsync(id);
        if (p is null) return null;

        var orden = (await _ordenes.GetByProyectosAsync(new[] { id }))
            .OrderByDescending(o => o.FechaRegistro)
            .FirstOrDefault();

        return Mapear(p, orden);
    }

    public async Task<ProyectoDto> CrearAsync(ProyectoCrearDto dto, int recepcionistaId)
    {
        var ids = (dto.ProblemaIds ?? new List<int>()).Distinct().ToList();
        var otros = dto.OtrosProblema?.Trim();

        if (ids.Count == 0 && string.IsNullOrWhiteSpace(otros))
            throw new ReglaNegocioException("Selecciona al menos un problema o descríbelo en la opción Otros.");

        if (!await _clientes.VehiculoPerteneceAsync(dto.VehiculoId, dto.ClienteId))
            throw new ReglaNegocioException("El vehículo no pertenece al cliente seleccionado.");

        if (ids.Count > 0 && await _repo.ContarProblemasValidosAsync(ids) != ids.Count)
            throw new ReglaNegocioException("Alguno de los problemas seleccionados no es válido.");

        var proyecto = new Proyectos
        {
            Estado = "Sin orden",
            FechaRegistro = DateTime.Now,
            RecepcionistaId = recepcionistaId > 0 ? recepcionistaId : null,
            VehiculoId = dto.VehiculoId,
            OtrosProblema = string.IsNullOrWhiteSpace(otros) ? null : otros,
            NotasRecepcion = dto.NotasRecepcion?.Trim(),
            Problemas = ids.Select(id => new ProyectoProblema { ProblemaCatalogoId = id }).ToList(),
        };

        await _repo.AgregarAsync(proyecto);
        return (await ObtenerAsync(proyecto.Id))!;
    }

    public async Task CancelarAsync(int id)
    {
        var proyecto = await _repo.ObtenerPorIdAsync(id)
            ?? throw new KeyNotFoundException("El proyecto no existe.");

        if (proyecto.Estado is "Finalizado" or "Cancelado")
            throw new InvalidOperationException($"Un proyecto {proyecto.Estado.ToLower()} no se puede cancelar.");

        var orden = await _ordenes.ObtenerPorProyectoIdAsync(id);
        if (orden is not null)
            // cancela la orden, devuelve el stock y deja el proyecto en "Cancelado"
            await _ordenesService.CambiarEstadoAsync(orden.Id, "Cancelado", "Administrador");
        else
            await _repo.ActualizarEstadoAsync(id, "Cancelado");
    }
}