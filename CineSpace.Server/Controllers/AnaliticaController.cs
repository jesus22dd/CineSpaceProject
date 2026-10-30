using CineSpace.Server.Services;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CineSpace.Server.Controllers;
[ApiController, Route("api/analitica"), Authorize(Roles = "Administrador")]
public sealed class AnaliticaController(IAnaliticaService analitica, IOcupacionDashboardService ocupacion, IResumenVentasService ventas) : ControllerBase
{
    [HttpGet("resumen")] public Task<ResumenSistema> Resumen(CancellationToken ct) => analitica.ResumenAsync(ct);
    [HttpGet] public Task<AnaliticaCine> Consultar([FromQuery] FiltroVentas filtro, CancellationToken ct) => analitica.ConsultarAsync(filtro, ct);
    [HttpGet("ocupacion")] public Task<OcupacionDashboard> Ocupacion(CancellationToken ct)
        => ocupacion.ConsultarHoyAsync(ct);
    [HttpGet("ventas")] public Task<ResultadoResumenVentas> Ventas([FromQuery] FiltroVentas filtro,
        [FromQuery] int pagina = 1, [FromQuery] int tamano = 10, CancellationToken ct = default)
        => ventas.ConsultarAsync(filtro, new ConsultaPagina { Pagina = pagina, Tamano = tamano }, ct);
}
