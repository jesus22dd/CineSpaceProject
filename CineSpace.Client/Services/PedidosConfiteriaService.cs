using System.Text.Json;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
using Microsoft.JSInterop;
namespace CineSpace.Client.Services;
public sealed class PedidosConfiteriaService(ApiCine api, IJSRuntime js, CatalogosService catalogos)
{
    private readonly Dictionary<string, PedidoConfiteria> cache = [];
    private List<ItemCarritoConfiteria> carrito = [];
    private List<PedidoConfiteria> pagina = [];
    private List<ComboConfiteria> cotizados = [];
    private bool cargado;
    public event Action? Changed;
    public string? ErrorCarga => null;
    public bool EsPersonal => api.Sesion.Usuario?.Rol is RolUsuario.Administrador or RolUsuario.Trabajador;
    public static string EstadoNombre(EstadoPedidoConfiteria estado) => estado == EstadoPedidoConfiteria.Pendiente ? "Pendiente de retiro" : estado.ToString();
    public IReadOnlyList<ItemCarritoConfiteria> Carrito => carrito;
    public int CantidadCarrito => carrito.Sum(x => x.Cantidad);
    public decimal TotalCarrito => carrito.Sum(x => x.Cantidad * x.PrecioUnitario);
    public string SolicitudCarrito { get; private set; } = Guid.NewGuid().ToString("N");
    public IReadOnlyList<PedidoConfiteria> MisPedidos => pagina;
    public async Task InicializarAsync()
    {
        if (cargado) return; cargado = true;
        try { var texto = await js.InvokeAsync<string?>("localStorage.getItem", "cinespace-cart-draft-v1"); if (texto is not null) carrito = JsonSerializer.Deserialize<List<ItemCarritoConfiteria>>(texto) ?? []; }
        catch (Exception ex) when (ex is JSException or JsonException) { carrito = []; }
        carrito = carrito.Where(x => x.Cantidad is > 0 and <= 20).Take(30).ToList();
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
    private async Task Guardar() { SolicitudCarrito = Guid.NewGuid().ToString("N"); await js.InvokeVoidAsync("localStorage.setItem", "cinespace-cart-draft-v1", JsonSerializer.Serialize(carrito)); Changed?.Invoke(); }
    public async Task<PedidoConfiteria> CrearAsync(string nombre, string solicitud)
    {
        PedidoConfiteria pedido;
        try { pedido = await api.Post<PedidoConfiteria>("api/pedidos", new SolicitudPedido(nombre, solicitud, carrito.Select(x => new LineaPedidoSolicitud(x.ComboId,x.Cantidad)).ToList(), TotalCarrito)); }
        catch (InvalidOperationException) { await CotizarCarritoAsync(); throw; }
        cache[pedido.Token] = pedido; await VaciarCarritoAsync(); return pedido;
    }
    public async Task<Pagina<PedidoConfiteria>> ConsultarAsync(ConsultaPagina f, bool personal = false) { var p = await api.Get<Pagina<PedidoConfiteria>>("api/pedidos" + (personal ? "?" : "/mios?") + ApiCine.Consulta(f)); pagina = p.Items; foreach (var item in pagina) cache[item.Token] = item; return p; }
    public PedidoConfiteria? ObtenerPorToken(string token) => cache.GetValueOrDefault(token);
    public async Task<PedidoConfiteria> ObtenerAsync(string token) { var pedido = await api.Get<PedidoConfiteria>("api/pedidos/" + Uri.EscapeDataString(token)); cache[pedido.Token] = pedido; return pedido; }
    public IReadOnlyList<PedidoConfiteria> ListadoPersonal() => pagina;
    public async Task<PedidoConfiteria?> Verificar(string codigo) { try { return await ObtenerAsync(codigo); } catch (InvalidOperationException) { return null; } }
    public async Task EntregarAsync(string token, string medioPago) { cache[token] = await api.Post<PedidoConfiteria>($"api/pedidos/{token}/entregar", new EntregarPedido(medioPago)); Changed?.Invoke(); }
    public async Task CancelarAsync(string token) { await api.Post<object>($"api/pedidos/{token}/cancelar"); cache.Remove(token); Changed?.Invoke(); }
}
