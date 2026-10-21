using CineSpace.Server.Data;
using CineSpace.Server.Services;
using CineSpace.Server.Repositories;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Claims;
using System.Threading.RateLimiting;
using CineSpace.Server.Models;
using CineSpace.Server.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace CineSpace.Server.Extensions;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCineSpace(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var frontendOrigins = DeploymentConfiguration.GetFrontendOrigins(configuration, environment);
        services.AddSingleton(TimeProvider.System);
        services.Configure<SupabaseOptions>(configuration.GetSection(SupabaseOptions.Section));
        services.Configure<Options.GoogleOptions>(configuration.GetSection(Options.GoogleOptions.Section));
        var connection = configuration.GetConnectionString("CineSpace");
        services.AddDbContext<CineSpaceDbContext>(options =>
        {
            if (!string.IsNullOrWhiteSpace(connection))
                options.UseNpgsql(connection);
        });
        services.AddIdentityCore<Usuario>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 10;
            options.SignIn.RequireConfirmedEmail = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<CineSpaceDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();
        var authentication = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
            options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        });
        authentication.AddIdentityCookies();
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "CineSpace.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
            options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
        });
        var google = configuration.GetSection(Options.GoogleOptions.Section).Get<Options.GoogleOptions>() ?? new();
        if (string.IsNullOrWhiteSpace(google.ClientId) != string.IsNullOrWhiteSpace(google.ClientSecret))
            throw new InvalidOperationException("Configura juntos Authentication:Google:ClientId y ClientSecret.");
        if (!string.IsNullOrWhiteSpace(google.ClientId))
            authentication.AddGoogle(options =>
            {
                options.ClientId = google.ClientId;
                options.ClientSecret = google.ClientSecret;
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.CallbackPath = "/signin-google";
                options.SaveTokens = false;
                options.Events.OnRemoteFailure = context => { context.HandleResponse(); context.Response.Redirect((configuration["Frontend:Url"] ?? "").TrimEnd('/') + "/login?error=google"); return Task.CompletedTask; };
                options.Events.OnCreatingTicket = context =>
                {
                    var user = context.User;
                    if ((user.TryGetProperty("verified_email", out var value) || user.TryGetProperty("email_verified", out value)) && value.ValueKind == System.Text.Json.JsonValueKind.True)
                        context.Identity!.AddClaim(new Claim("email_verified", "true"));
                    return Task.CompletedTask;
                };
            });
        services.AddAuthorization();
        services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(2));
        services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(2));
        services.AddDataProtection().SetApplicationName("CineSpace").PersistKeysToDbContext<CineSpaceDbContext>();
        services.AddMemoryCache();
        services.AddScoped<IRepositorioCine, RepositorioCine>();
        services.AddScoped<ICuentaService, CuentaService>();
        services.AddScoped<ICatalogoService, CatalogoService>();
        services.AddScoped<IFuncionesService, FuncionesService>();
        services.AddScoped<IVentasService, VentasService>();
        services.AddScoped<IPedidosService, PedidosService>();
        services.AddScoped<IPersonalService, PersonalService>();
        services.AddScoped<IAnaliticaService, AnaliticaService>();
        services.AddScoped<IOcupacionDashboardService, OcupacionDashboardService>();
        services.AddScoped<IExportacionResenasService, ExportacionResenasService>();
        services.AddScoped<IPeliculaCatalogoService, PeliculaCatalogoService>();
        services.AddScoped<IEstrenosService, EstrenosService>();
        services.AddHttpClient<ICorreoCuentaService, CorreoCuentaService>().RedactLoggedHeaders(new[] { "api-key" });
        services.AddHttpClient<ITmdbService, TmdbService>().RemoveAllLoggers();
        services.AddHttpClient<IImagenStorageService, ImagenStorageService>().RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddRateLimiter(options => options.AddPolicy("cuentas", context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions
            { PermitLimit = 12, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("pedidos", context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "local",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 12, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        services.AddExceptionHandler<CineSpace.Server.Filters.ErrorApiHandler>();
        services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
        // Cada escritura valida este token junto a la cookie de sesion.
        services.AddCors(options => options.AddPolicy("Frontend", policy =>
        {
            if (frontendOrigins.Length > 0) policy.WithOrigins(frontendOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }));
        return services;
    }

}
