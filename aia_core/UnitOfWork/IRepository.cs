using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Query;
using System.Data;
using System.Linq.Expressions;

namespace aia_core.UnitOfWork
{
    public interface IRepository<T> where T : class
    {
        void Add(T entity);
        Task<T> AddAsync(T entity);
        void Add(IEnumerable<T> entities);
        Task AddAsync(IEnumerable<T> entities);

        void Update(T entity);
        void Update(IEnumerable<T> entities);

        void Delete(T entity);
        void Delete(IEnumerable<T> entities);

        T Get(object id);
        Task<T> GetAsync(object id);
        T Single(Expression<Func<T, bool>> predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null);
        Task<T> SingleAsync(Expression<Func<T, bool>> predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null);
        IQueryable<T> GetAll();
        int GetMaxInt(Expression<Func<T, bool>> expression, Expression<Func<T, int>> field);
        Task<int> GetMaxIntAsync(Expression<Func<T, bool>> expression, Expression<Func<T, int>> field);
        DateTime GetMaxDateTime(Expression<Func<T, bool>> expression, Expression<Func<T, DateTime>> field);
        Task<DateTime> GetMaxDateTimeAsync(Expression<Func<T, bool>> expression, Expression<Func<T, DateTime>> field);
        decimal GetMaxDecimal(Expression<Func<T, bool>> expression, Expression<Func<T, decimal>> field);
        Task<decimal> GetMaxDecimalAsync(Expression<Func<T, bool>> expression, Expression<Func<T, decimal>> field);
        IQueryable<T> GetByCondition(Expression<Func<T, bool>> expression);
        IQueryable<T> Query(Expression<Func<T, bool>> expression = null, Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null, Func<IQueryable<T>, IOrderedQueryable<T>> order = null);
        PagedList<T> Paging(IQueryable<T> query, Expression<Func<T, bool>> expression, Func<IQueryable<T>, IOrderedQueryable<T>> order, int page, int size);
        Task<PagedList<T>> PagingAsync(IQueryable<T> query, Expression<Func<T, bool>> expression, Func<IQueryable<T>, IOrderedQueryable<T>> order, int page, int size);
        IQueryable<T> GroupBy(Expression<Func<T, bool>> expression = null,
            Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> order = null);
        PagedList<T> GetPageIndex(string table, string select, int pageIndex, int pageSize, string orderCol, string sortDir, string groupBy, string search, string conut_field = "1");
        IQueryable<T> FromSqlRaw(string query, SqlParameter[] Parameters = null);
        IEnumerable<T> FromSqlRaw(string query, SqlParameter[] Parameters = null, CommandType type = CommandType.Text);
        Task<IEnumerable<T>> FromSqlRawAsync(string query, SqlParameter[] Parameters = null, CommandType type = CommandType.Text);

        void Save();
        Task<int> SaveAsync();
    }
}
