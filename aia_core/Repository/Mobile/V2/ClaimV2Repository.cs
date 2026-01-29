using aia_core.Entities;
using aia_core.Model.Mobile.Request;
using aia_core.Model.Mobile.Response;
using aia_core.Repository.Cms;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Repository.Mobile.V2
{
    public interface IClaimV2Repository
    {
        Task<ResponseModel<List<InsuranceTypeResponse>>> GetInsuranceTypeList(string insuredId);
        Task<ResponseModel> ClaimNow(ClaimNowRequest model);
    }
    public class ClaimV2Repository : BaseRepository, IClaimV2Repository
    {
        private readonly IClaimRepository claimRepository;
        public ClaimV2Repository(IHttpContextAccessor httpContext, 
            IAzureStorageService azureStorage, 
            IErrorCodeProvider errorCodeProvider, 
            IUnitOfWork<Context> unitOfWork
            , IClaimRepository claimRepository) 
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.claimRepository = claimRepository;
        }

        public async Task<ResponseModel> ClaimNow(ClaimNowRequest model)
        {
            #region #IsOtpRequiredOrNot Validation
            var isOtpRequired =
                unitOfWork.GetRepository<Claim_Service_Otp_Setup>()
                       .Query(x => x.FormName == model.BenefitFormType.ToString() && x.FormType == "Claim")
                       .Select(x => x.IsOtpRequired)
                          .FirstOrDefault();
            #endregion

            if (!isOtpRequired)            
            {
                model.IsSkipOtpValidation = true;

                model.ClaimOtp = new ClaimOtp
                {
                    OtpCode = "",
                    ReferenceNo = ""
                };
            }           
            
            var response = await claimRepository.ClaimNowAsync(model);
            return errorCodeProvider.GetResponseModel(ErrorCode.E0);
        }

        public async Task<ResponseModel<List<InsuranceTypeResponse>>> GetInsuranceTypeList(string insuredId)
        {
            var claimList = await claimRepository.GetInsuranceTypeList(insuredId);

            if(claimList?.Code == 0 && claimList.Data.Count > 0)
            {
                claimList.Data.ForEach(groupClaimType =>
                {
                   groupClaimType.Benefits?.ForEach(claimType =>
                   {
                       
                       claimType.IsOtpRequired = 
                       unitOfWork.GetRepository<Claim_Service_Otp_Setup>()
                       .Query(x => x.FormName == claimType.BenefitFormType.ToString() && x.FormType == "Claim")
                       .Select(x => x.IsOtpRequired)
                          .FirstOrDefault();
                   });
                });

                return errorCodeProvider.GetResponseModel(ErrorCode.E0, claimList.Data);
            }

            return errorCodeProvider.GetResponseModel<List<InsuranceTypeResponse>>(ErrorCode.E500);
        }
    }
}
