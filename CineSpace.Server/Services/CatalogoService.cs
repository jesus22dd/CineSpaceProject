using CineSpace.Server.Data;
using CineSpace.Server.Models;
using CineSpace.Server.Repositories;
using CineSpace.Shared.Contracts;
using CineSpace.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CineSpace.Server.Services;

public interface ICatalogoService
{
    Task<ReferenciasCatalogo> ReferenciasAsync(CancellationToken ct);
    Task<Pagina<T>> PaginaAsync<T>(ConsultaPagina consulta, CancellationToken ct) where T : RegistroCatalogo;
    Task<int> GuardarAsync<T>(int? id, string nombre, string descripcion, CancellationToken ct) where T : RegistroCatalogo, new();
    Task EliminarAsync<T>(int id, CancellationToken ct) where T : class;
    Task<int> GuardarSalaAsync(int? id, SalaCine solicitud, CancellationToken ct);
    Task<int> GuardarDisenoAsync(int? id, PlanoSala solicitud, CancellationToken ct);
    Task<int> GuardarDistribuidoraAsync(int? id, Distribuidora solicitud, CancellationToken ct);
    Task<int> GuardarComboAsync(int? id, ComboConfiteria solicitud, CancellationToken ct);
}

public sealed class CatalogoService(CineSpaceDbContext db, IRepositorioCine repositorio, IImagenStorageService imagenes, IMemoryCache cache) : ICatalogoService
{
    public Task<ReferenciasCatalogo> ReferenciasAsync(CancellationToken ct) => ConstruirReferenciasAsync(ct);
    private async Task<ReferenciasCatalogo> ConstruirReferenciasAsync(CancellationToken ct)
    {
        // One round trip, bounded reference lists; seat plans remain on demand.
        var json = await db.Database.SqlQueryRaw<string>("""
            SELECT jsonb_build_object(
              'tiposSala',coalesce((SELECT jsonb_agg(jsonb_build_object('id',id,'nombre',nombre,'descripcion',descripcion) ORDER BY id) FROM cine.tipos_sala),'[]'),
              'experiencias',coalesce((SELECT jsonb_agg(jsonb_build_object('id',id,'nombre',nombre,'descripcion',descripcion) ORDER BY id) FROM cine.experiencias),'[]'),
              'generos',coalesce((SELECT jsonb_agg(jsonb_build_object('id',id,'nombre',nombre) ORDER BY nombre) FROM cine.generos),'[]'),
              'categorias',coalesce((SELECT jsonb_agg(jsonb_build_object('id',id,'nombre',nombre) ORDER BY nombre) FROM cine.categorias),'[]'),
              'distribuidoras',coalesce((SELECT jsonb_agg(jsonb_build_object('id',id,'nombre',nombre) ORDER BY nombre) FROM cine.distribuidoras),'[]'),
              'salas',coalesce((SELECT jsonb_agg(jsonb_build_object('numero',s.id,'nombre',s.nombre,'disenoId',s.diseno_id,'tipo',s.tipo_sala_id,'precio',s.precio,'descuento',s.descuento,'oferta',s.descuento>0,
                'capacidad',jsonb_array_length(d.plano_json->'asientos'),'experiencias',coalesce((SELECT jsonb_agg(e.experiencia_id ORDER BY e.experiencia_id) FROM cine.salas_experiencias e WHERE e.sala_id=s.id),'[]')) ORDER BY s.id) FROM cine.salas s JOIN cine.disenos d ON d.id=s.diseno_id WHERE s.activa),'[]'),
              'disenos',coalesce((SELECT jsonb_agg(jsonb_build_object('id',id,'plano',jsonb_build_object('numero',id,'nombre',nombre,'filas',filas,'columnas',columnas)) ORDER BY id) FROM cine.disenos),'[]'),
              'usosDistribuidoras',coalesce((SELECT jsonb_object_agg(id,n) FROM (SELECT distribuidora_id id,count(*) n FROM cine.peliculas WHERE distribuidora_id IS NOT NULL GROUP BY 1) t),'{{}}'),
              'usosGeneros',coalesce((SELECT jsonb_object_agg(id,n) FROM (SELECT genero_id id,count(*) n FROM cine.peliculas_generos GROUP BY 1) t),'{{}}'),
              'usosCategorias',coalesce((SELECT jsonb_object_agg(id,n) FROM (SELECT categoria_id id,count(*) n FROM cine.combos GROUP BY 1) t),'{{}}'),
              'usosDisenos',coalesce((SELECT jsonb_object_agg(id,n) FROM (SELECT diseno_id id,count(*) n FROM cine.salas GROUP BY 1) t),'{{}}')
            )::text AS "Value"
            """).SingleAsync(ct);
        return ReglasCine.Leer<ReferenciasCatalogo>(json);
    }

