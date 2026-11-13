using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TuningPro.Business.Config;
using TuningPro.Business.Dtos;
using TuningPro.Business.Services;

namespace TuningPro.Api.Controllers;

[ApiController]
[Route("api/ordenes-trabajo")]
[Authorize]
public class OrdenesTrabajoController : ControllerBase
{
    private readonly IOrdenTrabajoService _service;

    public OrdenesTrabajoController(IOrdenTrabajoService service)
    {
        _service = service;
    }

    // El token trae el claim "role" (se mapea a ClaimTypes.Role)
    private string ObtenerRol() =>
        User.FindFirst(ClaimTypes.Role)?.Value
        ?? User.FindFirst("role")?.Value
        ?? string.Empty;

    [HttpGet]
    public async Task<ActionResult<List<OrdenTrabajoDto>>> Listar()
        => Ok(await _service.ListarAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrdenTrabajoDto>> ObtenerPorId(int id)
    {
        var orden = await _service.ObtenerPorIdAsync(id);
        return orden is null ? NotFound(new { mensaje = "La orden de trabajo no existe." }) : Ok(orden);
    }

    [HttpGet("nueva/{proyectoId:int}")]
    [Authorize(Roles = RolesTaller.AdminTecnico)]
    public async Task<ActionResult<DatosNuevaOrdenDto>> DatosParaNueva(int proyectoId)
    {
        try { return Ok(await _service.ObtenerDatosParaNuevaAsync(proyectoId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
    }

    [HttpPost]
    [Authorize(Roles = RolesTaller.AdminTecnico)]
    public async Task<ActionResult<OrdenTrabajoDto>> Crear(CrearOrdenTrabajoDto dto)
    {
        try
        {
            var creada = await _service.CrearAsync(dto, ObtenerRol());
            return CreatedAtAction(nameof(ObtenerPorId), new { id = creada.Id }, creada);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { mensaje = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
    }

    [HttpPut("{id:int}/estado")]
    [Authorize(Roles = RolesTaller.AdminTecnico)]
    public async Task<ActionResult<OrdenTrabajoDto>> CambiarEstado(int id, CambiarEstadoOrdenDto dto)
    {
        try { return Ok(await _service.CambiarEstadoAsync(id, dto.Estado, ObtenerRol())); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { mensaje = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
    }
}