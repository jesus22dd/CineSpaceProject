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

public sealed class CorreoCuentaService(HttpClient http, IConfiguration configuration, UserManager<Usuario> usuarios, Microsoft.Extensions.Caching.Memory.IMemoryCache cache) : ICorreoCuentaService
{
    public async Task ConfirmacionAsync(Usuario usuario, CancellationToken cancelacion = default)
    {
        var token = await usuarios.GenerateEmailConfirmationTokenAsync(usuario);
        var url = Enlace("confirmar-cuenta", new() { ["usuario"] = usuario.Id.ToString(), ["token"] = Codificar(token) });
        await EnviarAsync(usuario, "Confirma tu cuenta de CineSpace", "Confirma tu correo", "Completa tu registro para reservar tus proximas funciones.", url, cancelacion);
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
        await EnviarAsync(usuario, "Restablece tu contraseña de CineSpace", "Cambiar contraseña", "Recibimos una solicitud para cambiar tu contraseña. Si no fuiste tu, puedes ignorar este correo.", url, cancelacion);
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

    private async Task EnviarAsync(Usuario usuario, string asunto, string accion, string texto, string url, CancellationToken cancelacion)
    {
        var apiKey = configuration["BrevoSettings:ApiKey"];
        var remitente = configuration["BrevoSettings:FromEmail"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(remitente))
            throw new OperacionCineException("El envio de correo no esta configurado. Intentalo mas tarde.");
        var nombre = WebUtility.HtmlEncode(usuario.Nombre);
        var enlace = WebUtility.HtmlEncode(url);
        var html = $"<html lang='es'><body style='margin:0;background:#111114;font-family:Arial,sans-serif;color:#222'><main style='max-width:560px;margin:32px auto;padding:32px;background:#fff;border-top:6px solid #a63c59'><h1 style='color:#642633'>CineSpace</h1><p>Hola, {nombre}.</p><p>{WebUtility.HtmlEncode(texto)}</p><p style='margin:32px 0'><a style='background:#a63c59;color:#fff;padding:14px 20px;text-decoration:none;border-radius:2px;display:inline-block;font-weight:bold' href='{enlace}'>{WebUtility.HtmlEncode(accion)}</a></p><p>Este enlace es personal y tiene una validez limitada.</p><p style='font-size:12px;color:#625d60'>CineSpace · Tu próxima experiencia comienza aquí.</p></main></body></html>";
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", apiKey);
        request.Content = JsonContent.Create(new
        {
            sender = new { email = remitente, name = configuration["BrevoSettings:FromName"] ?? "CineSpace" },
            to = new[] { new { email = usuario.Email, name = usuario.Nombre } }, subject = asunto, htmlContent = html
        });
        using var response = await http.SendAsync(request, cancelacion);
        if (response.StatusCode != HttpStatusCode.Created)
            throw new OperacionCineException("No pudimos confirmar el envio del correo. Puedes solicitar otro enlace mas tarde.");
    }
}
