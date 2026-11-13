namespace TuningPro.Data.Entities;

public class OrdenTrabajo
{
    public int Id { get; set; }
    public int Numero { get; set; }
    public string Codigo { get; set; } = string.Empty;

    public int ProyectoId { get; set; }
    public Proyectos Proyecto { get; set; } = null!;

    public int TecnicoId { get; set; }
    public Usuario Tecnico { get; set; } = null!;

    public string Estado { get; set; } = "Registrado"; // Registrado, En proceso, Finalizado

    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public decimal ManoObra { get; set; }
    public string? Observaciones { get; set; }

    public List<DetalleOrdenTrabajo> Detalles { get; set; } = new();

    public static string FormatearCodigo(int numero) => $"OT-{numero:D6}";
}