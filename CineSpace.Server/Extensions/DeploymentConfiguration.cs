namespace CineSpace.Server.Extensions;

public static class DeploymentConfiguration
{
    public static string NormalizeOrigin(string value, bool development)
    {
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
            (uri.Scheme == Uri.UriSchemeHttp && (!development || !uri.IsLoopback)) ||
            (!development && uri.IsLoopback) || uri.Host.Contains('*') ||
            uri.AbsolutePath != "/" || uri.Query.Length > 0 || uri.Fragment.Length > 0 || uri.UserInfo.Length > 0)
            throw new InvalidOperationException("Configura las URLs con origenes explicitos, sin rutas ni credenciales; en produccion deben ser HTTPS publicos.");

        return uri.GetLeftPart(UriPartial.Authority);
    }

    public static string[] GetFrontendOrigins(IConfiguration configuration, IHostEnvironment environment)
    {
        var values = configuration.GetSection("Frontend:Origins").Get<string[]>() ?? [];
        var frontend = configuration["Frontend:Url"];
        if (!string.IsNullOrWhiteSpace(frontend)) values = [.. values, frontend];
        return values.Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => NormalizeOrigin(value, environment.IsDevelopment()))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static Uri? GetBackendOrigin(IConfiguration configuration, IHostEnvironment environment)
    {
        var value = configuration["Backend:PublicUrl"];
        return string.IsNullOrWhiteSpace(value) ? null
            : new Uri(NormalizeOrigin(value, environment.IsDevelopment()));
    }
}
