
namespace TuningPro.Data.Entities;

public class DetalleProyecto
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }
    public int PiezaId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}
