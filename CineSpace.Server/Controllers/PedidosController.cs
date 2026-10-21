using System.Security.Claims;
using CineSpace.Server.Services;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CineSpace.Server.Controllers;

[ApiController, Route("api/pedidos")]
public sealed class PedidosController(IPedidosService pedidos, IDataProtectionProvider proteccion) : ControllerBase
{
    private Guid? Usuario => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private bool Personal => User.IsInRole("Administrador") || User.IsInRole("Trabajador");
    private string Invitado(bool crear = false)
    {
        var protector = proteccion.CreateProtector("CineSpace.Pedidos.Invitado.v1");
        if (Request.Cookies.TryGetValue("CineSpace.Invitado", out var cookie))
            try { return protector.Unprotect(cookie); } catch (System.Security.Cryptography.CryptographicException) { }
        if (!crear) return "";
        var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        Response.Cookies.Append("CineSpace.Invitado", protector.Protect(token), new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, MaxAge = TimeSpan.FromDays(30), IsEssential = true });
        return token;
    }
    [HttpPost, AllowAnonymous, EnableRateLimiting("pedidos")] public Task<PedidoConfiteria> Crear(SolicitudPedido solicitud, CancellationToken ct) => pedidos.CrearAsync(solicitud, Usuario, Invitado(true), ct);
    [HttpGet("mios"), AllowAnonymous] public Task<Pagina<PedidoConfiteria>> Mios([FromQuery] ConsultaPagina filtro, CancellationToken ct) => pedidos.ListarAsync(filtro, Usuario, Invitado(), false, ct);
    [HttpGet, Authorize(Roles = "Administrador,Trabajador")] public Task<Pagina<PedidoConfiteria>> Listar([FromQuery] ConsultaPagina filtro, CancellationToken ct) => pedidos.ListarAsync(filtro, Usuario, "", true, ct);
    [HttpGet("{token}"), AllowAnonymous] public Task<PedidoConfiteria> Obtener(string token, CancellationToken ct) => pedidos.ObtenerAsync(token, Usuario, Invitado(), Personal, ct);
    [HttpPost("{token}/entregar"), Authorize(Roles = "Administrador,Trabajador")] public Task<PedidoConfiteria> Entregar(string token, CancellationToken ct) => pedidos.EntregarAsync(token, Usuario!.Value, ct);
    [HttpPost("{token}/cancelar"), AllowAnonymous] public Task Cancelar(string token, CancellationToken ct) => pedidos.CancelarAsync(token, Usuario, Invitado(), Personal, ct);
}
