using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using CineSpace.Server.Models;

namespace CineSpace.Server.Services;

public interface ICorreoCuentaService
{
    Task ConfirmacionAsync(Usuario usuario, CancellationToken cancelacion = default);
    Task RecuperacionAsync(Usuario usuario, CancellationToken cancelacion = default);
}

public sealed class CorreoCuentaService(HttpClient http, IConfiguration configuration, UserManager<Usuario> usuarios, Microsoft.Extensions.Caching.Memory.IMemoryCache cache, Microsoft.Extensions.Options.IOptions<DataProtectionTokenProviderOptions> opcionesToken) : ICorreoCuentaService
{
    public async Task ConfirmacionAsync(Usuario usuario, CancellationToken cancelacion = default)
    {
        var token = await usuarios.GenerateEmailConfirmationTokenAsync(usuario);
        var url = Enlace("confirmar-cuenta", new() { ["usuario"] = usuario.Id.ToString(), ["token"] = Codificar(token) });
        await EnviarAsync(usuario, PlantillaCorreoCuenta.Confirmacion(usuario.Nombre, url, opcionesToken.Value.TokenLifespan), cancelacion);
    }

    public async Task RecuperacionAsync(Usuario usuario, CancellationToken cancelacion = default)
    {
        var key = "correo.recuperacion:" + usuario.Id;
        lock (cache)
        {
            if (cache.TryGetValue(key, out _)) return;
            Microsoft.Extensions.Caching.Memory.CacheExtensions.Set(cache, key, true, TimeSpan.FromMinutes(5));
        }
        var token = await usuarios.GeneratePasswordResetTokenAsync(usuario);
        var url = Enlace("restablecer-clave", new() { ["email"] = usuario.Email, ["token"] = Codificar(token) });
        await EnviarAsync(usuario, PlantillaCorreoCuenta.Recuperacion(usuario.Nombre, url, opcionesToken.Value.TokenLifespan), cancelacion);
    }

    private string Enlace(string ruta, Dictionary<string, string?> valores)
    {
        var origen = configuration["Frontend:Url"];
        if (string.IsNullOrWhiteSpace(origen) || !Uri.TryCreate(origen, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http")) throw new OperacionCineException("Falta configurar la URL publica del cine para los correos.");
        return QueryHelpers.AddQueryString(origen.TrimEnd('/') + "/" + ruta, valores);
    }
    private static string Codificar(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    public static string Decodificar(string token)
    {
        try { return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token)); }
        catch (FormatException) { throw new ArgumentException("El enlace no es valido o ha caducado."); }
    }

    private async Task EnviarAsync(Usuario usuario, ContenidoCorreoCuenta contenido, CancellationToken cancelacion)
    {
        var apiKey = configuration["BrevoSettings:ApiKey"];
        var remitente = configuration["BrevoSettings:FromEmail"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(remitente))
            throw new OperacionCineException("El envio de correo no esta configurado. Intentalo mas tarde.");
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", apiKey);
        request.Content = JsonContent.Create(new
        {
            sender = new { email = remitente, name = configuration["BrevoSettings:FromName"] ?? "CineSpace" },
            to = new[] { new { email = usuario.Email, name = usuario.Nombre } }, subject = contenido.Asunto, htmlContent = contenido.Html, textContent = contenido.Texto
        });
        using var response = await http.SendAsync(request, cancelacion);
        if (response.StatusCode != HttpStatusCode.Created)
            throw new OperacionCineException("No pudimos confirmar el envio del correo. Puedes solicitar otro enlace mas tarde.");
    }
}
