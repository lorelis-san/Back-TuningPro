using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuningPro.Business.Config;
using TuningPro.Business.Dtos;
using TuningPro.Business.Services;

namespace TuningPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesTaller.GestionProyectos)]
public class ProyectosController : ControllerBase
{
    private readonly IProyectoService _service;
    public ProyectosController(IProyectoService service) => _service = service;

    private int IdUsuario =>
    int.TryParse(User.FindFirstValue("sub"), out var id) ? id : 0;

    [HttpGet("nuevo")]
    public async Task<ActionResult<ProyectoNuevoInfoDto>> Nuevo() =>
        Ok(await _service.ObtenerInfoNuevoAsync(IdUsuario));

    [HttpGet("problemas")]
    public async Task<ActionResult<List<ProblemaDto>>> Problemas() =>
        Ok(await _service.ListarProblemasAsync());

    [HttpGet]
    public async Task<ActionResult<List<ProyectoDto>>> Listar() =>
        Ok(await _service.ListarAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProyectoDto>> Obtener(int id)
    {
        var p = await _service.ObtenerAsync(id);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpPost]
    [Authorize(Roles = RolesTaller.AdminRecepcion)]
    public async Task<ActionResult<ProyectoDto>> Crear(ProyectoCrearDto dto)
    {
        try
        {
            var creado = await _service.CrearAsync(dto, IdUsuario);
            return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
        }
        catch (ReglaNegocioException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }
}