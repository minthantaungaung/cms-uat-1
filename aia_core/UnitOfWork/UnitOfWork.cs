using Microsoft.EntityFrameworkCore;

namespace aia_core.UnitOfWork
{
    public class UnitOfWork<T> : IUnitOfWork<T>, IUnitOfWork where T : DbContext, IDisposable
    {
        private Dictionary<Type, object> _repositories;
        public UnitOfWork(T context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IRepository<TEntity> GetRepository<TEntity>() where TEntity : class
        {
            if (_repositories == null) _repositories = new Dictionary<Type, object>();

            var type = typeof(TEntity);
            if (!_repositories.ContainsKey(type)) _repositories[type] = new Repository<TEntity>(Context);
            return (IRepository<TEntity>)_repositories[type];
        }

        public T Context { get; }

        public int Commit()
        {
            return 1;
        }

        public int SaveChanges()
        {
            return Context.SaveChanges();
        }

        public async Task<int> SaveChangesAsync()
        {
            var result = await Context.SaveChangesAsync();
            return result;
        }

        public void Dispose()
        {
            Context?.Dispose();
        }
    }
}
