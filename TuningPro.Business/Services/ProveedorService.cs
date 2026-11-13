using TuningPro.Data.Entities;
using TuningPro.Data.Repositories;

namespace TuningPro.Business.Services;

public interface IProveedorService
{
    Task<List<Proveedor>> ListarAsync();
    Task<Proveedor> CrearAsync(Proveedor datos);
    Task<Proveedor> ActualizarAsync(int id, Proveedor datos);
}

public class ProveedorService : IProveedorService
{
    private readonly IRepository<Proveedor> _proveedores;
    public ProveedorService(IRepository<Proveedor> proveedores) => _proveedores = proveedores;

    public async Task<List<Proveedor>> ListarAsync() =>
        (await _proveedores.GetAllAsync()).OrderBy(p => p.Nombre).ToList();

    public async Task<Proveedor> CrearAsync(Proveedor datos)
    {
        await ValidarAsync(datos, null);
        var proveedor = new Proveedor
        {
            Nombre = datos.Nombre.Trim(),
            Contacto = datos.Contacto.Trim()
        };
        await _proveedores.AddAsync(proveedor);
        await _proveedores.SaveAsync();
        return proveedor;
    }

    public async Task<Proveedor> ActualizarAsync(int id, Proveedor datos)
    {
        var proveedor = await _proveedores.GetByIdAsync(id)
                        ?? throw new KeyNotFoundException("Proveedor no encontrado.");
        await ValidarAsync(datos, id);
        proveedor.Nombre = datos.Nombre.Trim();
        proveedor.Contacto = datos.Contacto.Trim();
        _proveedores.Update(proveedor);
        await _proveedores.SaveAsync();
        return proveedor;
    }

    private async Task ValidarAsync(Proveedor datos, int? idActual)
    {
        var nombre = (datos.Nombre ?? string.Empty).Trim();
        if (nombre.Length == 0) throw new InvalidOperationException("El nombre del proveedor es obligatorio.");

        var existentes = await _proveedores.GetAllAsync();
        if (existentes.Any(p => p.Id != idActual && string.Equals(p.Nombre, nombre, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Ya existe un proveedor con ese nombre.");
    }
}