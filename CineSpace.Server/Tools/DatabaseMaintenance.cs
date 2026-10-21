using CineSpace.Server.Services;
using Npgsql;

namespace CineSpace.Server.Tools;

/// <summary>Herramienta de consola explícita del propietario; nunca se invoca desde HTTP ni durante el arranque normal.</summary>
internal static class DatabaseMaintenance
{
    public static async Task<int> EjecutarAsync(string modo, IConfiguration configuration, IWebHostEnvironment entorno)
    {
        if (modo is not ("comprobar" or "actualizar" or "enlazar")) { Console.Error.WriteLine("Modo válido: comprobar o actualizar."); return 1; }
        try
        {
            var cadena = configuration.GetConnectionString("CineSpace") ?? throw new InvalidOperationException("Falta la conexión PostgreSQL.");
            var opciones = new NpgsqlConnectionStringBuilder(cadena);
            const string referencia = "beopqvzyjrqtcersuhcu";
            if (configuration["Supabase:ProjectRef"] != referencia || opciones.Host != "aws-1-us-west-2.pooler.supabase.com" || opciones.Username != "postgres." + referencia || opciones.Database != "postgres")
                throw new InvalidOperationException("La conexión no corresponde al proyecto Supabase del cine. No se ejecutó SQL.");
            if (opciones.SslMode is not (SslMode.Require or SslMode.VerifyCA or SslMode.VerifyFull)) throw new InvalidOperationException("La conexión debe exigir TLS.");
            await using var conexion = new NpgsqlConnection(cadena);
            await conexion.OpenAsync();
            Console.WriteLine("Conexión PostgreSQL correcta. Proyecto: CineSpace [" + referencia + "].");
            if (modo == "actualizar")
            {
                var carpeta = Path.GetFullPath(Path.Combine(entorno.ContentRootPath, "..", "supabase", "migrations"));
                var archivos = Directory.GetFiles(carpeta, "*.sql");
                if (archivos.Length != 1 || Path.GetFileName(archivos[0]) != "20261020173345_CineSpaceDB.sql") throw new InvalidOperationException("Debe existir un único SQL consolidado del cine.");
                var sql = await File.ReadAllTextAsync(archivos[0]);
                if (!sql.Contains("DROP SCHEMA IF EXISTS cine CASCADE;", StringComparison.Ordinal) || !sql.Contains("COMMIT;", StringComparison.Ordinal)) throw new InvalidOperationException("El SQL no contiene la reconstrucción transaccional esperada.");
                await using var bloqueo = new NpgsqlCommand("SELECT pg_try_advisory_lock(-777)", conexion);
                if (!(bool)(await bloqueo.ExecuteScalarAsync())!) throw new InvalidOperationException("Ya hay otra actualización de la base en ejecución.");
                Console.WriteLine("Reconstruyendo exclusivamente el esquema cine y sus datos de prueba. Se eliminará su contenido anterior.");
                await using var comando = new NpgsqlCommand(sql, conexion) { CommandTimeout = 180 };
                await comando.ExecuteNonQueryAsync();
                Console.WriteLine("SQL consolidado aplicado correctamente.");
            }
            var estado = await PreparacionBaseDatos.ComprobarEsquemaAsync(conexion, default);
            Console.WriteLine(estado.Mensaje);
            if (!estado.Preparada) return 3;
            await using var resumen = new NpgsqlCommand("SELECT (SELECT count(*) FROM cine.usuarios), (SELECT count(*) FROM cine.peliculas), (SELECT count(*) FROM cine.salas), (SELECT count(*) FROM cine.funciones)", conexion);
            await using var lector = await resumen.ExecuteReaderAsync();
            await lector.ReadAsync();
            Console.WriteLine($"Cuentas: {lector.GetInt64(0)} · Películas: {lector.GetInt64(1)} · Salas: {lector.GetInt64(2)} · Funciones: {lector.GetInt64(3)}");
            return 0;
        }
        catch (PostgresException ex) { Console.Error.WriteLine($"ERROR PostgreSQL {ex.SqlState}: {ex.MessageText} (posición {ex.Position})."); return 1; }
        catch (Exception ex) when (ex is NpgsqlException or IOException or InvalidOperationException or ArgumentException)
        { Console.Error.WriteLine("ERROR: " + (ex is NpgsqlException ? "No pudimos conectar con PostgreSQL. Revisa la conexión y la contraseña guardadas en el JSON Development." : ex.Message)); return 1; }
    }
}
