using System.Security.Claims;
using CineSpace.Server.Data;
using CineSpace.Server.Models;
using CineSpace.Server.Repositories;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace CineSpace.Server.Services;

public interface IVentasService
{
    Task<VentaTicket> ComprarAsync(VentaTicket solicitud, ClaimsPrincipal comprador, CancellationToken ct);
    Task<Pagina<VentaTicket>> ListarAsync(ConsultaPagina filtro, ClaimsPrincipal principal, bool todas, CancellationToken ct);
    Task<VentaTicket> VerificarAsync(string codigo, bool registrar, CancellationToken ct);
}

public sealed class VentasService(CineSpaceDbContext db, IRepositorioCine repositorio) : IVentasService
{
    public async Task<VentaTicket> ComprarAsync(VentaTicket dto, ClaimsPrincipal principal, CancellationToken ct)
    {
        var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var personal = principal.IsInRole("Administrador") || principal.IsInRole("Trabajador");
        var solicitud = ReglasCine.Texto(dto.Solicitud, 64, "la solicitud");
        if (!Guid.TryParse(dto.FuncionId, out var funcionId)) throw new ArgumentException("Selecciona una funcion valida.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var sala = await db.Funciones.Where(x => x.Id == funcionId).Select(x => (int?)x.SalaId).SingleOrDefaultAsync(ct) ?? throw new ArgumentException("La funcion ya no existe.");
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({sala})", ct);
        var anterior = await db.Ventas.AsNoTracking().SingleOrDefaultAsync(x => x.Solicitud == solicitud && x.UsuarioId == userId, ct);
        if (anterior is not null)
        {
            if (anterior.UsuarioId != userId && anterior.OperadorId != userId) throw new UnauthorizedAccessException();
            return MapasCine.Venta(anterior);
        }
        var funcion = await db.Funciones.Include(x => x.Pelicula).ThenInclude(x => x.Generos).ThenInclude(x => x.Genero)
            .Include(x => x.Sala).ThenInclude(x => x.Diseno).Include(x => x.Sala).ThenInclude(x => x.TipoSala).Include(x => x.Experiencia)
            .SingleOrDefaultAsync(x => x.Id == funcionId, ct) ?? throw new ArgumentException("La funcion ya no existe.");
        if (funcion.InicioUtc <= DateTime.UtcNow || !funcion.Pelicula.Publicada || !funcion.Sala.Activa) throw new OperacionCineException("La funcion ya cerro. Elige otro horario.");
        var cantidad = dto.CantidadTickets;
        var plano = ReglasCine.Leer<PlanoSala>(funcion.Sala.Diseno.PlanoJson);
        ReglasCine.Butacas(dto.Asientos, plano, cantidad);
        if (await db.ButacasVendidas.AnyAsync(x => x.FuncionId == funcionId && dto.Asientos.Contains(x.Asiento), ct)) throw new OperacionCineException("Una de estas butacas acaba de venderse. Elige otra butaca.");
        var comprador = await db.Users.FindAsync([userId], ct) ?? throw new UnauthorizedAccessException();
        if (!comprador.Activo) throw new UnauthorizedAccessException();
        var metodo = ReglasCine.MetodoPago(dto.MetodoPago, personal);
        var precio = ReglasCine.Precio(funcion.Sala.Precio, funcion.Sala.Descuento);
        var factura = "F-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + ReglasCine.Codigo(12);
        var venta = new VentaTicket
        {
            Solicitud = solicitud, FuncionId = funcion.Id.ToString("N"), FechaFuncion = ReglasCine.Local(funcion.InicioUtc).Date,
            PeliculaId = funcion.PeliculaId, Pelicula = funcion.Pelicula.Titulo, Generos = funcion.Pelicula.Generos.Select(x => x.Genero.Nombre).ToList(),
            NumeroSala = funcion.SalaId, TipoSala = funcion.Sala.TipoSalaId, TipoSalaNombre = funcion.Sala.TipoSala.Nombre,
            TipoExperiencia = funcion.ExperienciaId, ExperienciaNombre = funcion.Experiencia.Nombre, TicketsMayores = cantidad, TicketsMenores = 0,
            Horario = ReglasCine.Local(funcion.InicioUtc).ToString("HH:mm"), Asientos = dto.Asientos.OrderBy(x => x).ToList(),
            NumeroFactura = factura, CodigoValidacion = ReglasCine.Codigo(12), FechaVenta = ReglasCine.Ahora,
            Cliente = personal ? ReglasCine.Texto(dto.Cliente, 200, "el nombre del cliente") : $"{comprador.Nombre} {comprador.Apellido}",
            DocumentoCliente = ReglasCine.Texto(dto.DocumentoCliente, 30, "el documento"), EmailUsuario = comprador.Email!, MetodoPago = metodo,
            AsientosMayoresPorCategoria = new() { [CategoriaAsiento.Regular] = cantidad }, PreciosMayoresPorCategoria = new() { [CategoriaAsiento.Regular] = precio }
        };
        var registro = new VentaRegistro { UsuarioId = userId, OperadorId = personal ? userId : null, FuncionId = funcionId, Solicitud = solicitud,
            NumeroFactura = factura, Codigo = venta.CodigoValidacion, Total = precio * cantidad, Cantidad = cantidad, MetodoPago = metodo, SnapshotJson = ReglasCine.Serializar(venta) };
        registro.Butacas = dto.Asientos.Select(x => new ButacaVendidaRegistro { FuncionId = funcionId, Asiento = x, VentaId = registro.Id }).ToList();
        db.Ventas.Add(registro); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return venta;
    }

    public async Task<Pagina<VentaTicket>> ListarAsync(ConsultaPagina f, ClaimsPrincipal principal, bool todas, CancellationToken ct)
    {
        var q = db.Ventas.AsNoTracking().AsQueryable();
        if (!todas) { var id = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!); q = q.Where(x => x.UsuarioId == id); }
        if (!string.IsNullOrWhiteSpace(f.Buscar)) { var s = "%" + f.Buscar.Trim() + "%"; q = q.Where(x => EF.Functions.ILike(x.Funcion.Pelicula.Titulo, s) || EF.Functions.ILike(x.NumeroFactura, s) || EF.Functions.ILike(x.Usuario.Email!, s)); }
        if (f.Sala.HasValue) q = q.Where(x => x.Funcion.SalaId == f.Sala);
        if (f.Genero.HasValue) q = q.Where(x => x.Funcion.Pelicula.Generos.Any(g => g.GeneroId == f.Genero));
        if (f.Desde.HasValue) { var fecha = ReglasCine.Utc(f.Desde.Value.ToDateTime(TimeOnly.MinValue)); q = q.Where(x => x.FechaUtc >= fecha); }
        if (f.Hasta.HasValue) { var fecha = ReglasCine.Utc(f.Hasta.Value.AddDays(1).ToDateTime(TimeOnly.MinValue)); q = q.Where(x => x.FechaUtc < fecha); }
        var page = await repositorio.PaginarAsync(f.Orden == "antiguas" ? q.OrderBy(x => x.FechaUtc).ThenBy(x => x.Id) : q.OrderByDescending(x => x.FechaUtc).ThenBy(x => x.Id), f, ct);
        var result = page.Items.Select(MapasCine.Venta).ToList();
        if (!todas && result.Count > 0)
        {
            var id = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!); var peliculas = result.Select(x => x.PeliculaId).Distinct().ToArray();
            var calificadas = await db.Resenas.Where(x => x.UsuarioId == id && peliculas.Contains(x.PeliculaId)).Select(x => x.PeliculaId).ToListAsync(ct);
            foreach (var venta in result) venta.FueCalificada = calificadas.Contains(venta.PeliculaId);
        }
        return new(result, page.Total, page.Numero, page.Tamano);
    }

    public async Task<VentaTicket> VerificarAsync(string codigo, bool registrar, CancellationToken ct)
    {
        codigo = ReglasCine.Texto(codigo, 50, "el codigo").ToUpperInvariant();
        var venta = await db.Ventas.SingleOrDefaultAsync(x => x.Codigo == codigo, ct) ?? throw new ArgumentException("No encontramos este boleto.");
        if (registrar)
        {
            var verificadaUtc = DateTime.UtcNow;
            var actualizadas = await db.Ventas.Where(x => x.Id == venta.Id && x.VerificadaUtc == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.VerificadaUtc, verificadaUtc), ct);
            if (actualizadas == 0) throw new OperacionCineException("Este boleto ya fue verificado.");
            venta.VerificadaUtc = verificadaUtc;
        }
        return MapasCine.Venta(venta);
    }
}
