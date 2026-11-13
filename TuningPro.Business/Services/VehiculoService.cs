using TuningPro.Business.Config;
using TuningPro.Data.Entities;
using TuningPro.Data.Repositories;

namespace TuningPro.Business.Services;

public record ClienteCuentaDto(int Id, string NombreUsuario, string NombreCompleto);

public interface IVehiculoService
{
    Task<List<Vehiculo>> ListarAsync(string? buscar);
    Task<List<ClienteCuentaDto>> ListarClientesAsync();
    Task<Vehiculo> CrearAsync(Vehiculo datos);
    Task<Vehiculo> ActualizarAsync(int id, Vehiculo datos);
}

public class VehiculoService : IVehiculoService
{
    private readonly IRepository<Vehiculo> _vehiculos;
    private readonly IUsuarioRepository _usuarios;

    public VehiculoService(IRepository<Vehiculo> vehiculos, IUsuarioRepository usuarios)
    {
        _vehiculos = vehiculos; _usuarios = usuarios;
    }

    // RF02: buscar y listar por placa, marca, modelo o cliente
    public async Task<List<Vehiculo>> ListarAsync(string? buscar)
    {
        var todos = await _vehiculos.GetAllAsync();
        var texto = (buscar ?? string.Empty).Trim();

        var filtrados = string.IsNullOrEmpty(texto)
            ? todos
            : todos.Where(v => Contiene(v.Placa, texto) || Contiene(v.Marca, texto)
                            || Contiene(v.Modelo, texto) || Contiene(v.ClienteNombre, texto)).ToList();

        return filtrados.OrderBy(v => v.ClienteNombre).ThenBy(v => v.Placa).ToList();
    }

    public async Task<List<ClienteCuentaDto>> ListarClientesAsync()
    {
        var clientes = await _usuarios.GetByRolAsync(RolesTaller.Cliente);
        return clientes.Select(u => new ClienteCuentaDto(u.Id, u.NombreUsuario, u.NombreCompleto)).ToList();
    }

    // RF01: registrar vehículo
    public async Task<Vehiculo> CrearAsync(Vehiculo datos)
    {
        await ValidarAsync(datos, null);
        var vehiculo = new Vehiculo();
        Copiar(datos, vehiculo);
        await _vehiculos.AddAsync(vehiculo);
        await _vehiculos.SaveAsync();
        return vehiculo;
    }

    public async Task<Vehiculo> ActualizarAsync(int id, Vehiculo datos)
    {
        var vehiculo = await _vehiculos.GetByIdAsync(id)
                       ?? throw new KeyNotFoundException("Vehículo no encontrado.");
        await ValidarAsync(datos, id);
        Copiar(datos, vehiculo);
        _vehiculos.Update(vehiculo);
        await _vehiculos.SaveAsync();
        return vehiculo;
    }

    private async Task ValidarAsync(Vehiculo datos, int? idActual)
    {
        var placa = (datos.Placa ?? string.Empty).Trim().ToUpperInvariant();
        if (placa.Length == 0) throw new InvalidOperationException("La placa es obligatoria.");
        if (string.IsNullOrWhiteSpace(datos.Marca)) throw new InvalidOperationException("La marca es obligatoria.");
        if (string.IsNullOrWhiteSpace(datos.Modelo)) throw new InvalidOperationException("El modelo es obligatorio.");
        if (string.IsNullOrWhiteSpace(datos.ClienteNombre)) throw new InvalidOperationException("El nombre del cliente es obligatorio.");

        var maxAnio = DateTime.Now.Year + 1;
        if (datos.Anio < 1950 || datos.Anio > maxAnio)
            throw new InvalidOperationException($"El año debe estar entre 1950 y {maxAnio}.");

        var existentes = await _vehiculos.GetAllAsync();
        if (existentes.Any(v => v.Id != idActual && string.Equals(v.Placa, placa, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Ya existe un vehículo con esa placa.");

        if (datos.UsuarioId.HasValue)
        {
            var cuenta = await _usuarios.GetByIdAsync(datos.UsuarioId.Value);
            if (cuenta is null || cuenta.Rol != RolesTaller.Cliente || !cuenta.Activo)
                throw new InvalidOperationException("La cuenta de portal seleccionada no es válida.");
        }
    }

    private static void Copiar(Vehiculo origen, Vehiculo destino)
    {
        destino.Placa = origen.Placa.Trim().ToUpperInvariant();
        destino.Marca = origen.Marca.Trim();
        destino.Modelo = origen.Modelo.Trim();
        destino.Anio = origen.Anio;
        destino.ClienteNombre = origen.ClienteNombre.Trim();
        destino.UsuarioId = origen.UsuarioId;
    }

    private static bool Contiene(string origen, string texto) =>
        origen.Contains(texto, StringComparison.OrdinalIgnoreCase);
}