    public Task<Pagina<T>> PaginaAsync<T>(ConsultaPagina consulta, CancellationToken ct) where T : RegistroCatalogo
    {
        var buscar = ReglasCine.Texto(consulta.Buscar, 100, "la busqueda", false);
        var query = repositorio.Leer<T>();
        if (buscar.Length > 0) query = query.Where(x => EF.Functions.ILike(x.Nombre, "%" + buscar + "%"));
        query = consulta.Orden is "antiguos" or "antiguas" or "Más antiguos" ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id);
        return repositorio.PaginarAsync(query, consulta, ct);
    }

    public async Task<int> GuardarAsync<T>(int? id, string nombre, string descripcion, CancellationToken ct) where T : RegistroCatalogo, new()
    {
        var registro = id.HasValue ? await db.Set<T>().FindAsync([id.Value], ct) ?? throw new KeyNotFoundException() : new T();
        registro.Nombre = ReglasCine.Texto(nombre, 200, "el nombre");
        registro.Descripcion = ReglasCine.Texto(descripcion, 1000, "la descripcion", false);
        if (!id.HasValue) db.Add(registro);
        await db.SaveChangesAsync(ct); cache.Remove("catalogo.referencias");
        return registro.Id;
    }

    public async Task EliminarAsync<T>(int id, CancellationToken ct) where T : class
    {
        var registro = await db.Set<T>().FindAsync([id], ct) ?? throw new KeyNotFoundException();
        db.Remove(registro);
        await db.SaveChangesAsync(ct); cache.Remove("catalogo.referencias");
    }

    public async Task<int> GuardarDistribuidoraAsync(int? id, Distribuidora entrada, CancellationToken ct)
    {
        var registro = id.HasValue ? await db.Distribuidoras.FindAsync([id.Value], ct) ?? throw new KeyNotFoundException() : new DistribuidoraRegistro();
        registro.Nombre = ReglasCine.Texto(entrada.Nombre, 200, "el nombre");
        registro.Contacto = ReglasCine.Texto(entrada.Contacto, 150, "el contacto", false);
        registro.Email = ReglasCine.Texto(entrada.Email, 254, "el correo", false);
        if (registro.Email.Length > 0 && !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(registro.Email)) throw new ArgumentException("Escribe un correo valido.");
        registro.Telefono = ReglasCine.Texto(entrada.Telefono, 40, "el telefono", false);
        if (!id.HasValue) db.Add(registro);
        await db.SaveChangesAsync(ct); cache.Remove("catalogo.referencias"); return registro.Id;
    }

    public async Task<int> GuardarSalaAsync(int? id, SalaCine entrada, CancellationToken ct)
    {
        ReglasCine.Precio(entrada.Precio, entrada.Oferta ? entrada.Descuento : 0);
        var experiencias = entrada.Experiencias.Distinct().ToList();
        if (experiencias.Count == 0 || experiencias.Count != await db.Experiencias.CountAsync(x => experiencias.Contains(x.Id), ct)) throw new ArgumentException("Selecciona experiencias validas.");
        if (!await db.Disenos.AnyAsync(x => x.Id == entrada.DisenoId, ct) || !await db.TiposSala.AnyAsync(x => x.Id == entrada.Tipo, ct)) throw new ArgumentException("Selecciona un diseno y tipo de sala validos.");
        var registro = id.HasValue ? await db.Salas.Include(x => x.Experiencias).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException() : new SalaRegistro();
        if (id.HasValue && (registro.DisenoId != entrada.DisenoId || registro.TipoSalaId != entrada.Tipo || registro.Experiencias.Any(x => !experiencias.Contains(x.ExperienciaId))) &&
            await db.Funciones.AnyAsync(x => x.SalaId == id && (x.SalaListaUtc > DateTime.UtcNow || db.Ventas.Any(v => v.FuncionId == x.Id)), ct))
            throw new ArgumentException("La sala tiene funciones o ventas. Conserva su distribucion y experiencias.");
        registro.Nombre = ReglasCine.Texto(entrada.Nombre, 80, "el nombre de la sala"); registro.DisenoId = entrada.DisenoId; registro.TipoSalaId = entrada.Tipo;
        registro.Precio = entrada.Precio; registro.Descuento = entrada.Oferta ? entrada.Descuento : 0;
        foreach (var anterior in registro.Experiencias.Where(x => !experiencias.Contains(x.ExperienciaId)).ToList()) db.Remove(anterior);
        foreach (var nueva in experiencias.Where(e => registro.Experiencias.All(x => x.ExperienciaId != e))) registro.Experiencias.Add(new() { ExperienciaId = nueva });
        if (!id.HasValue) db.Add(registro);
        await db.SaveChangesAsync(ct); cache.Remove("catalogo.referencias"); return registro.Id;
    }

    public async Task<int> GuardarDisenoAsync(int? id, PlanoSala entrada, CancellationToken ct)
    {
        entrada.Validar();
        if (entrada.Categorias.Any(x => !entrada.Asientos.Contains(x.Key) || !Enum.IsDefined(x.Value))) throw new ArgumentException("Revisa las categorias de butacas.");
        if (id.HasValue && await db.Funciones.AnyAsync(x => x.Sala.DisenoId == id && (x.SalaListaUtc > DateTime.UtcNow || db.Ventas.Any(v => v.FuncionId == x.Id)), ct)) throw new ArgumentException("El diseno tiene funciones o ventas y no puede modificarse.");
        var registro = id.HasValue ? await db.Disenos.FindAsync([id.Value], ct) ?? throw new KeyNotFoundException() : new DisenoRegistro();
        registro.Nombre = entrada.Nombre.Trim(); registro.Filas = entrada.Filas; registro.Columnas = entrada.Columnas;
        registro.PlanoJson = ReglasCine.Serializar(entrada);
        if (!id.HasValue) db.Add(registro);
        await db.SaveChangesAsync(ct); cache.Remove("catalogo.referencias"); return registro.Id;
    }

    public async Task<int> GuardarComboAsync(int? id, ComboConfiteria entrada, CancellationToken ct)
    {
        if (entrada.Precio is <= 0 or > 10000 || !await db.Categorias.AnyAsync(x => x.Id == entrada.CategoriaId, ct)) throw new ArgumentException("Revisa el precio y la categoria del combo.");
        await using var archivos = new CargaImagenes(imagenes);
        var foto = await archivos.RecibirAsync(entrada.Foto, "confiteria", false, ct);
        var registro = id.HasValue ? await db.Combos.FindAsync([id.Value], ct) ?? throw new KeyNotFoundException() : new ComboRegistro();
        registro.Nombre = ReglasCine.Texto(entrada.Nombre, 100, "el nombre"); registro.Descripcion = ReglasCine.Texto(entrada.Descripcion, 1200, "la descripcion", false);
        registro.CategoriaId = entrada.CategoriaId; registro.Foto = foto; registro.Precio = entrada.Precio; registro.Publicado = entrada.Publicado;
        if (!id.HasValue) db.Add(registro);
        await db.SaveChangesAsync(ct); cache.Remove("catalogo.referencias"); archivos.Confirmar(); return registro.Id;
    }
}
