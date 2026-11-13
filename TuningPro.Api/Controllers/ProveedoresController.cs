using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuningPro.Api.Models;
using TuningPro.Business.Config;
using TuningPro.Business.Services;
using TuningPro.Data.Entities;

namespace TuningPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesTaller.AdminAuxiliar)]
public class ProveedoresController : ControllerBase
{
    private readonly IProveedorService _service;
    public ProveedoresController(IProveedorService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await _service.ListarAsync());

    [HttpPost]
    [Authorize(Roles = RolesTaller.AdminAuxiliar)]
    public async Task<IActionResult> Crear(ProveedorDto dto)
    {
        try
        {
            var proveedor = await _service.CrearAsync(Mapear(dto));
            return Created($"api/proveedores/{proveedor.Id}", proveedor);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = RolesTaller.AdminAuxiliar)]
    public async Task<IActionResult> Actualizar(int id, ProveedorDto dto)
    {
        try { return Ok(await _service.ActualizarAsync(id, Mapear(dto))); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
    }

    private static Proveedor Mapear(ProveedorDto d) => new()
    {
        Nombre = d.Nombre ?? string.Empty,
        Contacto = d.Contacto ?? string.Empty
    };
}