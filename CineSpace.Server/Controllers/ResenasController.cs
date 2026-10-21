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
        var p = await repositorio.PaginarAsync(orden.Select(x => new CalificacionPelicula { Destacada = x.Destacada, PeliculaId = x.PeliculaId, NombrePelicula = x.Pelicula.Titulo, EmailUsuario = x.Usuario.Email!, Calificacion = x.Puntuacion, Comentario = x.Comentario, FechaCalificacion = x.FechaUtc }), f, ct);
        foreach (var r in p.Items) r.FechaCalificacion = ReglasCine.Local(r.FechaCalificacion);
        return p;
    }
    [HttpGet("estadisticas"), Authorize(Roles = "Administrador")]
    public async Task<EstadisticasResenas> Estadisticas(CancellationToken ct)
    {
        var resumen = await db.Resenas.GroupBy(x => 1).Select(g => new { Total = g.Count(), Peliculas = g.Select(x => x.PeliculaId).Distinct().Count(), Usuarios = g.Select(x => x.UsuarioId).Distinct().Count(), Promedio = g.Average(x => x.Puntuacion) }).SingleOrDefaultAsync(ct);
        var distribucion = await db.Resenas.GroupBy(x => x.Puntuacion).OrderBy(g => g.Key).Select(g => new RecuentoCatalogo(g.Key, g.Count())).ToListAsync(ct);
        return new(resumen?.Total ?? 0, resumen?.Peliculas ?? 0, resumen?.Usuarios ?? 0, resumen?.Promedio ?? 0, distribucion);
    }
    [HttpDelete("{pelicula:int}"), Authorize(Roles = "Administrador")]
    public async Task Eliminar(int pelicula, [FromQuery] string email, CancellationToken ct)
    {
        if (await db.Resenas.Where(x => x.PeliculaId == pelicula && x.Usuario.NormalizedEmail == email.ToUpper()).ExecuteDeleteAsync(ct) == 0) throw new KeyNotFoundException();
    }
    [HttpPut("{pelicula:int}/destacada"), Authorize(Roles = "Administrador")]
    public async Task Destacar(int pelicula, [FromQuery] string email, CambiarEstado dto, CancellationToken ct)
    {
        if (await db.Resenas.Where(x => x.PeliculaId == pelicula && x.Usuario.NormalizedEmail == email.ToUpper()).ExecuteUpdateAsync(x => x.SetProperty(r => r.Destacada, dto.Activo), ct) == 0) throw new KeyNotFoundException();
    }
    [HttpPost] public async Task<CalificacionPelicula> Guardar(CalificacionPelicula solicitud, CancellationToken ct)
    {
        if (!solicitud.EsCalificacionValida) throw new ArgumentException("La calificacion debe estar entre 1 y 5 estrellas.");
        var comentario = ReglasCine.Texto(solicitud.Comentario, 4000, "el comentario", false);
        if (!await db.Ventas.AnyAsync(x => x.UsuarioId == Usuario && x.Funcion.PeliculaId == solicitud.PeliculaId && x.Funcion.InicioUtc < DateTime.UtcNow, ct)) throw new OperacionCineException("Puedes calificar despues de asistir a una funcion que hayas comprado.");
        var registro = await db.Resenas.SingleOrDefaultAsync(x => x.UsuarioId == Usuario && x.PeliculaId == solicitud.PeliculaId, ct);
        if (registro is null) { registro = new() { UsuarioId = Usuario, PeliculaId = solicitud.PeliculaId }; db.Resenas.Add(registro); }
        registro.Puntuacion = solicitud.Calificacion; registro.Comentario = comentario; registro.FechaUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        solicitud.EmailUsuario = User.Identity!.Name!; solicitud.FechaCalificacion = ReglasCine.Local(registro.FechaUtc); return solicitud;
    }
}
