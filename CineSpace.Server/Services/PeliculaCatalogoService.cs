using CineSpace.Server.Data;
using CineSpace.Server.Models;
using CineSpace.Server.Repositories;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace CineSpace.Server.Services;

public interface IPeliculaCatalogoService
{
    Task<Pagina<Pelicula>> ListarAsync(ConsultaPagina consulta, bool personal, CancellationToken ct);
    Task<Pelicula> ObtenerAsync(int id, CancellationToken ct);
    Task<Pelicula> GuardarAsync(int? id, Pelicula solicitud, CancellationToken ct);
    Task EliminarAsync(int id, CancellationToken ct);
}

public sealed class PeliculaCatalogoService(CineSpaceDbContext db, IRepositorioCine repositorio, IImagenStorageService imagenes) : IPeliculaCatalogoService
{
    private IQueryable<PeliculaRegistro> Fichas() => db.Peliculas.AsNoTracking().Include(x => x.Generos).ThenInclude(x => x.Genero);

    public async Task<Pagina<Pelicula>> ListarAsync(ConsultaPagina consulta, bool personal, CancellationToken ct)
    {
        var query = Fichas().Where(x => x.Publicada);
        var buscar = ReglasCine.Texto(consulta.Buscar, 100, "la busqueda", false);
        if (buscar.Length > 0) query = query.Where(x => EF.Functions.ILike(x.Titulo, "%" + buscar + "%"));
        if (consulta.Genero.HasValue) query = query.Where(x => x.Generos.Any(g => g.GeneroId == consulta.Genero));
        if (consulta.Categoria.HasValue) query = query.Where(x => x.DistribuidoraId == consulta.Categoria);
        if (consulta.Estado == "preventa") query = query.Where(x => x.Preventa);
        if (consulta.Estado == "cartelera") query = query.Where(x => !x.Preventa);
        if (!personal || consulta.Estado is "cartelera" or "preventa" or "publicas") query = query.Where(x => db.Funciones.Any(f => f.PeliculaId == x.Id && f.InicioUtc > DateTime.UtcNow));
        if (consulta.Experiencia.HasValue) query = query.Where(x => db.Funciones.Any(f => f.PeliculaId == x.Id && f.ExperienciaId == consulta.Experiencia && f.InicioUtc > DateTime.UtcNow));
        if (!string.IsNullOrWhiteSpace(consulta.Idioma)) { var idioma = ReglasCine.Serializar(new[] { consulta.Idioma }); query = query.Where(x => EF.Functions.JsonContains(x.IdiomasJson, idioma)); }
        query = consulta.Orden is "antiguos" or "antiguas" or "Más antiguos" ? query.OrderBy(x => x.CreadoUtc).ThenBy(x => x.Id) : query.OrderByDescending(x => x.CreadoUtc).ThenByDescending(x => x.Id);
        var pagina = await repositorio.PaginarAsync(query.Select(x => new {
            Pelicula=x,
            Experiencias=db.Funciones.Where(f=>f.PeliculaId==x.Id && f.InicioUtc>DateTime.UtcNow).Select(f=>f.ExperienciaId).Distinct().ToList(),
            Disponibles=db.Funciones.Count(f=>f.PeliculaId==x.Id && f.InicioUtc>DateTime.UtcNow)
        }), consulta, ct);
        var fichas = pagina.Items.Select(x=> { var ficha=MapasCine.Pelicula(x.Pelicula); ficha.Experiencias=x.Experiencias; ficha.FuncionesDisponibles=x.Disponibles; return ficha; }).ToList();
        return new(fichas,pagina.Total,pagina.Numero,pagina.Tamano);
    }

    public async Task<Pelicula> ObtenerAsync(int id, CancellationToken ct) => MapasCine.Pelicula(await Fichas().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException());

