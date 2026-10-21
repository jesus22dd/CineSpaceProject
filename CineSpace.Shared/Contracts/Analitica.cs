using CineSpace.Shared.Models;
namespace CineSpace.Shared.Contracts;
public sealed class AnaliticaCine
{
    public int Tickets { get; set; }
    public decimal Ingresos { get; set; }
    public int Salas { get; set; }
    public decimal Promedio => Tickets == 0 ? 0 : Ingresos / Tickets;
    public List<DatoGrafico> Publico { get; set; } = [];
    public List<DatoGrafico> Generos { get; set; } = [];
    public List<DatoGrafico> SalasTop { get; set; } = [];
    public List<DatoGrafico> Experiencias { get; set; } = [];
    public List<DatoGrafico> Categorias { get; set; } = [];
    public List<SerieGrafico> IngresosCinco { get; set; } = [];
    public List<SerieGrafico> IngresosMes { get; set; } = [];
    public List<string> EtiquetasCinco { get; set; } = [];
    public List<string> EtiquetasMes { get; set; } = [];
}
