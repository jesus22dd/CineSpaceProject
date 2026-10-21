using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
namespace CineSpace.Client.Services;
public sealed class SalaService(ApiCine api)
{
    private readonly Dictionary<int, bool> salasEnUso = [];
    public bool SalaEnUso(int numero) => salasEnUso.GetValueOrDefault(numero);
    public event Action? Changed;
    public string? ErrorCarga => null;
    private readonly Dictionary<int, PlanoSala> planos = [];
    private readonly Dictionary<int, int> experienciasPelicula = [];
    public IReadOnlyList<SalaCine> SalasGestion => api.Referencias.Salas;
    public IReadOnlyList<DisenoButacas> Disenos => api.Referencias.Disenos;
    public IReadOnlyList<ExperienciaCine> Experiencias => api.Referencias.Experiencias;
    public IReadOnlyList<TipoSalaCine> TiposSala => api.Referencias.TiposSala;
    public string NombreTipoSala(int id) => TiposSala.FirstOrDefault(x => x.Id == id)?.Nombre ?? "";
    public string NombreExperiencia(int id) => Experiencias.FirstOrDefault(x => x.Id == id)?.Nombre ?? "";
    public string DescripcionExperiencia(int id) => Experiencias.FirstOrDefault(x => x.Id == id)?.Descripcion ?? "";
    public SalaCine ObtenerSala(int numero) => SalasGestion.FirstOrDefault(x => x.Numero == numero) ?? throw new ArgumentException("La sala ya no existe.");
    public string ObtenerNombreSala(int numero) => ObtenerSala(numero).Nombre;
    public bool DisenoEnUso(int id) => api.Referencias.UsosDisenos.GetValueOrDefault(id) > 0;
    public async Task CargarPlanoAsync(int numero) { planos[numero] = await api.Get<PlanoSala>($"api/catalogos/salas/{numero}/plano"); }
    public async Task<PlanoSala> CargarDisenoAsync(int id)
    {
        var result = await api.Get<PlanoSala>($"api/catalogos/disenos/{id}");
        var existing = api.Referencias.Disenos.FirstOrDefault(x => x.Id == id); if (existing is not null) existing.Plano = result; return result;
    }
    public PlanoSala ObtenerPlano(int numero) => planos.TryGetValue(numero, out var plano) ? plano.Copiar() : throw new InvalidOperationException("Carga el plano de la sala antes de elegir butacas.");
    public ConfiguracionSala ObtenerConfiguracionSala(int numero)
    {
        var sala = ObtenerSala(numero); var plano = planos.GetValueOrDefault(numero);
        var diseno = Disenos.FirstOrDefault(x => x.Id == sala.DisenoId)?.Plano;
        var config = new ConfiguracionSala(numero) { Nombre = sala.Nombre, TipoSala = sala.Tipo, NumeroFilas = plano?.Filas ?? diseno?.Filas ?? 0, AsientosPorFila = plano?.Columnas ?? diseno?.Columnas ?? 0, ExperienciasDisponibles = sala.Experiencias.ToList(),
            MapaAsientosPorCategoria = Enum.GetValues<CategoriaAsiento>().ToDictionary(c => c, c => plano?.Asientos.Where(a => plano.Categorias.GetValueOrDefault(a) == c).ToList() ?? []) };
        return config;
    }
    public IReadOnlyList<ConfiguracionSala> ObtenerSalas() => SalasGestion.Select(x => ObtenerConfiguracionSala(x.Numero)).ToList();
    public int ObtenerTipoSala(int numero) => ObtenerSala(numero).Tipo;
    public int ObtenerTipoSalaParaExperiencia(int numero, int experiencia) => ObtenerTipoSala(numero);
    public List<int> ObtenerExperienciasDisponibles(int numero) => ObtenerSala(numero).Experiencias.ToList();
    public void EstablecerExperienciaPelicula(int pelicula, int experiencia) => experienciasPelicula[pelicula] = experiencia;
    public int ObtenerExperienciaPelicula(int pelicula) => experienciasPelicula.GetValueOrDefault(pelicula);
    public bool ExperienciaDisponibleEnSala(int numero, int experiencia) => SalasGestion.Any(x => x.Numero == numero && x.Experiencias.Contains(experiencia));
    public List<string> ObtenerTodosLosAsientos(int numero) => ObtenerPlano(numero).Asientos.ToList();
    public CategoriaAsiento ObtenerCategoriaAsiento(int numero, string asiento) => ObtenerPlano(numero).Categorias.GetValueOrDefault(asiento);
    public List<int> ObtenerTodasLasSalasPorExperiencia(int experiencia) => SalasGestion.Where(x => x.Experiencias.Contains(experiencia)).Select(x => x.Numero).ToList();
    public List<int> ObtenerSalasDisponiblesPorExperiencia(int experiencia, List<int> ocupadas) => ObtenerTodasLasSalasPorExperiencia(experiencia).Except(ocupadas).ToList();
    public decimal ObtenerPrecioAdulto(int numero, CategoriaAsiento categoria, int experiencia) => ObtenerSala(numero).PrecioFinal;
    public decimal ObtenerPrecioNino(int numero, CategoriaAsiento categoria, int experiencia) => ObtenerSala(numero).PrecioFinal;
    public decimal CalcularPrecioTotal(int pelicula, int sala, List<string> butacas, int adultos, int ninos) => ObtenerSala(sala).PrecioFinal * (adultos + ninos);
    public EstadoGestionSalas CopiarEstado() => new() { Salas = SalasGestion.ToList(), Disenos = Disenos.ToList(), Experiencias = Experiencias.ToList(), TiposSala = TiposSala.ToList() };
    public async Task<Pagina<SalaCine>> ConsultarSalasAsync(ConsultaPagina f)
    {
        var pagina = await api.Get<Pagina<SalaCine>>("api/catalogos/salas?" + ApiCine.Consulta(f));
        foreach (var sala in pagina.Items) salasEnUso[sala.Numero] = sala.TieneProgramacion;
        return pagina;
    }
    public Task<Pagina<DisenoButacas>> ConsultarDisenosAsync(ConsultaPagina f) => api.Get<Pagina<DisenoButacas>>("api/catalogos/disenos?" + ApiCine.Consulta(f));
    public Task<Pagina<ExperienciaCine>> ConsultarExperienciasAsync(ConsultaPagina f) => api.Get<Pagina<ExperienciaCine>>("api/catalogos/experiencias?" + ApiCine.Consulta(f));
    public Task<Pagina<TipoSalaCine>> ConsultarTiposAsync(ConsultaPagina f) => api.Get<Pagina<TipoSalaCine>>("api/catalogos/tipos-sala?" + ApiCine.Consulta(f));
    private async Task<int> Guardar(string ruta, int id, object dto, bool nuevo)
    { var result = nuevo ? await api.Post<int>("api/catalogos/" + ruta, dto) : await api.Put<int>("api/catalogos/" + ruta + "/" + id, dto); await Refrescar(); return result; }
    private async Task Refrescar() { await api.ReferenciasAsync(); planos.Clear(); Changed?.Invoke(); }
    private async Task Eliminar(string ruta, int id) { await api.Delete("api/catalogos/" + ruta + "/" + id); await Refrescar(); }
    public Task<int> GuardarSalaAsync(SalaCine dto) => Guardar("salas", dto.Numero, dto, dto.Numero == 0);
    public Task<int> GuardarDisenoAsync(PlanoSala dto) => Guardar("disenos", dto.Numero, dto, dto.Numero == 0);
    public Task<int> GuardarExperienciaAsync(ExperienciaCine dto) => Guardar("experiencias", dto.Id, dto, dto.Id < 0);
    public Task<int> GuardarTipoSalaAsync(TipoSalaCine dto) => Guardar("tipos-sala", dto.Id, dto, dto.Id < 0);
    public Task EliminarSalaAsync(int id) => Eliminar("salas", id);
    public Task EliminarDisenoAsync(int id) => Eliminar("disenos", id);
    public Task EliminarExperienciaAsync(int id) => Eliminar("experiencias", id);
    public Task EliminarTipoSalaAsync(int id) => Eliminar("tipos-sala", id);
}
