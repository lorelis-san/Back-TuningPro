using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TuningPro.Business.Dtos;
using TuningPro.Data.Entities;
using TuningPro.Data.Repositories;
namespace TuningPro.Business.Services;

public class ProyectoService : IProyectoService
{
    private readonly IProyectoRepository _repo;
    private readonly IClienteRepository _clientes;

    public ProyectoService(IProyectoRepository repo, IClienteRepository clientes)
    {
        _repo = repo;
        _clientes = clientes;
    }

    private static ProyectoDto Mapear(Proyectos p) => new(
        p.Id, p.Codigo, p.FechaRegistro,
        p.Vehiculo.UsuarioId, p.Vehiculo.ClienteNombre,
        p.VehiculoId, $"{p.Vehiculo.Marca} {p.Vehiculo.Modelo}", p.Vehiculo.Placa,
        p.Recepcionista?.NombreCompleto ?? string.Empty,
        p.Problemas.Select(x => x.ProblemaCatalogo.Nombre).ToList(),
        p.OtrosProblema, p.NotasRecepcion,
        p.Estado,
        p.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario));

    public async Task<ProyectoNuevoInfoDto> ObtenerInfoNuevoAsync(int usuarioId)
    {
        var numero = await _repo.ObtenerSiguienteNumeroAsync();
        var nombre = await _repo.ObtenerNombreUsuarioAsync(usuarioId);
        return new ProyectoNuevoInfoDto(numero, Proyectos.FormatearCodigo(numero), DateTime.Now, nombre);
    }

    public async Task<List<ProblemaDto>> ListarProblemasAsync() =>
        (await _repo.ListarProblemasAsync()).Select(x => new ProblemaDto(x.Id, x.Nombre)).ToList();

    public async Task<List<ProyectoDto>> ListarAsync() =>
        (await _repo.ListarAsync()).Select(Mapear).ToList();

    public async Task<ProyectoDto?> ObtenerAsync(int id)
    {
        var p = await _repo.ObtenerPorIdAsync(id);
        return p is null ? null : Mapear(p);
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
}
