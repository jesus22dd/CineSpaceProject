using CineSpace.Server.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
var builder = WebApplication.CreateBuilder(args);
if (args.Length == 2 && args[0] == "--database")
{
    Environment.ExitCode = await CineSpace.Server.Tools.DatabaseMaintenance.EjecutarAsync(args[1], builder.Configuration, builder.Environment);
    return;
}
// Orden estándar: JSON global, JSON del entorno, User Secrets, entorno y argumentos.
var browserOrigin = DeploymentConfiguration.GetBrowserOrigin(builder.Configuration, builder.Environment);
// También valida el origen interno público aunque el callback use el frontend.
_ = DeploymentConfiguration.GetBackendOrigin(builder.Configuration, builder.Environment);
if (builder.Configuration["PORT"] is { Length: > 0 } portValue)
{
    if (!int.TryParse(portValue, out var port) || port is < 1 or > 65535)
        throw new InvalidOperationException("PORT debe ser un puerto TCP válido.");
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddControllers(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.ServiceFilterAttribute(typeof(CineSpace.Server.Filters.ValidarAntiforgeryFilter)));
    options.Filters.Add(new CineSpace.Server.Filters.ResultadoVacioFilter());
    options.Filters.AddService<CineSpace.Server.Filters.CacheLecturasFilter>();
});
builder.Services.AddScoped<CineSpace.Server.Filters.ValidarAntiforgeryFilter>();
builder.Services.AddSingleton<CineSpace.Server.Filters.CacheLecturas>();
builder.Services.AddScoped<CineSpace.Server.Filters.CacheLecturasFilter>();
builder.Services.AddResponseCompression(o => { o.EnableForHttps = true; o.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream", "application/wasm"]); });
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<CineSpace.Server.Services.PreparacionBaseDatos>();
builder.Services.AddCineSpace(builder.Configuration, builder.Environment);
var behindProxy = builder.Configuration.GetValue<bool>("Deployment:BehindProxy");
if (behindProxy)
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        // Habilitar solo detrás del proxy de entrada de Render.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
var app = builder.Build();
if (behindProxy) app.UseForwardedHeaders();
if (browserOrigin is not null)
    app.Use((context, next) =>
    {
        // El retorno OAuth usa el origen configurado, nunca un X-Forwarded-Host arbitrario.
        if (context.Request.Path == "/signin-google" ||
            context.Request.Path == "/api/auth/google" ||
            context.Request.Path == "/api/auth/google-callback")
        {
            context.Request.Scheme = browserOrigin.Scheme;
            context.Request.Host = browserOrigin.IsDefaultPort ? new HostString(browserOrigin.Host)
                : new HostString(browserOrigin.Host, browserOrigin.Port);
        }
        return next(context);
    });
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path == "/signin-google")
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers["Netlify-CDN-Cache-Control"] = "no-store";
    }
    await next(context);
});
app.UseStatusCodePages();
app.UseResponseCompression();
if (app.Environment.IsDevelopment()) { app.UseWebAssemblyDebugging(); app.UseSwagger(); app.UseSwaggerUI(); }
else app.UseHsts();
// Render termina TLS; localmente la redirección está activa solo en el perfil HTTPS.
if (!behindProxy && !string.IsNullOrWhiteSpace(builder.Configuration["HTTPS_PORT"])) app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var path = context.Context.Request.Path.Value;
        if (path is "/service-worker.js" or "/service-worker-assets.js" or "/index.html" or "/manifest.webmanifest" or "/appsettings.json")
            context.Context.Response.Headers.CacheControl = "no-cache";
    }
});
app.UseRouting();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapHealthChecks("/healthz");
app.MapControllers();
app.Map("/api/{**path}", () => Results.Problem(statusCode: 404, title: "Endpoint no encontrado."));
// El contenedor incluye el frontend publicado; /api nunca debe devolver index.html.
app.MapFallbackToFile("index.html");
app.Run();
public partial class Program { }
