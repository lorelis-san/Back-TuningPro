using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TuningPro.Data.Entities;

public class ProyectoProblema
{
    public int ProyectoId { get; set; }
    public int ProblemaCatalogoId { get; set; }
    public Proyectos Proyecto { get; set; } = null!;
    public ProblemaCatalogo ProblemaCatalogo { get; set; } = null!;
}
