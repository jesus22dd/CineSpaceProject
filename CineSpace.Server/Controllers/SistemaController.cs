using System.Security.Claims;
using CineSpace.Shared.DTOs.Autenticacion;
using CineSpace.Shared.DTOs.Sistema;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CineSpace.Server.Controllers;
[ApiController]
[Route("api/sistema")]
public sealed class SistemaController(TimeProvider clock) : ControllerBase
{
    [HttpGet("estado")]
    public ActionResult<EstadoApiDto> Estado() => new EstadoApiDto("CineSpace.API", "Disponible", clock.GetUtcNow());

    [HttpGet("preparacion"), AllowAnonymous]
    public Task<CineSpace.Shared.Contracts.EstadoBaseDatos> Preparacion([FromServices] CineSpace.Server.Services.PreparacionBaseDatos preparacion, CancellationToken ct)
        => preparacion.ComprobarAsync(ct);

    [Authorize]
    [HttpGet("sesion")]
    public ActionResult<SesionDto> Sesion() => new SesionDto(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
        User.Identity?.Name ?? "",
        User.FindFirstValue(ClaimTypes.Email),
        User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray());
}
