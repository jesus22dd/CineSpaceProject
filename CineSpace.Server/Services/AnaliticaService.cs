using CineSpace.Server.Data;
using CineSpace.Server.Models;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CineSpace.Server.Services;

public interface IAnaliticaService
{
    Task<ResumenSistema> ResumenAsync(CancellationToken ct);
    Task<AnaliticaCine> ConsultarAsync(FiltroVentas filtro, CancellationToken ct);
}
public sealed class AnaliticaService(CineSpaceDbContext db, Microsoft.Extensions.Caching.Memory.IMemoryCache cache, CineSpace.Server.Filters.CacheLecturas lecturas) : IAnaliticaService
{
    public async Task<ResumenSistema> ResumenAsync(CancellationToken ct)
    {
        var m = await db.Database.SqlQueryRaw<MetricasSistema>("""
            SELECT
              (SELECT count(*)::int FROM cine.usuarios_roles ur JOIN cine.roles r ON r.id=ur.role_id WHERE r.name='Cliente') AS "Clientes",
              (SELECT count(*)::int FROM cine.usuarios_roles ur JOIN cine.roles r ON r.id=ur.role_id WHERE r.name='Administrador') AS "Administradores",
              (SELECT count(*)::int FROM cine.trabajadores) AS "Trabajadores",
              (SELECT count(*)::int FROM cine.peliculas) AS "Peliculas",
              (SELECT count(*)::int FROM cine.salas) AS "Salas",
              coalesce((SELECT sum(cantidad)::int FROM cine.ventas),0) AS "Tickets",
              coalesce((SELECT sum(total) FROM cine.ventas),0) AS "Ingresos",
              coalesce((SELECT sum(total) FROM cine.pedidos WHERE estado<>'Cancelado'),0) AS "IngresosConfiteria",
              coalesce((SELECT sum(sueldo) FROM cine.trabajadores),0) AS "Sueldos",
              coalesce((SELECT avg(extract(year FROM age((CURRENT_TIMESTAMP AT TIME ZONE 'America/La_Paz')::date,nacimiento)))::double precision FROM cine.trabajadores),0) AS "EdadPromedio"
            """).SingleAsync(ct);
        var cargos = await db.Trabajadores.GroupBy(x => x.Cargo).Select(g => new { Cargo = g.Key, Total = g.Sum(x=>x.Sueldo), Cantidad = g.Count() }).ToListAsync(ct);
        return new() { Usuarios = m.Clientes + m.Administradores, Clientes = m.Clientes, Administradores = m.Administradores,
            Trabajadores = m.Trabajadores, Peliculas = m.Peliculas, Salas = m.Salas, Tickets = m.Tickets, Ingresos = m.Ingresos,
            IngresosConfiteria = m.IngresosConfiteria, Sueldos = m.Sueldos, EdadPromedio = m.EdadPromedio,
            Series = cargos.Select(x => new SerieAnalitica(((Cargo)x.Cargo).ToString(), x.Total, x.Cantidad)).ToList() };
    }
    public async Task<AnaliticaCine> ConsultarAsync(FiltroVentas filtro, CancellationToken ct)
    {
        var q = Filtrar(db.Ventas.AsNoTracking(), filtro);
        var metricas = await q.GroupBy(x => 1).Select(g => new { Tickets = g.Sum(x => x.Cantidad), Total = g.Sum(x => x.Total), Salas = g.Select(x => x.Funcion.SalaId).Distinct().Count() }).SingleOrDefaultAsync(ct);
        var dto = new AnaliticaCine { Tickets = metricas?.Tickets ?? 0, Ingresos = metricas?.Total ?? 0, Salas = metricas?.Salas ?? 0 };
        var filas = await cache.GetOrCreateAsync(lecturas.Key("analitica.historico"), async entry => {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            return await db.Database.SqlQueryRaw<FilaAnalitica>("""
            WITH peliculas_top AS (SELECT 'peliculas' AS "Tipo",p.titulo AS "Nombre",sum(v.cantidad)::numeric AS "Valor",count(*)::int AS "Cantidad",sum(v.cantidad)::int AS "Tickets",sum(v.total)::numeric AS "Ingresos"
             FROM cine.ventas v JOIN cine.funciones f ON f.id=v.funcion_id JOIN cine.peliculas p ON p.id=f.pelicula_id
             GROUP BY p.id,p.titulo ORDER BY sum(v.cantidad) DESC,sum(v.total) DESC,p.id LIMIT 5)
            SELECT * FROM peliculas_top
            UNION ALL (SELECT 'generos',g.nombre,sum(v.cantidad)::numeric,count(*)::int,sum(v.cantidad)::int,NULL::numeric FROM cine.ventas v JOIN cine.funciones f ON f.id=v.funcion_id JOIN cine.peliculas_generos pg ON pg.pelicula_id=f.pelicula_id JOIN cine.generos g ON g.id=pg.genero_id GROUP BY g.nombre ORDER BY sum(v.cantidad) DESC LIMIT 5)
            UNION ALL (SELECT 'salas',s.nombre,count(*)::numeric,count(*)::int,sum(v.cantidad)::int,NULL::numeric FROM cine.ventas v JOIN cine.funciones f ON f.id=v.funcion_id JOIN cine.salas s ON s.id=f.sala_id GROUP BY s.nombre ORDER BY count(*) DESC LIMIT 5)
            UNION ALL (SELECT 'experiencias',e.nombre,count(*)::numeric,count(*)::int,sum(v.cantidad)::int,NULL::numeric FROM cine.ventas v JOIN cine.funciones f ON f.id=v.funcion_id JOIN cine.experiencias e ON e.id=f.experiencia_id GROUP BY e.nombre ORDER BY count(*) DESC LIMIT 5)
            UNION ALL (SELECT 'categorias',l.categoria,sum(l.cantidad*l.precio_unitario)::numeric,count(*)::int,sum(l.cantidad)::int,NULL::numeric FROM cine.pedido_lineas l JOIN cine.pedidos p ON p.id=l.pedido_id WHERE p.estado<>'Cancelado' GROUP BY l.categoria ORDER BY sum(l.cantidad*l.precio_unitario) DESC LIMIT 5)
            UNION ALL SELECT 'cine',to_char(fecha_utc AT TIME ZONE 'America/La_Paz','YYYY-MM-DD'),sum(total)::numeric,count(*)::int,sum(cantidad)::int,NULL::numeric FROM cine.ventas WHERE fecha_utc >= (((CURRENT_TIMESTAMP AT TIME ZONE 'America/La_Paz')::date - 29)::timestamp AT TIME ZONE 'America/La_Paz') GROUP BY 2
            UNION ALL SELECT 'confiteria',to_char(pagado_utc AT TIME ZONE 'America/La_Paz','YYYY-MM-DD'),sum(total)::numeric,count(*)::int,0,NULL::numeric FROM cine.pedidos WHERE estado<>'Cancelado' AND pagado_utc >= (((CURRENT_TIMESTAMP AT TIME ZONE 'America/La_Paz')::date - 29)::timestamp AT TIME ZONE 'America/La_Paz') GROUP BY 2
            """).ToListAsync(ct);
        }) ?? [];
        List<DatoGrafico> Serie(string tipo) => filas.Where(x => x.Tipo == tipo).Select(x => new DatoGrafico(x.Nombre,x.Valor,x.Cantidad,x.Tickets,x.Ingresos)).ToList();
        dto.PeliculasTop = Serie("peliculas"); dto.Generos = Serie("generos"); dto.SalasTop = Serie("salas"); dto.Experiencias = Serie("experiencias"); dto.Categorias = Serie("categorias");
        var hoy = ReglasCine.Ahora.Date;
        var dias = Enumerable.Range(0,30).Select(i => hoy.AddDays(i-29)).ToArray();
        decimal[] Diarios(string tipo) { var valores = filas.Where(x => x.Tipo == tipo).ToDictionary(x => x.Nombre,x => x.Valor); return dias.Select(d => valores.GetValueOrDefault(d.ToString("yyyy-MM-dd"))).ToArray(); }
        var a=Diarios("cine"); var b=Diarios("confiteria");
        dto.EtiquetasMes=dias.Select(d=>d.ToString("dd/MM")).ToList(); dto.EtiquetasCinco=dto.EtiquetasMes.TakeLast(5).ToList();
        dto.IngresosMes=[new("Cine",a),new("Confitería",b),new("Total",a.Zip(b).Select(x=>x.First+x.Second).ToArray())];
        dto.IngresosCinco=[new("Cine",a.TakeLast(5).ToArray()),new("Confitería",b.TakeLast(5).ToArray())];
        return dto;
    }

