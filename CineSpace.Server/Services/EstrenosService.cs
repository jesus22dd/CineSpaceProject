using CineSpace.Server.Data;
using CineSpace.Server.Models;
using CineSpace.Server.Repositories;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace CineSpace.Server.Services;

public interface IEstrenosService
{
    Task<Pagina<ProximoEstreno>> ListarAsync(ConsultaPagina filtro, bool administracion, CancellationToken ct);
    Task<ProximoEstreno> GuardarAsync(int? id, ProximoEstreno solicitud, CancellationToken ct);
    Task EliminarAsync(int id, CancellationToken ct);
}

public sealed class EstrenosService(CineSpaceDbContext db, IRepositorioCine repositorio, IImagenStorageService imagenes) : IEstrenosService
{
    public async Task<Pagina<ProximoEstreno>> ListarAsync(ConsultaPagina filtro, bool administracion, CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(ReglasCine.Ahora);
        var query = db.Estrenos.AsNoTracking().Include(x => x.Pelicula).AsQueryable();
        if (!administracion) query = query.Where(x => x.Publicado && x.Fecha >= hoy);
        if (!string.IsNullOrWhiteSpace(filtro.Buscar)) query = query.Where(x => EF.Functions.ILike(x.Titulo, "%" + filtro.Buscar.Trim() + "%"));
        if (filtro.Pelicula.HasValue) query = query.Where(x => x.PeliculaId == filtro.Pelicula);
        if (filtro.Genero.HasValue) query = query.Where(x => db.Generos.Any(g => g.Id == filtro.Genero && g.Nombre == x.Genero));
        if (filtro.Estado == "preventa") query = query.Where(x => x.Publicado && x.Fecha > hoy && x.Pelicula != null && x.Pelicula.Preventa &&
            db.Funciones.Any(f => f.PeliculaId == x.PeliculaId && f.InicioUtc > DateTime.UtcNow && f.Sala.Activa && DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(f.InicioUtc,"America/La_Paz")) >= x.Fecha));
        if (filtro.Estado == "anunciado") query = query.Where(x => x.Publicado && x.Fecha >= hoy && (x.Pelicula == null || !x.Pelicula.Preventa));
        if (filtro.Estado == "borrador") query = query.Where(x => !x.Publicado);
        if (filtro.Experiencia.HasValue || !string.IsNullOrWhiteSpace(filtro.Idioma)) query = query.Where(x => false);
        var ordenada = filtro.Orden switch
        {
            "nombre" => query.OrderBy(x => x.Titulo).ThenBy(x => x.Id),
            "antiguas" => query.OrderBy(x => x.Fecha).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.Fecha).ThenBy(x => x.Id)
        };
        var pagina = await repositorio.PaginarAsync(ordenada.Select(x => new
        {
            Estreno = x,
            Funciones = db.Funciones.Count(f => f.PeliculaId == x.PeliculaId && f.InicioUtc > DateTime.UtcNow && f.Sala.Activa &&
                DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(f.InicioUtc, "America/La_Paz")) >= x.Fecha)
        }), filtro, ct);
        return new(pagina.Items.Select(x => MapasCine.Estreno(x.Estreno) with { FuncionesDisponibles = x.Funciones, EnPreventa = MapasCine.Estreno(x.Estreno).EnPreventa && x.Funciones > 0 }).ToList(), pagina.Total, pagina.Numero, pagina.Tamano);
    }

    public async Task<ProximoEstreno> GuardarAsync(int? id, ProximoEstreno dto, CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(ReglasCine.Ahora);
        if (dto.Publicado && dto.FechaEstreno < hoy) throw new ArgumentException("Elige hoy o una fecha futura para publicar el estreno.");
        if (dto.EnPreventa && (!dto.Publicado || dto.FechaEstreno <= hoy || !dto.PeliculaId.HasValue))
            throw new ArgumentException("La preventa necesita un anuncio publicado, una película del catálogo y una fecha futura.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var entity = id.HasValue ? await db.Estrenos.Include(x => x.Pelicula).SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException() : new EstrenoRegistro();
        if (entity.PeliculaId.HasValue && entity.PeliculaId != dto.PeliculaId)
            throw new OperacionCineException("Este estreno ya está vinculado a una película. Conserva su vínculo para proteger las reservas.");
        PeliculaRegistro? pelicula = null;
        var funciones = 0;
        if (dto.PeliculaId.HasValue)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(-600, {dto.PeliculaId.Value})", ct);
            pelicula = await db.Peliculas.FindAsync([dto.PeliculaId.Value], ct) ?? throw new ArgumentException("La película vinculada no existe.");
            await db.Entry(pelicula).ReloadAsync(ct);
            if (!pelicula.Publicada) throw new ArgumentException("Publica la ficha de la película antes de vincular el estreno.");
            if (await db.Estrenos.AnyAsync(x => x.PeliculaId == dto.PeliculaId && x.Id != (id ?? 0), ct))
                throw new OperacionCineException("Esta película ya tiene un anuncio de estreno. Edita el anuncio existente.");
            if (pelicula.Estreno != dto.FechaEstreno && await db.Ventas.AnyAsync(x => x.Funcion.PeliculaId == pelicula.Id, ct))
                throw new OperacionCineException("No puedes cambiar la fecha de estreno de una película con entradas vendidas.");
            var desde = ReglasCine.Utc(dto.FechaEstreno.ToDateTime(TimeOnly.MinValue));
            funciones = await db.Funciones.CountAsync(f => f.PeliculaId == pelicula.Id && f.InicioUtc >= desde && f.InicioUtc > DateTime.UtcNow && f.Sala.Activa, ct);
            if (dto.EnPreventa && funciones == 0) throw new OperacionCineException("Primero programa al menos una función desde la fecha de estreno en una sala activa. Después podrás abrir la preventa.");
            pelicula.Estreno = dto.FechaEstreno;
            pelicula.Preventa = dto.EnPreventa && dto.Publicado;
        }
        await using var archivos = new CargaImagenes(imagenes);
        entity.Titulo = pelicula?.Titulo ?? ReglasCine.Texto(dto.Titulo, 250, "el título");
        entity.Imagen = await archivos.RecibirAsync(dto.ImagenUrl, "estrenos", false, ct);
        entity.Genero = ReglasCine.Texto(dto.Genero, 100, "el género");
        entity.Fecha = dto.FechaEstreno; entity.Sinopsis = ReglasCine.Texto(dto.Sinopsis, 10000, "la sinopsis", false);
        entity.Publicado = dto.Publicado; entity.PeliculaId = pelicula?.Id; entity.Pelicula = pelicula;
        if (!id.HasValue) db.Estrenos.Add(entity);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); archivos.Confirmar();
        return MapasCine.Estreno(entity) with { FuncionesDisponibles = funciones };
    }

    public async Task EliminarAsync(int id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var entity = await db.Estrenos.FindAsync([id], ct) ?? throw new KeyNotFoundException();
        if (entity.PeliculaId is { } peliculaId)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(-600, {peliculaId})", ct);
            await db.Peliculas.Where(x => x.Id == peliculaId).ExecuteUpdateAsync(x => x.SetProperty(p => p.Preventa, false), ct);
        }
        db.Estrenos.Remove(entity); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}
