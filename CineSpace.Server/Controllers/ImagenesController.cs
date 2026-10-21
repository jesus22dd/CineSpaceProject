using CineSpace.Server.Services;
using CineSpace.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineSpace.Server.Controllers;

[ApiController, Route("api/imagenes"), Authorize(Roles = "Administrador,Trabajador")]
public sealed class ImagenesController(IImagenStorageService imagenes) : ControllerBase
{
    [HttpPost, RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<ImagenGuardada> Subir(IFormFile archivo, [FromForm] string carpeta = "peliculas", [FromForm] bool panoramica = false, CancellationToken ct = default)
    {
        if (archivo.Length == 0) throw new ArgumentException("Selecciona una imagen.");
        if (!User.IsInRole("Administrador") && carpeta != "peliculas") throw new UnauthorizedAccessException();
        await using var stream = archivo.OpenReadStream();
        return await imagenes.GuardarAsync(stream, carpeta, panoramica, ct);
    }
}
