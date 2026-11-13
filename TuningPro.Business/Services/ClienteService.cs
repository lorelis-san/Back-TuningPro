using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuningPro.Business.Config;
using TuningPro.Business.Dtos;
using TuningPro.Data.Repositories;
namespace TuningPro.Business.Services;

public class ClienteService : IClienteService
{
    private readonly IClienteRepository _repo;
    public ClienteService(IClienteRepository repo) => _repo = repo;

    public async Task<List<ClienteItemDto>> ListarAsync(string? q) =>
        (await _repo.ListarAsync(RolesTaller.Cliente, q))
            .Select(u => new ClienteItemDto(u.Id, u.NombreCompleto)).ToList();

    public async Task<List<VehiculoItemDto>> ListarVehiculosAsync(int clienteId) =>
        (await _repo.ListarVehiculosAsync(clienteId))
            .Select(v => new VehiculoItemDto(v.Id, v.Placa, v.Marca, v.Modelo, v.Anio)).ToList();
}