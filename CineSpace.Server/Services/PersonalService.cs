using CineSpace.Server.Data;
using CineSpace.Server.Models;
using CineSpace.Server.Repositories;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Cuenta = CineSpace.Server.Models.Usuario;
using Perfil = CineSpace.Shared.Models.Usuario;

namespace CineSpace.Server.Services;

public interface IPersonalService
{
    Task<Pagina<Perfil>> UsuariosAsync(ConsultaPagina filtro, CancellationToken ct);
    Task<Perfil> GuardarUsuarioAsync(Perfil perfil, Guid? id, Guid operador, CancellationToken ct);
    Task EliminarUsuarioAsync(Guid id, Guid operador, CancellationToken ct);
    Task<Pagina<Trabajador>> TrabajadoresAsync(ConsultaPagina filtro, CancellationToken ct);
    Task<Trabajador> GuardarTrabajadorAsync(Trabajador perfil, bool nuevo, CancellationToken ct);
    Task EliminarTrabajadorAsync(int id, CancellationToken ct);
}

public sealed class PersonalService(CineSpaceDbContext db, IRepositorioCine repositorio, UserManager<Cuenta> usuarios) : IPersonalService
{
    public async Task<Pagina<Perfil>> UsuariosAsync(ConsultaPagina f, CancellationToken ct)
    {
        var q = db.Users.AsNoTracking().Where(u => !db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Trabajador")));
        if (!string.IsNullOrWhiteSpace(f.Buscar)) { var t = "%" + f.Buscar.Trim() + "%"; q = q.Where(x => EF.Functions.ILike(x.Nombre + " " + x.Apellido, t) || EF.Functions.ILike(x.Email!, t) || EF.Functions.ILike(x.Ci!, t)); }
        if (f.Rol is "Cliente" or "Administrador") q = q.Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == f.Rol)));
        var ordenada = f.Orden == "antiguas" ? q.OrderBy(x => x.FechaRegistroUtc).ThenBy(x => x.Id) : q.OrderByDescending(x => x.FechaRegistroUtc).ThenBy(x => x.Id);
        var perfiles = ordenada.Select(x => new Perfil { Id=x.Id, Nombre=x.Nombre, Apellido=x.Apellido, Email=x.Email ?? "", CI=x.Ci ?? "", Password="", TienePassword=x.PasswordHash!=null,
            FechaRegistroUtc=x.FechaRegistroUtc.UtcDateTime,
            Rol=db.UserRoles.Any(ur=>ur.UserId==x.Id && db.Roles.Any(r=>r.Id==ur.RoleId && r.Name=="Administrador")) ? RolUsuario.Administrador : RolUsuario.Cliente });
        return await repositorio.PaginarAsync(perfiles,f,ct);
    }

    public async Task<Perfil> GuardarUsuarioAsync(Perfil dto, Guid? id, Guid operador, CancellationToken ct)
    {
        if (dto.Rol is not (RolUsuario.Cliente or RolUsuario.Administrador)) throw new ArgumentException("Gestiona los trabajadores desde su propia seccion.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var user = id.HasValue ? await usuarios.FindByIdAsync(id.Value.ToString()) ?? throw new KeyNotFoundException() : new Cuenta();
        if (id == operador && dto.Rol != RolUsuario.Administrador) throw new OperacionCineException("No puedes quitarte el acceso de administrador.");
        await AplicarPerfil(user, dto.Nombre, dto.Apellido, dto.Email, dto.CI, ct);
        if (!id.HasValue)
        {
            user.EmailConfirmed = true;
            CuentaService.Exigir(await usuarios.CreateAsync(user, dto.Password));
        }
        else
        {
            CuentaService.Exigir(await usuarios.UpdateAsync(user));
            if (!string.IsNullOrEmpty(dto.Password)) CuentaService.Exigir(await usuarios.ResetPasswordAsync(user, await usuarios.GeneratePasswordResetTokenAsync(user), dto.Password));
            var roles = await usuarios.GetRolesAsync(user);
            if (roles.Contains("Trabajador")) throw new ArgumentException("Esta cuenta pertenece a un trabajador.");
            CuentaService.Exigir(await usuarios.RemoveFromRolesAsync(user, roles));
        }
        CuentaService.Exigir(await usuarios.AddToRoleAsync(user, dto.Rol.ToString()));
        CuentaService.Exigir(await usuarios.UpdateSecurityStampAsync(user));
        await tx.CommitAsync(ct); return CuentaService.PerfilDe(user, dto.Rol);
    }

    private async Task AplicarPerfil(Cuenta user, string nombre, string apellido, string email, string ci, CancellationToken ct)
    {
        user.Nombre = ReglasCine.Texto(nombre, 100, "el nombre"); user.Apellido = ReglasCine.Texto(apellido, 100, "el apellido");
        email = ReglasCine.Texto(email, 254, "el correo");
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email)) throw new ArgumentException("El correo no es valido.");
        user.Email = email; user.UserName = email; user.Ci = ReglasCine.Texto(ci, 30, "el documento");
        if (await db.Users.AnyAsync(x => x.Id != user.Id && x.Ci == user.Ci, ct)) throw new ArgumentException("El documento ya esta registrado.");
    }

    public async Task EliminarUsuarioAsync(Guid id, Guid operador, CancellationToken ct)
    {
        if (id == operador) throw new OperacionCineException("No puedes eliminar tu propia cuenta.");
        var user = await usuarios.FindByIdAsync(id.ToString()) ?? throw new KeyNotFoundException();
        if (await db.Ventas.AnyAsync(x => x.UsuarioId == id || x.OperadorId == id, ct) || await db.Pedidos.AnyAsync(x => x.UsuarioId == id || x.EntregadoPorId == id, ct))
            throw new OperacionCineException("Esta cuenta tiene operaciones registradas y debe conservarse para su historial.");
        CuentaService.Exigir(await usuarios.DeleteAsync(user));
    }

    public async Task<Pagina<Trabajador>> TrabajadoresAsync(ConsultaPagina f, CancellationToken ct)
    {
        var q = db.Trabajadores.AsNoTracking().Include(x => x.Usuario).AsQueryable();
        if (!string.IsNullOrWhiteSpace(f.Buscar)) { var t = "%" + f.Buscar.Trim() + "%"; q = q.Where(x => EF.Functions.ILike(x.Usuario.Nombre + " " + x.Usuario.Apellido, t) || EF.Functions.ILike(x.Usuario.Email!, t) || EF.Functions.ILike(x.Usuario.Ci!, t)); }
        if (int.TryParse(f.Estado, out var cargo)) q = q.Where(x => x.Cargo == cargo);
        var p = await repositorio.PaginarAsync(f.Orden == "antiguas" ? q.OrderBy(x => x.Usuario.FechaRegistroUtc).ThenBy(x => x.Id) : q.OrderByDescending(x => x.Usuario.FechaRegistroUtc).ThenBy(x => x.Id), f, ct);
        return new(p.Items.Select(x => CuentaService.EmpleadoDe(x, x.Usuario)).ToList(), p.Total, p.Numero, p.Tamano);
    }
    public async Task<Trabajador> GuardarTrabajadorAsync(Trabajador dto, bool nuevo, CancellationToken ct)
    {
        if (!dto.EsMayorDeEdad() || dto.ObtenerEdad() > 85 || !Enum.IsDefined(dto.CargoTrabajador) || dto.Sueldo is <= 0 or > 100000) throw new ArgumentException("Revisa la edad, el cargo y el sueldo del trabajador.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var registro = nuevo ? new TrabajadorRegistro { Usuario = new Cuenta { EmailConfirmed = true } } : await db.Trabajadores.Include(x => x.Usuario).SingleOrDefaultAsync(x => x.Id == dto.Id, ct) ?? throw new KeyNotFoundException();
        var user = registro.Usuario;
        await AplicarPerfil(user, dto.Nombre, dto.Apellido, dto.Email, dto.CI, ct);
        if (nuevo)
        {
            CuentaService.Exigir(await usuarios.CreateAsync(user, dto.Password));
            CuentaService.Exigir(await usuarios.AddToRoleAsync(user, "Trabajador")); registro.UsuarioId = user.Id; db.Trabajadores.Add(registro);
        }
        else
        {
            CuentaService.Exigir(await usuarios.UpdateAsync(user));
            if (!string.IsNullOrWhiteSpace(dto.Password)) CuentaService.Exigir(await usuarios.ResetPasswordAsync(user, await usuarios.GeneratePasswordResetTokenAsync(user), dto.Password));
            CuentaService.Exigir(await usuarios.UpdateSecurityStampAsync(user));
        }
        registro.Nacimiento = DateOnly.FromDateTime(dto.FechaNacimiento); registro.Telefono = ReglasCine.Texto(dto.Telefono, 30, "el telefono", false);
        registro.Direccion = ReglasCine.Texto(dto.Direccion, 250, "la direccion", false); registro.Nacionalidad = ReglasCine.Texto(dto.Nacionalidad, 80, "la nacionalidad");
        registro.Cargo = (int)dto.CargoTrabajador; registro.Horario = ReglasCine.Texto(dto.HorarioTrabajo, 100, "el horario"); registro.Sueldo = dto.Sueldo;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return CuentaService.EmpleadoDe(registro, user);
    }
    public async Task EliminarTrabajadorAsync(int id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var worker = await db.Trabajadores.Include(x => x.Usuario).SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException();
        db.Trabajadores.Remove(worker); await db.SaveChangesAsync(ct);
        await EliminarUsuarioAsync(worker.UsuarioId, Guid.Empty, ct);
        await tx.CommitAsync(ct);
    }
}
