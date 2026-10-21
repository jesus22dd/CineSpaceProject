namespace CineSpace.Client.Services;

public sealed class SessionTransitionService(ActividadSolicitudes solicitudes)
{
    public bool Visible { get; private set; }
    public bool Activa { get; private set; }
    public string Mensaje { get; private set; } = "";
    public event Action? Changed;
    private TaskCompletionSource? mostrado, cerrado;

    public void NotificarVisible() => mostrado?.TrySetResult();
    public void NotificarCerrado() => cerrado?.TrySetResult();

    public async Task<bool> ComenzarAsync(string mensaje)
    {
        if (Activa) return false;
        Activa = Visible = true;
        Mensaje = mensaje;
        mostrado = new(TaskCreationOptions.RunContinuationsAsynchronously);
        cerrado = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Changed?.Invoke();
        await mostrado.Task.WaitAsync(TimeSpan.FromSeconds(5));
        return true;
    }
    public async Task TerminarAsync()
    {
        if (!Activa) return;
        try { await solicitudes.EsperarVistaAsync(); }
        finally
        {
            Visible = false;
            Changed?.Invoke();
            try { if (cerrado is not null) await cerrado.Task.WaitAsync(TimeSpan.FromSeconds(5)); }
            finally { Activa = false; Mensaje = ""; Changed?.Invoke(); }
        }
    }
}
