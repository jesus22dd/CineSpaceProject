using System.Security.Claims;
using CineSpace.Server.Data;
using CineSpace.Shared.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Cuenta = CineSpace.Server.Models.Usuario;
using Perfil = CineSpace.Shared.Models.Usuario;
using Empleado = CineSpace.Shared.Models.Trabajador;
using CineSpace.Shared.Models;

namespace CineSpace.Server.Services;

public interface ICuentaService
{
    Task<SesionCuenta> SesionAsync(ClaimsPrincipal principal, CancellationToken cancelacion);
    Task<ResultadoOperacion> RegistrarAsync(Perfil perfil, CancellationToken cancelacion);
    Task<SesionCuenta> AccederAsync(AccesoCuenta acceso, CancellationToken cancelacion);
    Task CambiarClaveAsync(ClaimsPrincipal principal, CambioClave cambio);
    Task<Perfil> ActualizarPerfilAsync(ClaimsPrincipal principal, Perfil perfil);
}

public sealed class CuentaService(UserManager<Cuenta> usuarios, SignInManager<Cuenta> acceso, CineSpaceDbContext db, ICorreoCuentaService correo) : ICuentaService
{
    public async Task<SesionCuenta> SesionAsync(ClaimsPrincipal principal, CancellationToken cancelacion)
    {
        var usuario = await usuarios.GetUserAsync(principal);
        if (usuario is null || !usuario.Activo) return new(null, null);
        return await CrearSesionAsync(usuario,principal,cancelacion);
    }
    private async Task<SesionCuenta> CrearSesionAsync(Cuenta usuario, ClaimsPrincipal principal, CancellationToken cancelacion)
    {
        var rol=principal.IsInRole("Administrador") ? RolUsuario.Administrador : principal.IsInRole("Trabajador") ? RolUsuario.Trabajador : RolUsuario.Cliente;
        var perfil = PerfilDe(usuario, rol);
        Empleado? empleado = null;
        if (rol == RolUsuario.Trabajador)
        {
            var registro = await db.Trabajadores.AsNoTracking().FirstOrDefaultAsync(x => x.UsuarioId == usuario.Id, cancelacion);
            if (registro is not null) empleado = EmpleadoDe(registro, usuario);
        }
        return new(perfil, empleado);
    }

