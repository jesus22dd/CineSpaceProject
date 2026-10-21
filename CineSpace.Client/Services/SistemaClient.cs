using System.Net.Http.Json;
using CineSpace.Shared.DTOs.Sistema;
namespace CineSpace.Client.Services;
public sealed class SistemaClient(HttpClient http) : ISistemaClient
{
    public async Task<EstadoApiDto> ObtenerEstadoAsync(CancellationToken cancellationToken = default)
        => await http.GetFromJsonAsync<EstadoApiDto>("api/sistema/estado", cancellationToken)
            ?? throw new HttpRequestException("El servicio no devolvió una respuesta válida.");
}
