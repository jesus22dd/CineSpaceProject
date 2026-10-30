using System.Text.Json.Serialization;

namespace CineSpace.Shared.Contracts;

public sealed class FilaResumenVenta
{
    public Guid Id { get; set; }
    public DateTime FechaVenta { get; set; }
    public string Pelicula { get; set; } = "";
    public List<string> Generos { get; set; } = [];
    [JsonIgnore] public string GenerosTexto => string.Join(" · ", Generos);
    public int NumeroSala { get; set; }
    public string TipoSalaNombre { get; set; } = "";
    public string ExperienciaNombre { get; set; } = "";
    public int TicketsMayores { get; set; }
    public int TicketsMenores { get; set; }
    public int CantidadTickets { get; set; }
    public decimal TotalVenta { get; set; }
}

public sealed record TotalesResumenVentas(int Ventas, int TicketsMayores, int TicketsMenores, decimal Ingresos)
{
    public int Tickets => TicketsMayores + TicketsMenores;
    public static TotalesResumenVentas Vacio => new(0, 0, 0, 0);
}

public sealed record ResultadoResumenVentas(Pagina<FilaResumenVenta> Pagina, TotalesResumenVentas Totales);
