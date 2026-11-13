using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuningPro.Business.Config;
using TuningPro.Business.Services;

namespace TuningPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesTaller.GestionProyectos)]
public class HistorialController : ControllerBase
{
    private readonly IHistorialService _service;
    public HistorialController(IHistorialService service) => _service = service;

    [HttpGet("{vehiculoId}")]
    public async Task<IActionResult> Get(int vehiculoId)
    {
        try { return Ok(await _service.ObtenerAsync(vehiculoId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
    }
}