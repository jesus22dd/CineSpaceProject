using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
namespace CineSpace.Client.Services;
public sealed class BannersService(ApiCine api)
{
    private EstadoBanners estado = new();
    public event Action? Changed;
    public string? ErrorCarga { get; private set; }
    public bool Automatico => estado.Automatico;
    public IReadOnlyList<BannerInicio> Banners => estado.Banners;
    public async Task InicializarAsync() { estado = await api.Get<EstadoBanners>("api/presentacion/banners"); Changed?.Invoke(); }
    public Task<Pagina<BannerInicio>> ConsultarAsync(ConsultaPagina f) => api.Get<Pagina<BannerInicio>>("api/presentacion/banners/pagina?" + ApiCine.Consulta(f));
    public async Task ModoAsync(bool automatico) { await api.Put<object>("api/presentacion/banners/modo", new CambiarEstado(automatico)); await InicializarAsync(); }
    public async Task<int> GuardarAsync(BannerInicio dto, bool peliculaDisponible)
    { var result = dto.Id == 0 ? await api.Post<BannerInicio>("api/presentacion/banners", dto) : await api.Put<BannerInicio>("api/presentacion/banners/" + dto.Id, dto); await InicializarAsync(); return result.Id; }
    public async Task EliminarAsync(int id) { await api.Delete("api/presentacion/banners/" + id); await InicializarAsync(); }
    public async Task MoverAsync(int id, int direccion) { await api.Post<object>($"api/presentacion/banners/{id}/mover", new MoverElemento(direccion)); await InicializarAsync(); }
    public async Task ActivarAsync(int id, bool activo, bool disponible) { await api.Put<object>($"api/presentacion/banners/{id}/estado", new CambiarEstado(activo)); await InicializarAsync(); }
    public string Fondo(Pelicula pelicula) => pelicula.ImagenPanoramicaUrl;
    public static string UrlFondo(string ruta) => PeliculaService.UrlImagen(ruta, "w1280");
    private List<BannerDestacado> Manuales(IEnumerable<Pelicula> peliculas) => estado.Banners.Where(b => b.Activo).Join(estado.Peliculas.Concat(peliculas).DistinctBy(p=>p.Id), b => b.PeliculaId, p => p.Id, (b,p) => new BannerDestacado(p,b)).ToList();
    public int ManualesValidos(IEnumerable<Pelicula> peliculas) => Manuales(peliculas).Count;
    public bool UsaSeleccionManual(IEnumerable<Pelicula> peliculas) => !Automatico && ManualesValidos(peliculas) >= 5;
    public List<BannerDestacado> Destacados(IEnumerable<Pelicula> peliculas)
    { var lista = peliculas.DistinctBy(x => x.Id).ToList(); var manuales = Manuales(lista); return !Automatico && manuales.Count >= 5 ? manuales : lista.Take(5).Select(p => new BannerDestacado(p,new() { PeliculaId = p.Id, Imagen = p.ImagenPanoramicaUrl, FocoVertical = p.FocoPanoramicaVertical })).ToList(); }
}
public sealed class EstrenosService(ApiCine api)
{
    private List<ProximoEstreno> datos = [];
    public event Action? Changed;
    public string? ErrorCarga => null;
    public IReadOnlyList<ProximoEstreno> Todos => datos;
    public IReadOnlyList<ProximoEstreno> Publicados => datos.Where(x => x.Publicado).ToList();
    public async Task InicializarAsync() { await ConsultarAsync(new() { Tamano = 12, Orden = "antiguas" }); }
    public async Task<Pagina<ProximoEstreno>> ConsultarAsync(ConsultaPagina f) { var p = await api.Get<Pagina<ProximoEstreno>>("api/presentacion/estrenos?" + ApiCine.Consulta(f)); datos = p.Items; return p; }
    public async Task GuardarAsync(ProximoEstreno dto, bool nuevo) { if (!nuevo) await api.Put<ProximoEstreno>("api/presentacion/estrenos/" + dto.Id, dto); else await api.Post<ProximoEstreno>("api/presentacion/estrenos", dto); Changed?.Invoke(); }
    public async Task EliminarAsync(int id) { await api.Delete("api/presentacion/estrenos/" + id); Changed?.Invoke(); }
}
