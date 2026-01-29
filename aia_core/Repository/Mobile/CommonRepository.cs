using aia_core.Model.Mobile.Request;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace aia_core.Repository.Mobile
{
    public interface ICommonRepository
    {
        Guid? GetMemberIDFromToken();
        EnumPropositionBenefit GetMemberType();
        string GetFileFullUrl(EnumFileType fileType, string fileName);

        Task<ResponseModel<object>> SendOtp(string refNumber, EnumOtpType otpType);
    }
    public class CommonRepository : BaseRepository, ICommonRepository
    {
        private readonly IAzureStorageService azureStorageService;
        public CommonRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork, IConfiguration config, IAzureStorageService azureStorageService)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.azureStorageService = azureStorageService;
            rootUrl = config["Azure:BlobStorage:baseUrl"] ?? throw new NullReferenceException("azure blob storage baseUrl");
        }

        private readonly string rootUrl = "";
        private readonly string containerName = "";        

        public EnumPropositionBenefit GetMemberType()
        {
            return EnumPropositionBenefit.both;
        }

        public async Task<ResponseModel<object>> SendOtp(string refNumber, EnumOtpType otpType)
        {
            try
            {
                var memberId = GetMemberIDFromToken();

                var referenceNumber = Utils.ReferenceNumber(refNumber);

                var member = unitOfWork.GetRepository<Entities.Member>().Query(
                        expression: r => (r.Email == referenceNumber || r.Mobile == referenceNumber) && r.MemberId == memberId)
                    .FirstOrDefault();

                var previousOtp = unitOfWork.GetRepository<Entities.CommonOtp>()
                    .Query(x => x.OtpTo == referenceNumber && x.MemberId == memberId)
                    .OrderByDescending(x => x.CreatedOn)
                    .FirstOrDefault();

                if (previousOtp != null && previousOtp.CreatedOn != null)
                {
                    var otpLimit = Convert.ToInt32(AppSettingsHelper.GetSetting("Otp:LimitSeconds"));
                    if (previousOtp.CreatedOn.Value.AddSeconds(otpLimit) >= Utils.GetDefaultDate())
                    {
                        
                        return new ResponseModel<object> { Code = 400, Message = "Please try again in 30 seconds." };
                    }
                }

                if (member != null)
                {
                    var Id = Guid.NewGuid();
                    var otp = new Entities.CommonOtp();

                    otp.Id = Guid.NewGuid();
                    otp.OtpType = otpType.ToString();
                    otp.OtpTo = referenceNumber;
                    otp.OtpCode = Utils.GenerateOTP();
                    otp.OtpExpiry = Utils.GetDefaultDate().AddMinutes(15);
                    otp.CreatedOn = Utils.GetDefaultDate();
                    otp.MemberId = memberId;

                    unitOfWork.GetRepository<Entities.CommonOtp>().Add(otp);

                    unitOfWork.SaveChanges();

                    SendOTPCode(referenceNumber, string.Format($"{AppSettingsHelper.GetSetting("SmsPoh:OtpMessage")}", otp.OtpCode)
                        , $"{AppSettingsHelper.GetSetting("SmsPoh:Key")}"
                        , member.Name, otp.OtpCode);

                    return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0, new { refNumber = referenceNumber });
                }

                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);

            }
            catch (Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E500);
            }
        }

        // string ICommonRepository.GetFileFullUrl(EnumFileType fileType, string fileName)
        // {

        //     if (fileType == EnumFileType.Blog)
        //     {
        //         return rootUrl + "/" + fileName;
        //     }
        //     else if (fileType == EnumFileType.Product)
        //     {
        //         return rootUrl + "/" + fileName;
        //     }
        //     else if (fileType == EnumFileType.Proposition)
        //     {
        //         return rootUrl + "/" + fileName;
        //     }
        //     else
        //     {
        //         return rootUrl + "/" + fileName;
        //     }
        // }
        //string ICommonRepository.GetFileFullUrl(EnumFileType fileType, string fileName)
        //{
        //    return azureStorageService.GetUrlFromPrivate(fileName).Result;
        //}
    }
}
