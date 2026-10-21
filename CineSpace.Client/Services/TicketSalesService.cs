using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
namespace CineSpace.Client.Services;
public sealed class TicketSalesService(ApiCine api)
{
    private List<VentaTicket> pagina = [];
    public List<VentaTicket> ObtenerVentas() => pagina;
    public void RegistrarConfirmada(VentaTicket venta) { pagina.RemoveAll(x => x.NumeroFactura == venta.NumeroFactura); pagina.Insert(0,venta); }
    public List<VentaTicket> ObtenerVentasPorUsuario(string email) => pagina.Where(x => x.EmailUsuario.Equals(email, StringComparison.OrdinalIgnoreCase)).ToList();
    public async Task<Pagina<VentaTicket>> ConsultarAsync(ConsultaPagina f, bool todas = false)
    { var p = await api.Get<Pagina<VentaTicket>>("api/ventas" + (todas ? "?" : "/mias?") + ApiCine.Consulta(f)); pagina = p.Items; return p; }
    public async Task<(bool valido, VentaTicket? venta)> ValidarCodigoValidacion(string codigo)
    { try { return (true, await api.Get<VentaTicket>("api/ventas/verificar/" + Uri.EscapeDataString(codigo))); } catch (InvalidOperationException) { return (false, null); } }
    public Task<VentaTicket> ConfirmarIngresoAsync(string codigo) => api.Post<VentaTicket>("api/ventas/verificar/" + Uri.EscapeDataString(codigo));
    public Task<AnaliticaCine> AnaliticaAsync(FiltroVentas f) => api.Get<AnaliticaCine>("api/analitica?" + Filtro(f));
    public Task<Pagina<VentaTicket>> ResumenVentasAsync(FiltroVentas f, int pagina) => api.Get<Pagina<VentaTicket>>("api/analitica/ventas?pagina=" + pagina + "&tamano=10&" + Filtro(f));
    private static string Filtro(FiltroVentas f) => "periodo=" + Uri.EscapeDataString(f.Periodo) + "&tipoSala=" + Uri.EscapeDataString(f.TipoSala) + "&experiencia=" + Uri.EscapeDataString(f.Experiencia) + "&genero=" + Uri.EscapeDataString(f.Genero);
}
