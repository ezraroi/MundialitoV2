using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Mundialito.DAL;

public class GenericRepository<TEntity> : IDisposable where TEntity : class
{
    private bool disposed = false;
    protected readonly MundialitoDbContext Context;
    private readonly DbSet<TEntity> dbSet;

    public GenericRepository(MundialitoDbContext context)
    {
        this.Context = context;
        this.dbSet = context.Set<TEntity>();
    }

    public virtual IEnumerable<TEntity> GetWithRawSql(string query, params object[] parameters)
    {
        return dbSet.FromSqlRaw(query, parameters).ToList();
    }

    public virtual IQueryable<TEntity> Get(Expression<Func<TEntity, bool>> filter = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null, string includeProperties = "")
    {
        IQueryable<TEntity> query = dbSet;

        if (filter != null)
        {
            query = query.Where(filter);
        }

        query = includeProperties.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Aggregate(query, (current, includeProperty) => current.Include(includeProperty));

        if (orderBy != null)
        {
            return orderBy(query);
        }
        return query;
    }

    public virtual TEntity GetByID(object id)
    {
        return dbSet.Find(id);
    }

    public virtual TEntity Insert(TEntity entity)
    {
        return dbSet.Add(entity).Entity;
    }

    public virtual void Delete(object id)
    {
        TEntity entityToDelete = dbSet.Find(id);
        Delete(entityToDelete);
    }

    public virtual void Delete(TEntity entityToDelete)
    {
        if (Context.Entry(entityToDelete).State == EntityState.Detached)
        {
            dbSet.Attach(entityToDelete);
        }
        dbSet.Remove(entityToDelete);
    }

    public virtual void Update(TEntity entityToUpdate)
    {
        if (Context.Entry(entityToUpdate).State == EntityState.Detached)
        {
            dbSet.Attach(entityToUpdate);
        }
        Context.Entry(entityToUpdate).State = EntityState.Modified;
    }

    public virtual void Save()
    {
        Context.SaveChanges(); 
    }

    #region Implementation of IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                Context.Dispose();
            }
        }
        disposed = true;
    }
}