    public static IQueryable<VentaRegistro> Filtrar(IQueryable<VentaRegistro> q, FiltroVentas f)
    {
        var hoy = ReglasCine.Ahora.Date;
        var fecha = f.Periodo switch { "hoy" => hoy, "semana" => hoy.AddDays(-(int)hoy.DayOfWeek), "mes" => new DateTime(hoy.Year,hoy.Month,1), _ => DateTime.MinValue };
        if (fecha != DateTime.MinValue) { var desde = ReglasCine.Utc(fecha); q = q.Where(x => x.FechaUtc >= desde); }
        if (f.TipoSala != "todos" && !string.IsNullOrEmpty(f.TipoSala)) q = q.Where(x => x.Funcion.Sala.TipoSalaId == TipoSala.Leer(f.TipoSala));
        if (f.Experiencia != "todos" && !string.IsNullOrEmpty(f.Experiencia)) q = q.Where(x => x.Funcion.ExperienciaId == TipoExperiencia.Leer(f.Experiencia));
        if (f.Genero != "todos" && f.Genero.Length > 0) q = q.Where(x => x.Funcion.Pelicula.Generos.Any(g => g.Genero.Nombre == f.Genero));
        return q;
    }
}

public sealed class MetricasSistema
{
    public int Clientes {get;set;} public int Administradores {get;set;} public int Trabajadores {get;set;}
    public int Peliculas {get;set;} public int Salas {get;set;} public int Tickets {get;set;}
    public decimal Ingresos {get;set;} public decimal IngresosConfiteria {get;set;} public decimal Sueldos {get;set;}
    public double EdadPromedio {get;set;}
}
public sealed class FilaAnalitica
{
    public string Tipo {get;set;} = ""; public string Nombre {get;set;} = "";
    public decimal Valor {get;set;} public int Cantidad {get;set;} public int Tickets {get;set;}
    public decimal? Ingresos {get;set;}
}
