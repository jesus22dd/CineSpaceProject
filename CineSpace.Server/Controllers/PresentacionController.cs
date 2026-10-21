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

[ApiController, Route("api/presentacion")]
public sealed class PresentacionController(CineSpaceDbContext db, IRepositorioCine repo, IImagenStorageService imagenes, IEstrenosService estrenos) : ControllerBase
{
    [HttpGet("banners"), AllowAnonymous]
    public async Task<EstadoBanners> Banners(CancellationToken ct)
    {
        var automatico = await db.Ajustes.AsNoTracking().Where(x => x.Clave == "banners.automatico").Select(x => x.Valor).SingleOrDefaultAsync(ct) != "false";
        var q = db.Banners.AsNoTracking();
        if (!User.IsInRole("Administrador")) q = q.Where(x => x.Activo && x.Pelicula.Publicada && db.Funciones.Any(f => f.PeliculaId == x.PeliculaId && f.InicioUtc > DateTime.UtcNow));
        var rows = await q.Include(x=>x.Pelicula).ThenInclude(x=>x.Generos).ThenInclude(x=>x.Genero).OrderBy(x=>x.Orden).ThenBy(x=>x.Id).Take(50).ToListAsync(ct);
        return new() { Automatico = automatico, Banners = rows.Select(MapasCine.Banner).ToList(), Peliculas = rows.Select(x=>x.Pelicula).DistinctBy(x=>x.Id).Select(MapasCine.Pelicula).ToList() };
    }
    [HttpGet("banners/pagina"), Authorize(Roles = "Administrador")]
    public async Task<Pagina<BannerInicio>> PaginaBanners([FromQuery] ConsultaPagina filtro, CancellationToken ct)
    {
        var q = db.Banners.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filtro.Buscar))
        {
            var texto = "%" + filtro.Buscar.Trim() + "%";
            var etiquetas = Enum.GetValues<EtiquetaBanner>().Where(x => x.Nombre().Contains(filtro.Buscar, StringComparison.OrdinalIgnoreCase)).Select(x => (int)x).ToArray();
            q = q.Where(x => EF.Functions.ILike(x.Pelicula.Titulo,texto) || etiquetas.Contains(x.Etiqueta));
        }
        var page = await repo.PaginarAsync(q.OrderBy(x => x.Orden).ThenBy(x => x.Id),filtro,ct);
        return new(page.Items.Select(MapasCine.Banner).ToList(),page.Total,page.Numero,page.Tamano);
    }
    [HttpPut("banners/modo"), Authorize(Roles = "Administrador")]
    public async Task Modo(CambiarEstado solicitud, CancellationToken ct)
    {
        var ajuste = await db.Ajustes.FindAsync(["banners.automatico"], ct);
        if (ajuste is null) { ajuste = new() { Clave = "banners.automatico" }; db.Ajustes.Add(ajuste); }
        ajuste.Valor = solicitud.Activo ? "true" : "false"; await db.SaveChangesAsync(ct);
    }
    [HttpPost("banners"), Authorize(Roles = "Administrador")]
    public Task<BannerInicio> CrearBanner(BannerInicio solicitud, CancellationToken ct) => GuardarBanner(null, solicitud, ct);
    [HttpPut("banners/{id:int}"), Authorize(Roles = "Administrador")]
    public Task<BannerInicio> EditarBanner(int id, BannerInicio solicitud, CancellationToken ct) => GuardarBanner(id, solicitud, ct);
    private async Task<BannerInicio> GuardarBanner(int? id, BannerInicio dto, CancellationToken ct)
    {
        if (!await db.Peliculas.AnyAsync(x => x.Id == dto.PeliculaId, ct)) throw new ArgumentException("Selecciona una pelicula existente.");
        if (!Enum.IsDefined(dto.Etiqueta)) throw new ArgumentException("Etiqueta no valida.");
        if (!id.HasValue && await db.Banners.CountAsync(ct) >= 50) throw new ArgumentException("Puedes guardar hasta 50 banners.");
        await using var archivos = new CargaImagenes(imagenes);
        var imagen = await archivos.RecibirAsync(dto.Imagen, "banners", true, ct);
        var entity = id.HasValue ? await db.Banners.FindAsync([id.Value], ct) ?? throw new KeyNotFoundException() : new BannerRegistro { Orden = (await db.Banners.MaxAsync(x => (int?)x.Orden, ct) ?? -1) + 1 };
        entity.PeliculaId = dto.PeliculaId; entity.Imagen = imagen; entity.Etiqueta = (int)dto.Etiqueta; entity.Foco = Math.Clamp(dto.FocoVertical, 0, 100); entity.Activo = dto.Activo;
        if (!id.HasValue) db.Banners.Add(entity);
        await db.SaveChangesAsync(ct); archivos.Confirmar(); return MapasCine.Banner(entity);
    }
    [HttpDelete("banners/{id:int}"), Authorize(Roles = "Administrador")]
    public async Task EliminarBanner(int id, CancellationToken ct) { var entity = await db.Banners.FindAsync([id], ct) ?? throw new KeyNotFoundException(); db.Banners.Remove(entity); await db.SaveChangesAsync(ct); }
    [HttpPut("banners/{id:int}/estado"), Authorize(Roles = "Administrador")]
    public async Task ActivarBanner(int id, CambiarEstado dto, CancellationToken ct) { if (await db.Banners.Where(x => x.Id == id).ExecuteUpdateAsync(x => x.SetProperty(b => b.Activo, dto.Activo), ct) == 0) throw new KeyNotFoundException(); }
    [HttpPost("banners/{id:int}/mover"), Authorize(Roles = "Administrador")]
    public async Task Mover(int id, MoverElemento dto, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(-100)", ct);
        var banners = await db.Banners.OrderBy(x => x.Orden).ThenBy(x => x.Id).ToListAsync(ct);
        var indice = banners.FindIndex(x => x.Id == id); var destino = indice + Math.Sign(dto.Direccion);
        if (indice < 0 || destino < 0 || destino >= banners.Count) return;
        (banners[indice], banners[destino]) = (banners[destino], banners[indice]);
        for (var i = 0; i < banners.Count; i++) banners[i].Orden = i;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    [HttpGet("estrenos"), AllowAnonymous]
    public Task<Pagina<ProximoEstreno>> Estrenos([FromQuery] ConsultaPagina f, CancellationToken ct)
        => estrenos.ListarAsync(f, User.IsInRole("Administrador"), ct);
    [HttpPost("estrenos"), Authorize(Roles = "Administrador")]
    public Task<ProximoEstreno> CrearEstreno(ProximoEstreno dto, CancellationToken ct) => estrenos.GuardarAsync(null, dto, ct);
    [HttpPut("estrenos/{id:int}"), Authorize(Roles = "Administrador")]
    public Task<ProximoEstreno> EditarEstreno(int id, ProximoEstreno dto, CancellationToken ct) => estrenos.GuardarAsync(id, dto, ct);
    [HttpDelete("estrenos/{id:int}"), Authorize(Roles = "Administrador")]
    public Task EliminarEstreno(int id, CancellationToken ct) => estrenos.EliminarAsync(id, ct);
}
