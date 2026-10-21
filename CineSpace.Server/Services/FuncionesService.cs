using CineSpace.Server.Data;
using CineSpace.Server.Models;
using CineSpace.Server.Repositories;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace CineSpace.Server.Services;

public interface IFuncionesService
{
    Task<Pagina<ProgramacionSala>> ListarAsync(ConsultaPagina filtro, CancellationToken ct);
    Task<List<FuncionCine>> DisponiblesAsync(int pelicula, CancellationToken ct);
    Task<ProgramacionSala> GuardarAsync(ProgramacionSala solicitud, bool nueva, CancellationToken ct);
    Task EliminarAsync(Guid id, CancellationToken ct);
}

public sealed class FuncionesService(CineSpaceDbContext db, IRepositorioCine repositorio) : IFuncionesService
{
    public async Task<Pagina<ProgramacionSala>> ListarAsync(ConsultaPagina f, CancellationToken ct)
    {
        var q = db.Programaciones.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(f.Buscar)) q = q.Where(x => EF.Functions.ILike(x.Pelicula.Titulo, "%" + f.Buscar.Trim() + "%"));
        if (f.Sala.HasValue) q = q.Where(x => x.SalaId == f.Sala);
        if (f.Experiencia.HasValue) q = q.Where(x => x.ExperienciaId == f.Experiencia);
        if (f.Estado == "vigentes") q = q.Where(x => x.Hasta >= DateOnly.FromDateTime(ReglasCine.Ahora));
        var page = await repositorio.PaginarAsync(f.Orden == "antiguas" ? q.OrderBy(x => x.CreadoUtc).ThenBy(x => x.Id) : q.OrderByDescending(x => x.CreadoUtc).ThenBy(x => x.Id), f, ct);
        var ids = page.Items.Select(x => x.Id).ToArray();
        var cantidades = await db.Funciones.Where(x => ids.Contains(x.ProgramacionId)).GroupBy(x => x.ProgramacionId).Select(g => new { Id = g.Key, Total = g.Count() }).ToDictionaryAsync(x => x.Id, x => x.Total, ct);
        return new(page.Items.Select(x => { var p = MapasCine.Programacion(x); p.CantidadFunciones = cantidades.GetValueOrDefault(x.Id); return p; }).ToList(), page.Total, page.Numero, page.Tamano);
    }

    public async Task<List<FuncionCine>> DisponiblesAsync(int pelicula, CancellationToken ct)
    {
        var rows = await db.Database.SqlQuery<FilaFuncion>($"""
            WITH dias AS (
              SELECT DISTINCT (f.inicio_utc AT TIME ZONE 'America/La_Paz')::date AS dia
              FROM cine.funciones f JOIN cine.peliculas p ON p.id=f.pelicula_id JOIN cine.salas s ON s.id=f.sala_id
              WHERE f.pelicula_id={pelicula} AND f.inicio_utc > CURRENT_TIMESTAMP AND p.publicada AND s.activa
                AND (p.estreno IS NULL OR (f.inicio_utc AT TIME ZONE 'America/La_Paz')::date >= p.estreno)
                AND (p.estreno IS NULL OR p.estreno <= (CURRENT_TIMESTAMP AT TIME ZONE 'America/La_Paz')::date OR p.preventa)
              ORDER BY dia LIMIT 3
            )
            SELECT f.id AS "Id",f.programacion_id AS "ProgramacionId",f.pelicula_id AS "PeliculaId",f.sala_id AS "SalaId",f.experiencia_id AS "ExperienciaId",
              f.inicio_utc AS "InicioUtc",f.duracion AS "Duracion",f.limpieza AS "Limpieza",
              COALESCE(p.preventa AND p.estreno > (CURRENT_TIMESTAMP AT TIME ZONE 'America/La_Paz')::date, false) AS "Preventa",
              jsonb_array_length(d.plano_json->'asientos') AS "Capacidad",
              (SELECT count(*)::int FROM cine.butacas_vendidas b WHERE b.funcion_id=f.id) AS "Ocupadas"
            FROM cine.funciones f JOIN cine.salas s ON s.id=f.sala_id JOIN cine.disenos d ON d.id=s.diseno_id JOIN cine.peliculas p ON p.id=f.pelicula_id
            WHERE f.pelicula_id={pelicula} AND f.inicio_utc > CURRENT_TIMESTAMP AND s.activa AND p.publicada
              AND (f.inicio_utc AT TIME ZONE 'America/La_Paz')::date IN (SELECT dia FROM dias)
            ORDER BY f.inicio_utc,f.sala_id
            """).ToListAsync(ct);
        return rows.Select(x=>new FuncionCine { Id=x.Id.ToString("N"),ProgramacionId=x.ProgramacionId.ToString("N"),PeliculaId=x.PeliculaId,NumeroSala=x.SalaId,ExperienciaId=x.ExperienciaId,
            Inicio=ReglasCine.Local(x.InicioUtc),DuracionMinutos=x.Duracion,LimpiezaMinutos=x.Limpieza,EnPreventa=x.Preventa,Capacidad=x.Capacidad,ButacasOcupadas=x.Ocupadas }).ToList();
    }

    public async Task<ProgramacionSala> GuardarAsync(ProgramacionSala dto, bool nueva, CancellationToken ct)
    {
        dto.Horario.ValidarSeleccion();
        var desde = DateOnly.FromDateTime(dto.Desde); var hasta = DateOnly.FromDateTime(dto.Hasta);
        if (desde < DateOnly.FromDateTime(ReglasCine.Ahora) || hasta < desde || hasta.DayNumber - desde.DayNumber > 90)
            throw new ArgumentException("Programa entre hoy y un maximo de 90 dias consecutivos.");
        var movie = await db.Peliculas.FindAsync([dto.PeliculaId], ct) ?? throw new ArgumentException("La pelicula no existe.");
        if (!await db.Salas.AnyAsync(x => x.Id == dto.NumeroSala && x.Activa && x.Experiencias.Any(e => e.ExperienciaId == dto.ExperienciaId), ct))
            throw new ArgumentException("La sala no ofrece esta experiencia.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(-600, {dto.PeliculaId})", ct);
        await db.Entry(movie).ReloadAsync(ct);
        if (movie.Estreno.HasValue && desde < movie.Estreno.Value)
            throw new ArgumentException("Las funciones deben empezar en la fecha de estreno o después. No se pueden vender entradas anteriores al estreno.");
        dto.Horario.EnPreventa = movie.Preventa && movie.Estreno > DateOnly.FromDateTime(ReglasCine.Ahora);
        // La misma cerradura se usa al vender y al cambiar la programacion.
        var salaAnterior = !nueva && Guid.TryParse(dto.Id,out var programacionId) ? await db.Programaciones.Where(x => x.Id == programacionId).Select(x => (int?)x.SalaId).SingleOrDefaultAsync(ct) : null;
        foreach (var salaId in new[] { dto.NumeroSala, salaAnterior ?? dto.NumeroSala }.Distinct().OrderBy(x => x))
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({salaId})", ct);
        ProgramacionRegistro entity;
        if (nueva) { entity = new(); db.Programaciones.Add(entity); }
        else
        {
            if (!Guid.TryParse(dto.Id, out var id)) throw new ArgumentException("Programacion no valida.");
            entity = await db.Programaciones.FindAsync([id], ct) ?? throw new KeyNotFoundException();
            if (await db.Ventas.AnyAsync(x => x.Funcion.ProgramacionId == id, ct)) throw new OperacionCineException("Esta programacion ya tiene entradas vendidas.");
            await db.Funciones.Where(x => x.ProgramacionId == id).ExecuteDeleteAsync(ct);
        }
        entity.PeliculaId = dto.PeliculaId; entity.SalaId = dto.NumeroSala; entity.ExperienciaId = dto.ExperienciaId;
        entity.Desde = desde; entity.Hasta = hasta; entity.HorarioJson = ReglasCine.Serializar(dto.Horario);
        var funciones = new List<FuncionRegistro>();
        for (var dia = desde; dia <= hasta; dia = dia.AddDays(1))
            foreach (var hora in dto.Horario.Horarios.Distinct())
            {
                if (!TimeOnly.TryParse(hora, out var tiempo)) throw new ArgumentException("Horario no valido.");
                var inicio = ReglasCine.Utc(dia.ToDateTime(tiempo));
                if (inicio <= DateTime.UtcNow) continue;
                funciones.Add(new() { ProgramacionId = entity.Id, PeliculaId = entity.PeliculaId, SalaId = entity.SalaId, ExperienciaId = entity.ExperienciaId,
                    InicioUtc = inicio, Duracion = movie.Duracion, Limpieza = dto.Horario.LimpiezaMinutos,
                    SalaListaUtc = inicio.AddMinutes(movie.Duracion + dto.Horario.LimpiezaMinutos), Preventa = dto.Horario.EnPreventa });
            }
        if (funciones.Count == 0) throw new ArgumentException("Todos los horarios seleccionados ya pasaron.");
        var primero = funciones.Min(x => x.InicioUtc); var ultimo = funciones.Max(x => x.SalaListaUtc);
        var existentes = await db.Funciones.AsNoTracking().Where(x => x.SalaId == entity.SalaId && x.InicioUtc < ultimo && x.SalaListaUtc > primero)
            .Select(x => new { x.InicioUtc, x.SalaListaUtc }).ToListAsync(ct);
        if (funciones.Any(f => existentes.Any(x => f.InicioUtc < x.SalaListaUtc && f.SalaListaUtc > x.InicioUtc)))
            throw new OperacionCineException("La sala ya esta ocupada en uno de estos horarios, incluida su limpieza.");
        var ordenadas = funciones.OrderBy(x => x.InicioUtc).ToList();
        if (ordenadas.Zip(ordenadas.Skip(1)).Any(x => x.First.SalaListaUtc > x.Second.InicioUtc)) throw new ArgumentException("Las funciones se solapan entre si.");
        db.Funciones.AddRange(funciones); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return MapasCine.Programacion(entity);
    }

    public async Task EliminarAsync(Guid id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var programacion = await db.Programaciones.FindAsync([id], ct) ?? throw new KeyNotFoundException();
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({programacion.SalaId})", ct);
        if (await db.Ventas.AnyAsync(x => x.Funcion.ProgramacionId == id, ct)) throw new OperacionCineException("No puedes eliminar una programacion con entradas vendidas.");
        db.Programaciones.Remove(programacion); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
}

public sealed class FilaFuncion
{
    public Guid Id {get;set;} public Guid ProgramacionId {get;set;} public int PeliculaId {get;set;} public int SalaId {get;set;} public int ExperienciaId {get;set;}
    public DateTime InicioUtc {get;set;} public int Duracion {get;set;} public int Limpieza {get;set;} public bool Preventa {get;set;}
    public int Capacidad {get;set;} public int Ocupadas {get;set;}
}
