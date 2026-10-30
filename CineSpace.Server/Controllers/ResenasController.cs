using System.Security.Claims;
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

[ApiController, Route("api/resenas"), Authorize]
public sealed class ResenasController(CineSpaceDbContext db, IRepositorioCine repositorio, IExportacionResenasService exportacion) : ControllerBase
{
    private Guid Usuario => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("exportar"), Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Exportar(CancellationToken ct) => File(await exportacion.ExportarResenasAsync(ct), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Reseñas_CineSpace.xlsx");
    [HttpGet("mias")] public Task<Pagina<CalificacionPelicula>> Mias([FromQuery] ConsultaPagina filtro, CancellationToken ct) { filtro.Tamano = 6; return Listar(filtro, false, ct); }
    [HttpGet, Authorize(Roles = "Administrador")] public Task<Pagina<CalificacionPelicula>> Todas([FromQuery] ConsultaPagina filtro, CancellationToken ct) => Listar(filtro, true, ct);
    private async Task<Pagina<CalificacionPelicula>> Listar(ConsultaPagina f, bool todas, CancellationToken ct)
    {
        var q = db.Resenas.AsNoTracking().AsQueryable();
        if (!todas) q = q.Where(x => x.UsuarioId == Usuario);
        if (!string.IsNullOrWhiteSpace(f.Buscar)) { var t = "%" + f.Buscar.Trim() + "%"; q = q.Where(x => EF.Functions.ILike(x.Pelicula.Titulo, t) || EF.Functions.ILike(x.Usuario.Email!, t)); }
        if (int.TryParse(f.Estado, out var estrellas)) q = q.Where(x => x.Puntuacion == estrellas);
        if (f.Pelicula.HasValue) q = q.Where(x => x.PeliculaId == f.Pelicula);
        if (f.Minimo > 0) q = q.Where(x => x.Puntuacion >= f.Minimo);
        if (f.Maximo > 0) q = q.Where(x => x.Puntuacion <= f.Maximo);
        if (f.Destacadas) q = q.Where(x => x.Destacada);
        if (f.Desde.HasValue) { var desde = ReglasCine.Utc(f.Desde.Value.ToDateTime(TimeOnly.MinValue)); q = q.Where(x => x.FechaUtc >= desde); }
        if (f.Hasta.HasValue) { var hasta = ReglasCine.Utc(f.Hasta.Value.AddDays(1).ToDateTime(TimeOnly.MinValue)); q = q.Where(x => x.FechaUtc < hasta); }
        var asc = f.Orden == "antiguas";
        var orden = f.Campo switch
        {
            "Pelicula" => asc ? q.OrderBy(x => x.Pelicula.Titulo).ThenBy(x => x.Id) : q.OrderByDescending(x => x.Pelicula.Titulo).ThenBy(x => x.Id),
            "Usuario" => asc ? q.OrderBy(x => x.Usuario.Email).ThenBy(x => x.Id) : q.OrderByDescending(x => x.Usuario.Email).ThenBy(x => x.Id),
            "Calificacion" => asc ? q.OrderBy(x => x.Puntuacion).ThenBy(x => x.Id) : q.OrderByDescending(x => x.Puntuacion).ThenBy(x => x.Id),
            _ => asc ? q.OrderBy(x => x.FechaUtc).ThenBy(x => x.Id) : q.OrderByDescending(x => x.FechaUtc).ThenBy(x => x.Id)
        };
        var p = await repositorio.PaginarAsync(orden, f, x => new CalificacionPelicula { Id = x.Id.ToString("N"), FuncionId = x.FuncionId.ToString("N"), ImagenPanoramicaUrl = x.Pelicula.Fondo, FechaFuncion = x.Funcion.InicioUtc, Destacada = x.Destacada, PeliculaId = x.PeliculaId, NombrePelicula = x.Pelicula.Titulo, EmailUsuario = x.Usuario.Email!, Calificacion = x.Puntuacion, Comentario = x.Comentario, FechaCalificacion = x.FechaUtc }, ct);
        foreach (var r in p.Items) { r.FechaCalificacion = ReglasCine.Local(r.FechaCalificacion); if(r.FechaFuncion.HasValue)r.FechaFuncion = ReglasCine.Local(r.FechaFuncion.Value); }
        return p;
    }
    [HttpGet("estadisticas"), Authorize(Roles = "Administrador")]
    public async Task<EstadisticasResenas> Estadisticas(CancellationToken ct)
    {
        var resumen = await db.Database.SqlQueryRaw<FilaEstadisticasResenas>("""
            SELECT count(*)::int AS "Total",count(DISTINCT pelicula_id)::int AS "Peliculas",
                count(DISTINCT usuario_id)::int AS "Usuarios",coalesce(avg(puntuacion),0)::double precision AS "Promedio",
                count(*) FILTER(WHERE puntuacion=1)::int AS "Uno",count(*) FILTER(WHERE puntuacion=2)::int AS "Dos",
                count(*) FILTER(WHERE puntuacion=3)::int AS "Tres",count(*) FILTER(WHERE puntuacion=4)::int AS "Cuatro",
                count(*) FILTER(WHERE puntuacion=5)::int AS "Cinco"
            FROM cine.resenas
            """).SingleAsync(ct);
        var cantidades = new[] { resumen.Uno, resumen.Dos, resumen.Tres, resumen.Cuatro, resumen.Cinco };
        return new(resumen.Total, resumen.Peliculas, resumen.Usuarios, resumen.Promedio,
            cantidades.Select((cantidad, indice) => new RecuentoCatalogo(indice + 1, cantidad)).Where(x => x.Uso > 0).ToList());
    }
    [HttpDelete("{id:guid}"), Authorize(Roles = "Administrador")]
    public async Task Eliminar(Guid id, CancellationToken ct)
    {
        if (await db.Resenas.Where(x => x.Id == id).ExecuteDeleteAsync(ct) == 0) throw new KeyNotFoundException();
    }
    [HttpPut("{id:guid}/destacada"), Authorize(Roles = "Administrador")]
    public async Task Destacar(Guid id, CambiarEstado dto, CancellationToken ct)
    {
        if (await db.Resenas.Where(x => x.Id == id).ExecuteUpdateAsync(x => x.SetProperty(r => r.Destacada, dto.Activo), ct) == 0) throw new KeyNotFoundException();
    }
    [HttpPost] public async Task<CalificacionPelicula> Guardar(CalificacionPelicula solicitud, CancellationToken ct)
    {
        if (!solicitud.EsCalificacionValida) throw new ArgumentException("La calificacion debe estar entre 1 y 5 estrellas.");
        var comentario = ReglasCine.Texto(solicitud.Comentario, 4000, "el comentario", false);
        if (!Guid.TryParse(solicitud.FuncionId, out var funcionId)) throw new ArgumentException("Selecciona una función de tu historial.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({Usuario.ToString() + ":" + funcionId}, 0))", ct);
        var funcion = await db.Funciones.Where(f => f.Id == funcionId && db.Ventas.Any(v => v.FuncionId == f.Id && v.UsuarioId == Usuario))
            .Select(f => new { f.PeliculaId, Fin = f.InicioUtc.AddMinutes(f.Duracion), f.Pelicula.Titulo, f.Pelicula.Fondo, f.InicioUtc }).SingleOrDefaultAsync(ct);
        if (funcion is null || funcion.PeliculaId != solicitud.PeliculaId) throw new OperacionCineException("Solo puedes calificar una función que hayas comprado.");
        if (funcion.Fin > DateTime.UtcNow) throw new OperacionCineException("Podrás calificar cuando termine la función.");
        var registro = await db.Resenas.SingleOrDefaultAsync(x => x.UsuarioId == Usuario && x.FuncionId == funcionId, ct);
        if (registro is not null) throw new OperacionCineException("Ya calificaste esta función.");
        registro = new() { UsuarioId = Usuario, PeliculaId = funcion.PeliculaId, FuncionId = funcionId }; db.Resenas.Add(registro);
        registro.Puntuacion = solicitud.Calificacion; registro.Comentario = comentario; registro.FechaUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        solicitud.Id = registro.Id.ToString("N"); solicitud.NombrePelicula = funcion.Titulo; solicitud.ImagenPanoramicaUrl = funcion.Fondo; solicitud.FechaFuncion = ReglasCine.Local(funcion.InicioUtc);
        solicitud.EmailUsuario = User.Identity!.Name!; solicitud.FechaCalificacion = ReglasCine.Local(registro.FechaUtc); return solicitud;
    }

    [HttpGet("pendientes")]
    public async Task<Pagina<CalificacionPendiente>> Pendientes([FromQuery] ConsultaPagina filtro, CancellationToken ct)
    {
        filtro.Tamano = 6;
        var query = db.Funciones.AsNoTracking().Where(f => f.InicioUtc.AddMinutes(f.Duracion) <= DateTime.UtcNow &&
            db.Ventas.Any(v => v.FuncionId == f.Id && v.UsuarioId == Usuario) &&
            !db.Resenas.Any(r => r.FuncionId == f.Id && r.UsuarioId == Usuario));
        var pagina = await repositorio.PaginarAsync(query.OrderByDescending(f => f.InicioUtc).ThenBy(f => f.Id).Select(f => new CalificacionPendiente
        { FuncionId = f.Id.ToString("N"), PeliculaId = f.PeliculaId, NombrePelicula = f.Pelicula.Titulo, ImagenPanoramicaUrl = f.Pelicula.Fondo, FechaFuncion = f.InicioUtc, Sala = f.Sala.Nombre }), filtro, ct);
        foreach(var pendiente in pagina.Items)pendiente.FechaFuncion = ReglasCine.Local(pendiente.FechaFuncion);
        return pagina;
    }
}

public sealed class FilaEstadisticasResenas
{
    public int Total { get; set; }
    public int Peliculas { get; set; }
    public int Usuarios { get; set; }
    public double Promedio { get; set; }
    public int Uno { get; set; }
    public int Dos { get; set; }
    public int Tres { get; set; }
    public int Cuatro { get; set; }
    public int Cinco { get; set; }
}
