using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace CineSpace.Server.Filters;

// Process-local, bounded cache. Authentication/authorization run before this filter.
// Prices, seat occupancy, admission and order verification always reach PostgreSQL.
public sealed class CacheLecturas : IDisposable
{
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 32 * 1024 });
    private readonly SemaphoreSlim[] locks = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1)).ToArray();
    private long revision;
    public string Key(string request) => $"{Interlocked.Read(ref revision)}:{request}";
    public SemaphoreSlim Lock(string key) => locks[(key.GetHashCode() & int.MaxValue) % locks.Length];
    public bool TryGet(string key, out string? result) => cache.TryGetValue(key, out result);
    public void Set(string key, object result, int seconds)
    {
        var json=JsonSerializer.Serialize(result,new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var kb=Math.Max(1,(json.Length*2+key.Length*2+1023)/1024);
        if(kb<=256) cache.Set(key,json,new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow=TimeSpan.FromSeconds(seconds),Size=kb });
    }
    public void Invalidar() => Interlocked.Increment(ref revision);
    public void Dispose() { cache.Dispose(); foreach (var item in locks) item.Dispose(); }
    public static int Duracion(string path) => path switch
    {
        "/api/catalogos/referencias" => 120,
        "/api/analitica" or "/api/analitica/resumen" or "/api/analitica/ventas" or "/api/analitica/ocupacion" or "/api/resenas/estadisticas" => 30,
        "/api/peliculas" or "/api/peliculas/opciones" or "/api/presentacion/banners" or "/api/presentacion/estrenos" => 30,
        "/api/personal/usuarios" or "/api/personal/trabajadores" or "/api/resenas" or "/api/resenas/mias" or "/api/ventas" or "/api/ventas/mias" or "/api/pedidos" or "/api/pedidos/mios" or "/api/funciones" => 30,
        _ when path.StartsWith("/api/catalogos/", StringComparison.Ordinal) && !path.Contains("carrito", StringComparison.Ordinal) => 60,
        _ => 0
    };
}

public sealed class CacheLecturasFilter(CacheLecturas cache, IMemoryCache referencias) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        var path = request.Path.Value ?? "";
        if (!HttpMethods.IsGet(request.Method))
        {
            var result = await next();
            var status = (result.Result as ObjectResult)?.StatusCode ?? context.HttpContext.Response.StatusCode;
            if (result.Exception is null && status < 400 && path is not "/api/auth/login" and not "/api/auth/logout")
            { cache.Invalidar(); referencias.Remove("catalogo.referencias"); referencias.Remove("analitica.historico"); }
            return;
        }
        var seconds = CacheLecturas.Duracion(path);
        if (seconds == 0) { await next(); return; }
        var owner = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "public";
        var query = string.Join('&', request.Query.OrderBy(x => x.Key).Select(x => Uri.EscapeDataString(x.Key) + "=" + Uri.EscapeDataString(x.Value.ToString())));
        var key = cache.Key(owner + ":" + path + "?" + query);
        var gate = cache.Lock(key);
        await gate.WaitAsync(context.HttpContext.RequestAborted);
        try
        {
            if (cache.TryGet(key, out var value))
            {
                context.HttpContext.Response.Headers["X-CineSpace-Cache"] = "HIT";
                context.Result = new ContentResult { Content=value, ContentType="application/json; charset=utf-8", StatusCode=200 }; return;
            }
            context.HttpContext.Response.Headers["X-CineSpace-Cache"] = "MISS";
            var result = await next();
            if (result.Exception is null && result.Result is ObjectResult { Value: not null } data && (data.StatusCode ?? 200) == 200)
                cache.Set(key, data.Value, seconds);
        }
        finally { gate.Release(); }
    }
}
