using FamilyPlus.Api.Data;
using FamilyPlus.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Repositories;

public interface IRepository<T> where T : class
{
    IQueryable<T> Query();
    Task<T?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
}

public sealed class EfRepository<T>(FinanceDbContext db) : IRepository<T> where T : class
{
    public IQueryable<T> Query() => db.Set<T>().AsQueryable();
    public Task<T?> GetAsync(Guid id, CancellationToken cancellationToken = default) => db.Set<T>().FindAsync([id], cancellationToken).AsTask();
    public Task AddAsync(T entity, CancellationToken cancellationToken = default) => db.Set<T>().AddAsync(entity, cancellationToken).AsTask();
    public Task SaveAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
}
