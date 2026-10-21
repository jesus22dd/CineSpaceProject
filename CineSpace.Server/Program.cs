using CineSpace.Server.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
var builder = WebApplication.CreateBuilder(args);
if (args.Length == 2 && args[0] == "--database")
{
    Environment.ExitCode = await CineSpace.Server.Tools.DatabaseMaintenance.EjecutarAsync(args[1], builder.Configuration, builder.Environment);
    return;
}
// Orden estándar: JSON global, JSON del entorno, User Secrets, entorno y argumentos.
var publicBackend = DeploymentConfiguration.GetBackendOrigin(builder.Configuration, builder.Environment);
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
if (publicBackend is not null)
    app.Use((context, next) =>
    {
        // El retorno OAuth usa el origen configurado, nunca un X-Forwarded-Host arbitrario.
        if (context.Request.Path == "/signin-google" ||
            context.Request.Path == "/api/auth/google" ||
            context.Request.Path == "/api/auth/google-callback")
        {
            context.Request.Scheme = publicBackend.Scheme;
            context.Request.Host = HostString.FromUriComponent(publicBackend);
        }
        return next(context);
    });
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    if (context.Request.Path.StartsWithSegments("/api")) context.Response.Headers.CacheControl = "no-store";
    await next(context);
});
app.UseStatusCodePages();
app.UseResponseCompression();
if (app.Environment.IsDevelopment()) { app.UseWebAssemblyDebugging(); app.UseSwagger(); app.UseSwaggerUI(); }
else app.UseHsts();
// Render termina TLS; localmente la redirección está activa solo en el perfil HTTPS.
if (!behindProxy && !string.IsNullOrWhiteSpace(builder.Configuration["HTTPS_PORT"])) app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
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
