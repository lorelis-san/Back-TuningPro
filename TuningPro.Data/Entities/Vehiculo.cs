namespace TuningPro.Data.Entities;

public class Vehiculo
{
    public int Id { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Anio { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public int? UsuarioId { get; set; }   // Usuario (rol Cliente) dueño del vehículo
}