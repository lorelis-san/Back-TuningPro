using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TuningPro.Business.Reportes;

namespace TuningPro.Api.Reportes;

// PATRÓN ADAPTER: adapta la API de QuestPDF (adaptee) a la interfaz IReporteExportador del negocio
public class QuestPdfExportador : IReporteExportador
{
    public byte[] GenerarPdf(ReporteDocumento doc)
    {
        var documento = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(30);
                pagina.DefaultTextStyle(x => x.FontSize(9));

                // ---------- Cabecera ----------
                pagina.Header().Column(col =>
                {
                    col.Item().Row(fila =>
                    {
                        fila.RelativeItem().Column(c =>
                        {
                            c.Item().Text(doc.Taller).FontSize(9).FontColor(Colors.Grey.Darken2);
                            c.Item().Text(doc.Titulo).FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                            c.Item().Text(doc.Subtitulo).FontSize(10).FontColor(Colors.Grey.Darken2);
                        });
                        fila.ConstantItem(150).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Generado: {doc.Fecha:dd/MM/yyyy HH:mm}").FontSize(8);
                            c.Item().Text($"Por: {doc.GeneradoPor}").FontSize(8);
                        });
                    });
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Blue.Darken3);
                });

                // ---------- Contenido ----------
                pagina.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(14);
                    foreach (var seccion in doc.Secciones)
                        col.Item().Element(c => ComponerSeccion(c, seccion));
                });

                // ---------- Pie de página ----------
                pagina.Footer()
                    .DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1))
                    .Row(fila =>
                    {
                        fila.RelativeItem().Text($"{doc.Taller} · {doc.Titulo}");
                        fila.ConstantItem(90).AlignRight().Text(t =>
                        {
                            t.Span("Página ");
                            t.CurrentPageNumber();
                            t.Span(" de ");
                            t.TotalPages();
                        });
                    });
            });
        });

        return documento.GeneratePdf();
    }

    private static void ComponerSeccion(IContainer contenedor, ReporteSeccion seccion)
    {
        contenedor.Column(col =>
        {
            col.Spacing(6);

            if (!string.IsNullOrWhiteSpace(seccion.Titulo))
                col.Item().Text(seccion.Titulo).FontSize(12).Bold();

            // Tarjetas de resumen
            if (seccion.Resumen.Count > 0)
            {
                col.Item().Row(fila =>
                {
                    fila.Spacing(6);
                    foreach (var dato in seccion.Resumen)
                    {
                        fila.RelativeItem()
                            .Border(1).BorderColor(Colors.Grey.Lighten1).Padding(6)
                            .Column(c =>
                            {
                                c.Item().Text(dato.Valor).FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                                c.Item().Text(dato.Etiqueta).FontSize(8).FontColor(Colors.Grey.Darken2);
                            });
                    }
                });
            }

            // Tabla
            if (seccion.Tabla is { } t)
            {
                var numericas = (t.ColumnasNumericas ?? Array.Empty<int>()).ToHashSet();
                var resaltadas = (t.FilasResaltadas ?? Array.Empty<int>()).ToHashSet();

                col.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(columnas =>
                    {
                        for (var i = 0; i < t.Columnas.Length; i++)
                            columnas.RelativeColumn(t.Anchos is not null && i < t.Anchos.Length ? t.Anchos[i] : 1f);
                    });

                    tabla.Header(encabezado =>
                    {
                        for (var i = 0; i < t.Columnas.Length; i++)
                        {
                            IContainer celda = encabezado.Cell().Background(Colors.Blue.Darken3).Padding(4);
                            Alinear(celda, numericas.Contains(i))
                                .Text(t.Columnas[i]).Bold().FontColor(Colors.White);
                        }
                    });

                    for (var f = 0; f < t.Filas.Count; f++)
                    {
                        var fila = t.Filas[f];
                        var fondo = resaltadas.Contains(f)
                            ? Colors.Red.Lighten4
                            : (f % 2 == 0 ? Colors.White : Colors.Grey.Lighten4);

                        for (var i = 0; i < t.Columnas.Length; i++)
                        {
                            var valor = i < fila.Length ? fila[i] : string.Empty;
                            IContainer celda = tabla.Cell()
                                .Background(fondo)
                                .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .Padding(4);
                            Alinear(celda, numericas.Contains(i)).Text(valor);
                        }
                    }
                });
            }

            if (!string.IsNullOrWhiteSpace(seccion.Nota))
                col.Item().Text(seccion.Nota).FontSize(8).Italic().FontColor(Colors.Grey.Darken2);
        });
    }

    private static IContainer Alinear(IContainer celda, bool aLaDerecha) =>
        aLaDerecha ? celda.AlignRight() : celda.AlignLeft();
}