    public async Task<Pelicula> GuardarAsync(int? id, Pelicula entrada, CancellationToken ct)
    {
        if (entrada.DuracionMinutos is < 1 or > 720 || entrada.LimpiezaMinutos is < 0 or > 360 || entrada.FocoPosterVertical is < 0 or > 100 || entrada.FocoPanoramicaVertical is < 0 or > 100) throw new ArgumentException("Revisa la duracion, limpieza y encuadre de las imagenes.");
        var nombres = entrada.ListaGeneros.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var generos = await db.Generos.Where(x => nombres.Contains(x.Nombre)).Select(x => x.Id).ToListAsync(ct);
        if (generos.Count == 0 || generos.Count != nombres.Length) throw new ArgumentException("Selecciona uno o mas generos del catalogo.");
        if (!entrada.DistribuidoraId.HasValue || !await db.Distribuidoras.AnyAsync(x => x.Id == entrada.DistribuidoraId, ct)) throw new ArgumentException("Selecciona la distribuidora de la pelicula.");
        var registro = id.HasValue ? await db.Peliculas.Include(x => x.Generos).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException() : new PeliculaRegistro();
        var anteriorPoster = registro.Poster; var anteriorFondo = registro.Fondo;
        registro.Titulo = ReglasCine.Texto(entrada.Titulo, 300, "el titulo"); registro.Sinopsis = ReglasCine.Texto(entrada.Sinopsis, 10000, "la sinopsis", false);
        registro.Duracion = entrada.DuracionMinutos; registro.Limpieza = entrada.LimpiezaMinutos; registro.DistribuidoraId = entrada.DistribuidoraId;
        registro.Estreno = entrada.FechaEstreno.HasValue ? DateOnly.FromDateTime(entrada.FechaEstreno.Value) : null;
        registro.TmdbId = entrada.TmdbId; registro.FocoPoster = entrada.FocoPosterVertical; registro.FocoFondo = entrada.FocoPanoramicaVertical;
        registro.Preventa = entrada.EnPreventa; registro.IdiomasJson = ReglasCine.Serializar(entrada.IdiomasDisponibles.Take(20).ToArray());
        registro.TrailerUrl = ValidarTrailer(entrada.TrailerUrl);
        await using var archivos = new CargaImagenes(imagenes);
        registro.Poster = await archivos.RecibirAsync(entrada.ImagenUrl, "peliculas", false, ct);
        registro.Fondo = string.IsNullOrWhiteSpace(entrada.ImagenPanoramicaUrl) ? "" : await archivos.RecibirAsync(entrada.ImagenPanoramicaUrl, "peliculas", true, ct);
        foreach (var anterior in registro.Generos.Where(x => !generos.Contains(x.GeneroId)).ToList()) db.Remove(anterior);
        foreach (var genero in generos.Where(g => registro.Generos.All(x => x.GeneroId != g))) registro.Generos.Add(new() { GeneroId = genero });
        if (!id.HasValue) db.Add(registro);
        await db.SaveChangesAsync(ct);
        archivos.Confirmar();
        await RetirarImagenSiLibreAsync(anteriorPoster, registro.Poster, ct);
        await RetirarImagenSiLibreAsync(anteriorFondo, registro.Fondo, ct);
        return await ObtenerAsync(registro.Id, ct);
    }

    private static string ValidarTrailer(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return "";
        if (!Uri.TryCreate(valor, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            uri.Host is not ("www.youtube.com" or "youtube.com" or "youtu.be" or "www.youtube-nocookie.com")) throw new ArgumentException("Utiliza un enlace HTTPS de YouTube para el trailer.");
        return uri.AbsoluteUri;
    }

    public async Task EliminarAsync(int id, CancellationToken ct)
    {
        var registro = await db.Peliculas.FindAsync([id], ct) ?? throw new KeyNotFoundException();
        if (await db.Funciones.AnyAsync(x => x.PeliculaId == id, ct) || await db.Programaciones.AnyAsync(x => x.PeliculaId == id, ct)) throw new ArgumentException("Retira primero las programaciones sin ventas. Las peliculas con historial no se eliminan.");
        db.Remove(registro); await db.SaveChangesAsync(ct);
        await RetirarImagenSiLibreAsync(registro.Poster, "", ct); await RetirarImagenSiLibreAsync(registro.Fondo, "", ct);
    }
    private async Task RetirarImagenSiLibreAsync(string anterior, string actual, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(anterior) || anterior == actual || anterior.StartsWith("/images/", StringComparison.Ordinal)) return;
        if (!await db.Peliculas.AnyAsync(x => x.Poster == anterior || x.Fondo == anterior, ct) && !await db.Banners.AnyAsync(x => x.Imagen == anterior, ct))
            await imagenes.EliminarAsync(anterior, ct);
    }
}
