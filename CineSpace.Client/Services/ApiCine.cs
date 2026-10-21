using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CineSpace.Shared.Contracts;
using Microsoft.AspNetCore.Components.Forms;

namespace CineSpace.Client.Services;

public sealed class ApiCine(HttpClient http, ActividadSolicitudes actividad)
{
    public SesionCuenta Sesion { get; private set; } = new(null, null);
    public ReferenciasCatalogo Referencias { get; private set; } = new();
    public ResumenSistema Resumen { get; private set; } = new();
    public event Action? Cambiada;
    private string? csrf;
    private readonly Dictionary<string, (DateTime Hasta, Task<string> Datos)> lecturas = [];
    private static readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    public string Url(string ruta) => new Uri(http.BaseAddress!, ruta).AbsoluteUri;
    public async Task<T> Get<T>(string ruta, CancellationToken ct = default)
    {
        var path = ruta.Split('?')[0];
        var cacheable = !path.StartsWith("api/auth/", StringComparison.Ordinal) &&
            !path.StartsWith("api/funciones/", StringComparison.Ordinal) &&
            !path.Contains("/verificar", StringComparison.Ordinal) && !path.Contains("/carrito", StringComparison.Ordinal) &&
            (!path.StartsWith("api/pedidos/", StringComparison.Ordinal) || path == "api/pedidos/mios");
        if (!cacheable) return JsonSerializer.Deserialize<T>(await LeerJson(ruta, ct), json)!;
        if (!lecturas.TryGetValue(ruta, out var entrada) || entrada.Hasta <= DateTime.UtcNow)
        {
            if (lecturas.Count > 100) lecturas.Clear();
            entrada = (DateTime.UtcNow.AddSeconds(20), LeerJson(ruta, ct)); lecturas[ruta] = entrada;
        }
        try { return JsonSerializer.Deserialize<T>(await entrada.Datos.WaitAsync(ct), json)!; }
        catch { lecturas.Remove(ruta); throw; }
    }
    private async Task<string> LeerJson(string ruta, CancellationToken ct)
    {
        using var pendiente = actividad.Comenzar();
        using var request = new HttpRequestMessage(HttpMethod.Get,ruta);
        using var response = await RespuestaAsync(request,ct); await Exigir(response);
        return await response.Content.ReadAsStringAsync(ct);
    }
    private async Task<HttpResponseMessage> RespuestaAsync(HttpRequestMessage request, CancellationToken ct = default)
    {
        try { return await http.SendAsync(request,ct); }
        catch (HttpRequestException) { throw new InvalidOperationException("No pudimos conectar con CineSpace. Revisa tu conexión y vuelve a intentar."); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { throw new InvalidOperationException("El servidor tardó demasiado en responder. Vuelve a intentar."); }
    }
    public async Task<T> Enviar<T>(HttpMethod metodo, string ruta, object? valor = null)
    {
        using var response = await Solicitar(metodo, ruta, valor is null ? null : JsonContent.Create(valor));
        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0) return default!;
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
    public Task<T> Post<T>(string ruta, object? valor = null) => Enviar<T>(HttpMethod.Post, ruta, valor);
    public Task<T> Put<T>(string ruta, object? valor = null) => Enviar<T>(HttpMethod.Put, ruta, valor);
    public async Task Delete(string ruta) { using var response = await Solicitar(HttpMethod.Delete, ruta, null); }
    public async Task<HttpResponseMessage> Solicitar(HttpMethod metodo, string ruta, HttpContent? content)
    {
        using var pendiente = actividad.Comenzar();
        csrf ??= (await Get<TokenAntiforgery>("api/auth/csrf")).Token;
        using var request = new HttpRequestMessage(metodo, ruta) { Content = content };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        var response = await RespuestaAsync(request);
        await Exigir(response); lecturas.Clear(); return response;
    }
    private async Task Exigir(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode == HttpStatusCode.Unauthorized) { Sesion = new(null, null); csrf = null; lecturas.Clear(); Cambiada?.Invoke(); }
        string mensaje = response.StatusCode switch { HttpStatusCode.Unauthorized => "Inicia sesion para continuar.", HttpStatusCode.Forbidden => "No tienes permiso para realizar esta accion.", HttpStatusCode.TooManyRequests => "Espera un momento antes de volver a intentarlo.", _ => "No pudimos completar la solicitud. Vuelve a intentarlo." };
        try
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (body.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String) mensaje = detail.GetString()!;
            else if (body.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String) mensaje = title.GetString()!;
        }
        catch (JsonException) { }
        response.Dispose(); throw new InvalidOperationException(mensaje);
    }
    public async Task InicializarAsync() { var sesion = Get<SesionCuenta>("api/auth/sesion"); await Task.WhenAll(sesion, ReferenciasAsync()); Sesion = await sesion; Cambiada?.Invoke(); }
    public async Task ReferenciasAsync() { Referencias = await Get<ReferenciasCatalogo>("api/catalogos/referencias"); Cambiada?.Invoke(); }
    public async Task ResumenAsync() { if (Sesion.Usuario?.Rol == Shared.Models.RolUsuario.Administrador) Resumen = await Get<ResumenSistema>("api/analitica/resumen"); }
    public async Task<SesionCuenta> EntrarAsync(string email, string password, bool recordar = false)
    {
        Sesion = await Post<SesionCuenta>("api/auth/login", new AccesoCuenta(email, password, recordar)); csrf = null; Cambiada?.Invoke(); return Sesion;
    }
    public async Task SalirAsync() { await Post<object>("api/auth/logout"); Sesion = new(null, null); csrf = null; Cambiada?.Invoke(); }
    public async Task PerfilAsync(Shared.Models.Usuario perfil) { var actualizado = await Put<Shared.Models.Usuario>("api/auth/perfil", perfil); Sesion = new(actualizado, Sesion.Trabajador); Cambiada?.Invoke(); }
    public async Task<ImagenGuardada> ImagenAsync(IBrowserFile archivo, string carpeta, bool panoramica = false)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StreamContent(archivo.OpenReadStream(10 * 1024 * 1024)), "archivo", archivo.Name);
        form.Add(new StringContent(carpeta), "carpeta"); form.Add(new StringContent(panoramica.ToString()), "panoramica");
        using var response = await Solicitar(HttpMethod.Post, "api/imagenes", form);
        return (await response.Content.ReadFromJsonAsync<ImagenGuardada>())!;
    }
    public static string Consulta(ConsultaPagina f)
    {
        var valores = new Dictionary<string,string?> {
            ["pagina"] = f.Pagina.ToString(), ["tamano"] = f.Tamano.ToString(), ["buscar"] = f.Buscar,
            ["orden"] = f.Orden, ["campo"] = f.Campo, ["destacadas"] = f.Destacadas.ToString(),
            ["minimo"] = f.Minimo.ToString(), ["maximo"] = f.Maximo.ToString(), ["idioma"] = f.Idioma,
            ["estado"] = f.Estado, ["rol"] = f.Rol, ["categoria"] = f.Categoria?.ToString(), ["genero"] = f.Genero?.ToString(),
            ["sala"] = f.Sala?.ToString(), ["experiencia"] = f.Experiencia?.ToString(), ["pelicula"] = f.Pelicula?.ToString(),
            ["desde"] = f.Desde?.ToString("yyyy-MM-dd"), ["hasta"] = f.Hasta?.ToString("yyyy-MM-dd") };
        return string.Join("&",valores.Where(x=>!string.IsNullOrEmpty(x.Value)).Select(x=>x.Key+"="+Uri.EscapeDataString(x.Value!)));
    }
}
