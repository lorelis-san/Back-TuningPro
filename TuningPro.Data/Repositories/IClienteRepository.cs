using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuningPro.Data.Entities;
namespace TuningPro.Data.Repositories;

public interface IClienteRepository
{
    Task<List<Usuario>> ListarAsync(string rol, string? q);
    Task<List<Vehiculo>> ListarVehiculosAsync(int usuarioId);
    Task<bool> VehiculoPerteneceAsync(int vehiculoId, int usuarioId);
}
