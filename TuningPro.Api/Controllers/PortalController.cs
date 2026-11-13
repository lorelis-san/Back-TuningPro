using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuningPro.Business.Config;
using TuningPro.Business.Services;

namespace TuningPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RolesTaller.Cliente)]
public class PortalController : ControllerBase
{
    private readonly IPortalService _portal;
    public PortalController(IPortalService portal) => _portal = portal;

    [HttpGet("mis-proyectos")]
    public async Task<IActionResult> MisProyectos()
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var usuarioId))
            return Unauthorized();

        var nombre = User.FindFirst("nombre")?.Value ?? "Cliente";
        return Ok(await _portal.ObtenerResumenAsync(usuarioId, nombre));
    }
}