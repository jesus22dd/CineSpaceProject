namespace CineSpace.Client.Services;

public sealed class AuthDialogService
{
    public bool Abierto { get; private set; }
    public int? PeliculaId { get; private set; }
    public string? Aviso { get; private set; }
    public string RutaRetorno => reservaPendiente is { } pendiente ? $"/cine?pelicula={pendiente.Pelicula}&funcion={pendiente.Funcion}" : PeliculaId.HasValue ? $"/cine?pelicula={PeliculaId.Value}" : "/cine";
    public event Action? Changed;
    private bool conservarEnRedireccion;
    private (int Pelicula, string Funcion)? reservaPendiente;
    private bool reservaConfirmada;
    public void AbrirReserva(int pelicula, string funcion) { Abrir(pelicula); reservaPendiente = (pelicula, funcion); }
    public void ConfirmarReserva() => reservaConfirmada = reservaPendiente.HasValue;
    public string? ConsumirReserva(int pelicula)
    {
        if (!reservaConfirmada || reservaPendiente?.Pelicula != pelicula) return null;
        var funcion = reservaPendiente.Value.Funcion; reservaPendiente = null; reservaConfirmada = false; return funcion;
    }

    public void Abrir(int? peliculaId = null)
    {
        reservaPendiente = null; reservaConfirmada = false;
        PeliculaId = peliculaId;
        Abierto = true;
        Changed?.Invoke();
    }

    public void Cerrar()
    {
        if (!reservaConfirmada) reservaPendiente = null;
        conservarEnRedireccion = false;
        if (!Abierto) return;
        Abierto = false;
        PeliculaId = null;
        Changed?.Invoke();
    }

    public void AbrirDesdeRuta(int? peliculaId, string? aviso = null)
    {
        conservarEnRedireccion = true;
        Abrir(peliculaId); Aviso = aviso;
    }

    public void CambiarRuta()
    {
        if (conservarEnRedireccion) { conservarEnRedireccion = false; return; }
        Cerrar();
    }
}
