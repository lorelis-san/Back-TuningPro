using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuningPro.Api.Models;
using TuningPro.Business.Config;
using TuningPro.Business.Services;
using TuningPro.Data.Entities;
using TuningPro.Data.Repositories;

namespace TuningPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesTaller.Inventario)]
public class PiezasController : ControllerBase
{
    private readonly IRepository<Pieza> _repo;
    private readonly IPiezaService _piezas;
    private readonly IInventarioService _inventario;

    public PiezasController(IRepository<Pieza> repo, IPiezaService piezas, IInventarioService inventario)
    {
        _repo = repo; _piezas = piezas; _inventario = inventario;
    }

    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await _repo.GetAllAsync());

    [HttpPost]
    [Authorize(Roles = RolesTaller.AdminAuxiliar)]
    public async Task<IActionResult> Crear(PiezaDto dto)
    {
        try
        {
            var pieza = await _piezas.CrearAsync(Mapear(dto));
            return Created($"api/piezas/{pieza.Id}", pieza);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = RolesTaller.AdminAuxiliar)]
    public async Task<IActionResult> Actualizar(int id, PiezaDto dto)
    {
        try { return Ok(await _piezas.ActualizarAsync(id, Mapear(dto))); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
    }

    [HttpPost("{id}/movimiento")]
    [Authorize(Roles = RolesTaller.AdminAuxiliar)]
    public async Task<IActionResult> Movimiento(int id, MovimientoDto dto)
    {
        try { return Ok(await _inventario.RegistrarMovimientoAsync(id, dto.Cantidad, dto.EsEntrada)); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { mensaje = ex.Message }); }
    }

    [HttpGet("alertas")]
    public async Task<IActionResult> Alertas() => Ok(await _inventario.ObtenerAlertasAsync());

    private static Pieza Mapear(PiezaDto d) => new()
    {
        Codigo = d.Codigo ?? string.Empty,
        Nombre = d.Nombre ?? string.Empty,
        Marca = d.Marca ?? string.Empty,
        Categoria = d.Categoria ?? string.Empty,
        Stock = d.Stock,
        PrecioUnitario = d.PrecioUnitario,
        ProveedorId = d.ProveedorId
    };
}