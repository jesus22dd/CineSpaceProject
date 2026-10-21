using CineSpace.Server.Data;
using CineSpace.Server.Models;
using CineSpace.Server.Repositories;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace CineSpace.Server.Services;

public interface IPedidosService
{
    Task<PedidoConfiteria> CrearAsync(SolicitudPedido solicitud, Guid? usuario, string invitado, CancellationToken ct);
    Task<Pagina<PedidoConfiteria>> ListarAsync(ConsultaPagina filtro, Guid? usuario, string invitado, bool personal, CancellationToken ct);
    Task<PedidoConfiteria> ObtenerAsync(string token, Guid? usuario, string invitado, bool personal, CancellationToken ct);
    Task<PedidoConfiteria> EntregarAsync(string token, Guid operador, CancellationToken ct);
    Task CancelarAsync(string token, Guid? usuario, string invitado, bool personal, CancellationToken ct);
}

public sealed class PedidosService(CineSpaceDbContext db, IRepositorioCine repositorio) : IPedidosService
{
    private IQueryable<PedidoRegistro> Consulta => db.Pedidos.AsNoTracking().Include(x => x.Lineas).Include(x => x.Usuario).Include(x => x.EntregadoPor);
    public async Task<PedidoConfiteria> CrearAsync(SolicitudPedido solicitud, Guid? usuario, string invitado, CancellationToken ct)
    {
        var nombre = ReglasCine.Texto(solicitud.Nombre, 160, "el nombre de retiro");
        var clave = ReglasCine.Texto(solicitud.Solicitud, 64, "la solicitud");
        var metodo = ValidarPago(solicitud.Pago);
        if (!solicitud.TotalEsperado.HasValue || solicitud.TotalEsperado <= 0) throw new ArgumentException("Revisa el total del carrito antes de pagar.");
        if (solicitud.Items is null || solicitud.Items.Count is < 1 or > 30 || solicitud.Items.Any(x => x.Cantidad is < 1 or > 20) || solicitud.Items.Select(x => x.ComboId).Distinct().Count() != solicitud.Items.Count)
            throw new ArgumentException("Revisa las cantidades del carrito.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Serializa los reintentos del mismo carrito sin bloquear otros pedidos.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({clave}, 0))", ct);
        var existente = await Consulta.SingleOrDefaultAsync(x => x.Solicitud == clave, ct);
        if (existente is not null) { ExigirPropietario(existente, usuario, invitado, false); return MapasCine.Pedido(existente); }
        var ids = solicitud.Items.Select(x => x.ComboId).ToArray();
        var productos = await db.Combos.AsNoTracking().Include(x => x.Categoria).Where(x => ids.Contains(x.Id) && x.Publicado).ToListAsync(ct);
        if (productos.Count != ids.Length) throw new OperacionCineException("Un producto ya no esta disponible. Actualiza tu carrito.");
        var ahora = DateTime.UtcNow;
        var pedido = new PedidoRegistro { Numero = "P-" + ReglasCine.Codigo(12), Codigo = ReglasCine.Codigo(12), Token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24)).ToLowerInvariant(),
            Solicitud = clave, UsuarioId = usuario, PropietarioInvitado = usuario is null ? invitado : "", NombreRetiro = nombre, MetodoPago = metodo, CreadoUtc = ahora, PagadoUtc = ahora };
        var catalogo = productos.ToDictionary(x => x.Id);
        pedido.Lineas = solicitud.Items.Select(linea => { var combo = catalogo[linea.ComboId]; return new PedidoLineaRegistro
            { ComboId = combo.Id, Nombre = combo.Nombre, Categoria = combo.Categoria.Nombre, Cantidad = linea.Cantidad, PrecioUnitario = combo.Precio }; }).ToList();
        pedido.Total = pedido.Lineas.Sum(x => x.Cantidad * x.PrecioUnitario);
        if (solicitud.TotalEsperado.HasValue && solicitud.TotalEsperado.Value != pedido.Total)
            throw new OperacionCineException("El precio de un combo cambio. Actualiza el carrito y revisa el nuevo total.");
        db.Pedidos.Add(pedido); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return MapasCine.Pedido(pedido);
    }
    public async Task<Pagina<PedidoConfiteria>> ListarAsync(ConsultaPagina f, Guid? usuario, string invitado, bool personal, CancellationToken ct)
    {
        var q = Consulta;
        if (!personal) q = q.Where(x => usuario.HasValue ? x.UsuarioId == usuario : x.UsuarioId == null && x.PropietarioInvitado == invitado);
        if (!string.IsNullOrWhiteSpace(f.Buscar)) { var texto = "%" + f.Buscar.Trim() + "%"; q = q.Where(x => EF.Functions.ILike(x.Numero, texto) || EF.Functions.ILike(x.NombreRetiro, texto) || EF.Functions.ILike(x.Codigo, texto)); }
        if (Enum.TryParse<EstadoPedidoConfiteria>(f.Estado, out var estado)) q = q.Where(x => x.Estado == estado.ToString());
        var page = await repositorio.PaginarAsync(f.Orden == "antiguas" ? q.OrderBy(x => x.CreadoUtc).ThenBy(x => x.Id) : q.OrderByDescending(x => x.CreadoUtc).ThenBy(x => x.Id), f, ct);
        return new(page.Items.Select(MapasCine.Pedido).ToList(), page.Total, page.Numero, page.Tamano);
    }
    public async Task<PedidoConfiteria> ObtenerAsync(string token, Guid? usuario, string invitado, bool personal, CancellationToken ct)
    {
        var pedido = await Consulta.SingleOrDefaultAsync(x => x.Token == token || personal && x.Codigo == token.ToUpper(), ct) ?? throw new KeyNotFoundException("No encontramos este pedido.");
        ExigirPropietario(pedido, usuario, invitado, personal); return MapasCine.Pedido(pedido);
    }
    public async Task<PedidoConfiteria> EntregarAsync(string token, Guid operador, CancellationToken ct)
    {
        var ahora = DateTime.UtcNow;
        var cambios = await db.Pedidos.Where(x => x.Token == token && x.Estado == "Pendiente" && x.PagadoUtc <= ahora && (x.MetodoPago == "QR" || x.MetodoPago == "Tarjeta")).ExecuteUpdateAsync(p => p
            .SetProperty(x => x.Estado, "Retirado").SetProperty(x => x.EntregadoPorId, operador).SetProperty(x => x.RetiradoUtc, ahora), ct);
        if (cambios != 1) throw new OperacionCineException("El pedido no tiene un pago confirmado, ya fue entregado o no está disponible.");
        return await ObtenerAsync(token, operador, "", true, ct);
    }
    public Task CancelarAsync(string token, Guid? usuario, string invitado, bool personal, CancellationToken ct)
        => throw new OperacionCineException("Los pedidos pagados no admiten cancelación en línea. Contacta con el personal del cine.");
    private static string ValidarPago(PagoPedidoSolicitud? pago)
    {
        if (pago is null || pago.MetodoPago is not ("QR" or "Tarjeta")) throw new ArgumentException("Confirma un pago con QR o tarjeta de débito antes de crear el pedido.");
        if (pago.MetodoPago == "QR" && !pago.QrConfirmado) throw new ArgumentException("Confirma el pago QR antes de continuar.");
        if (pago.MetodoPago == "Tarjeta")
        {
            var error = PagosReserva.ValidarDebito(pago.Titular ?? "", pago.Numero ?? "", pago.Vencimiento ?? "", pago.Cvv ?? "", ReglasCine.Ahora.Date);
            if (error.Length > 0) throw new ArgumentException(error);
        }
        return pago.MetodoPago;
    }
    private static void ExigirPropietario(PedidoRegistro pedido, Guid? usuario, string invitado, bool personal)
    {
        if (!personal && !(usuario.HasValue && pedido.UsuarioId == usuario || usuario is null && pedido.UsuarioId is null && invitado.Length > 0 && pedido.PropietarioInvitado == invitado)) throw new KeyNotFoundException();
    }
}
