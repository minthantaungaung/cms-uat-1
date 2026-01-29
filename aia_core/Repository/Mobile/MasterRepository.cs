using aia_core.Model.Mobile.Response.Localization;
using aia_core.Model.Mobile.Response.Master;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using Newtonsoft.Json;
using aia_core.Entities;
using Microsoft.EntityFrameworkCore;

namespace aia_core.Repository.Mobile
{
    public interface IMasterRepository
    {
        ResponseModel<MasterResponse> GetMaster();
        ResponseModel<CountryResponse> GetCountry();
        ResponseModel<ProvinceResponse> GetProvince();
        ResponseModel<DistrictResponse> GetDistrict(string? code);
        ResponseModel<TownshipResponse> GetTownship(string? code);
    }
    public class MasterRepository : BaseRepository, IMasterRepository
    {
        public MasterRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {

        }

        public ResponseModel<MasterResponse> GetMaster()
        {
            try
            {
                var appConfig = unitOfWork.GetRepository<Entities.AppConfig>().GetAll().FirstOrDefault();
                var appVersion = unitOfWork.GetRepository<Entities.AppVersion>().GetAll().FirstOrDefault();

                Console.WriteLine($"GetMaster DB Check => {appConfig?.Maintenance_Title}");


                var appVersionResponse = new AppVersionResponse()
                {

                    AndroidMinVersion = appVersion?.MinimumAndroidVersion,
                    AndroidLatestVersion = appVersion?.LatestAndroidVersion,
                    IosMinVersion = appVersion?.MinimumIosVersion,
                    IosLatestVersion = appVersion?.LatestIosVersion,
                };

                var configResponse = new ConfigResponse()
                {
                    AiaContactInfo = new AiaContactInfo()
                    {
                        SHERContactNumber = appConfig?.SherContactNumber,
                        AiaCustomerCareEmail = appConfig?.AiaCustomerCareEmail,
                        AiaMyanmarWebsite = appConfig?.AiaMyanmarWebsite,
                        AiaMyanmarFacebook = appConfig?.AiaMyanmarFacebookUrl,
                        AiaMyanmarInstagram = appConfig?.AiaMyanmarInstagramUrl,
                        AiaCompanyAddressesAndBranches = appConfig?.AiaMyanmarAddresses,
                    },

                    ClaimAndServicing = new ClaimAndServicing()
                    {
                        ClaimTATHours = appConfig?.ClaimTatHours,
                        ServicingTATHours = appConfig?.ServicingTatHours,
                        ClaimArchiveFrequency = appConfig?.ClaimArchiveFrequency,
                        ServiceArchiveFrequency = appConfig?.ServicingArchiveFrequency,
                        ImageIndividualFileSizeLimit = appConfig?.ImagingIndividualFileSizeLimit,
                        ImageTotalSizeLimit = appConfig?.ImagingTotalFileSizeLimit,
                        ClaimEmail = appConfig?.ClaimEmail,
                        ServicingEmail = appConfig?.ServicingEmail,
                    },

                    OtherInfo = new OtherInfo()
                    {
                        Vitamin_Supply_Note = appConfig?.Vitamin_Supply_Note,
                        Doc_Upload_Note = appConfig?.Doc_Upload_Note,
                        Bank_Info_Upload_Note = appConfig?.Bank_Info_Upload_Note,
                    }
                };


                var isExistedFaqTopic = unitOfWork.GetRepository<Entities.FaqTopic>()
                .Query(x => x.IsDeleted == false && x.IsActive == true)
                .Any();

                return errorCodeProvider.GetResponseModel(ErrorCode.E0, new MasterResponse()
                {
                    AppVersionResponse = appVersionResponse,
                    ConfigResponse = configResponse,
                    IsShowGetHelpsButton = isExistedFaqTopic,
                });
            }
            catch(Exception ex)
            {
                Console.WriteLine($"GetMaster Ex => {ErrorCode.E500} {ex.Message} {JsonConvert.SerializeObject(ex)}");


                return errorCodeProvider.GetResponseModel<MasterResponse>(ErrorCode.E500);
            }
            
        }

        public ResponseModel<CountryResponse> GetCountry()
        {
            try
            {
                List<Entities.Country> list = unitOfWork.GetRepository<Entities.Country>().GetAll().ToList();
                return errorCodeProvider.GetResponseModel<CountryResponse>(ErrorCode.E0, new CountryResponse(){list =list});
            }
            catch(Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<CountryResponse>(ErrorCode.E500);
            }
        }

        public ResponseModel<ProvinceResponse> GetProvince()
        {
            try
            {
                List<Entities.Province> list = unitOfWork.GetRepository<Entities.Province>().GetAll().ToList();
                return errorCodeProvider.GetResponseModel<ProvinceResponse>(ErrorCode.E0, new ProvinceResponse(){list = list});
            }
            catch(Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<ProvinceResponse>(ErrorCode.E500);
            }
        }

        public ResponseModel<DistrictResponse> GetDistrict(string? code)
        {
            try
            {
                List<Entities.District> list = new List<District>();
                if(String.IsNullOrEmpty(code))
                {
                    list = unitOfWork.GetRepository<Entities.District>().GetAll().ToList();
                }
                else
                {
                    list = unitOfWork.GetRepository<Entities.District>().Query(x=> x.province_code == code).ToList();
                }
                return errorCodeProvider.GetResponseModel<DistrictResponse>(ErrorCode.E0, new DistrictResponse(){list = list});
            }
            catch(Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<DistrictResponse>(ErrorCode.E500);
            }
        }

        public ResponseModel<TownshipResponse> GetTownship(string? code)
        {
            try
            {
                List<Entities.Township> list = new List<Township>();
                if(String.IsNullOrEmpty(code))
                {
                    list = unitOfWork.GetRepository<Entities.Township>().GetAll().ToList();
                }
                else
                {
                    list = unitOfWork.GetRepository<Entities.Township>().Query(x=>x.district_code == code).ToList();
                }
                return errorCodeProvider.GetResponseModel<TownshipResponse>(ErrorCode.E0, new TownshipResponse(){list = list});
            }
            catch(Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<TownshipResponse>(ErrorCode.E500);
            }
        }
    }
}
