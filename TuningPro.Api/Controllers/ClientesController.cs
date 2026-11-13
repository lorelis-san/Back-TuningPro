using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuningPro.Business.Config;
using TuningPro.Business.Dtos;
using TuningPro.Business.Services;

namespace TuningPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesTaller.GestionProyectos)]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _service;
    public ClientesController(IClienteService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<ClienteItemDto>>> Listar([FromQuery] string? q) =>
        Ok(await _service.ListarAsync(q));

    [HttpGet("{id:int}/vehiculos")]
    public async Task<ActionResult<List<VehiculoItemDto>>> Vehiculos(int id) =>
        Ok(await _service.ListarVehiculosAsync(id));
}