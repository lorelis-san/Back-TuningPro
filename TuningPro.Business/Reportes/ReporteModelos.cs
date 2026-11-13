namespace TuningPro.Business.Reportes;

public record ReporteResumenItem(string Etiqueta, string Valor);

public record ReporteTabla(
    string[] Columnas,
    List<string[]> Filas,
    float[]? Anchos = null,
    int[]? ColumnasNumericas = null,
    int[]? FilasResaltadas = null);

public record ReporteSeccion(
    string Titulo,
    List<ReporteResumenItem> Resumen,
    ReporteTabla? Tabla,
    string? Nota = null);

public record ReporteDocumento(
    string Taller,
    string Titulo,
    string Subtitulo,
    string GeneradoPor,
    DateTime Fecha,
    List<ReporteSeccion> Secciones);

public record ArchivoReporte(byte[] Contenido, string NombreArchivo);

// Interfaz objetivo del patrón ADAPTER: el negocio solo conoce esta interfaz
public interface IReporteExportador
{
    byte[] GenerarPdf(ReporteDocumento documento);
}