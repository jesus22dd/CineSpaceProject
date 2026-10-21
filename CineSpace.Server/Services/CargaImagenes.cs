namespace CineSpace.Server.Services;

/// <summary>Compensa las cargas nuevas si falla la escritura del registro.</summary>
public sealed class CargaImagenes(IImagenStorageService storage) : IAsyncDisposable
{
    private readonly List<string> nuevas = [];
    private bool confirmada;
    public async Task<string> RecibirAsync(string valor, string carpeta, bool panoramica, CancellationToken ct)
    {
        if (valor.StartsWith("data:image/", StringComparison.Ordinal))
        {
            var coma = valor.IndexOf(',');
            if (coma < 0 || coma > 50 || !valor[..coma].EndsWith(";base64", StringComparison.Ordinal) || valor.Length > 14_000_000) throw new ArgumentException("La imagen no es valida o supera los 10 MB.");
            byte[] contenido;
            try { contenido = Convert.FromBase64String(valor[(coma + 1)..]); } catch (FormatException) { throw new ArgumentException("La imagen no es valida."); }
            using var stream = new MemoryStream(contenido);
            var archivo = await storage.GuardarAsync(stream, carpeta, panoramica, ct); nuevas.Add(archivo.Url); return archivo.Url;
        }
        if (Uri.TryCreate(valor, UriKind.Absolute, out var uri) && uri.Scheme == "https" && uri.Host == "image.tmdb.org") valor = "/" + uri.AbsolutePath.Split('/').Last();
        if (System.Text.RegularExpressions.Regex.IsMatch(valor, @"^/[A-Za-z0-9_-]+\.(jpg|png|webp)$"))
        {
            var archivo = await storage.ImportarTmdbAsync(valor, carpeta, panoramica, ct); nuevas.Add(archivo.Url); return archivo.Url;
        }
        storage.ValidarUrl(valor); return valor;
    }
    public void Confirmar() => confirmada = true;
    public async ValueTask DisposeAsync()
    {
        if (confirmada) return;
        foreach (var url in nuevas)
            try { await storage.EliminarAsync(url, CancellationToken.None); }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException) { /* Un fallo externo no debe ocultar el error original. */ }
    }
}
