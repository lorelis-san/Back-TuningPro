using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuningPro.Data.Entities;

public class DetalleOrdenTrabajo
{
    public int Id { get; set; }

    public int OrdenTrabajoId { get; set; }
    public OrdenTrabajo OrdenTrabajo { get; set; } = null!;

    public int PiezaId { get; set; }
    public Pieza Pieza { get; set; } = null!;

    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; } // copiado de Pieza al momento de agregarla
}
