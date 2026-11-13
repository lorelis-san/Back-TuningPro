using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuningPro.Business.Dtos;

public class RepuestoOrdenDto
{
    public int Id { get; set; }
    public int PiezaId { get; set; }
    public string PiezaCodigo { get; set; } = string.Empty;
    public string PiezaNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public class OrdenTrabajoDto
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }
    public string ProyectoCodigo { get; set; } = string.Empty;
    public string ClienteNombre { get; set; } = string.Empty;
    public string VehiculoDescripcion { get; set; } = string.Empty;
    public string VehiculoPlaca { get; set; } = string.Empty;
    public string TecnicoNombre { get; set; } = string.Empty;
    public List<string> ProblemasReportados { get; set; } = new();
    public List<RepuestoOrdenDto> Repuestos { get; set; } = new();
    public decimal ManoObra { get; set; }
    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
}

public class RepuestoSeleccionadoDto
{
    public int PiezaId { get; set; }
    public int Cantidad { get; set; }
}

public class CrearOrdenTrabajoDto
{
    public int ProyectoId { get; set; }
    public int TecnicoId { get; set; }
    public decimal ManoObra { get; set; }
    public string? Observaciones { get; set; }
    public List<RepuestoSeleccionadoDto> Repuestos { get; set; } = new();
}

public class CambiarEstadoOrdenDto
{
    public string Estado { get; set; } = string.Empty;
}

public class TecnicoDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
}

public class PiezaCompatibleDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal PrecioUnitario { get; set; }
    public int Stock { get; set; }
}

public class DatosNuevaOrdenDto
{
    public int NumeroSugerido { get; set; }
    public string CodigoSugerido { get; set; } = string.Empty;
    public int ProyectoId { get; set; }
    public string ProyectoCodigo { get; set; } = string.Empty;
    public string ClienteNombre { get; set; } = string.Empty;
    public string VehiculoDescripcion { get; set; } = string.Empty;
    public string VehiculoPlaca { get; set; } = string.Empty;
    public List<string> ProblemasReportados { get; set; } = new();
    public List<TecnicoDto> Tecnicos { get; set; } = new();
    public List<PiezaCompatibleDto> RepuestosCompatibles { get; set; } = new();

   
}