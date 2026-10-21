using System.Net.Http.Headers;
using CineSpace.Server.Options;
using CineSpace.Shared.Contracts;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace CineSpace.Server.Services;

public interface IImagenStorageService
{
    Task<ImagenGuardada> GuardarAsync(Stream entrada, string carpeta, bool panoramica, CancellationToken ct);
    Task<ImagenGuardada> ImportarTmdbAsync(string ruta, string carpeta, bool panoramica, CancellationToken ct);
    Task EliminarAsync(string url, CancellationToken ct);
    void ValidarUrl(string url);
}

public sealed class ImagenStorageService(HttpClient http, IOptions<SupabaseOptions> opciones) : IImagenStorageService
{
    private const int MaxEntrada = 10 * 1024 * 1024;
    private readonly SupabaseOptions settings = opciones.Value;

    public async Task<ImagenGuardada> GuardarAsync(Stream entrada, string carpeta, bool panoramica, CancellationToken ct)
    {
        if (carpeta is not ("peliculas" or "banners" or "confiteria" or "estrenos")) throw new ArgumentException("Destino de imagen no valido.");
        if (string.IsNullOrWhiteSpace(settings.SecretKey) || string.IsNullOrWhiteSpace(settings.Bucket)) throw new OperacionCineException("Storage no esta configurado.");
        using var buffer = new MemoryStream();
        var bloque = new byte[81920];
        int leidos;
        while ((leidos = await entrada.ReadAsync(bloque, ct)) > 0)
        {
            if (buffer.Length + leidos > MaxEntrada) throw new ArgumentException("La imagen admite hasta 10 MB de entrada.");
            await buffer.WriteAsync(bloque.AsMemory(0, leidos), ct);
        }
        var webp = ConvertirWebp(buffer.ToArray(), panoramica ? 2400 : 1200);
        var ruta = $"{carpeta}/{Guid.NewGuid():N}.webp";
        using var request = Peticion(HttpMethod.Post, ruta);
        request.Content = new ByteArrayContent(webp);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("image/webp");
        request.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = TimeSpan.FromDays(365), Public = true };
        request.Headers.Add("x-upsert", "false");
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) throw new OperacionCineException("No pudimos guardar la imagen en Storage. Revisa el bucket y vuelve a intentarlo.");
        return new($"{settings.Url.TrimEnd('/')}/storage/v1/object/public/{settings.Bucket}/{ruta}", ruta);
    }

    public async Task<ImagenGuardada> ImportarTmdbAsync(string ruta, string carpeta, bool panoramica, CancellationToken ct)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(ruta, @"^/[A-Za-z0-9_-]+\.(jpg|png|webp)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) throw new ArgumentException("La imagen TMDB no es valida.");
        var tamano = panoramica ? "w1280" : "w780";
        using var response = await http.GetAsync($"https://image.tmdb.org/t/p/{tamano}{ruta}", HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode) throw new ArgumentException("TMDB no pudo proporcionar esta imagen.");
        if (response.Content.Headers.ContentLength > MaxEntrada) throw new ArgumentException("La imagen excede el limite permitido.");
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return await GuardarAsync(stream, carpeta, panoramica, ct);
    }

    public void ValidarUrl(string url)
    {
        ValidarRutaGuardada(url);
        if (url.StartsWith("/images/", StringComparison.Ordinal)) return;
        var prefijo = settings.Url.TrimEnd('/') + "/storage/v1/object/public/" + settings.Bucket + "/";
        if (!url.StartsWith(prefijo, StringComparison.Ordinal) || !url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Guarda la nueva imagen en el bucket del cine antes de continuar.");
    }

    public static void ValidarRutaGuardada(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Length > 1000 || url.Contains("..", StringComparison.Ordinal) || url.Contains('\\') ||
            !(url.StartsWith("/images/", StringComparison.Ordinal) && url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
              Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo)))
            throw new ArgumentException("Selecciona una imagen WebP valida del cine.");
    }

    public async Task EliminarAsync(string url, CancellationToken ct)
    {
        if (url.StartsWith("/images/", StringComparison.Ordinal)) return;
        ValidarUrl(url);
        var prefijo = settings.Url.TrimEnd('/') + "/storage/v1/object/public/" + settings.Bucket + "/";
        var ruta = url[prefijo.Length..];
        using var request = Peticion(HttpMethod.Delete, ruta);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            throw new OperacionCineException("El registro se guardo, pero no se pudo retirar su imagen anterior.");
    }

    private HttpRequestMessage Peticion(HttpMethod metodo, string ruta)
    {
        var request = new HttpRequestMessage(metodo, $"{settings.Url.TrimEnd('/')}/storage/v1/object/{settings.Bucket}/{ruta}");
        request.Headers.Add("apikey", settings.SecretKey);
        if (!settings.SecretKey.StartsWith("sb_secret_", StringComparison.Ordinal)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.SecretKey);
        return request;
    }

    public static byte[] ConvertirWebp(byte[] bytes, int dimensionMaxima)
    {
        using var stream = new SKMemoryStream(bytes);
        using var codec = SKCodec.Create(stream) ?? throw new ArgumentException("El archivo no es una imagen compatible.");
        if (codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp) ||
            codec.Info.Width < 1 || codec.Info.Height < 1 || (long)codec.Info.Width * codec.Info.Height > 40_000_000)
            throw new ArgumentException("Utiliza una imagen JPG, PNG o WebP de hasta 40 megapixeles.");
        using var original = SKBitmap.Decode(codec) ?? throw new ArgumentException("La imagen no pudo decodificarse.");
        var girada = codec.EncodedOrigin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var ancho = girada ? original.Height : original.Width;
        var alto = girada ? original.Width : original.Height;
        using var orientada = new SKBitmap(ancho, alto);
        using (var canvas = new SKCanvas(orientada))
        {
            switch (codec.EncodedOrigin)
            {
                case SKEncodedOrigin.TopRight: canvas.Translate(ancho, 0); canvas.Scale(-1, 1); break;
                case SKEncodedOrigin.BottomRight: canvas.Translate(ancho, alto); canvas.RotateDegrees(180); break;
                case SKEncodedOrigin.BottomLeft: canvas.Translate(0, alto); canvas.Scale(1, -1); break;
                case SKEncodedOrigin.LeftTop: canvas.RotateDegrees(90); canvas.Scale(1, -1); break;
                case SKEncodedOrigin.RightTop: canvas.Translate(ancho, 0); canvas.RotateDegrees(90); break;
                case SKEncodedOrigin.RightBottom: canvas.Translate(ancho, alto); canvas.RotateDegrees(90); canvas.Scale(-1, 1); break;
                case SKEncodedOrigin.LeftBottom: canvas.Translate(0, alto); canvas.RotateDegrees(-90); break;
            }
            canvas.DrawBitmap(original, 0, 0);
        }
        var factor = Math.Min(1d, dimensionMaxima / (double)Math.Max(ancho, alto));
        using var reducida = orientada.Resize(new SKImageInfo(Math.Max(1, (int)(ancho * factor)), Math.Max(1, (int)(alto * factor))), new SKSamplingOptions(SKCubicResampler.Mitchell))
            ?? throw new ArgumentException("No pudimos optimizar esta imagen.");
        using var imagen = SKImage.FromBitmap(reducida);
        using var data = imagen.Encode(SKEncodedImageFormat.Webp, 82) ?? throw new ArgumentException("No pudimos convertir la imagen a WebP.");
        if (data.Size > 5 * 1024 * 1024) throw new ArgumentException("La imagen optimizada excede los 5 MB permitidos.");
        return data.ToArray();
    }
}
