using TuningPro.Data.Entities;
using TuningPro.Data.Repositories;

namespace TuningPro.Business.Services;

public interface IPiezaService
{
    Task<Pieza> CrearAsync(Pieza datos);
    Task<Pieza> ActualizarAsync(int id, Pieza datos);
}

public class PiezaService : IPiezaService
{
    // Coinciden con las categorías que reconocen las estrategias de compatibilidad
    public static readonly string[] Categorias = { "Frenos", "Suspensión", "Turbo", "General" };

    private readonly IRepository<Pieza> _piezas;
    private readonly IRepository<Proveedor> _proveedores;

    public PiezaService(IRepository<Pieza> piezas, IRepository<Proveedor> proveedores)
    {
        _piezas = piezas; _proveedores = proveedores;
    }

    // RF03: registrar pieza (el stock inicial solo se define al crearla)
    public async Task<Pieza> CrearAsync(Pieza datos)
    {
        await ValidarAsync(datos, null);
        if (datos.Stock < 0) throw new InvalidOperationException("El stock inicial no puede ser negativo.");

        var pieza = new Pieza { Stock = datos.Stock };
        Copiar(datos, pieza);
        await _piezas.AddAsync(pieza);
        await _piezas.SaveAsync();
        return pieza;
    }

    // Al editar NO se toca el stock: eso se hace con entradas, salidas y asignaciones
    public async Task<Pieza> ActualizarAsync(int id, Pieza datos)
    {
        var pieza = await _piezas.GetByIdAsync(id) ?? throw new KeyNotFoundException("Pieza no encontrada.");
        await ValidarAsync(datos, id);
        Copiar(datos, pieza);
        _piezas.Update(pieza);
        await _piezas.SaveAsync();
        return pieza;
    }

    private async Task ValidarAsync(Pieza datos, int? idActual)
    {
        var codigo = (datos.Codigo ?? string.Empty).Trim().ToUpperInvariant();
        if (codigo.Length == 0) throw new InvalidOperationException("El código es obligatorio.");
        if (string.IsNullOrWhiteSpace(datos.Nombre)) throw new InvalidOperationException("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(datos.Marca)) throw new InvalidOperationException("La marca es obligatoria.");
        if (!Categorias.Contains(datos.Categoria))
            throw new InvalidOperationException("La categoría no es válida.");
        if (datos.PrecioUnitario <= 0) throw new InvalidOperationException("El precio debe ser mayor a 0.");

        if (await _proveedores.GetByIdAsync(datos.ProveedorId) is null)
            throw new InvalidOperationException("El proveedor seleccionado no existe.");

        var existentes = await _piezas.GetAllAsync();
        if (existentes.Any(p => p.Id != idActual && string.Equals(p.Codigo, codigo, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Ya existe una pieza con ese código.");
    }

    private static void Copiar(Pieza origen, Pieza destino)
    {
        destino.Codigo = origen.Codigo.Trim().ToUpperInvariant();
        destino.Nombre = origen.Nombre.Trim();
        destino.Marca = origen.Marca.Trim();
        destino.Categoria = origen.Categoria;
        destino.PrecioUnitario = origen.PrecioUnitario;
        destino.ProveedorId = origen.ProveedorId;
    }
}