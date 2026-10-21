using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
namespace CineSpace.Client.Services;
public sealed class TrabajadorService(ApiCine api)
{
    private List<Trabajador> pagina = [];
    public async Task<Pagina<Trabajador>> ConsultarAsync(ConsultaPagina f) { var result = await api.Get<Pagina<Trabajador>>("api/personal/trabajadores?" + ApiCine.Consulta(f)); pagina = result.Items; return result; }
    public List<Trabajador> ObtenerTrabajadores() => pagina;
    public Trabajador? ObtenerTrabajadorPorId(int id) => pagina.FirstOrDefault(x => x.Id == id);
    public Trabajador? ObtenerTrabajadorActual() => api.Sesion.Trabajador;
    public bool EstaAutenticado() => ObtenerTrabajadorActual() is not null;
    public bool EsAdministrador() => api.Sesion.Usuario?.Rol == RolUsuario.Administrador;
    public Task CerrarSesion() => api.SalirAsync();
    public async Task<(bool exito, string mensaje, Trabajador? trabajador)> AgregarTrabajador(Trabajador dto) => await Guardar(dto, true);
    public async Task<(bool exito, string mensaje, Trabajador? trabajador)> ModificarTrabajador(Trabajador dto) => await Guardar(dto, false);
    private async Task<(bool exito, string mensaje, Trabajador? trabajador)> Guardar(Trabajador dto, bool nuevo)
    { try { return (true, "Trabajador guardado.", nuevo ? await api.Post<Trabajador>("api/personal/trabajadores", dto) : await api.Put<Trabajador>("api/personal/trabajadores/" + dto.Id, dto)); } catch (InvalidOperationException ex) { return (false, ex.Message, null); } }
    public async Task<(bool exito, string mensaje)> EliminarTrabajador(int id) { try { await api.Delete("api/personal/trabajadores/" + id); return (true, "Trabajador eliminado."); } catch (InvalidOperationException ex) { return (false, ex.Message); } }
    public int ObtenerTotalTrabajadores() => api.Resumen.Trabajadores;
    public decimal ObtenerSueldoTotalMensual() => api.Resumen.Sueldos;
    public double ObtenerEdadPromedio() => api.Resumen.EdadPromedio;
    public int ObtenerTotalTrabajadoresPorCargo(Cargo cargo) => api.Resumen.Series.Where(x => x.Nombre == cargo.ToString()).Sum(x => x.Cantidad);
}
