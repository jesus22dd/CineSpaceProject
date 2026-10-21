using Microsoft.AspNetCore.Identity;
namespace CineSpace.Server.Models;
public sealed class Usuario : IdentityUser<Guid>
{
    public Usuario() => Id = Guid.NewGuid();
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string? Ci { get; set; }
    public bool Activo { get; set; } = true;
    public DateTimeOffset FechaRegistroUtc { get; set; } = DateTimeOffset.UtcNow;
}
