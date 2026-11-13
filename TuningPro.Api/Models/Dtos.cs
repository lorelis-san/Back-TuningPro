namespace TuningPro.Api.Models;

public record MovimientoDto(int Cantidad, bool EsEntrada);
public record CompatibilidadDto(int VehiculoId, int PiezaId);
public record CrearProyectoDto(string Nombre, int VehiculoId);
public record AsignarPiezaDto(int PiezaId, int Cantidad);
public record CambiarEstadoDto(string Estado);
public record LoginDto(string Usuario, string Password);
public record GenerarOrdenDto(int ProyectoId, decimal ManoObra, string? Observaciones);
public record VehiculoDto(string? Placa, string? Marca, string? Modelo, int Anio, string? ClienteNombre, int? UsuarioId);
public record ProveedorDto(string? Nombre, string? Contacto);
public record PiezaDto(string? Codigo, string? Nombre, string? Marca, string? Categoria, int Stock, decimal PrecioUnitario, int ProveedorId);