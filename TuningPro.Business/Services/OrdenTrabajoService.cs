using TuningPro.Data.Entities;
using TuningPro.Data.Repositories;
using TuningPro.Business.Dtos;

namespace TuningPro.Business.Services;

public interface IOrdenTrabajoService
{
    Task<List<OrdenTrabajoDto>> ListarAsync();
    Task<OrdenTrabajoDto?> ObtenerPorIdAsync(int id);
    Task<DatosNuevaOrdenDto> ObtenerDatosParaNuevaAsync(int proyectoId);
    Task<OrdenTrabajoDto> CrearAsync(CrearOrdenTrabajoDto dto, string usuarioRol);
    Task<OrdenTrabajoDto> CambiarEstadoAsync(int id, string nuevoEstado, string usuarioRol);
}

public class OrdenTrabajoService : IOrdenTrabajoService
{
    private static readonly string[] EstadosValidos = { "Registrado", "En proceso", "Finalizado" };

    private readonly IOrdenTrabajoRepository _ordenes;
    private readonly IProyectoRepository _proyectos;
    private readonly IRepository<Pieza> _piezas;
    private readonly IUsuarioRepository _usuarios;
    private readonly ICompatibilidadService _compatibilidad;

    public OrdenTrabajoService(
        IOrdenTrabajoRepository ordenes,
        IProyectoRepository proyectos,
        IRepository<Pieza> piezas,
        IUsuarioRepository usuarios,
        ICompatibilidadService compatibilidad)
    {
        _ordenes = ordenes;
        _proyectos = proyectos;
        _piezas = piezas;
        _usuarios = usuarios;
        _compatibilidad = compatibilidad;
    }

    public async Task<List<OrdenTrabajoDto>> ListarAsync()
    {
        var ordenes = await _ordenes.ListarAsync();
        return ordenes.Select(MapearDto).ToList();
    }

    public async Task<OrdenTrabajoDto?> ObtenerPorIdAsync(int id)
    {
        var orden = await _ordenes.ObtenerPorIdAsync(id);
        return orden is null ? null : MapearDto(orden);
    }

    public async Task<DatosNuevaOrdenDto> ObtenerDatosParaNuevaAsync(int proyectoId)
    {
        var proyecto = await _proyectos.ObtenerPorIdAsync(proyectoId)
            ?? throw new KeyNotFoundException("El proyecto no existe.");

        var siguienteNumero = await _ordenes.ObtenerSiguienteNumeroAsync();
        var tecnicos = await _usuarios.GetByRolAsync("Tecnico");
        var repuestosCompatibles = await _compatibilidad.ListarPorVehiculoAsync(proyecto.VehiculoId);

        return new DatosNuevaOrdenDto
        {
            NumeroSugerido = siguienteNumero,
            CodigoSugerido = OrdenTrabajo.FormatearCodigo(siguienteNumero),
            ProyectoId = proyecto.Id,
            ProyectoCodigo = proyecto.Codigo,
            ClienteNombre = proyecto.Vehiculo.ClienteNombre,
            VehiculoDescripcion = $"{proyecto.Vehiculo.Marca} {proyecto.Vehiculo.Modelo}",
            VehiculoPlaca = proyecto.Vehiculo.Placa,
            ProblemasReportados = ObtenerProblemas(proyecto),
            Tecnicos = tecnicos.Select(t => new TecnicoDto { Id = t.Id, NombreCompleto = t.NombreCompleto }).ToList(),
            RepuestosCompatibles = repuestosCompatibles.Select(p => new PiezaCompatibleDto
            {
                Id = p.Id,
                Codigo = p.Codigo,
                Nombre = p.Nombre,
                PrecioUnitario = p.PrecioUnitario,
                Stock = p.Stock
            }).ToList()
        };
    }

