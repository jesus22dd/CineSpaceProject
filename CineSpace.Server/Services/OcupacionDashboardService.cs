using CineSpace.Server.Data;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace CineSpace.Server.Services;

public interface IOcupacionDashboardService
{
    Task<OcupacionDashboard> ConsultarAsync(DateOnly? fecha, int? experiencia, CancellationToken ct);
}

public sealed class OcupacionDashboardService(CineSpaceDbContext db) : IOcupacionDashboardService
{
    public async Task<OcupacionDashboard> ConsultarAsync(DateOnly? fecha, int? experiencia, CancellationToken ct)
    {
        if (experiencia is <= 0) throw new ArgumentException("Selecciona una experiencia válida.");
        var dia = fecha ?? DateOnly.FromDateTime(ReglasCine.Ahora);
        if (dia == DateOnly.MaxValue) throw new ArgumentException("Selecciona otra fecha.");
        var desde = ReglasCine.Utc(dia.ToDateTime(TimeOnly.MinValue));
        var hasta = ReglasCine.Utc(dia.AddDays(1).ToDateTime(TimeOnly.MinValue));
        // One bounded round trip. Filter by the indexed UTC range before counting
        // seats; only aggregates leave PostgreSQL, never individual seat plans.
        var filas = await db.Database.SqlQuery<FilaOcupacionDashboard>($"""
            WITH seleccion AS MATERIALIZED (
                SELECT f.id, f.inicio_utc, jsonb_array_length(d.plano_json->'asientos') AS capacidad
                FROM cine.funciones f
                JOIN cine.salas s ON s.id=f.sala_id
                JOIN cine.disenos d ON d.id=s.diseno_id
                JOIN cine.peliculas p ON p.id=f.pelicula_id
                WHERE f.inicio_utc >= {desde} AND f.inicio_utc < {hasta} AND s.activa AND p.publicada
                  AND ({experiencia}::integer IS NULL OR f.experiencia_id={experiencia}::integer)
            ), ocupacion AS MATERIALIZED (
                SELECT f.id, f.inicio_utc, f.capacidad, count(b.asiento)::int AS vendidas
                FROM seleccion f LEFT JOIN cine.butacas_vendidas b ON b.funcion_id=f.id
                GROUP BY f.id,f.inicio_utc,f.capacidad
            )
            SELECT 'estado' AS "Tipo", CASE WHEN vendidas=0 THEN 'Sin ventas'
                WHEN vendidas>=capacidad THEN 'Completas' ELSE 'Con disponibilidad' END AS "Nombre",
                count(*)::int AS "Funciones", sum(capacidad)::int AS "Capacidad", sum(vendidas)::int AS "Vendidas"
            FROM ocupacion GROUP BY 2
            UNION ALL
            SELECT 'hora',to_char(inicio_utc AT TIME ZONE 'America/La_Paz','HH24:00'),
                count(*)::int,sum(capacidad)::int,sum(vendidas)::int
            FROM ocupacion GROUP BY 2
            ORDER BY 1,2
            """).ToListAsync(ct);
        var estados = filas.Where(x => x.Tipo == "estado").ToDictionary(x => x.Nombre);
        return new()
        {
            Fecha = dia,
            Estados = new[] { "Completas", "Con disponibilidad", "Sin ventas" }.Select(nombre =>
                estados.TryGetValue(nombre, out var fila)
                    ? new DatoGrafico(nombre, fila.Funciones, Tickets: fila.Vendidas, Capacidad: fila.Capacidad)
                    : new DatoGrafico(nombre, 0, Capacidad: 0)).ToList(),
            Horarios = filas.Where(x => x.Tipo == "hora").Select(x => new DatoGrafico(x.Nombre,
                x.Capacidad == 0 ? 0 : x.Vendidas * 100m / x.Capacidad,
                Tickets: x.Vendidas, Capacidad: x.Capacidad)).ToList()
        };
    }
}

public sealed class FilaOcupacionDashboard
{
    public string Tipo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public int Funciones { get; set; }
    public int Capacidad { get; set; }
    public int Vendidas { get; set; }
}
