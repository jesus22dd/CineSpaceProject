using System.Net.Http.Json;
using System.Text.Json;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CineSpace.Server.Services;

public interface ITmdbService
{
    Task<Pagina<TMDBMovie>> BuscarAsync(string texto, int? anio, int pagina, CancellationToken ct);
    Task<Pelicula> FichaAsync(int id, CancellationToken ct);
    Task<GaleriaPelicula> GaleriaAsync(int id, CancellationToken ct);
}

public sealed class TmdbService(HttpClient http, IConfiguration configuration, IMemoryCache cache) : ITmdbService
{
    private string Ruta(string ruta)
    {
        var key = configuration["Tmdb:ApiKey"];
        if (string.IsNullOrWhiteSpace(key)) throw new OperacionCineException("TMDB no esta configurado.");
        return "https://api.themoviedb.org/3/" + ruta + (ruta.Contains('?') ? "&" : "?") + "api_key=" + Uri.EscapeDataString(key) + "&language=es-ES";
    }

    public async Task<Pagina<TMDBMovie>> BuscarAsync(string texto, int? anio, int pagina, CancellationToken ct)
    {
        texto = ReglasCine.Texto(texto, 100, "el titulo");
        if (anio.HasValue && anio is < 1900 or > 2200) throw new ArgumentException("El anio de busqueda no es valido.");
        pagina = Math.Clamp(pagina, 1, 500);
        var ruta = "search/movie?include_adult=false&query=" + Uri.EscapeDataString(texto) + "&page=" + pagina + (anio.HasValue ? "&year=" + anio : "");
        var resultado = await ConsultarAsync<TMDBResponse>(ruta, ct) ?? new();
        return new(resultado.results, Math.Min(resultado.total_results, 10000), pagina, 20);
    }

    public async Task<Pelicula> FichaAsync(int id, CancellationToken ct)
    {
        if (id < 1) throw new ArgumentException("La pelicula de TMDB no es valida.");
        var cacheKey = "tmdb:ficha:" + id;
        if (cache.TryGetValue<Pelicula>(cacheKey, out var existente)) return existente!.Copiar();
        var json = await ConsultarAsync<JsonElement>($"movie/{id}?append_to_response=videos", ct);
        static string Texto(JsonElement j, string key) => j.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
        var ficha = new Pelicula
        {
            Id = 0, TmdbId = id, Titulo = Texto(json, "title"), Sinopsis = Texto(json, "overview"), ImagenUrl = Texto(json, "poster_path"),
            ImagenPanoramicaUrl = Texto(json, "backdrop_path"), Genero = "Drama", HorariosDisponibles = [], ImagenManual = false
        };
        if (DateTime.TryParseExact(Texto(json, "release_date"), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var estreno)) ficha.FechaEstreno = estreno;
        if (json.TryGetProperty("runtime", out var runtime) && runtime.TryGetInt32(out var minutos) && minutos > 0) ficha.DuracionMinutos = minutos;
        if (json.TryGetProperty("genres", out var generos)) ficha.Generos = generos.EnumerateArray().Select(g => Texto(g, "name")).Where(g => g.Length > 0).ToList();
        if (json.TryGetProperty("spoken_languages", out var idiomas)) ficha.IdiomasDisponibles = idiomas.EnumerateArray().Select(i => Texto(i, "name")).Where(i => i.Length > 0).ToList();
        if (json.TryGetProperty("videos", out var videos) && videos.TryGetProperty("results", out var resultados))
        {
            var trailer = resultados.EnumerateArray().FirstOrDefault(v => Texto(v, "site") == "YouTube" && Texto(v, "type") == "Trailer");
            if (trailer.ValueKind == JsonValueKind.Object) ficha.TrailerUrl = "https://www.youtube.com/watch?v=" + Texto(trailer, "key");
        }
        cache.Set(cacheKey, ficha.Copiar(), TimeSpan.FromMinutes(30));
        return ficha;
    }

    public async Task<GaleriaPelicula> GaleriaAsync(int id, CancellationToken ct)
    {
        var cacheKey = "tmdb:galeria:" + id;
        if (cache.TryGetValue<GaleriaPelicula>(cacheKey, out var existente)) return existente!;
        var json = await ConsultarAsync<JsonElement>($"movie/{id}/images?include_image_language=es,en,null", ct);
        static List<ImagenPelicula> Imagenes(JsonElement j, string propiedad) => j.TryGetProperty(propiedad, out var valores) ? valores.EnumerateArray().Take(200).Select(v => new ImagenPelicula(
            v.GetProperty("file_path").GetString() ?? "", v.GetProperty("width").GetInt32(), v.GetProperty("height").GetInt32(),
            v.GetProperty("vote_average").GetDouble(), v.TryGetProperty("iso_639_1", out var idioma) && idioma.ValueKind == JsonValueKind.String ? idioma.GetString() : null)).ToList() : [];
        var galeria = new GaleriaPelicula(Imagenes(json, "posters"), Imagenes(json, "backdrops"));
        cache.Set(cacheKey, galeria, TimeSpan.FromMinutes(30)); return galeria;
    }

    private async Task<T?> ConsultarAsync<T>(string ruta, CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync(Ruta(ruta), ct);
            if (!response.IsSuccessStatusCode) throw new ArgumentException("TMDB no pudo completar la consulta. Intentalo de nuevo.");
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        }
        catch (HttpRequestException) { throw new ArgumentException("No pudimos conectar con TMDB."); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { throw new ArgumentException("TMDB tardo demasiado en responder."); }
    }
}
