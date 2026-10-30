using CineSpace.Shared.Contracts;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace CineSpace.Server.Repositories;

public interface IRepositorioCine
{
    IQueryable<T> Leer<T>() where T : class;
    Task<Pagina<T>> PaginarAsync<T>(IQueryable<T> query, ConsultaPagina consulta, CancellationToken cancelacion = default);
    Task<Pagina<TResult>> PaginarAsync<T, TResult>(IQueryable<T> query, ConsultaPagina consulta, Expression<Func<T, TResult>> proyeccion, CancellationToken cancelacion = default);
}

public sealed class RepositorioCine(Data.CineSpaceDbContext db) : IRepositorioCine
{
    public IQueryable<T> Leer<T>() where T : class => db.Set<T>().AsNoTracking();

    public Task<Pagina<T>> PaginarAsync<T>(IQueryable<T> query, ConsultaPagina consulta, CancellationToken cancelacion = default)
        => PaginarAsync(query, consulta, x => x, cancelacion);

    public async Task<Pagina<TResult>> PaginarAsync<T, TResult>(IQueryable<T> query, ConsultaPagina consulta, Expression<Func<T, TResult>> proyeccion, CancellationToken cancelacion = default)
    {
        var tamano = Math.Clamp(consulta.Tamano, 1, 100);
        var total = await query.CountAsync(cancelacion);
        var pagina = Math.Clamp(consulta.Pagina, 1, Math.Max(1, (int)Math.Ceiling(total / (double)tamano)));
        var items = await query.Skip((pagina - 1) * tamano).Take(tamano).Select(proyeccion).ToListAsync(cancelacion);
        return new(items, total, pagina, tamano);
    }
}
