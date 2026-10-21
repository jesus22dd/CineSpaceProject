using CineSpace.Shared.Contracts;
using Npgsql;

namespace CineSpace.Server.Services;

public sealed class PreparacionBaseDatos(IConfiguration configuration, ILogger<PreparacionBaseDatos> logger)
{
    public async Task<EstadoBaseDatos> ComprobarAsync(CancellationToken ct)
    {
        var cadena = configuration.GetConnectionString("CineSpace");
        if (string.IsNullOrWhiteSpace(cadena)) return new(false, false, "Falta configurar la conexión PostgreSQL del servidor.");
        try
        {
            await using var conexion = new NpgsqlConnection(cadena);
            await conexion.OpenAsync(ct);
            return await ComprobarEsquemaAsync(conexion, ct);
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException or ArgumentException)
        {
            logger.LogWarning("No se pudo comprobar PostgreSQL ({Tipo}).", ex.GetType().Name);
            return new(false, false, "No pudimos conectar con PostgreSQL. Revisa la conexión en appsettings.Development.json del servidor.");
        }
    }

    public static async Task<EstadoBaseDatos> ComprobarEsquemaAsync(NpgsqlConnection conexion, CancellationToken ct)
    {
        const string sql = "SELECT count(*) >= 30 AND to_regclass('cine.usuarios') IS NOT NULL AND to_regclass('cine.funciones') IS NOT NULL AND to_regclass('cine.ventas') IS NOT NULL FROM pg_catalog.pg_tables WHERE schemaname = 'cine'";
        await using var comando = new NpgsqlCommand(sql, conexion) { CommandTimeout = 15 };
        var preparada = (bool)(await comando.ExecuteScalarAsync(ct))!;
        return preparada ? new(true, true, "CineSpace está listo.")
            : new(true, false, "La conexión PostgreSQL funciona, pero falta crear las tablas del cine. Ejecuta update_DB.bat y vuelve a intentar.");
    }
}
