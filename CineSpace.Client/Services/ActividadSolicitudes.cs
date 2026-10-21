using Microsoft.JSInterop;
namespace CineSpace.Client.Services;

public sealed class ActividadSolicitudes(IJSRuntime js)
{
    private int activas;
    private TaskCompletionSource completadas = Nueva();
    private static TaskCompletionSource Nueva() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    public IDisposable Comenzar()
    {
        if (activas++ == 0) completadas = Nueva();
        var id = js is IJSInProcessRuntime sync ? sync.Invoke<int>("cineWork.begin") : 0;
        return new Fin(() => { if (--activas == 0) completadas.TrySetResult(); if (js is IJSInProcessRuntime runtime) runtime.InvokeVoid("cineWork.end", id); });
    }
    public async Task EsperarVistaAsync()
    {
        // Routed components must render and finish their requests before closing.
        do { await js.InvokeVoidAsync("cineWork.paint"); if (activas > 0) await completadas.Task; } while (activas > 0);
        await js.InvokeVoidAsync("cineWork.paint");
        if (activas > 0) await EsperarVistaAsync();
    }
    private sealed class Fin(Action terminar) : IDisposable
    {
        private Action? accion = terminar;
        public void Dispose() => Interlocked.Exchange(ref accion, null)?.Invoke();
    }
}
