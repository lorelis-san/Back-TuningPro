using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuningPro.Api.Models;
using TuningPro.Business.Config;
using TuningPro.Business.Services;
using TuningPro.Data.Entities;

namespace TuningPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesTaller.GestionProyectos)]
public class VehiculosController : ControllerBase
{
    private readonly IVehiculoService _service;
    public VehiculosController(IVehiculoService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? buscar) =>
        Ok(await _service.ListarAsync(buscar));

    // Cuentas de cliente para vincular un vehículo con el portal
    [HttpGet("clientes")]
    [Authorize(Roles = RolesTaller.AdminRecepcion)]
    public async Task<IActionResult> Clientes() => Ok(await _service.ListarClientesAsync());

    [HttpPost]
    [Authorize(Roles = RolesTaller.AdminRecepcion)]
    public async Task<IActionResult> Crear(VehiculoDto dto)
    {
        try
        {
            var vehiculo = await _service.CrearAsync(Mapear(dto));
            return Created($"api/vehiculos/{vehiculo.Id}", vehiculo);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = RolesTaller.AdminRecepcion)]
    public async Task<IActionResult> Actualizar(int id, VehiculoDto dto)
    {
        try { return Ok(await _service.ActualizarAsync(id, Mapear(dto))); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
    }

    private static Vehiculo Mapear(VehiculoDto d) => new()
    {
        Placa = d.Placa ?? string.Empty,
        Marca = d.Marca ?? string.Empty,
        Modelo = d.Modelo ?? string.Empty,
        Anio = d.Anio,
        ClienteNombre = d.ClienteNombre ?? string.Empty,
        UsuarioId = d.UsuarioId
    };
}