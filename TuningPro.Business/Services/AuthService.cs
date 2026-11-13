using TuningPro.Business.Config;
using TuningPro.Data.Entities;
using TuningPro.Data.Repositories;

namespace TuningPro.Business.Services;

public record LoginResultado(string Token, string NombreUsuario, string NombreCompleto, string Rol, DateTime Expira);

// La implementación vive en la capa Api (infraestructura) para no acoplar Business a JWT
public interface ITokenGenerator
{
    (string Token, DateTime Expira) Generar(Usuario usuario);
}

public interface IAuthService
{
    Task<LoginResultado> LoginAsync(string usuario, string password);
    Task SembrarUsuariosAsync();
}

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly ITokenGenerator _tokens;

    public AuthService(IUsuarioRepository usuarios, ITokenGenerator tokens)
    {
        _usuarios = usuarios; _tokens = tokens;
    }

    public async Task<LoginResultado> LoginAsync(string usuario, string password)
    {
        var u = await _usuarios.GetByNombreUsuarioAsync((usuario ?? string.Empty).Trim());

        if (u is null || !u.Activo || !BCrypt.Net.BCrypt.Verify(password ?? string.Empty, u.PasswordHash))
            throw new UnauthorizedAccessException("Usuario o contraseña incorrectos.");

        var (token, expira) = _tokens.Generar(u);
        return new LoginResultado(token, u.NombreUsuario, u.NombreCompleto, u.Rol, expira);
    }

    // Crea los usuarios de demostración que aún no existan
    public async Task SembrarUsuariosAsync()
    {
        var iniciales = new[]
        {
            (Usuario: "admin",     Nombre: "Administrador del Taller", Clave: "Admin123*",     Rol: RolesTaller.Administrador),
            (Usuario: "tecnico",   Nombre: "Carlos Mecánico",          Clave: "Tecnico123*",   Rol: RolesTaller.Tecnico),
            (Usuario: "auxiliar",  Nombre: "Ana Inventario",           Clave: "Auxiliar123*",  Rol: RolesTaller.Auxiliar),
            (Usuario: "recepcion", Nombre: "Rosa Recepción",           Clave: "Recepcion123*", Rol: RolesTaller.Recepcionista),
            (Usuario: "cliente",   Nombre: "Juan Pérez (Cliente)",     Clave: "Cliente123*",   Rol: RolesTaller.Cliente),
            (Usuario: "maria",     Nombre: "María Torres (Cliente)",   Clave: "Maria123*",     Rol: RolesTaller.Cliente),
        };

        var hayCambios = false;
        foreach (var i in iniciales)
        {
            if (await _usuarios.GetByNombreUsuarioAsync(i.Usuario) is not null) continue;

            await _usuarios.AddAsync(new Usuario
            {
                NombreUsuario = i.Usuario,
                NombreCompleto = i.Nombre,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(i.Clave),
                Rol = i.Rol,
                Activo = true
            });
            hayCambios = true;
        }

        if (hayCambios) await _usuarios.SaveAsync();
    }
}