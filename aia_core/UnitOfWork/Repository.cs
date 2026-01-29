using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;
using Microsoft.Data.SqlClient;

namespace aia_core.UnitOfWork
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly DbContext _dbContext;
        protected readonly DbSet<T> _dbSet;

        public Repository(DbContext context)
        {
            this._dbContext = context ?? throw new ArgumentException(nameof(context));
            this._dbSet = _dbContext.Set<T>();
        }
        public void Add(T entity)
        {
            this._dbSet.Add(entity);
        }
        public async Task<T> AddAsync(T entity)
        {
            var result = await this._dbSet.AddAsync(entity);
            return result.Entity;
        }

        public void Add(IEnumerable<T> entities)
        {
            this._dbSet.AddRange(entities);
        }

        public async Task AddAsync(IEnumerable<T> entities)
        {
            await this._dbSet.AddRangeAsync(entities);
        }


        public void Update(T entity)
        {
            this._dbSet.Update(entity);
        }
        public void Update(IEnumerable<T> entities)
        {
            this._dbSet.UpdateRange(entities);
        }

        public void Delete(T entity)
        {
            this._dbSet.Remove(entity);
        }
        public void Delete(IEnumerable<T> entities)
        {
            this._dbSet.RemoveRange(entities);
        }

        public T Get(object id)
        {
            return this._dbSet.Find(id);
        }

        public async Task<T> GetAsync(object id)
        {
            var result = await this._dbSet.FindAsync(id);
            return result;
        }

        public T Single(Expression<Func<T, bool>> predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null)
        {
            IQueryable<T> query = this._dbSet;
            if (predicate != null) query = query.Where(predicate);
            if (orderBy != null)
                return orderBy(query).FirstOrDefault();
            return query.FirstOrDefault();
        }

        public async Task<T> SingleAsync(Expression<Func<T, bool>> predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null)
        {
            IQueryable<T> query = this._dbSet;
            if (predicate != null) query = query.Where(predicate);
            if (orderBy != null)
            {
                var data = await orderBy(query).FirstOrDefaultAsync();
                return data;
            }
            else
            {
                var data = await query.FirstOrDefaultAsync();
                return data;
            }
        }

        public IQueryable<T> GetAll()
        {
            IQueryable<T> query = this._dbSet;
            return query;
        }

        public int GetMaxInt(Expression<Func<T, bool>> expression, Expression<Func<T, int>> field)
        {
            int result;
            if (expression != null)
            {
                result = this._dbSet.Where(expression).Max(field);
            }
            else
            {
                result = this._dbSet.Max(field);
            }
            return result;
        }
        public async Task<int> GetMaxIntAsync(Expression<Func<T, bool>> expression, Expression<Func<T, int>> field)
        {
            int result;
            if (expression != null)
            {
                result = await this._dbSet.Where(expression).MaxAsync(field);
            }
            else
            {
                result = await this._dbSet.MaxAsync(field);
            }
            return result;
        }

        public DateTime GetMaxDateTime(Expression<Func<T, bool>> expression, Expression<Func<T, DateTime>> field)
        {
            DateTime result;
            if (expression != null)
            {
                result = this._dbSet.Where(expression).Max(field);
            }
            else
            {
                result = this._dbSet.Max(field);
            }
            return result;
        }

        public async Task<DateTime> GetMaxDateTimeAsync(Expression<Func<T, bool>> expression, Expression<Func<T, DateTime>> field)
        {
            DateTime result;
            if (expression != null)
            {
                result = await this._dbSet.Where(expression).MaxAsync(field);
            }
            else
            {
                result = await this._dbSet.MaxAsync(field);
            }
            return result;
        }

        public decimal GetMaxDecimal(Expression<Func<T, bool>> expression, Expression<Func<T, decimal>> field)
        {
            decimal result;
            if (expression != null)
            {
                result = this._dbSet.Where(expression).Max(field);
            }
            else
            {
                result = this._dbSet.Max(field);
            }
            return result;
        }
        public async Task<decimal> GetMaxDecimalAsync(Expression<Func<T, bool>> expression, Expression<Func<T, decimal>> field)
        {
            decimal result;
            if (expression != null)
            {
                result = await this._dbSet.Where(expression).MaxAsync(field);
            }
            else
            {
                result = await this._dbSet.MaxAsync(field);
            }
            return result;
        }

        public IQueryable<T> GetByCondition(Expression<Func<T, bool>> expression)
        {
            IQueryable<T> query = this._dbSet;
            return query.Where(expression);
        }

        public IQueryable<T> Query(Expression<Func<T, bool>> expression = null, Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null
            , Func<IQueryable<T>, IOrderedQueryable<T>> order = null)
        {
            IQueryable<T> query = this._dbSet;
            if (include != null)
            {
                query = include(query);
            }
            if (expression != null)
            {
                query = query.Where(expression);
            }
            if (order != null)
            {
                query = order(query);
            }


            return query;
        }

        public PagedList<T> Paging(IQueryable<T> query, Expression<Func<T, bool>> expression, Func<IQueryable<T>, IOrderedQueryable<T>> order, int page, int size)
        {
            if (page <= 0)
                throw new Exception("Invalid page number");
            if (page <= 0)
                throw new Exception("Invalid page size");

            if (expression != null)
            {
                query = query.Where(expression);
            }
            if (order != null)
            {
                query = order(query);
            }

            int totalCount = query.Count();
            var source = query.Skip((page - 1) * size).Take(size).ToList();

            return new PagedList<T>(
                source: source,
                totalCount: totalCount,
                pageNumber: page,
                pageSize: size);
        }

        public async Task<PagedList<T>> PagingAsync(IQueryable<T> query, Expression<Func<T, bool>> expression, Func<IQueryable<T>, IOrderedQueryable<T>> order, int page, int size)
        {
            if (page <= 0)
                throw new Exception("Invalid page number");
            if (page <= 0)
                throw new Exception("Invalid page size");

            if (expression != null)
            {
                query = query.Where(expression);
            }
            if (order != null)
            {
                query = order(query);
            }

            int totalCount = await query.CountAsync();
            var source = await query.Skip((page - 1) * size).Take(size).ToListAsync();

            return new PagedList<T>(
                source: source,
                totalCount: totalCount,
                pageNumber: page,
                pageSize: size);
        }

        public IQueryable<T> GroupBy(Expression<Func<T, bool>> expression = null,
            Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> order = null)
        {
            IQueryable<T> query = this._dbSet;
            if (include != null)
            {
                query = include(query);
            }
            if (expression != null)
            {
                query = query.Where(expression);
            }
            if (order != null)
            {
                query = order(query);
            }

            return query;
        }

        private I ReadEntity<I>(List<T> entities)
        {
            I obj = (I)Activator.CreateInstance(typeof(I));
            var parentProperties = obj.GetType().GetProperties();

            foreach (var prop in parentProperties)
            {
                try
                {
                    //var data = entities.Where(r => r.Type == prop.Name).FirstOrDefault();
                    //if (data != null)
                    //{
                    //    var type = System.Type.GetType(data.DataType);
                    //    var converter = System.ComponentModel.TypeDescriptor.GetConverter(type);
                    //    var result = converter.ConvertFrom(data.Value);

                    //    prop.SetValue(obj, result);
                    //}
                    //else
                    //{
                    //    prop.SetValue(obj, null);
                    //}
                }
                catch
                {

                }
            }
            return obj;
        }

        public IQueryable<T> FromSqlRaw(string query, SqlParameter[] Parameters = null)
        {
            if (Parameters == null)
            {
                SqlParameter[] sqlParameters = { };
                return _dbSet.FromSqlRaw<T>(query, sqlParameters);
            }

            return _dbSet.FromSqlRaw<T>(query, Parameters);
        }

        public IEnumerable<T> FromSqlRaw(string query, SqlParameter[] Parameters = null, CommandType type = CommandType.Text)
        {
            List<T> lstReturn = new List<T>();
            using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                command.CommandType = type;
                _dbContext.Database.OpenConnection();
                if (Parameters != null && type == CommandType.StoredProcedure)
                {
                    command.Parameters.Clear();
                    foreach (var para in Parameters)
                    {
                        command.Parameters.Add(para);
                    }
                }
                var reader = command.ExecuteReader();

                DataTable dataTable = new DataTable();
                dataTable.Load(reader);
                foreach (System.Data.DataRow row in dataTable.Rows)
                {
                    lstReturn.Add(GetObject<T>(row));
                }

                _dbContext.Database.CloseConnection();
                return lstReturn;
            }
        }

        public async Task<IEnumerable<T>> FromSqlRawAsync(string query, SqlParameter[] Parameters = null, CommandType type = CommandType.Text)
        {
            List<T> lstReturn = new List<T>();
            using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                command.CommandType = type;
                _dbContext.Database.OpenConnection();
                if (Parameters != null && type == CommandType.StoredProcedure)
                {
                    command.Parameters.Clear();
                    foreach (var para in Parameters)
                    {
                        command.Parameters.Add(para);
                    }
                }
                var reader = await command.ExecuteReaderAsync();

                DataTable dataTable = new DataTable();
                dataTable.Load(reader);
                foreach (System.Data.DataRow row in dataTable.Rows)
                {
                    lstReturn.Add(GetObject<T>(row));
                }

                _dbContext.Database.CloseConnection();
                return lstReturn;
            }
        }

        public PagedList<T> GetPageIndex(string table, string select, int pageIndex, int pageSize, string orderCol, string sortDir, string groupBy, string search, string conut_field = "1")
        {
            try
            {
                if (pageIndex <= -1)
                    throw new Exception("invalid page.");
                else if (pageSize <= -1)
                    throw new Exception("invalid size.");
                else if (string.IsNullOrEmpty(table))
                    throw new Exception("invalid table name.");
                else if (string.IsNullOrEmpty(select))
                    throw new Exception("invalid select fields.");
                else if (string.IsNullOrEmpty(orderCol))
                    throw new Exception("invalid order.");
                else if (string.IsNullOrEmpty(sortDir))
                    sortDir = "desc";
                else if (string.IsNullOrEmpty(search))
                    search = "1=1";

                SqlParameter[] sqlParameters =
                {
                    new SqlParameter { ParameterName = "from_table", DbType = DbType.String, Direction = ParameterDirection.Input, Size = 1000, Value = table },
                    new SqlParameter { ParameterName = "select_field", DbType = DbType.String, Direction = ParameterDirection.Input, Size = 1000, Value = select },
                    new SqlParameter { ParameterName = "search_query", DbType = DbType.String, Direction = ParameterDirection.Input, Size = 1000, Value = search },
                    new SqlParameter { ParameterName = "order_column", DbType = DbType.String, Direction = ParameterDirection.Input, Size = 200, Value = orderCol },
                    new SqlParameter { ParameterName = "order_direction", DbType = DbType.String, Direction = ParameterDirection.Input, Size = 10, Value = sortDir },
                    new SqlParameter { ParameterName = "group_by", DbType = DbType.String, Direction = ParameterDirection.Input, Size = 1000, Value = groupBy },
                    new SqlParameter { ParameterName = "skip_row", DbType = DbType.Int32, Direction = ParameterDirection.Input, Value = pageIndex },
                    new SqlParameter { ParameterName = "take_row", DbType = DbType.Int32, Direction = ParameterDirection.Input, Value = pageSize },
                    new SqlParameter { ParameterName = "count_field", DbType = DbType.String, Direction = ParameterDirection.Input, Size = 100, Value = conut_field },
                    new SqlParameter { ParameterName = "total_count", DbType = DbType.Int32, Direction = ParameterDirection.Output, Value = 0}
                };
                var _source = this.FromSqlRaw("sp_GetPagingData", sqlParameters, CommandType.StoredProcedure);
                var _count = Convert.ToInt32(sqlParameters[sqlParameters.Count() - 1].Value);

                return new PagedList<T>(_source, _count, pageIndex, pageSize);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public void Save()
        {
            this._dbContext.SaveChanges();
        }
        public async Task<int> SaveAsync()
        {
            var result = await this._dbContext.SaveChangesAsync();
            return result;
        }

        public void Dispose()
        {
            this._dbContext?.Dispose();
        }

        #region ### GetObject ###
        private I GetObject<I>(System.Data.DataRow dr)
        {
            I obj = (I)Activator.CreateInstance(typeof(I));
            var parentProperties = obj.GetType().GetProperties();
            foreach (var prop in parentProperties)
            {
                try
                {
                    var propertyInstance = prop.GetValue(obj, null);
                    if (propertyInstance != null && propertyInstance.ToString() != "0" && propertyInstance.GetType() != typeof(Guid)
                        && propertyInstance.GetType() != typeof(DateTime) && propertyInstance.GetType() != typeof(bool))
                    {
                        var mainObjectsProperties = prop.PropertyType.GetProperties();
                        foreach (var property in mainObjectsProperties)
                        {
                            try
                            {
                                object dbObject = dr[property.Name];
                                if (dbObject == DBNull.Value)
                                    property.SetValue(propertyInstance, null);
                                else
                                    property.SetValue(propertyInstance, dbObject);
                            }
                            catch { }
                        }
                    }
                    else
                    {
                        object dbObject = dr[prop.Name];
                        if (dbObject == DBNull.Value)
                            prop.SetValue(obj, null);
                        else
                            prop.SetValue(obj, dbObject);
                    }
                }
                catch (Exception ex)
                {

                }
            }
            return obj;
        }
        #endregion
    }
}
