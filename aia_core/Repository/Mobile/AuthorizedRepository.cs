using aia_core.Entities;
using aia_core.Model.Mobile.Request;
using aia_core.Services;
using aia_core.UnitOfWork;
using FastMember;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Transactions;
using Newtonsoft.Json;
using aia_core.Model.Mobile.Response;
using aia_core.Model.Mobile.Response.Notification;

namespace aia_core.Repository.Mobile
{
    public interface IAuthorizedRepository
    {
        ResponseModel<AuthorizationResult> CheckAuthorization();

        ResponseModel<AuthorizationResult> CheckAuthorizationByNrc(string nrc, string otp);
    }
    public class AuthorizedRepository : BaseRepository, IAuthorizedRepository
    {
        private readonly IConfiguration config;
        private readonly IOktaService oktaService;
        private readonly ICommonRepository commonRepository;
        private readonly INotificationService notificationService;
        private readonly ITemplateLoader templateLoader;
        
        public AuthorizedRepository(IConfiguration config, IOktaService oktaService, IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork, ICommonRepository commonRepository
            , INotificationService notificationService, ITemplateLoader templateLoader)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.config = config;
            this.oktaService = oktaService;
            this.commonRepository = commonRepository;
            this.notificationService = notificationService;
            this.templateLoader = templateLoader;
        }

        ResponseModel<AuthorizationResult> IAuthorizedRepository.CheckAuthorization()
        {
            try
            {
                var memberId = GetMemberIDFromToken();

                return errorCodeProvider.GetResponseModel<AuthorizationResult>(ErrorCode.E0, CheckAuthorization(memberId, null));
            }
            catch(Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<AuthorizationResult>(ErrorCode.E500);
            }
        }

        ResponseModel<AuthorizationResult> IAuthorizedRepository.CheckAuthorizationByNrc(string nrc, string otp)
        {
            try
            {
                if (ValidateTestEndpointsOtp(otp) == false)
                {
                    return errorCodeProvider.GetResponseModel<AuthorizationResult>(ErrorCode.E403);
                }

                var memberId = GetMemberIDFromToken();

                return errorCodeProvider.GetResponseModel<AuthorizationResult>(ErrorCode.E0, CheckAuthorization(null, nrc));
            }
            catch (Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<AuthorizationResult>(ErrorCode.E500);
            }
        }
    }
}
