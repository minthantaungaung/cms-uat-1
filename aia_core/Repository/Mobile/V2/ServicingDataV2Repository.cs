using aia_core.Entities;
using aia_core.Model.Mobile.Response;
using aia_core.Model.Mobile.Servicing.Data.Response;
using aia_core.Repository.Cms;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Repository.Mobile.V2
{
    public interface IServicingDataV2Repository
    {
        Task<ResponseModel<List<ServiceTypeResponse>>> GetServiceTypeList();

    }
    public class ServicingDataV2Repository : BaseRepository, IServicingDataV2Repository
    {
        private readonly IServicingDataRepository servicingDataRepository;
        public ServicingDataV2Repository(IHttpContextAccessor httpContext
            , IAzureStorageService azureStorage
            , IErrorCodeProvider errorCodeProvider
            , IUnitOfWork<Context> unitOfWork
            , IServicingDataRepository servicingDataRepository) 
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.servicingDataRepository = servicingDataRepository;
        }

        public async Task<ResponseModel<List<ServiceTypeResponse>>> GetServiceTypeList()
        {
            try
            {
                var serviceTypeList = await servicingDataRepository.GetServiceTypeList();

                Console.WriteLine($"V2 GetServiceTypeList => {serviceTypeList?.Code} {serviceTypeList.Data.Count}");
                if (serviceTypeList?.Code == 0 && serviceTypeList?.Data.Count > 0)
                {
                    
                    serviceTypeList.Data.ForEach(groupServiceType =>
                    {
                        groupServiceType.ServiceTypeList?.ForEach(serviceType =>
                        {

                            var isOtpRequired = unitOfWork.GetRepository<Claim_Service_Otp_Setup>()
                            .Query(x => x.FormName == serviceType.ServiceType.ToString() && x.FormType == "Service")
                            .Select(x => x.IsOtpRequired)
                               .FirstOrDefault();

                            serviceType.IsOtpRequired = isOtpRequired;

                            Console.WriteLine($"{serviceType.ServiceType} {isOtpRequired}");
                        });
                    });

                    
                }

                return errorCodeProvider.GetResponseModel(ErrorCode.E0, serviceTypeList.Data);
            }
            catch (Exception ex)
            {                
                return new ResponseModel<List<ServiceTypeResponse>>
                {
                    Code = 500,
                    Message = $"An error has occurred. Please try again. {ex.Message} {ex.StackTrace}",                    
                };
            }
        }
    }
}
