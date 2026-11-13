using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuningPro.Business.Config;
using TuningPro.Business.Reportes;
using TuningPro.Business.Services;

namespace TuningPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesTaller.SoloAdmin)]
public class ReportesController : ControllerBase
{
    private readonly IReporteService _service;
    public ReportesController(IReporteService service) => _service = service;

    private string Usuario => User.FindFirst("nombre")?.Value ?? User.Identity?.Name ?? "Sistema";

    private FileContentResult Pdf(ArchivoReporte archivo) =>
        File(archivo.Contenido, "application/pdf", archivo.NombreArchivo);

    [HttpGet("inventario")]
    public async Task<IActionResult> Inventario() => Pdf(await _service.InventarioAsync(Usuario));

    [HttpGet("proyectos")]
    public async Task<IActionResult> Proyectos() => Pdf(await _service.ProyectosAsync(Usuario));

    [HttpGet("historial/{vehiculoId}")]
    public async Task<IActionResult> Historial(int vehiculoId)
    {
        try { return Pdf(await _service.HistorialVehiculoAsync(vehiculoId, Usuario)); }
        catch (KeyNotFoundException ex) { return NotFound(new { mensaje = ex.Message }); }
    }
}