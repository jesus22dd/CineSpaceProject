using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
namespace CineSpace.Client.Services;
public sealed class CatalogosService(ApiCine api)
{
    public event Action? Changed;
    public string? ErrorCarga { get; private set; }
    public IReadOnlyList<EtiquetaCatalogo> Categorias => api.Referencias.Categorias;
    public IReadOnlyList<EtiquetaCatalogo> Generos => api.Referencias.Generos;
    public Task<List<GeneroDisponible>> GenerosDisponiblesAsync() => api.Get<List<GeneroDisponible>>("api/catalogos/generos/disponibles");
    public IReadOnlyList<Distribuidora> Distribuidoras => api.Referencias.Distribuidoras;
    public IReadOnlyList<ComboConfiteria> Combos { get; private set; } = [];
    public Func<int, bool>? DistribuidoraEnUso { get; set; }
    public string DistribuidoraNombre(int? id) => Distribuidoras.FirstOrDefault(x => x.Id == id)?.Nombre ?? "Sin asignar";
    public string CategoriaNombre(int id) => Categorias.FirstOrDefault(x => x.Id == id)?.Nombre ?? "Sin categoria";
    public int UsoCategoria(int id) => api.Referencias.UsosCategorias.GetValueOrDefault(id);
    public string ResolverGenero(string nombre, int peliculaId = 0) => Generos.FirstOrDefault(x => x.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase))?.Nombre ?? nombre;
    public IReadOnlyList<string> ResolverGeneros(Pelicula pelicula) => pelicula.ListaGeneros.ToList();
    public void AplicarGenero(Pelicula pelicula) { pelicula.Generos = ResolverGeneros(pelicula).ToList(); pelicula.Genero = pelicula.Generos.FirstOrDefault() ?? ""; }
    public List<string> ValidarGeneros(IEnumerable<string> generos) => generos.Select(x => Generos.FirstOrDefault(g => g.Nombre.Equals(x, StringComparison.OrdinalIgnoreCase))?.Nombre ?? throw new ArgumentException("Selecciona generos del catalogo.")).Distinct().ToList();
    private async Task Cambiado() { await api.ReferenciasAsync(); ErrorCarga = null; Changed?.Invoke(); }
    public async Task<Pagina<ComboConfiteria>> ConsultarCombosAsync(ConsultaPagina f) { var p = await api.Get<Pagina<ComboConfiteria>>("api/catalogos/combos?" + ApiCine.Consulta(f)); Combos = p.Items; return p; }
    public Task<Pagina<Distribuidora>> ConsultarDistribuidorasAsync(ConsultaPagina f) => api.Get<Pagina<Distribuidora>>("api/catalogos/distribuidoras?" + ApiCine.Consulta(f));
    public Task<Pagina<EtiquetaCatalogo>> ConsultarEtiquetasAsync(bool genero, ConsultaPagina f) => api.Get<Pagina<EtiquetaCatalogo>>("api/catalogos/" + (genero ? "generos?" : "categorias?") + ApiCine.Consulta(f));
    public async Task<int> GuardarDistribuidoraAsync(Distribuidora dto) { var r = dto.Id == 0 ? await api.Post<int>("api/catalogos/distribuidoras", dto) : await api.Put<int>("api/catalogos/distribuidoras/" + dto.Id, dto); await Cambiado(); return r; }
    public async Task EliminarDistribuidoraAsync(int id) { await api.Delete("api/catalogos/distribuidoras/" + id); await Cambiado(); }
    public async Task<int> GuardarEtiquetaAsync(bool genero, EtiquetaCatalogo dto) { var ruta = "api/catalogos/" + (genero ? "generos" : "categorias"); var r = dto.Id == 0 ? await api.Post<int>(ruta, dto) : await api.Put<int>(ruta + "/" + dto.Id, dto); await Cambiado(); return r; }
    public async Task EliminarEtiquetaAsync(bool genero, int id, bool enUso = false) { await api.Delete("api/catalogos/" + (genero ? "generos/" : "categorias/") + id); await Cambiado(); }
    public async Task<int> GuardarComboAsync(ComboConfiteria dto) { var r = dto.Id == 0 ? await api.Post<int>("api/catalogos/combos", dto) : await api.Put<int>("api/catalogos/combos/" + dto.Id, dto); await Cambiado(); return r; }
    public async Task EliminarComboAsync(int id) { await api.Delete("api/catalogos/combos/" + id); await Cambiado(); }
}
