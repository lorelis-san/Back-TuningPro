using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TuningPro.Business.Services;
using TuningPro.Data.Entities;

namespace TuningPro.Api.Security;

public class JwtTokenGenerator : ITokenGenerator
{
    private readonly IConfiguration _config;
    public JwtTokenGenerator(IConfiguration config) => _config = config;

    public (string Token, DateTime Expira) Generar(Usuario u)
    {
        var jwt = _config.GetSection("Jwt");
        var expira = DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpireMinutes"] ?? "120"));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, u.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, u.NombreUsuario),
            new Claim("nombre", u.NombreCompleto),
            new Claim("role", u.Rol)
        };

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"], audience: jwt["Audience"],
            claims: claims, expires: expira, signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}