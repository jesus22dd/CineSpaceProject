namespace CineSpace.Shared.DTOs.Autenticacion;
public sealed record SesionDto(string Id, string Nombre, string? Email, IReadOnlyList<string> Roles);
