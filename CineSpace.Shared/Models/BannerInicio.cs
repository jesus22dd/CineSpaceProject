namespace CineSpace.Shared.Models;

public sealed class BannerInicio
{
    public int Id { get; set; }
    public int PeliculaId { get; set; }
    public string Imagen { get; set; } = "";
    public EtiquetaBanner Etiqueta { get; set; } = EtiquetaBanner.SinEtiqueta;
    // Read older local records and backups; migrated to Etiqueta and never displayed.
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public string Mensaje { get; set; } = "";
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public string Texto { get; set; } = "";
    public int FocoVertical { get; set; } = 35;
    public bool Activo { get; set; } = true;
    public BannerInicio Copiar() => (BannerInicio)MemberwiseClone();
}

public sealed class EstadoBanners
{
    public bool Automatico { get; set; } = true;
    public List<Pelicula> Peliculas { get; set; } = [];
    public List<BannerInicio> Banners { get; set; } = new();
    public Dictionary<int, string> FondosRecuperados { get; set; } = new();
    public EstadoBanners Copiar() => new() { Automatico = Automatico, Peliculas = Peliculas.Select(p => p.Copiar()).ToList(), Banners = Banners.Select(b => b.Copiar()).ToList(), FondosRecuperados = new(FondosRecuperados) };
}

public sealed record ImagenPelicula(string Ruta, int Ancho, int Alto, double Puntuacion, string? Idioma)
{
    public string Url => $"https://image.tmdb.org/t/p/w1280/{Ruta.TrimStart('/')}";
    public string Miniatura => $"https://image.tmdb.org/t/p/w300/{Ruta.TrimStart('/')}";
}
public sealed record BannerDestacado(Pelicula Pelicula, BannerInicio Banner);
