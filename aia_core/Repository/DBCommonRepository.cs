using System.Data;
using System.Reflection;
using aia_core.Entities;
using aia_core.Model;
using aia_core.Model.Mobile.Request;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace aia_core.Repository
{
    public interface IDBCommonRepository
    {
        List<T> GetListBySP<T>(string spname, Dictionary<string, object> parameters) where T : class;
    }
    public class DBCommonRepository : BaseRepository, IDBCommonRepository
    {
        private readonly IAzureStorageService azureStorageService;
        private readonly IConfiguration _config;
        public DBCommonRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork, IConfiguration _config)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this._config = _config;
        }

        public List<T> GetListBySP<T>(string spname, Dictionary<string, object> parameters) where T : class
        {
            List<T> list = new List<T>();

            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(_config["Database:connectionString"]))
            {
                try
                {
                    connection.Open();

                    using (SqlCommand sqlCommand = new SqlCommand(spname, connection))
                    {
                        sqlCommand.CommandType = CommandType.StoredProcedure;

                        // Add parameters to the stored procedure using a Dictionary
                        foreach (var param in parameters)
                        {
                            sqlCommand.Parameters.AddWithValue(param.Key, param.Value);
                        }

                        using (SqlDataAdapter adp = new SqlDataAdapter(sqlCommand))
                        {
                            adp.Fill(dataTable);
                        }
                    }
                }
                catch (Exception err)
                {
                    return null;
                }
                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                        connection.Close();
                }
            }

            foreach (DataRow dr in dataTable.Rows)
            {
                list.Add(GetObject<T>(dr));
            }
            return list;
        }
        private T GetObject<T>(DataRow dr)
        {

            T obj = (T)System.Activator.CreateInstance(typeof(T));
            PropertyInfo[] propertyInfos = obj.GetType().GetProperties();
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                if (dr.Table.Columns.Contains(propertyInfo.Name))
                {
                    object dbObject = dr[propertyInfo.Name];
                    if (dbObject == DBNull.Value)
                        propertyInfo.SetValue(obj, null);
                    else
                        propertyInfo.SetValue(obj, dbObject);
                }
            }
            return obj;
        }
    }


}
