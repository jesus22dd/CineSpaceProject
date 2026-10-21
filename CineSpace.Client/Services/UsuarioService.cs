using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
namespace CineSpace.Client.Services;

public sealed class UsuarioService(ApiCine api)
{
    private Pagina<Usuario> pagina = Pagina<Usuario>.Vacia();
    private Pagina<CalificacionPelicula> resenas = Pagina<CalificacionPelicula>.Vacia(6);
    public Usuario? ObtenerUsuarioActual() => api.Sesion.Usuario;
    public bool EsAdministrador() => api.Sesion.Usuario?.Rol == RolUsuario.Administrador;
    public List<Usuario> ObtenerUsuarios() => pagina.Items;
    public Usuario? ObtenerUsuarioPorEmail(string email) => pagina.Items.FirstOrDefault(x => x.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    public async Task<Pagina<Usuario>> ConsultarAsync(ConsultaPagina f) => pagina = await api.Get<Pagina<Usuario>>("api/personal/usuarios?" + ApiCine.Consulta(f));
    public async Task<(bool exito, Usuario? usuario, string mensaje)> AutenticarUsuario(string email, string password)
    {
        try { var sesion = await api.EntrarAsync(email, password); return (true, sesion.Usuario, "Sesion iniciada."); }
        catch (InvalidOperationException ex) { return (false, null, ex.Message); }
    }
    public async Task<(bool exito, string mensaje, Usuario? usuario)> RegistrarUsuario(Usuario usuario)
    {
        try { var result = EsAdministrador() ? new ResultadoOperacion(true, "Usuario creado.") : await api.Post<ResultadoOperacion>("api/auth/registro", usuario);
            if (EsAdministrador()) usuario = await api.Post<Usuario>("api/personal/usuarios", usuario); return (true, result.Mensaje, usuario); }
        catch (InvalidOperationException ex) { return (false, ex.Message, null); }
    }
    public async Task<(bool exito, string mensaje, Usuario? usuario)> ModificarUsuario(Usuario usuario)
    {
        try { if (EsAdministrador() && usuario.Id != Guid.Empty) usuario = await api.Put<Usuario>("api/personal/usuarios/" + usuario.Id, usuario); else await api.PerfilAsync(usuario); return (true, "Datos actualizados.", usuario); }
        catch (InvalidOperationException ex) { return (false, ex.Message, null); }
    }
    public async Task<(bool exito, string mensaje)> EliminarUsuario(string email)
    {
        try { var usuario = ObtenerUsuarioPorEmail(email) ?? throw new InvalidOperationException("El usuario ya no esta en esta pagina."); await api.Delete("api/personal/usuarios/" + usuario.Id); return (true, "Usuario eliminado."); }
        catch (InvalidOperationException ex) { return (false, ex.Message); }
    }
    public async Task<(bool exito, string mensaje, Usuario? usuario)> CambiarRolUsuario(string email, RolUsuario rol)
    { var usuario = ObtenerUsuarioPorEmail(email); if (usuario is null) return (false, "Usuario no encontrado.", null); usuario.Rol = rol; return await ModificarUsuario(usuario); }
    public Task CerrarSesion() => api.SalirAsync();
    public int ObtenerTotalUsuarios() => api.Resumen.Usuarios;
    public int ObtenerTotalUsuariosPorRol(RolUsuario rol) => rol == RolUsuario.Administrador ? api.Resumen.Administradores : rol == RolUsuario.Cliente ? api.Resumen.Clientes : api.Resumen.Trabajadores;
    public Dictionary<RolUsuario, int> ObtenerDistribucionPorRoles() => Enum.GetValues<RolUsuario>().ToDictionary(x => x, ObtenerTotalUsuariosPorRol);
    public async Task<Pagina<CalificacionPelicula>> ConsultarResenasAsync(ConsultaPagina f, bool todas = false) => resenas = await api.Get<Pagina<CalificacionPelicula>>("api/resenas" + (todas ? "?" : "/mias?") + ApiCine.Consulta(f));
    public List<CalificacionPelicula> ObtenerTodasLasCalificaciones() => resenas.Items;
    public Task GuardarCalificacionAsync(CalificacionPelicula dato) => api.Post<CalificacionPelicula>("api/resenas", dato);
    public async Task<bool> EliminarCalificacion(string email, int pelicula) { await api.Delete("api/resenas/" + pelicula + "?email=" + Uri.EscapeDataString(email)); return true; }
}
