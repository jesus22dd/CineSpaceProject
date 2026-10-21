using CineSpace.Shared.DTOs.Sistema;
namespace CineSpace.Client.Services;
public interface ISistemaClient
{
    Task<EstadoApiDto> ObtenerEstadoAsync(CancellationToken cancellationToken = default);
}
