using System.Security.Claims;
using CineSpace.Server.Data;
using CineSpace.Server.Services;
using CineSpace.Shared.Contracts;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Cuenta = CineSpace.Server.Models.Usuario;

namespace CineSpace.Server.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(ICuentaService cuentas, SignInManager<Cuenta> acceso, UserManager<Cuenta> usuarios,
    ICorreoCuentaService correo, IAntiforgery antiforgery, IConfiguration configuration, CineSpaceDbContext db, ILogger<AuthController> logger) : ControllerBase
{
    [HttpGet("csrf"), AllowAnonymous]
    public TokenAntiforgery Csrf()
    {
        Response.Headers.CacheControl = "no-store";
        return new(antiforgery.GetAndStoreTokens(HttpContext).RequestToken!);
    }

    [HttpGet("sesion"), AllowAnonymous]
    public Task<SesionCuenta> Sesion(CancellationToken ct) => cuentas.SesionAsync(User, ct);

    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("cuentas")]
    public Task<SesionCuenta> Login(AccesoCuenta solicitud, CancellationToken ct) => cuentas.AccederAsync(solicitud, ct);

    [HttpPost("registro"), AllowAnonymous, EnableRateLimiting("cuentas")]
    public Task<ResultadoOperacion> Registro(Shared.Models.Usuario solicitud, CancellationToken ct) => cuentas.RegistrarAsync(solicitud, ct);

    [HttpPost("logout"), AllowAnonymous]
    public async Task<IActionResult> Logout() { await acceso.SignOutAsync(); return NoContent(); }

    [HttpPut("perfil"), Authorize]
    public Task<Shared.Models.Usuario> Perfil(Shared.Models.Usuario solicitud) => cuentas.ActualizarPerfilAsync(User, solicitud);

    [HttpPost("cambiar-clave"), Authorize, EnableRateLimiting("cuentas")]
    public async Task<IActionResult> CambiarClave(CambioClave solicitud) { await cuentas.CambiarClaveAsync(User, solicitud); return NoContent(); }

    [HttpPost("confirmar"), AllowAnonymous, EnableRateLimiting("cuentas")]
    public async Task<ResultadoOperacion> Confirmar(ConfirmarCuenta solicitud)
    {
        var usuario = Guid.TryParse(solicitud.Usuario, out var id) ? await usuarios.FindByIdAsync(id.ToString()) : null;
        if (usuario is null || !usuario.Activo) throw new ArgumentException("El enlace no es valido o ha caducado.");
        var resultado = await usuarios.ConfirmEmailAsync(usuario, CorreoCuentaService.Decodificar(solicitud.Token));
        if (!resultado.Succeeded) throw new ArgumentException("El enlace no es valido o ha caducado. Solicita otro enlace.");
        return new(true, "Correo confirmado. Ya puedes iniciar sesion.");
    }

    [HttpPost("recuperar"), AllowAnonymous, EnableRateLimiting("cuentas")]
    public async Task<ResultadoOperacion> Recuperar(SolicitudCorreo solicitud, CancellationToken ct)
    {
        var usuario = await usuarios.FindByEmailAsync(solicitud.Email.Trim());
        try { if (usuario is { Activo: true, EmailConfirmed: true, PasswordHash: not null }) await correo.RecuperacionAsync(usuario, ct); }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException) { logger.LogWarning("No se pudo entregar el correo de recuperacion; TraceId {TraceId}", HttpContext.TraceIdentifier); }
        return new(true, "Si existe una cuenta con ese correo, recibiras las instrucciones para recuperar el acceso.");
    }

    [HttpPost("restablecer"), AllowAnonymous, EnableRateLimiting("cuentas")]
    public async Task<ResultadoOperacion> Restablecer(RestablecerClave solicitud)
    {
        var usuario = await usuarios.FindByEmailAsync(solicitud.Email.Trim());
        if (usuario is null || !usuario.Activo || !usuario.EmailConfirmed) throw new ArgumentException("El enlace no es valido o ha caducado.");
        var resultado = await usuarios.ResetPasswordAsync(usuario, CorreoCuentaService.Decodificar(solicitud.Token), solicitud.Password);
        if (!resultado.Succeeded) throw new ArgumentException("El enlace no es valido, ha caducado o la contraseña no cumple los requisitos.");
        return new(true, "Contraseña actualizada. Inicia sesion con tu nueva contraseña.");
    }

    [HttpGet("google"), AllowAnonymous, EnableRateLimiting("cuentas")]
    public IActionResult Google([FromQuery] string? volver)
    {
        if (string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"])) return Problem(statusCode: 503, title: "Google no esta configurado.");
        var ruta = RutaLocal(volver);
        var retorno = Url.Action(nameof(GoogleCallback), values: new { volver = ruta })!;
        return Challenge(acceso.ConfigureExternalAuthenticationProperties("Google", retorno), "Google");
    }

    [HttpGet("google-callback"), AllowAnonymous]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? volver, [FromQuery] string? remoteError)
    {
        if (remoteError is not null) return Volver("/login?error=google");
        var info = await acceso.GetExternalLoginInfoAsync();
        if (info is null || info.LoginProvider != "Google" || info.Principal.FindFirstValue("email_verified") != "true") return Volver("/login?error=google");
        await using var transaction = await db.Database.BeginTransactionAsync();
        var identidad = info.LoginProvider + ":" + info.ProviderKey;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(-500, hashtext({identidad}))");
        var usuario = await usuarios.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (usuario is null)
        {
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email)) return Volver("/login?error=google");
            // Una coincidencia de correo no vincula una cuenta local ni otorga roles existentes.
            if (await usuarios.FindByEmailAsync(email) is not null) return Volver("/login?error=cuenta-existente");
            usuario = new Cuenta
            {
                Email = email, UserName = email, EmailConfirmed = true,
                Nombre = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? info.Principal.Identity?.Name ?? "Cliente",
                Apellido = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "", Ci = null
            };
            CuentaService.Exigir(await usuarios.CreateAsync(usuario)); // Sin password: la identidad pertenece a Google.
            CuentaService.Exigir(await usuarios.AddToRoleAsync(usuario, "Cliente"));
            CuentaService.Exigir(await usuarios.AddLoginAsync(usuario, info));
        }
        if (!usuario.Activo) return Volver("/login?error=cuenta-inactiva");
        await transaction.CommitAsync();
        await acceso.SignInAsync(usuario, isPersistent: true, authenticationMethod: "Google");
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        return Volver(RutaLocal(volver));
    }

    private IActionResult Volver(string ruta) => Redirect((configuration["Frontend:Url"] ?? "").TrimEnd('/') + ruta);
    private static string RutaLocal(string? ruta) => !string.IsNullOrWhiteSpace(ruta) && ruta.StartsWith('/') &&
        !ruta.StartsWith("//", StringComparison.Ordinal) && !ruta.Contains('\\') && !ruta.Any(char.IsControl) ? ruta : "/cine";
}
