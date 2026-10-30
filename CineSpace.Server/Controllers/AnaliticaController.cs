using CineSpace.Server.Data;
using CineSpace.Server.Repositories;
using CineSpace.Server.Services;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CineSpace.Server.Controllers;
[ApiController, Route("api/analitica"), Authorize(Roles = "Administrador")]
public sealed class AnaliticaController(IAnaliticaService analitica, IOcupacionDashboardService ocupacion, CineSpaceDbContext db, IRepositorioCine repo) : ControllerBase
{
    [HttpGet("resumen")] public Task<ResumenSistema> Resumen(CancellationToken ct) => analitica.ResumenAsync(ct);
    [HttpGet] public Task<AnaliticaCine> Consultar([FromQuery] FiltroVentas filtro, CancellationToken ct) => analitica.ConsultarAsync(filtro, ct);
    [HttpGet("ocupacion")] public Task<OcupacionDashboard> Ocupacion(CancellationToken ct)
        => ocupacion.ConsultarHoyAsync(ct);
    [HttpGet("ventas")] public async Task<Pagina<VentaTicket>> Ventas([FromQuery] FiltroVentas filtro, [FromQuery] ConsultaPagina pagina, CancellationToken ct)
    {
        var p = await repo.PaginarAsync(AnaliticaService.Filtrar(db.Ventas.AsNoTracking(), filtro).OrderByDescending(x => x.FechaUtc).ThenBy(x => x.Id), pagina, ct);
        return new(p.Items.Select(MapasCine.Venta).ToList(), p.Total, p.Numero, p.Tamano);
    }
}
