using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuningPro.Business.Dtos;
namespace TuningPro.Business.Services
{
    public interface IProyectoService
    {
        Task<ProyectoNuevoInfoDto> ObtenerInfoNuevoAsync(int usuarioId);
        Task<List<ProblemaDto>> ListarProblemasAsync();
        Task<List<ProyectoDto>> ListarAsync();
        Task<ProyectoDto?> ObtenerAsync(int id);
        Task<ProyectoDto> CrearAsync(ProyectoCrearDto dto, int recepcionistaId);
        Task CancelarAsync(int id);
    }
}
