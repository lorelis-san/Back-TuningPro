using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuningPro.Api.Models;
using TuningPro.Business.Config;
using TuningPro.Business.Services;

namespace TuningPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesTaller.AdminTecnico)]
public class CompatibilidadController : ControllerBase
{
    private readonly ICompatibilidadService _service;
    public CompatibilidadController(ICompatibilidadService service) => _service = service;

    // Piezas compatibles con un vehículo
    [HttpGet]
    public async Task<IActionResult> Listar(int vehiculoId)
    {
        try { return Ok(await _service.ListarPorVehiculoAsync(vehiculoId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
    }

    [HttpGet("validar")]
    [Authorize(Roles = RolesTaller.AdminTecnico)]
    public async Task<IActionResult> Validar(int vehiculoId, int piezaId)
    {
        try { return Ok(await _service.ValidarAsync(vehiculoId, piezaId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
    }

    [HttpPost]
    [Authorize(Roles = RolesTaller.AdminTecnico)]
    public async Task<IActionResult> Registrar(CompatibilidadDto dto)
    {
        try
        {
            await _service.RegistrarAsync(dto.VehiculoId, dto.PiezaId);
            return Ok(new { mensaje = "Compatibilidad registrada." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { mensaje = ex.Message }); }
    }

    [HttpDelete]
    [Authorize(Roles = RolesTaller.AdminTecnico)]
    public async Task<IActionResult> Eliminar(int vehiculoId, int piezaId)
    {
        try
        {
            await _service.EliminarAsync(vehiculoId, piezaId);
            return Ok(new { mensaje = "Compatibilidad eliminada." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
    }
}