    public async Task<OrdenTrabajoDto> CrearAsync(CrearOrdenTrabajoDto dto, string usuarioRol)
    {
        if (usuarioRol != "Administrador" && usuarioRol != "Tecnico")
            throw new UnauthorizedAccessException("Tu rol no puede crear órdenes de trabajo.");

        var proyecto = await _proyectos.ObtenerPorIdAsync(dto.ProyectoId)
            ?? throw new KeyNotFoundException("El proyecto no existe.");

        if (await _ordenes.ObtenerPorProyectoIdAsync(dto.ProyectoId) is not null)
            throw new InvalidOperationException("Este proyecto ya tiene una orden de trabajo registrada.");

        var tecnico = await _usuarios.GetByIdAsync(dto.TecnicoId)
            ?? throw new KeyNotFoundException("El técnico seleccionado no existe.");

        if (dto.Repuestos.Count == 0)
            throw new InvalidOperationException("Agrega al menos un repuesto a la orden.");

        var numero = await _ordenes.ObtenerSiguienteNumeroAsync();
        var orden = new OrdenTrabajo
        {
            Numero = numero,
            Codigo = OrdenTrabajo.FormatearCodigo(numero),
            ProyectoId = proyecto.Id,
            TecnicoId = tecnico.Id,
            Estado = "Registrado",
            FechaRegistro = DateTime.Now,
            ManoObra = dto.ManoObra < 0 ? 0 : dto.ManoObra,
            Observaciones = dto.Observaciones
        };

        foreach (var item in dto.Repuestos)
        {
            var pieza = await _piezas.GetByIdAsync(item.PiezaId)
                ?? throw new KeyNotFoundException($"La pieza #{item.PiezaId} no existe.");

            var resultado = await _compatibilidad.ValidarAsync(proyecto.VehiculoId, pieza.Id);
            if (!resultado.EsCompatible)
                throw new InvalidOperationException(resultado.Mensaje);

            if (pieza.Stock < item.Cantidad)
                throw new InvalidOperationException($"Stock insuficiente de \"{pieza.Nombre}\" (disponible: {pieza.Stock}).");

            orden.Detalles.Add(new DetalleOrdenTrabajo
            {
                PiezaId = pieza.Id,
                Cantidad = item.Cantidad,
                PrecioUnitario = pieza.PrecioUnitario
            });

            pieza.Stock -= item.Cantidad;
            _piezas.Update(pieza);
        }

        await _piezas.SaveAsync();
        await _ordenes.AgregarAsync(orden);

        await _ordenes.AgregarAsync(orden);
        await _proyectos.ActualizarEstadoAsync(proyecto.Id, "En proceso");


        var creada = await _ordenes.ObtenerPorIdAsync(orden.Id)
            ?? throw new InvalidOperationException("No se pudo leer la orden recién creada.");
        return MapearDto(creada);
    }

    public async Task<OrdenTrabajoDto> CambiarEstadoAsync(int id, string nuevoEstado, string usuarioRol)
    {
        if (usuarioRol != "Administrador" && usuarioRol != "Tecnico")
            throw new UnauthorizedAccessException("Tu rol solo permite consultar el estado.");

        if (!EstadosValidos.Contains(nuevoEstado))
            throw new InvalidOperationException("Estado no válido.");

        var orden = await _ordenes.ObtenerPorIdAsync(id)
            ?? throw new KeyNotFoundException("La orden de trabajo no existe.");

        await _ordenes.ActualizarEstadoAsync(orden, nuevoEstado);

        await _ordenes.ActualizarEstadoAsync(orden, nuevoEstado);

        // Proyecto: Finalizado solo cuando la orden finaliza; en cualquier otro caso sigue En proceso
        var estadoProyecto = nuevoEstado == "Finalizado" ? "Finalizado" : "En proceso";
        await _proyectos.ActualizarEstadoAsync(orden.ProyectoId, estadoProyecto);

        return MapearDto(orden);
    }

    private static List<string> ObtenerProblemas(Proyectos proyecto)
    {
        var lista = proyecto.Problemas?
            .Select(pp => pp.ProblemaCatalogo?.Nombre ?? "Problema")
            .ToList() ?? new List<string>();

        if (!string.IsNullOrWhiteSpace(proyecto.OtrosProblema))
            lista.Add($"Otros: {proyecto.OtrosProblema}");

        return lista;
    }

    private static OrdenTrabajoDto MapearDto(OrdenTrabajo orden)
    {
        return new OrdenTrabajoDto
        {
            Id = orden.Id,
            ProyectoId = orden.ProyectoId,
            Codigo = orden.Codigo,
            ProyectoCodigo = orden.Proyecto.Codigo,
            ClienteNombre = orden.Proyecto.Vehiculo.ClienteNombre,
            VehiculoDescripcion = $"{orden.Proyecto.Vehiculo.Marca} {orden.Proyecto.Vehiculo.Modelo}",
            VehiculoPlaca = orden.Proyecto.Vehiculo.Placa,
            TecnicoNombre = orden.Tecnico.NombreCompleto,
            ProblemasReportados = ObtenerProblemas(orden.Proyecto),
            Repuestos = orden.Detalles.Select(d => new RepuestoOrdenDto
            {
                Id = d.Id,
                PiezaId = d.PiezaId,
                PiezaCodigo = d.Pieza.Codigo,
                PiezaNombre = d.Pieza.Nombre,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario
            }).ToList(),
            ManoObra = orden.ManoObra,
            Observaciones = orden.Observaciones,
            FechaCreacion = orden.FechaRegistro,
            Estado = orden.Estado
        };
    }
}