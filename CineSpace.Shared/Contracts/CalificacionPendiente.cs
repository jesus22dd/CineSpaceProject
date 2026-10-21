namespace CineSpace.Shared.Contracts;

public sealed class CalificacionPendiente
{
    public string FuncionId { get; set; } = "";
    public int PeliculaId { get; set; }
    public string NombrePelicula { get; set; } = "";
    public string ImagenPanoramicaUrl { get; set; } = "";
    public DateTime FechaFuncion { get; set; }
    public string Sala { get; set; } = "";
}
