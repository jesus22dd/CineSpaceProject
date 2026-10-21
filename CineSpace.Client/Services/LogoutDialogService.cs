namespace CineSpace.Client.Services;

public class LogoutDialogService(UsuarioService usuarios, TrabajadorService trabajadores, SessionTransitionService transicion, Microsoft.AspNetCore.Components.NavigationManager navigation)
{
    public bool Abierto { get; private set; }
    public event Action? Changed;

    public void Solicitar()
    {
        if (usuarios.ObtenerUsuarioActual() == null && !trabajadores.EstaAutenticado()) return;
        Abierto = true;
        Changed?.Invoke();
    }

    public void Cerrar()
    {
        Abierto = false;
        Changed?.Invoke();
    }

    public async Task ConfirmarAsync()
    {
        if (!Abierto || transicion.Activa) return;
        Cerrar();
        if (!await transicion.ComenzarAsync("Cerrando sesión…")) return;
        try { await usuarios.CerrarSesion(); navigation.NavigateTo("/"); }
        finally { await transicion.TerminarAsync(); }
    }
}
