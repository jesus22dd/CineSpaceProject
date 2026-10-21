using CineSpace.Server.Services;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CineSpace.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace CineSpace.Server.Controllers;

[ApiController, Route("api/peliculas")]
public sealed class PeliculasController(IPeliculaCatalogoService peliculas, ITmdbService tmdb, CineSpaceDbContext db) : ControllerBase
{
    [HttpGet("opciones"), Authorize(Roles = "Administrador,Trabajador")]
    public Task<List<Pelicula>> Opciones([FromQuery] string buscar = "", CancellationToken ct = default) => db.Peliculas.AsNoTracking()
        .Where(x => buscar == "" || EF.Functions.ILike(x.Titulo, "%" + buscar + "%")).OrderBy(x => x.Titulo).Take(200)
        .Select(x => new Pelicula { Id = x.Id, TmdbId = x.TmdbId, Titulo = x.Titulo, DuracionMinutos = x.Duracion, ImagenUrl = x.Poster, ImagenPanoramicaUrl = x.Fondo, FechaAgregadaUtc = x.CreadoUtc }).ToListAsync(ct);
    [HttpGet, AllowAnonymous]
    public Task<Pagina<Pelicula>> Listar([FromQuery] ConsultaPagina consulta, CancellationToken ct)
    {
        var personal=User.IsInRole("Administrador") || User.IsInRole("Trabajador");
        if (!personal) consulta.Tamano=8;
        return peliculas.ListarAsync(consulta,personal,ct);
    }
    [HttpGet("{id:int}"), AllowAnonymous]
    public Task<Pelicula> Detalle(int id, CancellationToken ct) => peliculas.ObtenerAsync(id, ct);
    [HttpPost, Authorize(Roles = "Administrador,Trabajador")]
    public Task<Pelicula> Crear(Pelicula entrada, CancellationToken ct) => peliculas.GuardarAsync(null, entrada, ct);
    [HttpPut("{id:int}"), Authorize(Roles = "Administrador,Trabajador")]
    public Task<Pelicula> Guardar(int id, Pelicula entrada, CancellationToken ct) => peliculas.GuardarAsync(id, entrada, ct);
    [HttpDelete("{id:int}"), Authorize(Roles = "Administrador,Trabajador")]
    public Task Eliminar(int id, CancellationToken ct) => peliculas.EliminarAsync(id, ct);
    [HttpGet("tmdb/buscar"), Authorize(Roles = "Administrador,Trabajador")]
    public Task<Pagina<TMDBMovie>> Buscar([FromQuery] string buscar, [FromQuery] int? anio, [FromQuery] int pagina = 1, CancellationToken ct = default) => tmdb.BuscarAsync(buscar, anio, pagina, ct);
    [HttpGet("tmdb/{id:int}"), Authorize(Roles = "Administrador,Trabajador")]
    public Task<Pelicula> Ficha(int id, CancellationToken ct) => tmdb.FichaAsync(id, ct);
    [HttpGet("tmdb/{id:int}/galeria"), Authorize(Roles = "Administrador,Trabajador")]
    public Task<GaleriaPelicula> Galeria(int id, CancellationToken ct) => tmdb.GaleriaAsync(id, ct);
}
