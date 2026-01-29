using aia_core.Entities;
using aia_core.Services;
using aia_core.UnitOfWork;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using Newtonsoft.Json;

namespace aia_core.Repository.Cms
{
    public class LocalizationDto
    {
        public string? Key { get; set; }
        public string? English { get; set; }
        public string? Burmese { get; set; }
    }
    public interface ILocalizationRepository
    {
        Task<ResponseModel<object>> Upload(IFormFile csvFile);

        Task<List<LocalizationDto>> Download();
    }

    public class LocalizationRepository : BaseRepository, ILocalizationRepository
    {
        private IWebHostEnvironment Environment;
        private IConfiguration Configuration;

        public LocalizationRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider, IUnitOfWork<Context> unitOfWork
            , IWebHostEnvironment _environment, IConfiguration _configuration)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            Environment = _environment;
            Configuration = _configuration;
        }

        async Task<List<LocalizationDto>> ILocalizationRepository.Download()
        {
            var response = new List<LocalizationDto>();
            var localizations = await unitOfWork.GetRepository<Entities.Localization>().Query(x => x.IsDeleted == false).ToListAsync();

            if (localizations.Any())
            {
                localizations.ForEach(locale =>
                    response.Add(new LocalizationDto()
                    {
                        Key = locale.Key,
                        English = locale.English,
                        Burmese = locale.Burmese
                    }
                    )
                );
            }

            await CmsAuditLog(
                objectGroup: EnumObjectGroup.Localizations,
                objectAction: EnumObjectAction.Download);
            return response;

        }

        async Task<ResponseModel<object>> ILocalizationRepository.Upload(IFormFile file)
        {
            try
            {

                DataSet dataSet = new DataSet();

                using (MemoryStream stream1 = new MemoryStream())
                {
                    file.CopyTo(stream1);

                    var workbook = new XLWorkbook(stream1);
                    var workSheet = workbook.Worksheet(1);

                    DataTable dt = new DataTable(workSheet.Name);

                    workSheet.FirstRowUsed().CellsUsed().ToList()
                    .ForEach(x => { dt.Columns.Add(x.Value.ToString()); });

                    foreach (IXLRow row in workSheet.RowsUsed().Skip(1))
                    {
                        DataRow dr = dt.NewRow();
                        for (int i = 0; i < dt.Columns.Count; i++)
                        {
                            dr[i] = row.Cell(i + 1).Value.ToString();
                        }
                        dt.Rows.Add(dr);
                    }
                    
                    dataSet.Tables.Add(dt);

                    if (dataSet != null && dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0)
                    {
                        var localizations = unitOfWork.GetRepository<Entities.Localization>().Query(x => x.IsDeleted == false).ToList();

                        if (!localizations.IsNullOrEmpty())
                        {
                            localizations.ForEach(localization =>
                            {
                                localization.IsDeleted = true;
                            });


                            unitOfWork.SaveChanges();
                        }

                        var conString = Configuration["Database:connectionString"];

                        using (SqlConnection con = new SqlConnection(conString))
                        {
                            using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(con))
                            {
                                sqlBulkCopy.DestinationTableName = "dbo.Localization";
                                sqlBulkCopy.ColumnMappings.Add("Key", "Key");
                                sqlBulkCopy.ColumnMappings.Add("English", "English");
                                sqlBulkCopy.ColumnMappings.Add("Burmese", "Burmese");

                                con.Open();
                                sqlBulkCopy.WriteToServer(dataSet.Tables[0]);
                                con.Close();
                            }
                        }
                    }
                }                

                
                

                await CmsAuditLog(
                objectGroup: EnumObjectGroup.Localizations,
                objectAction: EnumObjectAction.Update);
                return   errorCodeProvider.GetResponseModel<object>(ErrorCode.E0);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);


                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E500, ex.Message);
            }
        }
    }
}
