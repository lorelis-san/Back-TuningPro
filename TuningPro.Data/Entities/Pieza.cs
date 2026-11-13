namespace TuningPro.Data.Entities;
public class Pieza
{
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty; // Frenos, Suspensión, Turbo
        public int Stock { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int ProveedorId { get; set; }
}
