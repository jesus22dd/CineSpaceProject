namespace CineSpace.Shared.Models;

public sealed record FiltroVentas(string Periodo = "hoy", string TipoSala = "todos", string Experiencia = "todos", string Genero = "todos");
public sealed record DatoGrafico(string Nombre, decimal Valor, int Ventas = 0, int Tickets = 0, decimal? Ingresos = null, int? Capacidad = null);
public sealed record SerieGrafico(string Nombre, IReadOnlyList<decimal> Valores);

// Historical sale lines keep their category and price, even when the catalogue changes.
public sealed class VentaConfiteria
{
    public string NumeroVenta { get; set; } = "";
    public DateTime FechaVenta { get; set; }
    public List<LineaVentaConfiteria> Lineas { get; set; } = new();
    public decimal Total => Lineas.Sum(x => x.Cantidad * x.PrecioUnitario);
    public VentaConfiteria Copiar() => new() { NumeroVenta = NumeroVenta, FechaVenta = FechaVenta, Lineas = Lineas.Select(x => x with { }).ToList() };
}
public sealed record LineaVentaConfiteria(string Producto, string Categoria, int Cantidad, decimal PrecioUnitario);
