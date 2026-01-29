using System.Reflection;
using aia_core.Entities;
using aia_core.Model;
using aia_core.Model.Mobile.Request;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace aia_core.Repository
{
    public interface IDevRepository
    {
        Task<ResponseModel<PagedList<ErrorLogCms>>> GetCMSErrorLogList(ErrorLogRequest model);
        Task<ResponseModel<PagedList<ErrorLogMobile>>> GetMobileErrorLogList(ErrorLogRequest model);
        ResponseModel<string> TestMobileError();
        ResponseModel<string> TestCmsError();
        void ErrorLog(
            string? LogMessage = null,
            string? ExceptionMessage = null,
            string? Exception = null,
            string? EndPoint = null,
            string? UserID = null);
        
        ResponseModel<string> UpdateServicingStatus();
    }
    public class DevRepository : BaseRepository, IDevRepository
    {
        private readonly IAzureStorageService azureStorageService;
        public DevRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
        }

        public async Task<ResponseModel<PagedList<ErrorLogCms>>> GetCMSErrorLogList(ErrorLogRequest model)
        {
            var memberResponses = new List<ErrorLogCms>();

            try
            {
                var query = unitOfWork.GetRepository<Entities.ErrorLogCms>().Query(); 

                if (!string.IsNullOrEmpty(model.path))
                {
                    query = query.Where(x => x.EndPoint.Contains(model.path));
                }

                if (!string.IsNullOrEmpty(model.search))
                {
                    query = query.Where(x => x.LogMessage.Contains(model.search) || x.ExceptionMessage.Contains(model.search) || x.Exception.Contains(model.search));

                }
                
                int totalCount = 0;
                totalCount = await query.CountAsync();

                var source = query
                    .OrderByDescending(x => x.LogDate)
                    .Skip(((int)model.Page - 1) * (int)model.Size).Take((int)model.Size)
                    .ToList();



                var data = new PagedList<ErrorLogCms>(
                source: source,
                totalCount: totalCount,
                pageNumber: (int)model.Page,
                pageSize: (int)model.Size);

                return errorCodeProvider.GetResponseModel<PagedList<ErrorLogCms>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(ex.StackTrace, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<PagedList<ErrorLogCms>>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<PagedList<ErrorLogMobile>>> GetMobileErrorLogList(ErrorLogRequest model)
        {
            var memberResponses = new List<ErrorLogMobile>();

            try
            {
                var query = unitOfWork.GetRepository<Entities.ErrorLogMobile>().Query(); 

                if (!string.IsNullOrEmpty(model.path))
                {
                    query = query.Where(x => x.EndPoint.Contains(model.path));
                }

                if (!string.IsNullOrEmpty(model.search))
                {
                    query = query.Where(x => x.LogMessage.Contains(model.search) || x.ExceptionMessage.Contains(model.search) || x.Exception.Contains(model.search));

                }
                
                int totalCount = 0;
                totalCount = await query.CountAsync();

                var source = query
                    .OrderByDescending(x => x.LogDate)
                    .Skip(((int)model.Page - 1) * (int)model.Size).Take((int)model.Size)
                    .ToList();



                var data = new PagedList<ErrorLogMobile>(
                source: source,
                totalCount: totalCount,
                pageNumber: (int)model.Page,
                pageSize: (int)model.Size);

                return errorCodeProvider.GetResponseModel<PagedList<ErrorLogMobile>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(ex.StackTrace, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<PagedList<ErrorLogMobile>>(ErrorCode.E500);
            }
        }

        public ResponseModel<string> TestMobileError()
        {
            try
            {
                int i = 0;
                int result = 5/i;
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0, "Ok");
            }
            catch (System.Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500, "Ok");
            }
        }

        public ResponseModel<string> TestCmsError()
        {
            try
            {
                int i = 0;
                int result = 5/i;
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0, "Ok");
            }
            catch (System.Exception ex)
            {
                CmsErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500, "Ok");
            }
        }

        public void ErrorLog(
            string? LogMessage = null,
            string? ExceptionMessage = null,
            string? Exception = null,
            string? EndPoint = null,
            string? UserID = null)
            {
                CmsErrorLog(LogMessage,ExceptionMessage,Exception,EndPoint,UserID);
            }

        public ResponseModel<string> UpdateServicingStatus()
        {
            try
            {
                List<ServicingRequest> list = unitOfWork.GetRepository<ServicingRequest>().Query(x=> x.Status == EnumServiceStatus.Received.ToString()).ToList();
                foreach (var item in list)
                {
                    item.Status = EnumServiceStatus.Approved.ToString();
                }

                List<ServicePaymentFrequency> pflist = unitOfWork.GetRepository<ServicePaymentFrequency>().Query(x=> x.Status == EnumServiceStatus.Received.ToString()).ToList();
                foreach (var item in pflist)
                {
                    item.Status = EnumServiceStatus.Approved.ToString();
                }

                unitOfWork.SaveChanges();
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0,  "Ok"); 
            }
            catch (System.Exception ex)
            {
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500, "Not Ok");
            }
        }
    }
}
