using CineSpace.Server.Data;
using CineSpace.Server.Models;
using CineSpace.Server.Repositories;
using CineSpace.Server.Services;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineSpace.Server.Controllers;

[ApiController, Route("api/catalogos")]
public sealed class CatalogosController(ICatalogoService catalogos, CineSpaceDbContext db, IRepositorioCine repositorio) : ControllerBase
{
    private const string Administracion = "Administrador";

    [HttpGet("referencias"), AllowAnonymous]
    public Task<ReferenciasCatalogo> Referencias(CancellationToken ct) => catalogos.ReferenciasAsync(ct);

    [HttpGet("generos/disponibles"), AllowAnonymous]
    public async Task<List<GeneroDisponible>> GenerosDisponibles(CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(ReglasCine.Ahora);
        // Counts cover the complete public catalogue, not only its first page.
        var peliculas = db.Peliculas.Where(p => p.Publicada && (
            ((p.Estreno == null || p.Estreno <= hoy || p.Preventa) && db.Funciones.Any(f => f.PeliculaId == p.Id && f.Sala.Activa && f.InicioUtc > DateTime.UtcNow &&
                (p.Estreno == null || DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(f.InicioUtc, "America/La_Paz")) >= p.Estreno))) ||
            db.Estrenos.Any(e => e.PeliculaId == p.Id && e.Publicado && e.Fecha >= hoy)));
        return await db.Generos.AsNoTracking().Select(g => new { g.Id, g.Nombre,
            Cantidad = peliculas.Count(p => p.Generos.Any(pg => pg.GeneroId == g.Id)) +
            db.Estrenos.Count(e => e.PeliculaId == null && e.Publicado && e.Fecha >= hoy && e.Genero == g.Nombre) })
            .Where(g => g.Cantidad > 0).OrderBy(g => g.Nombre)
            .Select(g => new GeneroDisponible(g.Id, g.Nombre, g.Cantidad)).ToListAsync(ct);
    }

    [HttpGet("disenos/{id:int}"), Authorize(Roles = "Administrador,Trabajador")]
    public async Task<PlanoSala> Diseno(int id, CancellationToken ct) => ReglasCine.Leer<PlanoSala>(await db.Disenos.Where(x => x.Id == id).Select(x => x.PlanoJson).SingleOrDefaultAsync(ct) ?? throw new KeyNotFoundException());

    [HttpGet("generos"), AllowAnonymous]
    public async Task<Pagina<EtiquetaCatalogo>> Generos([FromQuery] ConsultaPagina consulta, CancellationToken ct) =>
        Convertir(await catalogos.PaginaAsync<GeneroRegistro>(consulta, ct), x => new EtiquetaCatalogo { Id = x.Id, Nombre = x.Nombre });
    [HttpPost("generos"), Authorize(Roles = Administracion)]
    public Task<int> NuevoGenero(EtiquetaCatalogo entrada, CancellationToken ct) => catalogos.GuardarAsync<GeneroRegistro>(null, entrada.Nombre, "", ct);
    [HttpPut("generos/{id:int}"), Authorize(Roles = Administracion)]
    public Task<int> GuardarGenero(int id, EtiquetaCatalogo entrada, CancellationToken ct) => catalogos.GuardarAsync<GeneroRegistro>(id, entrada.Nombre, "", ct);
    [HttpDelete("generos/{id:int}"), Authorize(Roles = Administracion)]
    public Task EliminarGenero(int id, CancellationToken ct) => catalogos.EliminarAsync<GeneroRegistro>(id, ct);

    [HttpGet("categorias"), AllowAnonymous]
    public async Task<Pagina<EtiquetaCatalogo>> Categorias([FromQuery] ConsultaPagina consulta, CancellationToken ct) =>
        Convertir(await catalogos.PaginaAsync<CategoriaRegistro>(consulta, ct), x => new EtiquetaCatalogo { Id = x.Id, Nombre = x.Nombre });
    [HttpPost("categorias"), Authorize(Roles = Administracion)]
    public Task<int> NuevaCategoria(EtiquetaCatalogo entrada, CancellationToken ct) => catalogos.GuardarAsync<CategoriaRegistro>(null, entrada.Nombre, "", ct);
    [HttpPut("categorias/{id:int}"), Authorize(Roles = Administracion)]
    public Task<int> GuardarCategoria(int id, EtiquetaCatalogo entrada, CancellationToken ct) => catalogos.GuardarAsync<CategoriaRegistro>(id, entrada.Nombre, "", ct);
    [HttpDelete("categorias/{id:int}"), Authorize(Roles = Administracion)]
    public Task EliminarCategoria(int id, CancellationToken ct) => catalogos.EliminarAsync<CategoriaRegistro>(id, ct);

    [HttpGet("experiencias"), AllowAnonymous]
    public async Task<Pagina<ExperienciaCine>> Experiencias([FromQuery] ConsultaPagina consulta, CancellationToken ct) =>
        Convertir(await catalogos.PaginaAsync<ExperienciaRegistro>(consulta, ct), x => new ExperienciaCine { Id = x.Id, Nombre = x.Nombre, Descripcion = x.Descripcion });
    [HttpPost("experiencias"), Authorize(Roles = Administracion)]
    public Task<int> NuevaExperiencia(ExperienciaCine entrada, CancellationToken ct) => catalogos.GuardarAsync<ExperienciaRegistro>(null, entrada.Nombre, entrada.Descripcion, ct);
    [HttpPut("experiencias/{id:int}"), Authorize(Roles = Administracion)]
    public Task<int> GuardarExperiencia(int id, ExperienciaCine entrada, CancellationToken ct) => catalogos.GuardarAsync<ExperienciaRegistro>(id, entrada.Nombre, entrada.Descripcion, ct);
    [HttpDelete("experiencias/{id:int}"), Authorize(Roles = Administracion)]
    public Task EliminarExperiencia(int id, CancellationToken ct) => catalogos.EliminarAsync<ExperienciaRegistro>(id, ct);

    [HttpGet("tipos-sala"), AllowAnonymous]
    public async Task<Pagina<TipoSalaCine>> TiposSala([FromQuery] ConsultaPagina consulta, CancellationToken ct) =>
        Convertir(await catalogos.PaginaAsync<TipoSalaRegistro>(consulta, ct), x => new TipoSalaCine { Id = x.Id, Nombre = x.Nombre, Descripcion = x.Descripcion });
    [HttpPost("tipos-sala"), Authorize(Roles = Administracion)]
    public Task<int> NuevoTipo(TipoSalaCine entrada, CancellationToken ct) => catalogos.GuardarAsync<TipoSalaRegistro>(null, entrada.Nombre, entrada.Descripcion, ct);
    [HttpPut("tipos-sala/{id:int}"), Authorize(Roles = Administracion)]
    public Task<int> GuardarTipo(int id, TipoSalaCine entrada, CancellationToken ct) => catalogos.GuardarAsync<TipoSalaRegistro>(id, entrada.Nombre, entrada.Descripcion, ct);
    [HttpDelete("tipos-sala/{id:int}"), Authorize(Roles = Administracion)]
    public Task EliminarTipo(int id, CancellationToken ct) => catalogos.EliminarAsync<TipoSalaRegistro>(id, ct);

    [HttpGet("distribuidoras"), Authorize(Roles = "Administrador,Trabajador")]
    public async Task<Pagina<Distribuidora>> Distribuidoras([FromQuery] ConsultaPagina consulta, CancellationToken ct) =>
        Convertir(await catalogos.PaginaAsync<DistribuidoraRegistro>(consulta, ct), MapasCine.Distribuidora);
    [HttpPost("distribuidoras"), Authorize(Roles = Administracion)]
    public Task<int> NuevaDistribuidora(Distribuidora entrada, CancellationToken ct) => catalogos.GuardarDistribuidoraAsync(null, entrada, ct);
    [HttpPut("distribuidoras/{id:int}"), Authorize(Roles = Administracion)]
    public Task<int> GuardarDistribuidora(int id, Distribuidora entrada, CancellationToken ct) => catalogos.GuardarDistribuidoraAsync(id, entrada, ct);
    [HttpDelete("distribuidoras/{id:int}"), Authorize(Roles = Administracion)]
    public Task EliminarDistribuidora(int id, CancellationToken ct) => catalogos.EliminarAsync<DistribuidoraRegistro>(id, ct);

    [HttpGet("combos"), AllowAnonymous]
    public async Task<Pagina<ComboConfiteria>> Combos([FromQuery] ConsultaPagina consulta, CancellationToken ct)
    {
        var query = db.Combos.AsNoTracking();
        if (!User.IsInRole(Administracion)) query = query.Where(x => x.Publicado);
        if (consulta.Categoria.HasValue) query = query.Where(x => x.CategoriaId == consulta.Categoria);
        if (!string.IsNullOrWhiteSpace(consulta.Buscar)) query = query.Where(x => EF.Functions.ILike(x.Nombre, "%" + consulta.Buscar.Trim() + "%"));
        if (consulta.Estado == "publicados") query = query.Where(x => x.Publicado);
        if (consulta.Estado == "ocultos") query = query.Where(x => !x.Publicado);
        var antiguas = consulta.Orden is "antiguos" or "antiguas" or "Más antiguos";
        if (!consulta.Categoria.HasValue)
        {
            // Prioriza la categoría antes de paginar, sin depender de los IDs del seed.
            var ordenados = query.OrderByDescending(x => EF.Functions.ILike(x.Categoria.Nombre, "Combos"));
            query = antiguas ? ordenados.ThenBy(x => x.Id) : ordenados.ThenByDescending(x => x.Id);
        }
        else query = antiguas ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id);
        return Convertir(await repositorio.PaginarAsync(query, consulta, ct), MapasCine.Combo);
    }
    [HttpGet("combos/carrito"), AllowAnonymous]
    public async Task<List<ComboConfiteria>> CotizarCarrito([FromQuery] int[] ids, CancellationToken ct)
    {
        if (ids.Length > 30) throw new ArgumentException("El carrito admite hasta 30 productos diferentes.");
        return (await db.Combos.AsNoTracking().Where(x => ids.Contains(x.Id) && x.Publicado).ToListAsync(ct)).Select(MapasCine.Combo).ToList();
    }
    [HttpPost("combos"), Authorize(Roles = Administracion)]
    public Task<int> NuevoCombo(ComboConfiteria entrada, CancellationToken ct) => catalogos.GuardarComboAsync(null, entrada, ct);
    [HttpPut("combos/{id:int}"), Authorize(Roles = Administracion)]
    public Task<int> GuardarCombo(int id, ComboConfiteria entrada, CancellationToken ct) => catalogos.GuardarComboAsync(id, entrada, ct);
    [HttpDelete("combos/{id:int}"), Authorize(Roles = Administracion)]
    public Task EliminarCombo(int id, CancellationToken ct) => catalogos.EliminarAsync<ComboRegistro>(id, ct);

    [HttpGet("salas"), AllowAnonymous]
    public async Task<Pagina<SalaCine>> Salas([FromQuery] ConsultaPagina consulta, CancellationToken ct)
    {
        var query = db.Salas.AsNoTracking().Where(x => x.Activa).Include(x => x.Experiencias).Include(x => x.Diseno).AsQueryable();
        if (!string.IsNullOrWhiteSpace(consulta.Buscar)) query = query.Where(x => EF.Functions.ILike(x.Nombre, "%" + consulta.Buscar.Trim() + "%"));
        if (consulta.Categoria.HasValue) query = query.Where(x => x.TipoSalaId == consulta.Categoria);
        if (consulta.Experiencia.HasValue) query = query.Where(x => x.Experiencias.Any(e => e.ExperienciaId == consulta.Experiencia));
        query = consulta.Orden is "antiguos" or "antiguas" or "Más antiguos" ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id);
        var pagina = Convertir(await repositorio.PaginarAsync(query, consulta, ct), MapasCine.Sala);
        var ids = pagina.Items.Select(x => x.Numero).ToArray();
        var ocupadas = await db.Funciones.Where(x => ids.Contains(x.SalaId) && (x.SalaListaUtc > DateTime.UtcNow || db.Ventas.Any(v => v.FuncionId == x.Id)))
            .Select(x => x.SalaId).Distinct().ToListAsync(ct);
        foreach (var sala in pagina.Items) sala.TieneProgramacion = ocupadas.Contains(sala.Numero);
        return pagina;
    }
    [HttpGet("salas/{id:int}/plano"), AllowAnonymous]
    public async Task<PlanoSala> PlanoSala(int id, CancellationToken ct)
    {
        var sala = await db.Salas.AsNoTracking().Include(x => x.Diseno).FirstOrDefaultAsync(x => x.Id == id && x.Activa, ct) ?? throw new KeyNotFoundException();
        var plano = ReglasCine.Leer<PlanoSala>(sala.Diseno.PlanoJson); plano.Numero = sala.Id; plano.Nombre = sala.Nombre; return plano;
    }
    [HttpPost("salas"), Authorize(Roles = Administracion)]
    public Task<int> NuevaSala(SalaCine entrada, CancellationToken ct) => catalogos.GuardarSalaAsync(null, entrada, ct);
    [HttpPut("salas/{id:int}"), Authorize(Roles = Administracion)]
    public Task<int> GuardarSala(int id, SalaCine entrada, CancellationToken ct) => catalogos.GuardarSalaAsync(id, entrada, ct);
    [HttpDelete("salas/{id:int}"), Authorize(Roles = Administracion)]
    public Task EliminarSala(int id, CancellationToken ct) => catalogos.EliminarAsync<SalaRegistro>(id, ct);

    [HttpGet("disenos"), Authorize(Roles = "Administrador,Trabajador")]
    public async Task<Pagina<DisenoButacas>> Disenos([FromQuery] ConsultaPagina consulta, CancellationToken ct)
    {
        IQueryable<DisenoRegistro> query = db.Disenos.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(consulta.Buscar)) query = query.Where(x => EF.Functions.ILike(x.Nombre, "%" + consulta.Buscar.Trim() + "%"));
        var recientes = consulta.Orden is not ("antiguos" or "Más antiguos" or "menos-salas-antiguos");
        if (consulta.Estado is "mas-salas" or "menos-salas")
        {
            var usados = consulta.Estado == "mas-salas" ? query.OrderByDescending(x => db.Salas.Count(s => s.DisenoId == x.Id)) : query.OrderBy(x => db.Salas.Count(s => s.DisenoId == x.Id));
            query = recientes ? usados.ThenByDescending(x => x.Id) : usados.ThenBy(x => x.Id);
        }
        else query = recientes ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id);
        return Convertir(await repositorio.PaginarAsync(query, consulta, ct), x => new DisenoButacas { Id = x.Id, Plano = ReglasCine.Leer<PlanoSala>(x.PlanoJson) });
    }
    [HttpPost("disenos"), Authorize(Roles = Administracion)]
    public Task<int> NuevoDiseno(PlanoSala entrada, CancellationToken ct) => catalogos.GuardarDisenoAsync(null, entrada, ct);
    [HttpPut("disenos/{id:int}"), Authorize(Roles = Administracion)]
    public Task<int> GuardarDiseno(int id, PlanoSala entrada, CancellationToken ct) => catalogos.GuardarDisenoAsync(id, entrada, ct);
    [HttpDelete("disenos/{id:int}"), Authorize(Roles = Administracion)]
    public Task EliminarDiseno(int id, CancellationToken ct) => catalogos.EliminarAsync<DisenoRegistro>(id, ct);

    public static Pagina<TDestino> Convertir<TOrigen, TDestino>(Pagina<TOrigen> pagina, Func<TOrigen, TDestino> convertir) =>
        new(pagina.Items.Select(convertir).ToList(), pagina.Total, pagina.Numero, pagina.Tamano);
}
