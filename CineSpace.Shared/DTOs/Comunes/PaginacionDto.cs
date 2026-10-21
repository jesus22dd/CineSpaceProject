using System.ComponentModel.DataAnnotations;
namespace CineSpace.Shared.DTOs.Comunes;
public sealed class PaginacionDto
{
    [Range(1, int.MaxValue)] public int Pagina { get; init; } = 1;
    [Range(1, 100)] public int TamanoPagina { get; init; } = 10;
    [StringLength(100)] public string? Busqueda { get; init; }
}
public sealed record PaginaDto<T>(IReadOnlyList<T> Items, int Total, int Pagina, int TamanoPagina)
{
    public int TotalPaginas => TamanoPagina > 0 ? (int)Math.Ceiling(Total / (double)TamanoPagina) : 0;
}
