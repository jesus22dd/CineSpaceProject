using CineSpace.Server.Data;
using CineSpace.Server.Services;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CineSpace.Server.Controllers;

[ApiController, Route("api/funciones")]
public sealed class FuncionesController(IFuncionesService funciones, CineSpaceDbContext db) : ControllerBase
{
    [HttpGet, Authorize(Roles = "Administrador,Trabajador")]
    public Task<Pagina<ProgramacionSala>> Listar([FromQuery] ConsultaPagina filtro, CancellationToken ct) => funciones.ListarAsync(filtro, ct);
    [HttpGet("pelicula/{id:int}"), AllowAnonymous]
    public Task<List<FuncionCine>> Disponibles(int id, CancellationToken ct) => funciones.DisponiblesAsync(id, ct);
    [HttpGet("{id:guid}/ocupados"), AllowAnonymous]
    public Task<List<string>> Ocupados(Guid id, CancellationToken ct) => db.ButacasVendidas.AsNoTracking().Where(x => x.FuncionId == id).Select(x => x.Asiento).ToListAsync(ct);
    [HttpPost, Authorize(Roles = "Administrador,Trabajador")]
    public Task<ProgramacionSala> Crear(ProgramacionSala solicitud, CancellationToken ct) => funciones.GuardarAsync(solicitud, true, ct);
    [HttpPut("{id:guid}"), Authorize(Roles = "Administrador,Trabajador")]
    public Task<ProgramacionSala> Editar(Guid id, ProgramacionSala solicitud, CancellationToken ct) { solicitud.Id = id.ToString("N"); return funciones.GuardarAsync(solicitud, false, ct); }
    [HttpDelete("{id:guid}"), Authorize(Roles = "Administrador,Trabajador")]
    public Task Eliminar(Guid id, CancellationToken ct) => funciones.EliminarAsync(id, ct);
}

[ApiController, Route("api/ventas"), Authorize]
public sealed class VentasController(IVentasService ventas) : ControllerBase
{
    [HttpPost] public Task<VentaTicket> Comprar(VentaTicket solicitud, CancellationToken ct) => ventas.ComprarAsync(solicitud, User, ct);
    [HttpGet("mias")] public Task<Pagina<VentaTicket>> Mias([FromQuery] ConsultaPagina filtro, CancellationToken ct) { filtro.Tamano = 6; return ventas.ListarAsync(filtro, User, false, ct); }
    [HttpGet, Authorize(Roles = "Administrador,Trabajador")] public Task<Pagina<VentaTicket>> Todas([FromQuery] ConsultaPagina filtro, CancellationToken ct) => ventas.ListarAsync(filtro, User, true, ct);
    [HttpGet("verificar/{codigo}"), Authorize(Roles = "Administrador,Trabajador")] public Task<VentaTicket> Verificar(string codigo, CancellationToken ct) => ventas.VerificarAsync(codigo, false, ct);
    [HttpPost("verificar/{codigo}"), Authorize(Roles = "Administrador,Trabajador")] public Task<VentaTicket> RegistrarVerificacion(string codigo, CancellationToken ct) => ventas.VerificarAsync(codigo, true, ct);
}
