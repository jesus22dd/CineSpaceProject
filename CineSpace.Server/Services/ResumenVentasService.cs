using System.Text.Json;
using CineSpace.Server.Data;
using CineSpace.Server.Filters;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CineSpace.Server.Services;

public interface IResumenVentasService
{
    Task<ResultadoResumenVentas> ConsultarAsync(FiltroVentas filtro, ConsultaPagina consulta, CancellationToken ct);
}

public sealed class ResumenVentasService(CineSpaceDbContext db, IMemoryCache cache, CacheLecturas lecturas) : IResumenVentasService
{
    public async Task<ResultadoResumenVentas> ConsultarAsync(FiltroVentas filtro, ConsultaPagina consulta, CancellationToken ct)
    {
        var ventas = AnaliticaService.Filtrar(db.Ventas.AsNoTracking(), filtro);
        var key = lecturas.Key("ventas.totales:" + ReglasCine.Ahora.ToString("yyyy-MM-dd") + ":" + JsonSerializer.Serialize(filtro));
        // Una agregación en SQL por filtro, reutilizable al cambiar de página.
        // La revisión cambia en cada escritura; las entradas anteriores caducan en 30 s.
        var totales = await cache.GetOrCreateAsync(key, async entrada =>
        {
            entrada.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            return await ventas.GroupBy(x => 1).Select(g => new TotalesResumenVentas(
                g.Count(),
                g.Sum(x => x.Cantidad - Convert.ToInt32(CineSpaceDbContext.JsonTexto(x.SnapshotJson, "ticketsMenores") ?? "0")),
                g.Sum(x => Convert.ToInt32(CineSpaceDbContext.JsonTexto(x.SnapshotJson, "ticketsMenores") ?? "0")),
                g.Sum(x => x.Total))).SingleOrDefaultAsync(ct) ?? TotalesResumenVentas.Vacio;
        }) ?? TotalesResumenVentas.Vacio;

        var tamano = Math.Clamp(consulta.Tamano, 1, 100);
        var numero = Math.Clamp(consulta.Pagina, 1, Math.Max(1, (int)Math.Ceiling(totales.Ventas / (double)tamano)));
        var filas = await ventas.OrderByDescending(x => x.FechaUtc).ThenBy(x => x.Id)
            .Skip((numero - 1) * tamano).Take(tamano)
            .Select(x => new
            {
                x.Id, x.FechaUtc, x.Cantidad, x.Total,
                Pelicula = CineSpaceDbContext.JsonTexto(x.SnapshotJson, "pelicula") ?? x.Funcion.Pelicula.Titulo,
                Generos = CineSpaceDbContext.JsonTexto(x.SnapshotJson, "generos"),
                Genero = CineSpaceDbContext.JsonTexto(x.SnapshotJson, "genero"),
                NumeroSala = x.Funcion.SalaId,
                TipoSala = CineSpaceDbContext.JsonTexto(x.SnapshotJson, "tipoSalaNombre") ?? x.Funcion.Sala.TipoSala.Nombre,
                Experiencia = CineSpaceDbContext.JsonTexto(x.SnapshotJson, "experienciaNombre") ?? x.Funcion.Experiencia.Nombre,
                Menores = Convert.ToInt32(CineSpaceDbContext.JsonTexto(x.SnapshotJson, "ticketsMenores") ?? "0")
            }).ToListAsync(ct);
        var items = filas.Select(x => new FilaResumenVenta
        {
            Id = x.Id, FechaVenta = ReglasCine.Local(x.FechaUtc), Pelicula = x.Pelicula,
            Generos = x.Generos is not null ? ReglasCine.Leer<List<string>>(x.Generos) : string.IsNullOrWhiteSpace(x.Genero) ? [] : [x.Genero],
            NumeroSala = x.NumeroSala, TipoSalaNombre = x.TipoSala, ExperienciaNombre = x.Experiencia,
            TicketsMayores = x.Cantidad - x.Menores, TicketsMenores = x.Menores, CantidadTickets = x.Cantidad, TotalVenta = x.Total
        }).ToList();
        return new(new(items, totales.Ventas, numero, tamano), totales);
    }
}
