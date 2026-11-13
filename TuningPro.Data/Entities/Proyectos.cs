using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuningPro.Data.Entities;

public class Proyectos
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int VehiculoId { get; set; }
    public Vehiculo Vehiculo { get; set; } = null!;
    public string Estado { get; set; } = "Registrado";
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public List<DetalleProyecto> Detalles { get; set; } = new();

    // Recepción
    public int Numero { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public int? RecepcionistaId { get; set; }
    public Usuario? Recepcionista { get; set; }
    public string? OtrosProblema { get; set; }
    public string? NotasRecepcion { get; set; }
    public ICollection<ProyectoProblema> Problemas { get; set; } = new List<ProyectoProblema>();

    public static string FormatearCodigo(int numero) => $"PRY-{numero:D6}";
}