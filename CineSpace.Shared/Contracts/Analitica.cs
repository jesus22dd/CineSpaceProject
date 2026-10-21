using CineSpace.Shared.Models;
namespace CineSpace.Shared.Contracts;
public sealed class AnaliticaCine
{
    public int Tickets { get; set; }
    public decimal Ingresos { get; set; }
    public int Salas { get; set; }
    public decimal Promedio => Tickets == 0 ? 0 : Ingresos / Tickets;
    public List<DatoGrafico> PeliculasTop { get; set; } = [];
    public List<DatoGrafico> Generos { get; set; } = [];
    public List<DatoGrafico> SalasTop { get; set; } = [];
    public List<DatoGrafico> Experiencias { get; set; } = [];
    public List<DatoGrafico> Categorias { get; set; } = [];
    public List<SerieGrafico> IngresosCinco { get; set; } = [];
    public List<SerieGrafico> IngresosMes { get; set; } = [];
    public List<string> EtiquetasCinco { get; set; } = [];
    public List<string> EtiquetasMes { get; set; } = [];
}

public sealed class OcupacionDashboard
{
    public DateOnly Fecha { get; set; }
    public List<DatoGrafico> Estados { get; set; } = [];
    public List<DatoGrafico> Horarios { get; set; } = [];
    public int Funciones => (int)Estados.Sum(x => x.Valor);
    public int Capacidad => Estados.Sum(x => x.Capacidad ?? 0);
    public int Vendidas => Estados.Sum(x => x.Tickets);
    public decimal Porcentaje => Capacidad == 0 ? 0 : Vendidas * 100m / Capacidad;
}
