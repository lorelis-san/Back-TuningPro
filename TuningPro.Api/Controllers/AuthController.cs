using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TuningPro.Api.Models;
using TuningPro.Business.Services;

namespace TuningPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        try { return Ok(await _auth.LoginAsync(dto.Usuario, dto.Password)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { mensaje = ex.Message }); }
    }

    [HttpGet("perfil")]
    [Authorize]
    public IActionResult Perfil() =>
        Ok(new { usuario = User.Identity?.Name, rol = User.FindFirst("role")?.Value });
}