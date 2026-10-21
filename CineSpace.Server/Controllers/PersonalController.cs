using System.Security.Claims;
using CineSpace.Server.Services;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineSpace.Server.Controllers;

[ApiController, Route("api/personal"), Authorize(Roles = "Administrador")]
public sealed class PersonalController(IPersonalService personal) : ControllerBase
{
    private Guid Operador => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("usuarios")] public Task<Pagina<Usuario>> Usuarios([FromQuery] ConsultaPagina f, CancellationToken ct) => personal.UsuariosAsync(f, ct);
    [HttpPost("usuarios")] public Task<Usuario> CrearUsuario(Usuario dto, CancellationToken ct) => personal.GuardarUsuarioAsync(dto, null, Operador, ct);
    [HttpPut("usuarios/{id:guid}")] public Task<Usuario> EditarUsuario(Guid id, Usuario dto, CancellationToken ct) => personal.GuardarUsuarioAsync(dto, id, Operador, ct);
    [HttpDelete("usuarios/{id:guid}")] public Task EliminarUsuario(Guid id, CancellationToken ct) => personal.EliminarUsuarioAsync(id, Operador, ct);
    [HttpGet("trabajadores")] public Task<Pagina<Trabajador>> Trabajadores([FromQuery] ConsultaPagina f, CancellationToken ct) => personal.TrabajadoresAsync(f, ct);
    [HttpPost("trabajadores")] public Task<Trabajador> CrearTrabajador(Trabajador dto, CancellationToken ct) => personal.GuardarTrabajadorAsync(dto, true, ct);
    [HttpPut("trabajadores/{id:int}")] public Task<Trabajador> EditarTrabajador(int id, Trabajador dto, CancellationToken ct) { dto.Id = id; return personal.GuardarTrabajadorAsync(dto, false, ct); }
    [HttpDelete("trabajadores/{id:int}")] public Task EliminarTrabajador(int id, CancellationToken ct) => personal.EliminarTrabajadorAsync(id, ct);
}
