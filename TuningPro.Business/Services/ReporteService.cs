using System.Globalization;
using TuningPro.Business.Config;
using TuningPro.Business.Reportes;
using TuningPro.Data.Entities;
using TuningPro.Data.Repositories;

namespace TuningPro.Business.Services;

public interface IReporteService
{
    Task<ArchivoReporte> InventarioAsync(string generadoPor);
    Task<ArchivoReporte> ProyectosAsync(string generadoPor);
    Task<ArchivoReporte> HistorialVehiculoAsync(int vehiculoId, string generadoPor);
}

public class ReporteService : IReporteService
{
    private const decimal TasaIgv = 0.18m;

    private readonly IReporteExportador _exportador;
    private readonly ConfiguracionTaller _config;
    private readonly IRepository<Pieza> _piezas;
    private readonly IRepository<Proveedor> _proveedores;
    private readonly IProyectoRepository _proyectos;
    private readonly IOrdenTrabajoRepository _ordenes;
    private readonly IHistorialService _historial;

    public ReporteService(IReporteExportador exportador, ConfiguracionTaller config,
        IRepository<Pieza> piezas, IRepository<Proveedor> proveedores,
        IProyectoRepository proyectos, IOrdenTrabajoRepository ordenes, IHistorialService historial)
    {
        _exportador = exportador; _config = config; _piezas = piezas; _proveedores = proveedores;
        _proyectos = proyectos; _ordenes = ordenes; _historial = historial;
    }

    // ---------- Reporte 1: inventario ----------
    public async Task<ArchivoReporte> InventarioAsync(string generadoPor)
    {
        var piezas = (await _piezas.GetAllAsync()).OrderBy(p => p.Categoria).ThenBy(p => p.Nombre).ToList();
        var proveedores = (await _proveedores.GetAllAsync()).ToDictionary(p => p.Id, p => p.Nombre);
        var minimo = _config.StockMinimo;

        var filas = piezas.Select(p => new[]
        {
            p.Codigo, p.Nombre, p.Marca, p.Categoria,
            proveedores.GetValueOrDefault(p.ProveedorId, "—"),
            p.Stock.ToString(), Soles(p.PrecioUnitario), Soles(p.Stock * p.PrecioUnitario)
        }).ToList();

        var stockBajo = piezas
            .Select((p, i) => (Pieza: p, Indice: i))
            .Where(x => x.Pieza.Stock <= minimo)
            .Select(x => x.Indice)
            .ToArray();

        var seccion = new ReporteSeccion(
            "Estado actual del inventario",
            new List<ReporteResumenItem>
            {
                new("Piezas registradas", piezas.Count.ToString()),
                new("Unidades en stock", piezas.Sum(p => p.Stock).ToString()),
                new("Valor del inventario", Soles(piezas.Sum(p => p.Stock * p.PrecioUnitario))),
                new("Con stock bajo", stockBajo.Length.ToString())
            },
            new ReporteTabla(
                new[] { "Código", "Pieza", "Marca", "Categoría", "Proveedor", "Stock", "Precio unit.", "Valor" },
                filas,
                new float[] { 1.1f, 2.6f, 1.2f, 1.3f, 1.5f, 0.8f, 1.3f, 1.4f },
                new[] { 5, 6, 7 },
                stockBajo),
            $"Las filas resaltadas tienen stock igual o menor al mínimo configurado ({minimo} unidades).");

        return Pdf(Documento("Reporte de inventario de piezas", "Piezas de alto rendimiento", generadoPor,
            new List<ReporteSeccion> { seccion }), "inventario");
    }

    // ---------- Reporte 2: proyectos y órdenes ----------
    public async Task<ArchivoReporte> ProyectosAsync(string generadoPor)
    {
        var proyectos = await _proyectos.ListarAsync();   // incluye Vehiculo

        // Una orden por proyecto (si hubiera varias, se toma la más reciente)
        var ordenes = (await _ordenes.GetTodasAsync())
            .GroupBy(o => o.ProyectoId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(o => o.FechaRegistro).First());

        var filas = new List<string[]>();
        decimal totalOrdenes = 0;

        foreach (var p in proyectos.OrderBy(p => p.Numero))
        {
            var v = p.Vehiculo;
            var codigoOrden = "—";
            var estadoOrden = "—";
            var monto = "—";

            if (ordenes.TryGetValue(p.Id, out var orden))
            {
                codigoOrden = orden.Codigo;
                estadoOrden = orden.Estado;

                // Cancelados y rechazados no muestran monto ni suman al total
                var sinMonto = p.Estado is "Cancelado" or "Rechazado";
                if (!sinMonto)
                {
                    var total = TotalOrden(orden);
                    monto = Soles(total);
                    totalOrdenes += total;
                }
            }

            filas.Add(new[]
            {
                p.Codigo,
                v is null ? $"#{p.VehiculoId}" : $"{v.Marca} {v.Modelo} ({v.Placa})",
                v?.ClienteNombre ?? string.Empty,
                p.Estado,
                p.FechaRegistro.ToString("dd/MM/yy"),
                codigoOrden,
                estadoOrden,
                monto
            });
        }

        var cancelados = proyectos.Count(p => p.Estado == "Cancelado");
        var rechazados = proyectos.Count(p => p.Estado == "Rechazado");

        var seccion = new ReporteSeccion(
            "Proyectos de personalización",
            new List<ReporteResumenItem>
            {
                new("Proyectos", proyectos.Count.ToString()),
                new("Sin orden", proyectos.Count(p => p.Estado == "Sin orden").ToString()),
                new("En proceso", proyectos.Count(p => p.Estado == "En proceso").ToString()),
                new("Finalizados", proyectos.Count(p => p.Estado == "Finalizado").ToString()),
                new("Total en órdenes", Soles(totalOrdenes))
            },
            new ReporteTabla(
                new[] { "Código", "Vehículo", "Cliente", "Estado", "Fecha", "Orden", "Estado orden", "Total" },
                filas,
                new float[] { 1.1f, 2.2f, 1.6f, 1.0f, 0.9f, 1.1f, 1.1f, 1.2f },
                new[] { 7 }),
            "El total es el monto de la orden de trabajo (repuestos + IGV + mano de obra); " +
            "si el proyecto aún no tiene orden, no se muestra monto. " +
            "Los proyectos cancelados o rechazados no muestran monto ni suman al total. " +
            $"Cancelados: {cancelados} · Rechazados: {rechazados}.");

        return Pdf(Documento("Reporte de proyectos y órdenes de trabajo", "Trazabilidad de proyectos", generadoPor,
            new List<ReporteSeccion> { seccion }), "proyectos");
    }

