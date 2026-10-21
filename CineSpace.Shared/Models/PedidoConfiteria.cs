namespace CineSpace.Shared.Models;

public enum EstadoPedidoConfiteria { Pendiente, Retirado, Cancelado }
public sealed record ItemCarritoConfiteria(int ComboId, string Nombre, int Cantidad, decimal PrecioUnitario);
public sealed class PedidoConfiteria
{
    public string Numero { get; set; } = "";
    public string Token { get; set; } = "";
    public string Solicitud { get; set; } = "";
    public string CodigoRetiro { get; set; } = "";
    public string NombreRetiro { get; set; } = "";
    public string EmailUsuario { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaPago { get; set; }
    public DateTime? FechaRetiro { get; set; }
    public string EntregadoPor { get; set; } = "";
    public string MedioPago { get; set; } = "";
    public EstadoPedidoConfiteria Estado { get; set; }
    public List<LineaVentaConfiteria> Lineas { get; set; } = new();
    public decimal Total => Lineas.Sum(x => x.Cantidad * x.PrecioUnitario);
    public int Unidades => Lineas.Sum(x => x.Cantidad);
    public PedidoConfiteria Copiar() => new() { Numero = Numero, Token = Token, Solicitud = Solicitud, CodigoRetiro = CodigoRetiro, NombreRetiro = NombreRetiro, EmailUsuario = EmailUsuario, FechaCreacion = FechaCreacion, FechaPago = FechaPago, FechaRetiro = FechaRetiro, EntregadoPor = EntregadoPor, MedioPago = MedioPago, Estado = Estado, Lineas = Lineas.Select(x => x with { }).ToList() };
}
public sealed class EstadoPedidosConfiteria
{
    public int Revision { get; set; }
    public string SolicitudCarrito { get; set; } = Guid.NewGuid().ToString("N");
    public List<ItemCarritoConfiteria> Carrito { get; set; } = new();
    public List<PedidoConfiteria> Pedidos { get; set; } = new();
    public List<string> PedidosInvitado { get; set; } = new();
    public EstadoPedidosConfiteria Copiar() => new() { Revision = Revision, SolicitudCarrito = SolicitudCarrito, Carrito = Carrito.Select(x => x with { }).ToList(), Pedidos = Pedidos.Select(x => x.Copiar()).ToList(), PedidosInvitado = PedidosInvitado.ToList() };
}
