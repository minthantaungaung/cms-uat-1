using aia_core.RecurringJobs;
using aia_core.Services;
using aia_core.UnitOfWork;
using Azure;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Repository.Mobile
{
    public interface IDevTestRepository
    {
        ResponseModel<List<OktaFactorResponse>> GetListEnrollFactors(Guid memberGuid, string otp);
        ResponseModel UpdateClaimStatus(string otp);
    }

    public class DevTestRepository : BaseRepository, IDevTestRepository
    {
        #region "const"
        private readonly IOktaService oktaService;
        

        public DevTestRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork,
            IOktaService oktaService )
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
           this.oktaService = oktaService;
            
        }

        ResponseModel<List<OktaFactorResponse>> IDevTestRepository.GetListEnrollFactors(Guid memberGuid, string otp)
        {
            try
            {

                


                if (ValidateTestEndpointsOtp(otp) == false)
                {
                    return errorCodeProvider.GetResponseModel<List<OktaFactorResponse>>(ErrorCode.E403);
                }

                var profile = unitOfWork.GetRepository<Entities.Member>()
                    .Query(x => x.MemberId == memberGuid)
                    .FirstOrDefault();

                if(profile == null)
                {
                    return errorCodeProvider.GetResponseModel<List<OktaFactorResponse>>(ErrorCode.E400);
                }

                var oktaFactors = oktaService.ListEnrollFactors(profile.Auth0Userid).Result;
                if (oktaFactors?.Code == (long)ErrorCode.E0)
                {
                    return oktaFactors;
                }

                
            }
            catch (Exception ex)
            {

                Console.WriteLine($"GetListEnrollFactors > {ex.Message} {JsonConvert.SerializeObject(ex)}");
                
            }

            return errorCodeProvider.GetResponseModel<List<OktaFactorResponse>>(ErrorCode.E500);
        }

        ResponseModel IDevTestRepository.UpdateClaimStatus(string otp)
        {
            try
            {
                if (ValidateTestEndpointsOtp(otp) == false)
                {
                    return errorCodeProvider.GetResponseModel(ErrorCode.E403);
                }

                


                return errorCodeProvider.GetResponseModel(ErrorCode.E0);


            }
            catch (Exception ex)
            {

                Console.WriteLine($"UpdateClaimStatus > {ex.Message} {JsonConvert.SerializeObject(ex)}");

            }

            return errorCodeProvider.GetResponseModel(ErrorCode.E500);
        }



        #endregion
    }

}