    public async Task<ResultadoOperacion> RegistrarAsync(Perfil perfil, CancellationToken cancelacion)
    {
        var usuario = new Cuenta
        {
            UserName = ReglasCine.Texto(perfil.Email, 254, "el correo"), Email = perfil.Email.Trim(),
            Nombre = ReglasCine.Texto(perfil.Nombre, 100, "el nombre"), Apellido = ReglasCine.Texto(perfil.Apellido, 100, "el apellido"),
            Ci = ReglasCine.Texto(perfil.CI, 30, "el documento")
        };
        if (perfil.Password.Length > 128) throw new ArgumentException("La contraseña admite hasta 128 caracteres.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancelacion);
        var normalizado = usuarios.NormalizeEmail(usuario.Email!);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(-600, hashtext({normalizado}))", cancelacion);
        var pendiente = await usuarios.FindByEmailAsync(usuario.Email!);
        if (pendiente is not null)
        {
            if (pendiente.EmailConfirmed) throw new ArgumentException("El correo ya esta registrado.");
            if (pendiente.FechaRegistroUtc > DateTimeOffset.UtcNow.AddDays(-1))
                return new(true, "Revisa el enlace enviado a tu correo. Si no confirmas el registro, podrás intentarlo de nuevo después de 24 horas.");
            // Expired, unconfirmed registrations cannot hold an e-mail forever.
            // Validate the new password before replacing the pending identity.
            foreach (var validator in usuarios.PasswordValidators) Exigir(await validator.ValidateAsync(usuarios, usuario, perfil.Password));
            Exigir(await usuarios.DeleteAsync(pendiente));
        }
        if (await db.Users.AnyAsync(x => x.Ci == usuario.Ci, cancelacion)) throw new ArgumentException("El documento ya esta registrado.");
        var resultado = await usuarios.CreateAsync(usuario, perfil.Password);
        Exigir(resultado);
        Exigir(await usuarios.AddToRoleAsync(usuario, "Cliente"));
        await transaction.CommitAsync(cancelacion);
        try { await correo.ConfirmacionAsync(usuario, cancelacion); }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
        {
            // A delivery failure must not leave an unusable registration behind.
            if (!usuario.EmailConfirmed) Exigir(await usuarios.DeleteAsync(usuario));
            throw new OperacionCineException("No pudimos enviar el correo. Vuelve a intentar el registro en unos minutos.");
        }
        return new(true, "Revisa tu correo para confirmar el registro. El enlace tiene una validez de 2 horas.");
    }

    public async Task<SesionCuenta> AccederAsync(AccesoCuenta solicitud, CancellationToken cancelacion)
    {
        var usuario = await usuarios.FindByEmailAsync(solicitud.Email.Trim());
        if (usuario is null || !usuario.Activo) throw new ArgumentException("Correo o contraseña incorrectos.");
        var resultado = await acceso.PasswordSignInAsync(usuario, solicitud.Password, true, lockoutOnFailure: true);
        if (resultado.IsNotAllowed) throw new ArgumentException("Confirma tu correo antes de iniciar sesion.");
        if (resultado.IsLockedOut) throw new ArgumentException("Demasiados intentos. Espera unos minutos antes de volver a intentarlo.");
        if (!resultado.Succeeded) throw new ArgumentException("Correo o contraseña incorrectos.");
        return await CrearSesionAsync(usuario,acceso.Context.User,cancelacion);
    }

    public async Task CambiarClaveAsync(ClaimsPrincipal principal, CambioClave cambio)
    {
        var usuario = await usuarios.GetUserAsync(principal) ?? throw new UnauthorizedAccessException();
        Exigir(await usuarios.ChangePasswordAsync(usuario, cambio.Actual, cambio.Nueva));
        await acceso.RefreshSignInAsync(usuario);
    }

    public async Task<Perfil> ActualizarPerfilAsync(ClaimsPrincipal principal, Perfil perfil)
    {
        var usuario = await usuarios.GetUserAsync(principal) ?? throw new UnauthorizedAccessException();
        usuario.Nombre = ReglasCine.Texto(perfil.Nombre, 100, "el nombre");
        usuario.Apellido = ReglasCine.Texto(perfil.Apellido, 100, "el apellido");
        var ci = ReglasCine.Texto(perfil.CI, 30, "el documento");
        if (await db.Users.AnyAsync(x => x.Id != usuario.Id && x.Ci == ci)) throw new ArgumentException("El documento ya esta registrado.");
        usuario.Ci = ci;
        Exigir(await usuarios.UpdateAsync(usuario));
        return (await SesionAsync(principal, CancellationToken.None)).Usuario!;
    }

    public static void Exigir(IdentityResult resultado)
    {
        if (!resultado.Succeeded) throw new ArgumentException(string.Join(" ", resultado.Errors.Select(x => x.Code switch
        {
            "DuplicateEmail" or "DuplicateUserName" => "El correo ya esta registrado.",
            "PasswordTooShort" or "PasswordRequiresDigit" or "PasswordRequiresLower" or "PasswordRequiresUpper" or "PasswordRequiresNonAlphanumeric" => "Utiliza una contraseña de al menos 10 caracteres con mayusculas, minusculas, numero y simbolo.",
            "PasswordMismatch" => "La contraseña actual no es correcta.",
            _ => "Revisa los datos de la cuenta."
        }).Distinct()));
    }

    public static Perfil PerfilDe(Cuenta usuario, RolUsuario rol) => new()
    {
        Id = usuario.Id, TienePassword = usuario.PasswordHash is not null, Email = usuario.Email ?? "", Nombre = usuario.Nombre, Apellido = usuario.Apellido, CI = usuario.Ci ?? "",
        Rol = rol, FechaRegistroUtc = usuario.FechaRegistroUtc.UtcDateTime, Password = ""
    };
    public static Empleado EmpleadoDe(Models.TrabajadorRegistro registro, Cuenta usuario) => new()
    {
        Id = registro.Id, Nombre = usuario.Nombre, Apellido = usuario.Apellido, Email = usuario.Email ?? "", CI = usuario.Ci ?? "",
        FechaRegistroUtc = usuario.FechaRegistroUtc.UtcDateTime, FechaNacimiento = registro.Nacimiento.ToDateTime(TimeOnly.MinValue),
        Telefono = registro.Telefono, Direccion = registro.Direccion, Nacionalidad = registro.Nacionalidad, CargoTrabajador = (Cargo)registro.Cargo,
        HorarioTrabajo = registro.Horario, Sueldo = registro.Sueldo, Password = ""
    };
}
