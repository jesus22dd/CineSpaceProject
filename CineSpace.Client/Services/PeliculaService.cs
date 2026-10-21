using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
namespace CineSpace.Client.Services;
public sealed class PeliculaService(ApiCine api, SalaService salas)
{
    private readonly Dictionary<int, Pelicula> fichas = [];
    public Func<int, List<int>>? ExperienciasProgramadas { get; set; }
    public async Task<Pagina<Pelicula>> ConsultarAsync(ConsultaPagina f)
    { var p = await api.Get<Pagina<Pelicula>>("api/peliculas?" + ApiCine.Consulta(f)); foreach (var movie in p.Items) fichas[movie.Id] = movie; return p; }
    public Task<List<Pelicula>> OpcionesAsync() => api.Get<List<Pelicula>>("api/peliculas/opciones");
    public async Task<List<Pelicula>> ObtenerTodasLasPeliculas() => (await ConsultarAsync(new() { Tamano = 24 })).Items;
    public async Task<List<Pelicula>> ObtenerPeliculasPorGenero(string genero)
    { var id = api.Referencias.Generos.FirstOrDefault(x => x.Nombre == genero)?.Id; return (await ConsultarAsync(new() { Genero = id, Tamano = 8, Estado = "cartelera" })).Items; }
    public async Task<Pelicula> ObtenerAsync(int id) { var result = await api.Get<Pelicula>("api/peliculas/" + id); fichas[id] = result; return result; }
    public Pelicula ObtenerPeliculaPorId(int id) => fichas.GetValueOrDefault(id)!;
    public static string UrlImagen(string? valor, string tamano = "w500") => string.IsNullOrWhiteSpace(valor) ? "/images/poster-placeholder.svg" : valor.StartsWith("/images/", StringComparison.Ordinal) || valor.StartsWith("images/", StringComparison.Ordinal) || Uri.TryCreate(valor, UriKind.Absolute, out _) ? valor : "https://image.tmdb.org/t/p/" + tamano + "/" + valor.TrimStart('/');
    public Task<Pagina<TMDBMovie>> BuscarPeliculasEnTMDB(string consulta, int? anio = null, int pagina = 1) => api.Get<Pagina<TMDBMovie>>("api/peliculas/tmdb/buscar?buscar=" + Uri.EscapeDataString(consulta) + "&pagina=" + pagina + (anio.HasValue ? "&anio=" + anio : ""));
    public Task<Pelicula> ObtenerFichaTMDBAsync(int id) => api.Get<Pelicula>("api/peliculas/tmdb/" + id);
    public Task<GaleriaPelicula> ObtenerGaleriaAsync(int id, bool actualizar = false) => api.Get<GaleriaPelicula>($"api/peliculas/tmdb/{id}/galeria");
    public async Task<List<ImagenPelicula>> ObtenerFondosAsync(int id) => (await ObtenerGaleriaAsync(id)).Panoramicas.ToList();
    public async Task GuardarFichaAsync(Pelicula dto)
    { var guardada = dto.Id <= 0 ? await api.Post<Pelicula>("api/peliculas", dto) : await api.Put<Pelicula>("api/peliculas/" + dto.Id, dto); dto.Id = guardada.Id; dto.ImagenUrl = guardada.ImagenUrl; dto.ImagenPanoramicaUrl = guardada.ImagenPanoramicaUrl; dto.FechaAgregadaUtc = guardada.FechaAgregadaUtc; fichas[dto.Id] = guardada; }
    public async Task<bool> ActualizarPelicula(Pelicula dto) { await GuardarFichaAsync(dto); return true; }
    public async Task<bool> EliminarPelicula(int id) { await api.Delete("api/peliculas/" + id); fichas.Remove(id); return true; }
    public int ObtenerTipoExperienciaPelicula(int id) => ExperienciasProgramadas?.Invoke(id).FirstOrDefault() ?? 0;
    public List<int> ObtenerExperienciasDisponiblesParaPelicula(int id) => fichas.GetValueOrDefault(id)?.Experiencias ?? ExperienciasProgramadas?.Invoke(id) ?? [];
    public int ObtenerTipoSalaPelicula(int id) => fichas.TryGetValue(id, out var pelicula) && pelicula.NumeroSala > 0 ? salas.ObtenerTipoSala(pelicula.NumeroSala) : 0;
    public Task<Pagina<ProximoEstreno>> ConsultarEstrenosAsync(ConsultaPagina filtro) => api.Get<Pagina<ProximoEstreno>>("api/presentacion/estrenos?"+ApiCine.Consulta(filtro));
    public async Task<List<ProximoEstreno>> ObtenerProximosEstrenos() => (await ConsultarEstrenosAsync(new() { Tamano=12, Orden="antiguas" })).Items;
    public static string GeneroEstreno(int id) => id switch { 28 => "Acción", 12 => "Aventura", 16 => "Animación", 35 => "Comedia", 80 => "Crimen", 99 => "Documental", 18 => "Drama", 10751 => "Familiar", 14 => "Fantasía", 36 => "Historia", 27 => "Terror", 10402 => "Música", 9648 => "Misterio", 10749 => "Romance", 878 => "Ciencia ficción", 53 => "Suspenso", 10752 => "Bélico", 37 => "Wéstern", _ => "Cine" };
}
