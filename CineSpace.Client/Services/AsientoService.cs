using CineSpace.Shared.Models;
namespace CineSpace.Client.Services;

public sealed class AsientoService(SalaService salas)
{
    public (bool valido, string mensaje, bool esError) ValidarCantidadEntradas(int mayores, int menores, int asientos) => mayores + menores == asientos
        ? (true,"",false) : (false,"Selecciona una butaca por cada entrada.",true);
    public (int mayores,int menores) AjustarCantidadEntradas(int mayores,int menores,int asientos) => (Math.Min(mayores,asientos),Math.Min(menores,Math.Max(0,asientos-mayores)));
    public decimal CalcularPrecioTotal(int pelicula,int sala,List<string> asientos,int mayores,int menores) => salas.ObtenerSala(sala).PrecioFinal*(mayores+menores);
    public (decimal precioAdulto,decimal precioNino) ObtenerPrecioPorTipoAsiento(int sala,CategoriaAsiento categoria,int experiencia) => (salas.ObtenerSala(sala).PrecioFinal,salas.ObtenerSala(sala).PrecioFinal);
    public Dictionary<CategoriaAsiento,(decimal adulto,decimal nino)> ObtenerDesglosePreciosPorCategoria(int sala,int experiencia) => Enum.GetValues<CategoriaAsiento>().ToDictionary(x=>x,x=>ObtenerPrecioPorTipoAsiento(sala,x,experiencia));
    public (decimal adulto,decimal nino) CalcularPrecioAsiento(int sala,string asiento,int experiencia) => ObtenerPrecioPorTipoAsiento(sala,salas.ObtenerCategoriaAsiento(sala,asiento),experiencia);
    public Dictionary<CategoriaAsiento,List<string>> AgruparAsientosPorCategoria(int sala,List<string> asientos) => asientos.GroupBy(x=>salas.ObtenerCategoriaAsiento(sala,x)).ToDictionary(x=>x.Key,x=>x.ToList());
}
