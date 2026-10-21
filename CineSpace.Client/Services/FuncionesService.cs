using System.Text.Json;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
namespace CineSpace.Client.Services;
public sealed class FuncionesService(ApiCine api, PeliculaService peliculas, SalaService salas, TicketSalesService ventas)
{
    private readonly Dictionary<int, List<FuncionCine>> funciones = [];
    private readonly Dictionary<string, HashSet<string>> ocupados = [];
    private List<ProgramacionSala> programaciones = [];
    public IReadOnlyList<ProgramacionSala> Programaciones => programaciones;
    public IReadOnlyList<FuncionCine> Todas => funciones.Values.SelectMany(x => x).ToList();
    public FuncionesService Inicializar() { peliculas.ExperienciasProgramadas = id => Disponibles(id).Select(x => x.ExperienciaId).Distinct().ToList(); return this; }
    public Task InicializarAsync() { Inicializar(); return Task.CompletedTask; }
    public async Task CargarPeliculaAsync(int id) { funciones[id] = await api.Get<List<FuncionCine>>("api/funciones/pelicula/" + id); }
    public async Task CargarOcupadosAsync(string id) { ocupados[id] = (await api.Get<List<string>>("api/funciones/" + id + "/ocupados")).ToHashSet(); var funcion = Obtener(id); if (funcion is not null) await salas.CargarPlanoAsync(funcion.NumeroSala); }
    public IReadOnlyList<FuncionCine> Disponibles(int id, int? experiencia = null) => funciones.GetValueOrDefault(id, []).Where(x => !experiencia.HasValue || x.ExperienciaId == experiencia).ToList();
    public FuncionCine? Obtener(string? id) => Todas.FirstOrDefault(x => x.Id == id);
    public bool TieneFunciones(int id) => funciones.GetValueOrDefault(id, []).Count > 0;
    public bool TieneVentas(string id) => ocupados.GetValueOrDefault(id, []).Count > 0;
    public HashSet<string> Ocupados(string id) => ocupados.GetValueOrDefault(id, []);
    public int ButacasDisponibles(string id) { var funcion = Obtener(id); return funcion is null ? 0 : funcion.Capacidad - (ocupados.TryGetValue(id, out var vendidos) ? vendidos.Count : funcion.ButacasOcupadas); }
    public void ValidarCompra(string id, IEnumerable<string> seleccion) { if (seleccion.Any(x => Ocupados(id).Contains(x))) throw new ArgumentException("Una de las butacas ya esta ocupada."); }
    public async Task<Pagina<ProgramacionSala>> ConsultarAsync(ConsultaPagina f) { var p = await api.Get<Pagina<ProgramacionSala>>("api/funciones?" + ApiCine.Consulta(f)); programaciones = p.Items; return p; }
    public async Task GuardarAsync(ProgramacionSala dto)
    {
        var existente = programaciones.Any(x => x.Id == dto.Id);
        var result = existente ? await api.Put<ProgramacionSala>("api/funciones/" + dto.Id, dto) : await api.Post<ProgramacionSala>("api/funciones", dto);
        dto.Id = result.Id; await CargarPeliculaAsync(dto.PeliculaId);
    }
    public async Task EliminarAsync(string id) { await api.Delete("api/funciones/" + id); var afectadas = programaciones.Where(x => x.Id == id).Select(x => x.PeliculaId).Distinct().ToArray(); programaciones.RemoveAll(x => x.Id == id); foreach (var movie in afectadas) funciones.Remove(movie); }
    public async Task<VentaTicket> RegistrarCompraAsync(VentaTicket dto)
    {
        var guardada = await api.Post<VentaTicket>("api/ventas", dto);
        ventas.RegistrarConfirmada(guardada);
        if (guardada.FuncionId is not null) ocupados[guardada.FuncionId] = Ocupados(guardada.FuncionId).Concat(guardada.Asientos).ToHashSet();
        return guardada;
    }
}
