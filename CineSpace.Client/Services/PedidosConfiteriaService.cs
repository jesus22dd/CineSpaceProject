using System.Text.Json;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
using Microsoft.JSInterop;
namespace CineSpace.Client.Services;
public sealed class PedidosConfiteriaService(ApiCine api, IJSRuntime js, CatalogosService catalogos)
{
    private const string CarritoKey = "cinespace-cart-draft-v2";
    private sealed record CarritoLocal(string Solicitud, List<ItemCarritoConfiteria>? Items);
    private readonly Dictionary<string, PedidoConfiteria> cache = [];
    private List<ItemCarritoConfiteria> carrito = [];
    private List<PedidoConfiteria> pagina = [];
    private List<ComboConfiteria> cotizados = [];
    private bool cargado;
    public event Action? Changed;
    public string? ErrorCarga => null;
    public bool EsPersonal => api.Sesion.Usuario?.Rol is RolUsuario.Administrador or RolUsuario.Trabajador;
    public static string EstadoNombre(EstadoPedidoConfiteria estado) => estado == EstadoPedidoConfiteria.Pendiente ? "Pagado · por retirar" : estado.ToString();
    public IReadOnlyList<ItemCarritoConfiteria> Carrito => carrito;
    public int CantidadCarrito => carrito.Sum(x => x.Cantidad);
    public decimal TotalCarrito => carrito.Sum(x => x.Cantidad * x.PrecioUnitario);
    public string SolicitudCarrito { get; private set; } = Guid.NewGuid().ToString("N");
    public IReadOnlyList<PedidoConfiteria> MisPedidos => pagina;
    public async Task InicializarAsync()
    {
        if (cargado) return; cargado = true;
        try
        {
            var texto = await js.InvokeAsync<string?>("localStorage.getItem", CarritoKey);
            if (texto is not null)
            {
                var borrador = JsonSerializer.Deserialize<CarritoLocal>(texto);
                carrito = borrador?.Items ?? [];
                if (Guid.TryParse(borrador?.Solicitud, out var solicitud)) SolicitudCarrito = solicitud.ToString("N");
            }
            else
            {
                var anterior = await js.InvokeAsync<string?>("localStorage.getItem", "cinespace-cart-draft-v1");
                carrito = anterior is null ? [] : JsonSerializer.Deserialize<List<ItemCarritoConfiteria>>(anterior) ?? [];
            }
        }
        catch (Exception ex) when (ex is JSException or JsonException) { carrito = []; }
        carrito = carrito.Where(x => x.Cantidad is > 0 and <= 20).Take(30).ToList();
        if (carrito.Count > 0) { try { await Persistir(); } catch (JSException) { } }
    }
    public async Task AñadirAsync(int id, int cantidad = 1)
    {
        var combo = catalogos.Combos.FirstOrDefault(x => x.Id == id && x.Publicado) ?? throw new InvalidOperationException("Este producto ya no esta disponible.");
        var anterior = carrito.FirstOrDefault(x => x.ComboId == id); var total = (anterior?.Cantidad ?? 0) + cantidad;
        if (total is < 1 or > 20) throw new ArgumentException("Elige entre 1 y 20 unidades por producto.");
        carrito.RemoveAll(x => x.ComboId == id); carrito.Add(new(id, combo.Nombre, total, combo.Precio)); await Guardar();
    }
    public async Task CantidadAsync(int id, int cantidad) { var item = carrito.FirstOrDefault(x => x.ComboId == id); if (item is null) return; if (cantidad is < 0 or > 20) throw new ArgumentException("Cantidad no valida."); carrito.RemoveAll(x => x.ComboId == id); if (cantidad > 0) carrito.Add(item with { Cantidad = cantidad }); await Guardar(); }
    public bool CarritoDesactualizado { get; private set; }
    public string? FotoCarrito(int id) => cotizados.FirstOrDefault(x => x.Id == id)?.Foto;
    public async Task CotizarCarritoAsync()
    {
        cotizados = carrito.Count == 0 ? [] : await api.Get<List<ComboConfiteria>>("api/catalogos/combos/carrito?" + string.Join("&", carrito.Select(x => "ids=" + x.ComboId)));
        CarritoDesactualizado = carrito.Any(x => !cotizados.Any(c => c.Id == x.ComboId && c.Precio == x.PrecioUnitario));
        Changed?.Invoke();
    }
    public async Task VaciarCarritoAsync() { carrito.Clear(); await Guardar(); }
    public async Task ActualizarCarritoAsync()
    {
        await CotizarCarritoAsync();
        carrito = carrito.Where(x => cotizados.Any(c => c.Id == x.ComboId)).Select(x => x with { Nombre = cotizados.Single(c => c.Id == x.ComboId).Nombre, PrecioUnitario = cotizados.Single(c => c.Id == x.ComboId).Precio }).ToList();
        CarritoDesactualizado = false; await Guardar();
    }
    private Task Persistir() => js.InvokeVoidAsync("localStorage.setItem", CarritoKey, JsonSerializer.Serialize(new CarritoLocal(SolicitudCarrito, carrito))).AsTask();
    private async Task Guardar() { SolicitudCarrito = Guid.NewGuid().ToString("N"); await Persistir(); Changed?.Invoke(); }
    public async Task<PedidoConfiteria> CrearAsync(string nombre, string solicitud, PagoPedidoSolicitud pago)
    {
        PedidoConfiteria pedido;
        try { pedido = await api.Post<PedidoConfiteria>("api/pedidos", new SolicitudPedido(nombre, solicitud, carrito.Select(x => new LineaPedidoSolicitud(x.ComboId,x.Cantidad)).ToList(), TotalCarrito, pago)); }
        catch (InvalidOperationException) { try { await CotizarCarritoAsync(); } catch (InvalidOperationException) { } throw; }
        cache[pedido.Token] = pedido;
        try { await VaciarCarritoAsync(); } catch (JSException) { Changed?.Invoke(); }
        return pedido;
    }
    public async Task<Pagina<PedidoConfiteria>> ConsultarAsync(ConsultaPagina f, bool personal = false) { var p = await api.Get<Pagina<PedidoConfiteria>>("api/pedidos" + (personal ? "?" : "/mios?") + ApiCine.Consulta(f)); pagina = p.Items; foreach (var item in pagina) cache[item.Token] = item; return p; }
    public PedidoConfiteria? ObtenerPorToken(string token) => cache.GetValueOrDefault(token);
    public async Task<PedidoConfiteria> ObtenerAsync(string token) { var pedido = await api.Get<PedidoConfiteria>("api/pedidos/" + Uri.EscapeDataString(token)); cache[pedido.Token] = pedido; return pedido; }
    public IReadOnlyList<PedidoConfiteria> ListadoPersonal() => pagina;
    public async Task<PedidoConfiteria?> Verificar(string codigo) { try { return await ObtenerAsync(codigo); } catch (InvalidOperationException) { return null; } }
    public async Task EntregarAsync(string token) { cache[token] = await api.Post<PedidoConfiteria>($"api/pedidos/{token}/entregar"); Changed?.Invoke(); }
}
