using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TuningPro.Business.Dtos;
namespace TuningPro.Business.Services;

public interface IClienteService
{
    Task<List<ClienteItemDto>> ListarAsync(string? q);
    Task<List<VehiculoItemDto>> ListarVehiculosAsync(int clienteId);
}