    // ---------- Reporte 3: historial técnico de un vehículo ----------
    public async Task<ArchivoReporte> HistorialVehiculoAsync(int vehiculoId, string generadoPor)
    {
        var h = await _historial.ObtenerAsync(vehiculoId);   // lanza KeyNotFoundException si no existe

        var secciones = new List<ReporteSeccion>
        {
            new($"{h.Marca} {h.Modelo}",
                new List<ReporteResumenItem>
                {
                    new("Placa", h.Placa),
                    new("Año", h.Anio.ToString()),
                    new("Cliente", h.Cliente),
                    new("Proyectos", h.TotalProyectos.ToString()),
                    new("Piezas instaladas", h.TotalPiezasInstaladas.ToString()),
                    new("Inversión total", Soles(h.InversionTotal))
                },
                null)
        };

        if (h.Proyectos.Count == 0)
        {
            secciones.Add(new ReporteSeccion("Modificaciones", new List<ReporteResumenItem>(), null,
                "Este vehículo aún no tiene modificaciones registradas."));
        }

        foreach (var p in h.Proyectos)
        {
            var titulo = $"Proyecto {p.Nombre} · {p.Estado} · {p.FechaRegistro:dd/MM/yyyy}";

            ReporteTabla? tabla = p.Piezas.Count == 0
                ? null
                : new ReporteTabla(
                    new[] { "Código", "Pieza", "Marca", "Categoría", "Cant.", "Precio unit.", "Subtotal" },
                    p.Piezas.Select(d => new[]
                    {
                        d.Codigo, d.Pieza, d.Marca, d.Categoria,
                        d.Cantidad.ToString(), Soles(d.PrecioUnitario), Soles(d.Subtotal)
                    }).ToList(),
                    new float[] { 1.1f, 2.6f, 1.3f, 1.4f, 0.7f, 1.3f, 1.3f },
                    new[] { 4, 5, 6 });

            string nota;
            if (p.Estado == "Cancelado")
                nota = "Proyecto cancelado: no se contabiliza en la inversión.";
            else if (p.Estado == "Rechazado")
                nota = "Proyecto rechazado: no se contabiliza en la inversión.";
            else if (p.Orden is { } o)
                nota = $"{o.Codigo} ({o.Estado}) registrada el {o.FechaRegistro:dd/MM/yyyy} · Técnico: {o.TecnicoNombre}. " +
                       $"Repuestos: {Soles(o.SubtotalRepuestos)} · IGV: {Soles(o.Igv)} · " +
                       $"Mano de obra: {Soles(o.ManoObra)} · Total: {Soles(o.Total)}";
            else
                nota = "Sin orden de trabajo registrada todavía.";

            secciones.Add(new ReporteSeccion(titulo, new List<ReporteResumenItem>(), tabla, nota));
        }

        return Pdf(Documento("Historial técnico del vehículo", $"{h.Marca} {h.Modelo} · Placa {h.Placa}",
            generadoPor, secciones), $"historial-{Limpiar(h.Placa)}");
    }

    // ---------- Ayudas ----------
    private static decimal TotalOrden(OrdenTrabajo orden)
    {
        var subtotal = orden.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);
        var igv = Math.Round(subtotal * TasaIgv, 2);
        return subtotal + igv + orden.ManoObra;
    }

    private ReporteDocumento Documento(string titulo, string subtitulo, string generadoPor,
        List<ReporteSeccion> secciones) =>
        new(_config.NombreTaller, titulo, subtitulo, generadoPor, DateTime.Now, secciones);

    private ArchivoReporte Pdf(ReporteDocumento documento, string nombreBase) =>
        new(_exportador.GenerarPdf(documento), $"{nombreBase}-{DateTime.Now:yyyyMMdd-HHmm}.pdf");

    private static string Soles(decimal monto) => "S/ " + monto.ToString("N2", CultureInfo.InvariantCulture);

    private static string Limpiar(string texto)
    {
        var limpio = new string(texto.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
        return limpio.Length == 0 ? "vehiculo" : limpio;
    }
}