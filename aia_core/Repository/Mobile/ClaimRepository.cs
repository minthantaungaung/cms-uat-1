using aia_core.Entities;
using aia_core.Model.Mobile.Response;
using aia_core.Model.Mobile.Response.MemberPolicyResponse;
using aia_core.Services;
using aia_core.UnitOfWork;
using Azure;
using FastMember;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static Google.Apis.Requests.BatchRequest;
using Newtonsoft.Json;
using CsvHelper.Configuration;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.AspNetCore.Http.HttpResults;
using aia_core.Model.Mobile.Request;
using System.Security.Claims;
using System.Linq.Expressions;
using aia_core.Model.AiaCrm;
using DocumentFormat.OpenXml.Vml.Office;
using Google.Apis.Util;
using Microsoft.AspNetCore.Hosting;
using DinkToPdf;
using DinkToPdf.Contracts;
using DocumentFormat.OpenXml.Spreadsheet;
using aia_core.RecurringJobs;
using Microsoft.Extensions.DependencyInjection;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using aia_core.Repository.Cms;
using Microsoft.Extensions.Azure;
using DocumentFormat.OpenXml;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response;
using System.Data;
using Azure.Storage.Blobs.Models;
using FirebaseAdmin.Messaging;
using System.Xml;
using aia_core.Model.Cms.Response.MemberPolicyResponse;
using aia_core.Model.ClaimProcess;
using System.Runtime.ConstrainedExecution;

namespace aia_core.Repository.Mobile
{
    public interface IClaimRepository
    {
        Task<ResponseModel<List<InsuredPersonResponse>>> GetInsuredPersonList();
        Task<ResponseModel<List<InsuranceTypeResponse>>> GetInsuranceTypeList(string InsuredId);

        Task<ResponseModel<List<BenefitListResponse>>> GetBenefitList();
        Task<ResponseModel<string>> UploadDoc(IFormFile doc);
        Task<ResponseModel<string>> DeleteDoc(string name);
        Task<ResponseModel<List<SampleDocumentsResponse>>> GetClaimSampleDocuments(string benefitFormType);

        Task<ResponseModel<ClaimNowResponse>> ClaimNowAsync(ClaimNowRequest model);

        Task<ResponseModel<ValidationResult>> ValidateClaim(ClaimValidationRequest model);
        Task<ResponseModel<List<ClaimSettingResponse>>> GetSetting(string? name = null, string type = "", string? productCodes = "");

        Task<ResponseModel<GetSaveBankResponse>> GetSaveBankInfo();

        Task<ResponseModel<List<BenefitSummaryResponse>>> GetBenefitSummary(string InsuredId, string[] productCodes, EnumBenefitFormType formType
            , string? policyNo, Guid? criticalIllnessId);

        Task<ResponseModel<PagedList<ClaimListRsp>>> GetClaimList(ClaimStatusListRequest model);
        Task<ResponseModel<ClaimDetailRsp>> GetClaimDetail(Guid claimId);

        Task<ResponseModel<string>> FollowupClaim(FollowupClaimRequest model);
      
        Task CommonClaimService(ClaimNowRequest model, Entities.Policy policy, Guid mainClaimId, Guid claimId
            , string reasonCode = null, string componentCode = null, List<Entities.InOutPatientReasonBenefitCode>? benefitList = null, Guid? memberId = null);


        Task CommonClaimServiceCaller(ClaimNowRequest model, Guid? memberId, List<Entities.Policy>? policies);

        Task<ResponseModel<ValidationResult>> ValidateDeathClaim(EnumBenefitFormType? claimType, string policyNo);


        #region #Test
        ResponseModel<ClaimContact> Get72HoursTest(DateTime dt);

        Task<ResponseModel<List<InsuranceTypeResponse>>> GetInsuranceTypeListTest(Guid? memberId, string InsuredId);

        #endregion

        Task<ResponseModel<UploadDocResponseModel>> ValidateMedicalBillDoc(IFormFile doc);
    }

    public class ClaimRepository : BaseRepository, IClaimRepository
    {
        private readonly ICommonRepository commonRepository;
        private readonly IAiaCmsApiService aiaCmsApiService;
        private readonly IAiaILApiService aiaILApiService;
        private readonly IAiaCrmApiService aiaCrmApiService;
        private readonly IHostingEnvironment environment;
        private readonly IConverter converter;
        private readonly INotificationService notificationService;
            private readonly IServiceProvider serviceProvider;
        private readonly ITemplateLoader templateLoader;
        private readonly ILogService logService;
        private readonly IAiaOcrApiService ocrApiService;



        public ClaimRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider, IUnitOfWork<Context> unitOfWork
            , ICommonRepository commonRepository, IAiaCmsApiService aiaCmsApiService, IAiaILApiService aiaILApiService, IAiaCrmApiService aiaCrmApiService
            , IHostingEnvironment environment, IConverter converter, INotificationService notificationService, IServiceProvider serviceProvider, ITemplateLoader templateLoader, ILogService logService, IAiaOcrApiService ocrApiService)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.commonRepository = commonRepository;
            this.aiaILApiService = aiaILApiService;
            this.aiaCmsApiService = aiaCmsApiService;
            this.aiaCrmApiService = aiaCrmApiService;
            this.environment = environment;
            this.converter = converter;
            this.notificationService = notificationService;
            this.serviceProvider = serviceProvider;
            this.templateLoader = templateLoader;
            this.logService = logService;
            this.ocrApiService = ocrApiService;
        }

        public async Task<ResponseModel<List<InsuredPersonResponse>>> GetInsuredPersonList()
        {
            try
            {
                var insuredPersonResponses = new List<InsuredPersonResponse>();

                var memberId = commonRepository.GetMemberIDFromToken();


                if (CheckAuthorization(memberId, null)?.Claim == false)
                    return new ResponseModel<List<InsuredPersonResponse>> 
                    { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };


                var clientIdenValue = unitOfWork.GetRepository<Entities.Member>()
                    .Query(x => x.MemberId == memberId && x.IsActive == true && x.IsVerified == true)
                    .Select(x => new { x.Nrc, x.Passport, x.Others })
                    .FirstOrDefault();

                if(clientIdenValue == null ) return errorCodeProvider.GetResponseModel<List<InsuredPersonResponse>>(ErrorCode.E400);

                Console.WriteLine($"GetInsuredPersonList => {clientIdenValue.Nrc} {clientIdenValue.Passport} {clientIdenValue.Others}");

                var clientNoList = unitOfWork.GetRepository<Entities.Client>()
                    .Query(x => (!string.IsNullOrEmpty(clientIdenValue.Nrc) && x.Nrc == clientIdenValue.Nrc)
                    || (!string.IsNullOrEmpty(clientIdenValue.Passport) && x.PassportNo == clientIdenValue.Passport)
                    || (!string.IsNullOrEmpty(clientIdenValue.Others) && x.Other == clientIdenValue.Others)
                    ).Select(x => x.ClientNo).ToList();

                

                
                if(clientNoList?.Any() == true)
                {
                    Console.WriteLine($"GetInsuredPersonList => holderClientNoList => {string.Join(",", clientNoList)}");

                    var insuredClientNoList = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => clientNoList.Contains(x.PolicyHolderClientNo)
                        && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                        .Select(x => x.InsuredPersonClientNo)
                        .Distinct()
                        .ToList();


                    if(insuredClientNoList?.Any() == true)
                    {
                        Console.WriteLine($"GetInsuredPersonList => insuredClientNoList => {string.Join(",", insuredClientNoList)}");

                        var insuredPersonList = unitOfWork.GetRepository<Entities.Client>()
                        .Query(x => insuredClientNoList.Contains(x.ClientNo))
                        .ToList();

                        insuredPersonList?.ForEach(insuredPerson =>
                        {
                            var IdentityPerson = insuredPerson;
                            var insuredPersonNrc = string.IsNullOrEmpty(IdentityPerson?.Nrc)
                                ? (string.IsNullOrEmpty(IdentityPerson?.PassportNo)
                                ? (IdentityPerson?.Other)
                                : IdentityPerson?.PassportNo)
                                : IdentityPerson?.Nrc;

                            Console.WriteLine($"GetInsuredPersonList => insuredPerson => {insuredPersonNrc} {insuredPerson.Name}");

                            insuredPersonResponses.Add(new InsuredPersonResponse()
                            {
                                InsuredId = insuredPerson.ClientNo,
                                InsuredName = insuredPerson.Name,
                                InsuredImage = "",
                                InsuredNrc = insuredPersonNrc,
                                InsuredNrcToLower = insuredPersonNrc?.ToLower(),
                            });
                        });

                        insuredPersonResponses = insuredPersonResponses
                    .DistinctBy(x => x.InsuredNrcToLower).ToList();

                    }
                    
                }
                
                

                return errorCodeProvider.GetResponseModel<List<InsuredPersonResponse>>(ErrorCode.E0, insuredPersonResponses);

            }
            catch(Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<InsuredPersonResponse>>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<List<InsuranceTypeResponse>>> GetInsuranceTypeList(string InsuredId)
        {
            try
            {
                var memberId = commonRepository.GetMemberIDFromToken();
                if (CheckAuthorization(memberId, null)?.Claim == false)
                    return new ResponseModel<List<InsuranceTypeResponse>> 
                    { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var insuranceMappingList = new List<InsuranceTypeResponse>();
                var insuranceBenefitList = new List<InsuranceTypeResponse>();
                var insuranceTypeList = new List<InsuranceTypeResponse>();

                var holderList = GetClientNoListByIdValue(memberId);

                try
                {
                    var itemList = "";
                    holderList?.ForEach(item =>
                    {
                        itemList += $"{item} \n";
                    });

                    Console.WriteLine($"TLS HolderClientNoList => {itemList}");

                }
                catch { }


                //var insuredNrc = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == InsuredId)
                //    .Select(x => new { x.Nrc, x.PassportNo, x.Other })
                //    .FirstOrDefault();

                List<string>? insuredClientNoList = new List<string>();
                //if (insuredNrc != null)
                //{

                //    insuredClientNoList = unitOfWork.GetRepository<Entities.Client>()
                //        .Query(x => (!string.IsNullOrEmpty(x.Nrc) && (x.Nrc == insuredNrc.Nrc))
                //        || (!string.IsNullOrEmpty(x.PassportNo) && x.PassportNo == insuredNrc.PassportNo)
                //        || (!string.IsNullOrEmpty(x.Other) && x.Other == insuredNrc.Other))
                //        .Select(x => x.ClientNo).ToList();
                //}


                insuredClientNoList = GetAllClientNoListByClientNo(InsuredId);

                var policies = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x =>
                    insuredClientNoList.Contains(x.InsuredPersonClientNo)
                    && holderList.Contains(x.PolicyHolderClientNo)
                    && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus)
                    )
                    .Select(x => new { x.ProductType, x.Components, x.PolicyNo, x.PolicyStatus })
                    .ToList();


                #region #Dummy
                //////var policies = new List<Entities.Policy>();
                //////var mer = new Entities.Policy()
                //////{
                //////    ProductType = "MER",
                //////    Components = "HL21,HL01,HL11,MER1",
                //////    PolicyNo = "H012119001",
                //////};
                //////var ohi = new Entities.Policy()
                //////{
                //////    ProductType = "OHI",
                //////    Components = "OHI1",
                //////    PolicyNo = "H012135909",
                //////};
                //////var ohg = new Entities.Policy()
                //////{
                //////    ProductType = "OHG",
                //////    Components = "DCB1,OHG1,OPB1",
                //////    PolicyNo = "H012127602",
                //////};

                //////policies.Add(mer);
                //////policies.Add(ohg);
                //////policies.Add(ohi);


                #endregion

                foreach (var policy in policies)
                {

                    //MobileErrorLog($"GetInsuranceTypeList memberId => {memberId}", $"Policy {policy.PolicyNo} ProductCode {policy.ProductType} Status {policy.PolicyStatus}"
                    //    , $"Components {policy.Components}"
                    //    , httpContext?.HttpContext.Request.Path);

                    var components = policy.Components?.Trim().Split(",");
                    var query = unitOfWork.GetRepository<Entities.InsuranceMapping>().Query();
                    
                    query = query.Where(BuildSearchExpression(components.ToArray()));
                    query = query.Where(x => x.ProductCode == policy.ProductType.Trim());

                    var insuranceMappings = query
                        .Include(x => x.Benefit).ThenInclude(x => x.InsuranceType)
                        .ToList();


                    #region #BenefitForm
                    try
                    {
                        insuranceMappings?.ForEach(mapp => 
                        {
                            Console.WriteLine($"TLS BenefitForm => {mapp.Benefit.BenefitFormType}");
                        });
                    }
                    catch 
                    { }
                    #endregion


                    foreach (var insuranceMapping in insuranceMappings)
                    {
                        //if (insuranceMapping.Benefit.BenefitNameEn != "Death/Accidental Death") // hide Death claim
                        //{
                            var insuranceMapp = new InsuranceTypeResponse();
                            insuranceMapp.InsuranceTypeId = insuranceMapping.Benefit.InsuranceType.InsuranceTypeId;
                            insuranceMapp.InsuranceTypeEn = insuranceMapping.Benefit.InsuranceType.InsuranceTypeEn;
                            insuranceMapp.InsuranceTypeMm = insuranceMapping.Benefit.InsuranceType.InsuranceTypeMm;
                            insuranceMapp.InsuranceTypeImage = insuranceMapping.Benefit.InsuranceType.InsuranceTypeImage;

                            insuranceMapp.BenefitId = insuranceMapping.ClaimId;
                            insuranceMapp.EligileBenefitNameListEn = insuranceMapping.Benefit.BenefitNameEn;
                            insuranceMapp.EligileBenefitNameListMm = insuranceMapping.Benefit.BenefitNameMm;
                            insuranceMapp.ClaimNameEn = insuranceMapping.Benefit.ClaimNameEn;
                            insuranceMapp.BenefitImage = insuranceMapping.Benefit.BenefitImage;
                            insuranceMapp.BenefitFormType = insuranceMapping.Benefit.BenefitFormType;
                            insuranceMapp.ProductCode = insuranceMapping.ProductCode;
                            insuranceMapp.PolicyNumber = policy.PolicyNo;
                            insuranceMapp.Components = insuranceMapping.ComponentCode;
                            insuranceMappingList.Add(insuranceMapp);
                        //}
                        
                    }
                }

                if (AppSettingsHelper.GetSetting("Deploy:Environment") == "stag")
                {
                    #region #Temp
                    insuranceMappingList.Clear();
                    var query = unitOfWork.GetRepository<Entities.InsuranceMapping>().Query();

                    var insuranceMappings = query
                        .Include(x => x.Benefit).ThenInclude(x => x.InsuranceType)
                        .ToList();


                    foreach (var insuranceMapping in insuranceMappings)
                    {
                        var insuranceMapp = new InsuranceTypeResponse();
                        insuranceMapp.InsuranceTypeId = insuranceMapping.Benefit.InsuranceType.InsuranceTypeId;
                        insuranceMapp.InsuranceTypeEn = insuranceMapping.Benefit.InsuranceType.InsuranceTypeEn;
                        insuranceMapp.InsuranceTypeMm = insuranceMapping.Benefit.InsuranceType.InsuranceTypeMm;
                        insuranceMapp.InsuranceTypeImage = insuranceMapping.Benefit.InsuranceType.InsuranceTypeImage;

                        insuranceMapp.BenefitId = insuranceMapping.ClaimId;
                        insuranceMapp.EligileBenefitNameListEn = insuranceMapping.Benefit.BenefitNameEn;
                        insuranceMapp.EligileBenefitNameListMm = insuranceMapping.Benefit.BenefitNameMm;
                        insuranceMapp.ClaimNameEn = insuranceMapping.Benefit.ClaimNameEn;
                        insuranceMapp.BenefitImage = insuranceMapping.Benefit.BenefitImage;
                        insuranceMapp.BenefitFormType = insuranceMapping.Benefit.BenefitFormType;
                        insuranceMapp.ProductCode = insuranceMapping.ProductCode;
                        //insuranceMapp.PolicyNumber = policy.PolicyNo;
                        insuranceMapp.Components = insuranceMapping.ComponentCode;
                        insuranceMappingList.Add(insuranceMapp);
                    }
                    #endregion
                }

                Console.WriteLine($"TLS insuranceMappingList => {JsonConvert.SerializeObject(insuranceMappingList)}");

                var listGrpByBenefit = insuranceMappingList.OrderBy(x => x.EligileBenefitNameListEn).GroupBy(x => x.BenefitFormType).ToList();

                foreach (var grpByBenefit in listGrpByBenefit)
                {
                    var insuranceBenefit = new InsuranceTypeResponse();
                    insuranceBenefit.InsuranceTypeId = grpByBenefit.First().InsuranceTypeId;
                    insuranceBenefit.InsuranceTypeEn = grpByBenefit.First().InsuranceTypeEn;
                    insuranceBenefit.InsuranceTypeMm = grpByBenefit.First().InsuranceTypeMm;
                    insuranceBenefit.InsuranceTypeImage = grpByBenefit.First().InsuranceTypeImage;

                    insuranceBenefit.BenefitId = grpByBenefit.First().BenefitId;
                    insuranceBenefit.EligileBenefitNameListEn = grpByBenefit.First().EligileBenefitNameListEn;
                    insuranceBenefit.EligileBenefitNameListMm = grpByBenefit.First().EligileBenefitNameListMm;
                    insuranceBenefit.BenefitImage = grpByBenefit.First().BenefitImage;
                    insuranceBenefit.BenefitFormType = grpByBenefit.First().BenefitFormType;
                    insuranceBenefit.ProductCode = string.Join(",", grpByBenefit.Select(x => x.ProductCode).ToArray());
                    insuranceBenefit.PolicyList = grpByBenefit.Select(x => x.PolicyNumber).ToArray();
                    insuranceBenefit.ClaimTypeList = grpByBenefit.Select(x => x.ClaimNameEn).ToArray();

                    insuranceBenefitList.Add(insuranceBenefit);
                }

                var listGrpByInsuranceType = insuranceBenefitList.OrderBy(x => x.InsuranceTypeEn).GroupBy(x => x.InsuranceTypeId).ToList();

                foreach (var grpByInsuranceType in listGrpByInsuranceType)
                {
                    var insuranceType = new InsuranceTypeResponse();
                    insuranceType.InsuranceTypeId = grpByInsuranceType.First().InsuranceTypeId;
                    insuranceType.InsuranceTypeEn = grpByInsuranceType.First().InsuranceTypeEn;
                    insuranceType.InsuranceTypeMm = grpByInsuranceType.First().InsuranceTypeMm;
                    insuranceType.InsuranceTypeImage = grpByInsuranceType.First().InsuranceTypeImage;                  
                   
                    insuranceType.EligileBenefitNameListEn = string.Join(", ", grpByInsuranceType.Select(x => x.EligileBenefitNameListEn).ToArray());
                    insuranceType.EligileBenefitNameListMm = string.Join("၊ ", grpByInsuranceType.Select(x => x.EligileBenefitNameListMm).ToArray());
                    insuranceType.BenefitFormType = grpByInsuranceType.First().BenefitFormType;

                    insuranceType.Benefits = grpByInsuranceType.Select(x => 
                    new BenefitResponse() { 
                        InsuredId = InsuredId,
                        BenefitId = x.BenefitId,
                        BenefitNameEn = x.EligileBenefitNameListEn,
                        BenefitNameMm = x.EligileBenefitNameListMm,
                        BenefitImage = x.BenefitImage,
                        ProductCode = grpByInsuranceType.Select(x => x.ProductCode).Distinct().ToArray(),
                        BenefitFormType = (EnumBenefitFormType?)Enum.Parse(typeof(EnumBenefitFormType), x.BenefitFormType) ,
                        
                })
                        .ToList();

                    insuranceTypeList.Add(insuranceType);
                }

                Console.WriteLine($"TLS insuranceTypeList => {JsonConvert.SerializeObject(insuranceTypeList)}");


                foreach (var item in insuranceTypeList)
                {
                    foreach (var benefit in item.Benefits)
                    {
                        var policyProdCodeList = unitOfWork.GetRepository<Entities.Policy>()
                                    .Query(x => insuredClientNoList.Contains(x.InsuredPersonClientNo) && holderList.Contains(x.PolicyHolderClientNo)
                                    && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                                    .Select(x => x.ProductType)
                                    .ToArray();

                        var prodCodeList = unitOfWork.GetRepository<Entities.InsuranceMapping>()
                            .Query(x => x.Benefit.BenefitFormType == benefit.BenefitFormType.ToString()
                            && policyProdCodeList.Contains(x.ProductCode))
                            .Select(x => x.ProductCode)
                            .ToArray();

                        benefit.ProductCode = prodCodeList.Distinct().ToArray();


                        try
                        {
                            // 
                            // ComponentCode
                            #region #Dummy
                            //////var policyComponents = policies
                            //////            .Select(x => x.Components)
                            //////            .ToArray();
                            #endregion

                            var policyComponents = unitOfWork.GetRepository<Entities.Policy>()
                                        //.Query(x => x.InsuredPersonClientNo == InsuredId
                                        .Query(x => insuredClientNoList.Contains(x.InsuredPersonClientNo) && holderList.Contains(x.PolicyHolderClientNo)
                                        && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                                        .Select(x => x.Components)
                                        .ToList();

                            // MER1,HL01,HL21,OHG1,OHI7,OPB2
                            // MER1,HL01,HL21,OHG1,OHI7,OPB2
                            // MER1,HL01,HL21,OHG1,OHI7,OPB2

                            var aggregateString = string.Join(",", policyComponents); //MER1,HL01,HL21,OHG1,OHI7,OPB2,MER1,HL01,HL21,OHG1,OHI7,OPB2,MER1,HL01,HL21,OHG1,OHI7,OPB2
                            var aggregateList = aggregateString.Split(','); //["","","" ,""] Duplicates
                            //aggregateList = aggregateList.Distinct().ToArray(); //["","","" ,""] Removed duplicates

                            var compoQuery = unitOfWork.GetRepository<Entities.InsuranceMapping>()
                                .Query(x => x.Benefit.BenefitFormType == benefit.BenefitFormType.ToString());

                            var permissionComponents = compoQuery.Where(BuildSearchExpression(aggregateList))
                                            .Select(x => x.ComponentCode)
                                            .ToList();

                            // MER1,HL01,HL21,OHG1,OHI7,OPB2
                            // MER1,HL01,HL21,OHG1,OHI7,OPB2
                            // MER1,HL01,HL21,OHG1,OHI7,OPB2

                            var permAggregateString = string.Join(",", permissionComponents);
                            var permAggregateList = permAggregateString.Split(',');
                            //permAggregateList = permAggregateList.Distinct().ToArray(); //["","","" ,""] Removed duplicates

                            var commonElements = permAggregateList.Intersect(aggregateList);

                            benefit.ComponentCodes = commonElements.Distinct().ToArray();

                            #region #08-02-2024
                            var filterProductCodeQuery = unitOfWork.GetRepository<Entities.InsuranceMapping>()
                                .Query(x => x.Benefit.BenefitFormType == benefit.BenefitFormType.ToString());

                            var filterProductCodeList = filterProductCodeQuery.Where(BuildSearchExpression(benefit.ComponentCodes))
                                            .Select(x => x.ProductCode)
                                            .ToList();

                            benefit.ProductCode = filterProductCodeList.Distinct().ToArray();

                            #endregion

                            try
                            {
                                MobileErrorLog("insuranceTypeList => Test", 
                                    $"benefit.BenefitFormType => {benefit.BenefitFormType.ToString()} aggregateString => {aggregateString}" +
                                    $" permAggregateString => {permAggregateString} commonElements => {JsonConvert.SerializeObject(commonElements)}" +
                                    $" prodCodeList => {JsonConvert.SerializeObject(prodCodeList)}"
                            , "", httpContext?.HttpContext.Request.Path);
                            }
                            catch { }
                        }
                        catch (Exception ex)
                        {
                            MobileErrorLog("insuranceTypeList => ComponentCode Ex", $""
                        , JsonConvert.SerializeObject(insuranceTypeList), httpContext?.HttpContext.Request.Path);
                        }
                        

                    }
                }

                insuranceTypeList = insuranceTypeList.OrderBy(x => x.InsuranceTypeEn).ToList();

                MobileErrorLog("insuranceTypeList", $"insuranceTypeListResponse"
                    , JsonConvert.SerializeObject(insuranceTypeList), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<InsuranceTypeResponse>>(ErrorCode.E0, insuranceTypeList);
            }
            catch (Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<InsuranceTypeResponse>>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<string>> UploadDoc(IFormFile doc)
        {
            try
            {
                var memberId = commonRepository.GetMemberIDFromToken();
                //if (CheckAuthorization(memberId, null)?.Claim == false)
                //    return new ResponseModel<string> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var docName = $"{Utils.GetDefaultDate().Ticks}-{doc.FileName}";
                var result = await azureStorage.UploadAsync(docName, doc);
                //entity.CoverImage = result.Code == 200 ? coverImageName : null;
                if(result.Code == 200)
                {
                    return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0,docName);
                }
                else
                {
                    return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500);
                }
            }
            catch (System.Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<string>> DeleteDoc(string name)
        {
            try
            {
                var memberId = commonRepository.GetMemberIDFromToken();
                //if (CheckAuthorization(memberId, null)?.Claim == false)
                //    return new ResponseModel<string> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var result = await azureStorage.DeleteAsync(name);
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0);
            }
            catch (System.Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<List<BenefitListResponse>>> GetBenefitList()
        {
            try
            {
                var memberId = commonRepository.GetMemberIDFromToken();
                if (CheckAuthorization(memberId, null)?.Claim == false)
                    return new ResponseModel<List<BenefitListResponse>> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var benefitList = unitOfWork.GetRepository<Entities.InsuranceBenefit>().Query().ToList();

                var benefitListResponse = new List<BenefitListResponse>();
                foreach (var benefit in benefitList)
                {
                    benefitListResponse.Add(
                        new BenefitListResponse()
                        {
                            BenefitId = benefit.BenefitId,
                            BenefitNameEn = benefit.BenefitNameEn,
                            BenefitNameMm = benefit.BenefitNameMm,
                            BenefitNameEnEnum = benefit.BenefitFormType,
                        }
                        );
                }

                return errorCodeProvider.GetResponseModel<List<BenefitListResponse>>(ErrorCode.E0, benefitListResponse);

            }
            catch (Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<BenefitListResponse>>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<List<SampleDocumentsResponse>>> GetClaimSampleDocuments(string benefitFormType)
        {
            try
            {
                var memberId = commonRepository.GetMemberIDFromToken();
                if (CheckAuthorization(memberId, null)?.Claim == false)
                    return new ResponseModel<List<SampleDocumentsResponse>> 
                    { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var claimDocuments = unitOfWork.GetRepository<Entities.ClaimDocumentMapping>()
                    .Query(x => x.BenefitFormType == benefitFormType).FirstOrDefault();

                if(claimDocuments == null) return errorCodeProvider.GetResponseModel<List<SampleDocumentsResponse>>(ErrorCode.E500);

                var documentList = new List<SampleDocumentsResponse>();

                var typeNameList = claimDocuments.TypeNameList.Split(",");
                foreach ( var typeName in typeNameList)
                {
                    var document = new SampleDocumentsResponse();

                    var docType = unitOfWork.GetRepository<Entities.ClaimDocType>().Query(x => x.Name == typeName).FirstOrDefault();
                    document.DocumentTypeID = docType?.Code;
                    document.DocumentTypeName = docType?.NameSample;
                    document.DocTypeNameMm = docType?.NameMmSample;

                    document.TypeNameEn = docType?.Name;
                    document.TypeNameMm = docType?.NameMm;

                    document.TypeNameSampleEn = docType?.NameSample;
                    document.TypeNameSampleMm = docType?.NameMmSample;

                    var documentUrlList = unitOfWork.GetRepository<Entities.InsuranceClaimDocument>()
                        .Query(x => x.DocTypeName == typeName.Trim() && x.IsActive == true && x.IsDeleted == false)
                        .ToList();
                    
                    document.DocumentList = documentUrlList.Select(x => GetFileFullUrl(EnumFileType.Product, x.DocumentUrl)).ToArray();

                    documentList.Add(document);
                }


                return errorCodeProvider.GetResponseModel<List<SampleDocumentsResponse>>(ErrorCode.E0, documentList);

            }
            catch (Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<SampleDocumentsResponse>>(ErrorCode.E500);
            }
        }

        
        private void InsertClaimTran(IUnitOfWork<Entities.Context> dbContext, ClaimReq model)
        {
            var entity = new Entities.ClaimTran()
            {

            };
            dbContext.GetRepository<Entities.ClaimTran>().Add(entity);
        }

        public List<T> CreateList<T>()
        {
            return new List<T>();
        }
        public async Task<ResponseModel<List<ClaimSettingResponse>>> GetSetting(string? name = null, string type = "", string? productCodes = "")
        {
            try
            {
                var memberId = commonRepository.GetMemberIDFromToken();
                //if (CheckAuthorization(memberId, null)?.Claim == false)
                //    return new ResponseModel<List<ClaimSettingResponse>> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                List<ClaimSettingResponse> data = new List<ClaimSettingResponse>();
                if (type.ToLower() == EnumClaimSetting.Hospital.ToString().ToLower())
                {
                    var list = unitOfWork.GetRepository<Entities.Hospital>().Query(
                        x => x.IsDelete == false && (String.IsNullOrEmpty(name) || x.Name.Contains(name))).ToList();

                    if (list != null)
                    {
                        var otherHospital = list.Where(x => x.Name.Contains("Other")).FirstOrDefault();

                        if (otherHospital != null)
                        {
                            list.RemoveAll(x => x.Name.Contains("Other"));
                            list.Add(otherHospital); // Send to the last
                        }

                        data.AddRange(list.Select(item => MapToClaimSettingResponse(item)));
                    }

                }
                else if (type.ToLower() == EnumClaimSetting.ClaimIncurredLocation.ToString().ToLower())
                {
                    var claimIncurredLocation = new List<Entities.ClaimIncurredLocation>();

                    List<string> topCountryList = new List<string>() { "Myanmar", "Thailand", "Singapore", "Taiwan", "Malaysia", "India" };
                    Dictionary<string, int> countryOrder = topCountryList.Select((country, index) => new { country, index })
                                                     .ToDictionary(x => x.country, x => x.index);

                    var localIncurredLocation = unitOfWork.GetRepository<Entities.ClaimIncurredLocation>().Query(
                        x => x.IsDelete == false && (String.IsNullOrEmpty(name) || x.Name.Contains(name))
                        && topCountryList.Contains(x.Name))
                        .ToList();



                    var list = unitOfWork.GetRepository<Entities.ClaimIncurredLocation>().Query(
                        x => x.IsDelete == false && (String.IsNullOrEmpty(name) || x.Name.Contains(name))
                        && topCountryList.Contains(x.Name) == false)
                        .OrderBy(x => x.Name)
                        .ToList();

                    if (localIncurredLocation != null)
                    {
                        // Order the list based on the predefined country order
                        var orderedIncurredLocation = localIncurredLocation.OrderBy(x => countryOrder[x.Name]).ToList();

                        claimIncurredLocation.AddRange(orderedIncurredLocation);
                    }

                    if (list?.Any() == true)
                    {
                        claimIncurredLocation.AddRange(list);
                    }


                    data.AddRange(claimIncurredLocation.Select(item => MapToClaimSettingResponse(item)));
                }
                else if (type.ToLower() == EnumClaimSetting.Diagnosis.ToString().ToLower())
                {
                    var list = unitOfWork.GetRepository<Entities.Diagnosis>().Query(
                        x => x.IsDelete == false && (String.IsNullOrEmpty(name) || x.Name.Contains(name))).ToList();
                    data.AddRange(list.Select(item => MapToClaimSettingResponse(item)));
                }
                else if (type.ToLower() == EnumClaimSetting.PartialDisability.ToString().ToLower())
                {

                    if (!string.IsNullOrEmpty(productCodes))
                    {
                        var productShortCodes = productCodes.Trim().Split(",");

                        if (productShortCodes?.Any() == true)
                        {

                            var productGuidList = unitOfWork.GetRepository<Entities.Product>()
                                .Query(x => productShortCodes.Contains(x.ProductTypeShort) && x.IsActive == true && x.IsDelete == false)
                                .Select(x => x.ProductId)
                                .ToList();

                            if (productGuidList?.Any() == true)
                            {
                                var disabilityGuidList = unitOfWork.GetRepository<Entities.PartialDisabilityProduct>()
                                    .Query(x => productGuidList.Contains(x.ProductId.Value))
                                    .Select(x => x.DisabiltiyId)
                                    .ToList();

                                var disabilityList = unitOfWork.GetRepository<Entities.PartialDisability>()
                                    .Query(x => disabilityGuidList.Contains(x.ID) && x.IsDelete == false && x.IsActive == true)
                                    .ToList();

                                if (disabilityList?.Any() == true)
                                {
                                    data.AddRange(disabilityList.Select(item => MapToClaimSettingResponse(item)));
                                }

                            }
                        }


                    }
                    else
                    {
                        var list = unitOfWork.GetRepository<Entities.PartialDisability>().Query(
                        x => x.IsDelete == false && (String.IsNullOrEmpty(name) || x.Name.Contains(name))).ToList();
                        data.AddRange(list.Select(item => MapToClaimSettingResponse(item)));
                    }

                }
                else if (type.ToLower() == EnumClaimSetting.PermanentDisability.ToString().ToLower())
                {
                    var list = unitOfWork.GetRepository<Entities.PermanentDisability>().Query(
                        x => x.IsDelete == false && (String.IsNullOrEmpty(name) || x.Name.Contains(name))).ToList();
                    data.AddRange(list.Select(item => MapToClaimSettingResponse(item)));
                }
                else if (type.ToLower() == EnumClaimSetting.CriticalIllness.ToString().ToLower())
                {



                    #region #CI Rider

                    if (!string.IsNullOrEmpty(productCodes))
                    {
                        var productShortCodes = productCodes.Trim().Split(",");

                        if (productShortCodes?.Any() == true)
                        {

                            var productGuidList = unitOfWork.GetRepository<Entities.Product>()
                                .Query(x => productShortCodes.Contains(x.ProductTypeShort) && x.IsActive == true && x.IsDelete == false)
                                .Select(x => x.ProductId)
                                .ToList();

                            if (productGuidList?.Any() == true)
                            {
                                var _CIGuidList = unitOfWork.GetRepository<Entities.CI_Product>()
                                    .Query(x => productGuidList.Contains(x.ProductId.Value))
                                    .Select(x => x.DisabiltiyId)
                                    .ToList();

                                var _CIList = unitOfWork.GetRepository<Entities.CriticalIllness>()
                                    .Query(x => _CIGuidList.Contains(x.ID) && x.IsDelete == false && x.IsActive == true)
                                    .ToList();

                                if (_CIList?.Any() == true)
                                {
                                    data.AddRange(_CIList.Select(item => MapToClaimSettingResponse(item)));
                                }

                            }
                        }


                    }
                    else
                    {


                        var list = unitOfWork.GetRepository<Entities.CriticalIllness>().Query(
                        x => x.IsDelete == false && (String.IsNullOrEmpty(name) || x.Name.Contains(name))).ToList();
                        data.AddRange(list.Select(item => MapToClaimSettingResponse(item)));
                    }

                    #endregion

                }
                else if (type.ToLower() == EnumClaimSetting.Death.ToString().ToLower())
                {
                    var list = unitOfWork.GetRepository<Entities.Death>().Query(
                        x => x.IsDelete == false && (String.IsNullOrEmpty(name) || x.Name.Contains(name))).ToList();
                    data.AddRange(list.Select(item => MapToClaimSettingResponse(item)));
                }
                else if (type.ToLower() == EnumClaimSetting.Relationship.ToString().ToLower())
                {
                    var list = unitOfWork.GetRepository<Entities.Relationship>().Query(
                        x => x.IsDelete == false && (String.IsNullOrEmpty(name) || x.Name.Contains(name))).ToList();
                    data.AddRange(list.Select(item => MapToClaimSettingResponse(item)));
                }

                return errorCodeProvider.GetResponseModel<List<ClaimSettingResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<List<ClaimSettingResponse>>(ErrorCode.E400);
            }
        }

        private ClaimSettingResponse MapToClaimSettingResponse(ClaimSetting item)
        {
            return new ClaimSettingResponse
            {
                Id = item.ID,
                Name = item.Name,
                NameMM = item.Name_MM,
                Code = item.Code
            };
        }

        public async Task<ResponseModel<GetSaveBankResponse>> GetSaveBankInfo()
        {
            try
            {
                var memberId = commonRepository.GetMemberIDFromToken();
                if (CheckAuthorization(memberId, null)?.Claim == false)
                    return new ResponseModel<GetSaveBankResponse> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var savedBank = unitOfWork.GetRepository<Entities.ClaimSaveBank>().Query(x => x.AppMemberId == memberId).FirstOrDefault();

                if (savedBank != null)
                {
                    var bankLogo = unitOfWork.GetRepository<Entities.Bank>().Query(x => x.BankCode == savedBank.BankCode)
                        .FirstOrDefault()?.BankLogo;

                    if (bankLogo != null)
                    {
                        bankLogo = GetFileFullUrl(EnumFileType.Bank, bankLogo);
                    }

                    return errorCodeProvider.GetResponseModel<GetSaveBankResponse>(ErrorCode.E0
                        , new GetSaveBankResponse
                        {
                            BankCode = savedBank.BankCode
                        ,
                            AccountNo = savedBank.AccountNumber
                        ,
                            AccountName = savedBank.AccountName
                        ,
                            BankLogo = bankLogo
                        }); ;
                }                

                return new ResponseModel<GetSaveBankResponse> { Code = 400, Message = "Not found save bank info." };

            }
            catch (Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<GetSaveBankResponse>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<List<BenefitSummaryResponse>>> GetBenefitSummary(string InsuredId, string[] productCodes, EnumBenefitFormType formType
            , string? policyNo, Guid? criticalIllnessId)
        {
            try
            {
                var respList = new List<BenefitSummaryResponse>();

                var memberId = commonRepository.GetMemberIDFromToken();
                if (CheckAuthorization(memberId, null)?.Claim == false)
                    return new ResponseModel<List<BenefitSummaryResponse>> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var holderList = GetClientNoListByIdValue(memberId);

                var policies = new List<Entities.Policy>();


                var insuredClientNoList = new List<string>();

                //var idValue = unitOfWork.GetRepository<Entities.Client>()
                //    .Query(x => x.ClientNo == InsuredId)
                //    .Select(x => new { x.Nrc, x.PassportNo, x.Other })
                //    .FirstOrDefault();

                //if (idValue != null)
                //{
                //    var nrc = !string.IsNullOrEmpty(idValue.Nrc) ? idValue.Nrc.ToLower() : "";
                //    var passport = !string.IsNullOrEmpty(idValue.PassportNo) ? idValue.PassportNo.ToLower() : "";
                //    var other = !string.IsNullOrEmpty(idValue.Other) ? idValue.Other.ToLower() : "";

                //    insuredClientNoList = unitOfWork.GetRepository<Entities.Client>()
                //           .Query(x => (!string.IsNullOrEmpty(nrc) && x.Nrc.ToLower() == nrc)
                //           || (!string.IsNullOrEmpty(passport) && x.PassportNo.ToLower() == passport)
                //           || (!string.IsNullOrEmpty(other) && x.Other.ToLower() == other))
                //           .Select(x => x.ClientNo).ToList();

                    
                //}


                insuredClientNoList = GetAllClientNoListByClientNo(InsuredId);

                if (formType == EnumBenefitFormType.DeathAndAccidentalDeath)
                {
                    policies = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => insuredClientNoList.Contains(x.InsuredPersonClientNo) && x.PolicyNo == policyNo
                    && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus)
                    )
                    .ToList();
                }
                else
                {
                    List<Entities.Policy>? policyList = null;
                    

                    if (formType == EnumBenefitFormType.CriticalIllnessBenefit && criticalIllnessId != null)
                    {
                        var ciProductIdList = unitOfWork.GetRepository<Entities.CI_Product>()
                        .Query(x => x.DisabiltiyId == criticalIllnessId)
                        .Select(x => x.ProductId)
                        .ToList(); //ProductId of OHI,ULI << MH << GENERALIZED TETANUS

                        if (ciProductIdList?.Any() == true)
                        {
                            var ciProductCodeList = unitOfWork.GetRepository<Entities.Product>()
                            .Query(x => ciProductIdList.Contains(x.ProductId) && x.IsActive == true && x.IsDelete == false)
                            .Select(x => x.ProductTypeShort)
                            .ToList(); //OHI,ULI

                            if (ciProductCodeList?.Any() == true)
                            {
                                policyList = unitOfWork.GetRepository<Entities.Policy>()
                                .Query(x => insuredClientNoList.Contains(x.InsuredPersonClientNo)
                                && holderList.Contains(x.PolicyHolderClientNo)
                                && ciProductCodeList.Contains(x.ProductType)
                                && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                                .ToList();
                            }

                        }
                    }
                    else
                    {
                        policyList = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => insuredClientNoList.Contains(x.InsuredPersonClientNo) && productCodes.Contains(x.ProductType)
                        && holderList.Contains(x.PolicyHolderClientNo)
                        && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                        .ToList();
                    }



                    policyList?.ForEach(policy =>
                    {
                        var policyComponents = policy.Components?.Trim().Split(",");

                        Console.WriteLine($"GetBenefitSummary => {formType} {policy.PolicyNo} {policy.ProductType} {policy.Components}");




                        var mappingList = unitOfWork.GetRepository<Entities.InsuranceMapping>()
                            .Query(x => x.ProductCode == policy.ProductType && x.Benefit.BenefitFormType == formType.ToString())
                            .Include(x => x.Benefit)
                            .ToList();

                        mappingList?.ForEach(mapping =>
                        {
                            Console.WriteLine($"GetBenefitSummary => InsuranceMapping => {formType} {mapping.ProductCode} {mapping.ComponentCode}");

                            string[] mappingComponents = mapping.ComponentCode.Split(",");
                            var isMatched = mappingComponents.Intersect(policyComponents).Any();

                            if (isMatched)
                            {
                                Console.WriteLine($"GetBenefitSummary => isMatched => {formType} {policy.PolicyNo} {isMatched}");
                                policies.Add(policy);
                            }
                        });




                    });
                }
                

                var claimFormType = unitOfWork.GetRepository<Entities.InsuranceBenefit>()
                .Query(x => x.BenefitFormType == formType.ToString())
                .Include(x => x.InsuranceType)
                .FirstOrDefault();

                policies?.ForEach(policy =>
                {
                    var insured = unitOfWork.GetRepository<Entities.Client>()
                                            .Query(x => x.ClientNo == policy.InsuredPersonClientNo)
                                            .FirstOrDefault();

                    var insuredNrc = string.IsNullOrEmpty(insured?.Nrc)
                        ? (string.IsNullOrEmpty(insured?.PassportNo)
                        ? (insured?.Other)
                        : insured?.PassportNo)
                        : insured?.Nrc;

                    var owner = unitOfWork.GetRepository<Entities.Client>()
                                    .Query(x => x.ClientNo == policy.PolicyHolderClientNo)
                                    .Select(x => x.Name)
                                    .FirstOrDefault();

                    var product = unitOfWork.GetRepository<Entities.Product>()
                                    .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false)
                                    .Select(x => new { x.TitleEn, x.TitleMm })
                                    .FirstOrDefault();



                    var response = new BenefitSummaryResponse
                    {
                        BenefitType = claimFormType?.BenefitNameEn,
                        BenefitTypeMm = claimFormType?.BenefitNameMm,
                        PolicyNumber = policy.PolicyNo,
                        InsurredId = policy.InsuredPersonClientNo,
                        InsuredName = insured?.Name,
                        InsurredNrc = insuredNrc,
                        OwnerName = owner,
                        SubmittedDate = Utils.GetDefaultDate(),
                        InsuranceType = product?.TitleEn,//claimFormType?.InsuranceType?.InsuranceTypeEn,
                        InsuranceTypeMm = product?.TitleMm,//claimFormType?.InsuranceType?.InsuranceTypeMm,
                    };

                    respList.Add(response);


                });


                    return errorCodeProvider.GetResponseModel<List<BenefitSummaryResponse>>(ErrorCode.E0, respList);

            }
            catch (Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<BenefitSummaryResponse>>(ErrorCode.E500);
            }
        }

        private Expression<Func<Entities.InOutPatientReasonBenefitCode, bool>> BuildSearchExpReasonAndBenefitCodeForInOutPatient(string[] values)
        {

            Expression<Func<Entities.InOutPatientReasonBenefitCode, bool>> searchExpression = entity => false;

            foreach (var value in values)
            {
                searchExpression = searchExpression.OrElse(entity => EF.Functions.Like(entity.ComponentCode, $"%{value}%"));
            }

            return searchExpression;
        }

        private Expression<Func<Entities.ReasonCode, bool>> BuildSearchExpReasonCode(string[] values)
        {

            Expression<Func<Entities.ReasonCode, bool>> searchExpression = entity => false;

            foreach (var value in values)
            {
                searchExpression = searchExpression.OrElse(entity => EF.Functions.Like(entity.ComponentCode, $"%{value}%"));
            }

            return searchExpression;
        }

        private Expression<Func<Entities.InsuranceMapping, bool>> BuildSearchExpression(string[] values)
        {
            //string[] values = searchString
            //    .Trim('[', ']')
            //    .Split(',')
            //    .Select(s => s.Trim())
            //    .ToArray();

            // Build the OR condition for each value
            Expression<Func<Entities.InsuranceMapping, bool>> searchExpression = entity => false; // Start with a condition that's always false
            foreach (var value in values)
            {
                searchExpression = searchExpression.OrElse(entity => EF.Functions.Like(entity.ComponentCode, $"%{value}%"));
            }

            return searchExpression;
        }

        private List<string> ClaimValidation(ClaimValidationRequest model, Entities.Policy policy)
        {
            
            var result = new List<string>();
            var claimDate = GetILCoastClaimDate();
            var todayDate = GetILCoastClaimDate();
            var policyEffectiveDate = policy.PolicyIssueDate;
            var policyCommencementDate = policy.RiskCommencementDate;

            if (model.BenefitFormType == EnumBenefitFormType.DeathAndAccidentalDeath)
            {


                DateTime? deathDate = null;
                DateTime? claimantDob = null;

                #region #RequiredCheck


                if (model.CausedBy != null && model.CausedBy.ByType != null && model.CausedBy.ByType == EnumCauseByType.Death && model.CausedBy.ByDate != null)
                {
                    deathDate = model.CausedBy.ByDate;
                }
                else
                {
                    result.Add("Death date required.");
                }

                if (model.ClaimantDetail != null && model.ClaimantDetail.Dob != null)
                {
                    claimantDob = model.ClaimantDetail.Dob;
                }
                else
                {
                    result.Add("Your Claimant date of birth required.");
                }

                if (policyCommencementDate == null)
                {
                    result.Add("Policy commencement date required.");
                }

               

                if (result.Any()) return result;
                #endregion


                #region #DateCheck
                if (deathDate > todayDate)
                {
                    result.Add("Death date must not be greater than today date.");
                }

                if (claimantDob > todayDate)
                {
                    result.Add("Your Claimant date of birth must not be greater than today date.");
                }

                if (claimantDob > deathDate)
                {
                    result.Add("Your Claimant date of birth must not be greater than death date.");
                }

                if (claimDate < policyCommencementDate)
                {
                    result.Add("Your claim date must not be less than policy commencement date.");
                }

                if (deathDate < policyCommencementDate)
                {
                    result.Add("Death date must not be less than policy commencement date.");
                }

                return result;
                #endregion
            }

            else if (model.BenefitFormType == EnumBenefitFormType.MaternityCare)
            {
                DateTime? fromDate = null;
                DateTime? toDate = null;

                #region #RequiredCheck
                if (model.TreatmentDetail != null && model.TreatmentDetail.TreatmentFromDate != null)
                {
                    fromDate = model.TreatmentDetail.TreatmentFromDate;
                }
                else
                {
                    result.Add("From date required.");
                }

                if (model.TreatmentDetail != null && model.TreatmentDetail.TreatmentToDate != null)
                {
                    toDate = model.TreatmentDetail.TreatmentToDate;
                }
                else
                {
                    result.Add("From date required.");
                }

                if (policyCommencementDate == null)
                {
                    result.Add("Policy commencement date required.");
                }

                if (policyEffectiveDate == null) //TODO
                {
                    result.Add("Policy effective date required.");
                }

                if (result.Any()) return result;
                #endregion


                #region #DateCheck
                if (claimDate < policyEffectiveDate)
                {
                    result.Add("Your claim date must not be less than policy effective date.");
                }

                if (fromDate > todayDate)
                {
                    result.Add("Your From date must not be greater than today date.");
                }

                if (fromDate > toDate)
                {
                    result.Add("Your From date must not be greater than To date.");
                }

                if (claimDate < fromDate)
                {
                    result.Add("Your claim date must not be less than your From date.");
                }

                if (toDate > todayDate)
                {
                    result.Add("Your To date must not be greater than today date.");
                }

                if (toDate < fromDate)
                {
                    result.Add("Your To date must not be less than From date.");
                }

                return result;
                #endregion
            }

            if (model.BenefitFormType == EnumBenefitFormType.DentalCare
                || model.BenefitFormType == EnumBenefitFormType.Vaccination
                || model.BenefitFormType == EnumBenefitFormType.VisionCare
                || model.BenefitFormType == EnumBenefitFormType.PhysicalCheckup
                )
            {
                List<DateTime>? treatmentDates = null;

                #region #RequiredCheck
                if (model.TreatmentDetail != null && model.TreatmentDetail.TreatmentDates != null)
                {
                    treatmentDates = model.TreatmentDetail.TreatmentDates;
                }
                else
                {
                    result.Add("Treatment date required.");
                }

                if (policyCommencementDate == null)
                {
                    result.Add("Policy commencement date required.");
                }

                if (policyEffectiveDate == null) //TODO
                {
                    result.Add("Policy effective date required.");
                }

                if (result.Any()) return result;
                #endregion


                #region #DateCheck

                treatmentDates = treatmentDates.Order().ToList();

                if (treatmentDates.Last() > todayDate)
                {
                    result.Add("Your treatment date must not be greater than today date.");
                }

                
                if (claimDate < treatmentDates.Last())
                {
                    result.Add("Your claim date must not be less than treatment date.");
                }

                if (treatmentDates.Last() > todayDate)
                {
                    result.Add("Your treatment date must not be greater than today date.");
                }

                return result;
                #endregion
            }

            else if (model.BenefitFormType == EnumBenefitFormType.OutpatientAndAmbulatoryCare)
            {
                var productCode = policy.ProductType;

                DateTime? treatmentDateMER = null;
                DateTime? treatmentDateOHI = null;
                DateTime? treatmentDateOHG = null;

                if (productCode == "MER")
                {
                    

                    if (model.TreatmentDetail != null && model.TreatmentDetail.TreatmentDates != null && model.TreatmentDetail.TreatmentDates.Any())
                    {
                        treatmentDateMER = model.TreatmentDetail.TreatmentDates.OrderDescending().First();

                        if (treatmentDateMER > todayDate)
                        {
                            result.Add("Your treatment date must not be greater than today date.");
                        }
                    }
                    else
                    {
                        result.Add("Treatment date required.");
                    }
                }
                else if (productCode == "OHI")
                {
                    if (model.TreatmentDetail != null && model.TreatmentDetail.TreatmentFromDate != null)
                    {
                        treatmentDateOHI = model.TreatmentDetail.TreatmentFromDate;

                        if (treatmentDateOHI > todayDate)
                        {
                            result.Add("Your From date must not be greater than today date.");
                        }
                    }
                    else
                    {
                        result.Add("Treatment date required.");
                    }
                }
                else if (productCode == "OHG")
                {
                    List<DateTime>? dateList = new List<DateTime>();
                    if (model.BenefitList != null && model.BenefitList.Any())
                    {
                        model.BenefitList.ForEach(benefit =>
                        {
                            var fromDate = benefit.FromDate != null ? benefit.FromDate.Value : Utils.GetDefaultDate();
                            var toDate = benefit.ToDate != null ? benefit.ToDate.Value : fromDate;

                            dateList.Add(fromDate);
                            dateList.Add(toDate);

                            if (toDate < fromDate)
                            {
                                result.Add("Your To date must not be less than From date.");
                            }
                        }
                        );

                        treatmentDateOHG = dateList.OrderDescending().First();

                        if (treatmentDateOHG > todayDate)
                        {
                            result.Add("Your To date must not be greater than today date.");
                        }
                    }
                    else
                    {
                        result.Add("Treatment date required.");
                    }
                }

                

                if (policyEffectiveDate == null) //TODO
                {
                    result.Add("Policy effective date required.");
                }

                if (result.Any()) return result;


                if (claimDate < policyEffectiveDate)
                {
                    result.Add("Your claim date must not be less than policy effective date.");
                }

                if ((treatmentDateMER != null && claimDate < treatmentDateMER)
                    || (treatmentDateOHI != null && claimDate < treatmentDateOHI) 
                    || (treatmentDateOHG != null && claimDate < treatmentDateOHG)
                    )
                {
                    result.Add("Your claim date must not be less than incurred date.");
                }

                if ((treatmentDateMER != null && treatmentDateMER < policyEffectiveDate)
                    || (treatmentDateOHI != null && treatmentDateOHI < policyEffectiveDate)
                    || (treatmentDateOHG != null && treatmentDateOHG < policyEffectiveDate)
                    )
                {
                    result.Add("Your incurred date must not be less than policy effective date.");
                }

                return result;
            }

            else if (model.BenefitFormType == EnumBenefitFormType.Inpatient)
            {
                var productCode = policy.ProductType;

                //TODO:

                if (productCode == "MER" || productCode == "OHI" || productCode == "OHG")
                {
                    DateTime? maxFromDate = null;
                    DateTime? maxToDate = null;

                    if (model.BenefitList != null && model.BenefitList.Any())
                    {
                        var fromDateList = new List<DateTime>();
                        var toDateList = new List<DateTime>();
                        model.BenefitList.ForEach(benefit =>
                        {
                            fromDateList.Add(benefit.FromDate.Value);
                            toDateList.Add(benefit.ToDate.Value);

                            if(benefit.FromDate > benefit.ToDate)
                            {
                                result.Add("Your From date must not be greater than To date.");
                            }
                        }
                        );

                        

                        maxFromDate = fromDateList.OrderDescending().First();
                        maxToDate = toDateList.OrderDescending().First();


                        if (maxFromDate != null && maxFromDate > todayDate)
                        {
                            result.Add("Your From date must not be greater than today date.");
                        }

                        if (maxToDate != null && maxToDate > todayDate)
                        {
                            result.Add("Your To date must not be greater than today date.");
                        }
                    }
                    else
                    {
                        result.Add("From date required.");
                    }

                    if (result.Any()) return result;
                }

                if (policyEffectiveDate != null)
                {
                    if (claimDate < policyEffectiveDate)
                    {
                        result.Add("Your claim date must not be less than policy effective date.");
                    }
                }
                else
                {
                    result.Add("Policy effective date required.");
                }

                if (policyCommencementDate != null)
                {

                }
                else
                {
                    result.Add("Policy commencement date required.");
                }

                return result;
            }

            else if (model.BenefitFormType == EnumBenefitFormType.PartialDisabilityAndInjury)
            {
                DateTime? disibilityDate = null;

                #region #RequiredCheck
                if (model.CausedBy != null && model.CausedBy.ByType != null && model.CausedBy.ByType == EnumCauseByType.PartialDisability && model.CausedBy.ByDate != null)
                {
                    disibilityDate = model.CausedBy.ByDate;
                }
                else
                {
                    result.Add("Disibility date required.");
                }

                if (policyCommencementDate == null)
                {
                    result.Add("Policy commencement date required.");
                }

                if (policyEffectiveDate == null) //TODO
                {
                    result.Add("Policy effective date required.");
                }

                if (result.Any()) return result;
                #endregion


                #region #DateCheck

                

                if (claimDate < policyEffectiveDate)
                {
                    result.Add("Your claim date must not be less than policy effective date.");
                }


                if (claimDate < disibilityDate)
                {
                    result.Add("Your claim date must not be less than disibility date.");
                }

                if (disibilityDate < policyEffectiveDate)
                {
                    result.Add("Your disibility date must not be less than policy effective date.");
                }

                if (disibilityDate > todayDate)
                {
                    result.Add("Your disability date must not be greater than today date.");
                }

                return result;
                #endregion
            }

            else if (model.BenefitFormType == EnumBenefitFormType.AcceleratedCancerBenefit)
            {
                DateTime? treatmentDate = null;

                #region #RequiredCheck
                if (model.TreatmentDetail != null && model.TreatmentDetail.TreatmentDates != null && model.TreatmentDetail.TreatmentDates.Any())
                {
                    treatmentDate = model.TreatmentDetail.TreatmentDates.First();
                }
                else
                {
                    result.Add("Treatment date required.");
                }

                if (policyCommencementDate == null)
                {
                    result.Add("Policy commencement date required.");
                }

                if (policyEffectiveDate == null) //TODO
                {
                    result.Add("Policy effective date required.");
                }

                if (result.Any()) return result;
                #endregion


                #region #DateCheck



                if (claimDate < policyEffectiveDate)
                {
                    result.Add("Your claim date must not be less than policy effective date.");
                }


                if (claimDate < treatmentDate)
                {
                    result.Add("Your claim date must not be less than treatment date.");
                }

                if (treatmentDate < policyEffectiveDate)
                {
                    result.Add("Your treatment date must not be less than policy effective date.");
                }

                return result;
                #endregion
            }

            else if (model.BenefitFormType == EnumBenefitFormType.TotalPermanentDisability)
            {
                DateTime? disibilityDate = null;

                #region #RequiredCheck
                if (model.CausedBy != null && model.CausedBy.ByType != null && model.CausedBy.ByType == EnumCauseByType.PermanentDisability && model.CausedBy.ByDate != null)
                {
                    disibilityDate = model.CausedBy.ByDate;
                }
                else
                {
                    result.Add("Disibility date required.");
                }

                if (policyCommencementDate == null)
                {
                    result.Add("Policy commencement date required.");
                }

                if (policyEffectiveDate == null) //TODO
                {
                    result.Add("Policy effective date required.");
                }

                if (result.Any()) return result;
                #endregion


                #region #DateCheck



                if (claimDate < policyCommencementDate)
                {
                    result.Add("Your claim date must not be less than original policy commencement date.");
                }

                if (disibilityDate < policyCommencementDate)
                {
                    result.Add("Your disability date must not be less than policy commencement date.");
                }

                if (disibilityDate > todayDate)
                {
                    result.Add("Your disability date must not be greater than today date.");
                }

                return result;
                #endregion
            }

            else if (model.BenefitFormType == EnumBenefitFormType.CriticalIllnessBenefit)
            {
                DateTime? diagnosisDate = null;

                #region #RequiredCheck
                if (model.CausedBy != null && model.CausedBy.ByType != null && model.CausedBy.ByType == EnumCauseByType.CriticalIllness && model.CausedBy.ByDate != null)
                {
                    diagnosisDate = model.CausedBy.ByDate;
                }
                else
                {
                    result.Add("Diagnosis date required.");
                }

                if (policyCommencementDate == null)
                {
                    result.Add("Policy commencement date required.");
                }

                if (policyEffectiveDate == null) //TODO
                {
                    result.Add("Policy effective date required.");
                }

                if (result.Any()) return result;
                #endregion


                #region #DateCheck



                if (claimDate < policyCommencementDate)
                {
                    result.Add("Your claim date must not be less than original policy commencement date.");
                }

                if (diagnosisDate < policyCommencementDate)
                {
                    result.Add("Your diagnosis date must not be less than policy commencement date.");
                }

                if (diagnosisDate > todayDate)
                {
                    result.Add("Your diagnosis date must not be greater than today date.");
                }

                return result;
                #endregion
            }

            return null;
        }

        public async Task<ResponseModel<PagedList<ClaimListRsp>>> GetClaimList(ClaimStatusListRequest model)
        {
            MobileErrorLog("ClaimList => request", JsonConvert.SerializeObject(model), "", httpContext?.HttpContext.Request.Path);

            try
            {
                var memberId = commonRepository.GetMemberIDFromToken();
                var selectID = Guid.NewGuid();

                if (CheckAuthorization(memberId, null)?.Claim == false)
                    return new ResponseModel<PagedList<ClaimListRsp>> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };


                //var clientNoList = GetClientNoListByIdValue(memberId);

                //model.HolderClientNoList = clientNoList;

                model.AppMemberId = memberId;
                model.Size = 10;

                var queryStrings = PrepareClaimStatusListQuery(model);

                var count = unitOfWork.GetRepository<ClaimCount>()
                    .FromSqlRaw(queryStrings?.CountQuery, null, CommandType.Text)
                    .FirstOrDefault();

                var claims = unitOfWork.GetRepository<ClaimTranResp>()
                    .FromSqlRaw(queryStrings?.ListQuery, null, CommandType.Text)
                    .ToList();


                #region #Dummy
                ////var count = new ClaimCount { SelectCount = 1 };

                ////var claims = new List<ClaimTranResp>
                ////{
                ////    new ClaimTranResp
                ////    { 
                ////        TransactionDate = DateTime.Now,
                ////        ClaimId = new Guid("c924f0f4-935b-454d-ac8b-e634551a3ad9"),
                ////        ClaimType = "Ambulatory/Out-patient",
                ////        ClaimTypeMm = "Ambulatory/Out-patient",
                ////        ClaimFormType = EnumBenefitFormType.DentalCare.ToString(),
                ////        ClaimStatusCode = "BT",
                ////        ClaimStatus = "Approved",
                ////        ProgressAsHours = "50:12 Hours",
                ////        ProgressAsPercent = 75,
                ////    }
                ////};

                #endregion

                

                var claimList = new List<ClaimListRsp>();
                
                foreach ( var claim in claims )
                {
                    MobileErrorLog($"ClaimList => {selectID} claimId => {claim.ClaimId}", $"Dt => {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")}", "", httpContext?.HttpContext.Request.Path);

                    var claimm = new ClaimListRsp()
                    {
                        ClaimId = claim.ClaimId.ToString(),
                        ClaimDate = claim.TransactionDate,
                        ClaimType = claim.ClaimType,
                        ClaimTypeMm = claim.ClaimTypeMm,
                        ClaimTypeEnum = claim.ClaimFormType,
                    };

                    var claimStatusList = unitOfWork.GetRepository<Entities.ClaimsStatusUpdate>()
                        .Query(x => x.ClaimId == claim.ClaimId.ToString())
                        .Select(x => new { x.NewStatusDesc, x.CreatedDate })
                        .ToList();


                    var changedStatusList = claimStatusList?
                            .GroupBy(x => new { x.NewStatusDesc })
                            .Select(group => group.OrderByDescending(g => g.CreatedDate).First())
                            .ToList()
                            .OrderByDescending(result => result.CreatedDate)
                            .ToList();                    


                    MobileErrorLog($"ClaimList => {selectID} claimId => {claim.ClaimId}", $"ClaimsStatusUpdate => Dt => {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")}", "", httpContext?.HttpContext.Request.Path);

                    if (changedStatusList?.Any() ?? false)
                    {
                        
                        var changedList = changedStatusList.Select(x => x.NewStatusDesc).ToList();

                        var matchedList = claimm.ClaimStatusList?.Where(x => changedList.Contains(x.Status)).ToList();

                        

                        matchedList?.ForEach(matched => 
                        {
                            matched.IsCompleted = true;
                            matched.Remove = false;
                            matched.StatusChangedDt = changedStatusList.Where(x => x.NewStatusDesc == matched.Status).Select(x => x.CreatedDate).FirstOrDefault();
                        });

                        //var conditionList = new List<string> { "Rejected", "Withdrawn", "Closed" };
                        //if (matchedList?.Where(x => conditionList.Contains(x.Status)).Any() ?? false)
                        //{
                        //    claimm.ClaimStatusList?.RemoveAll(x => x.Status == "Approved");
                        //}


                        var conditionList = new List<string> { "Approved", "Rejected", "Withdrawn", "Closed" };
                        if (matchedList?.Where(x => conditionList.Contains(x.Status)).Any() ?? false)
                        {
                            var theLastOne = matchedList
                                .Where(x => conditionList.Contains(x.Status))
                                .OrderByDescending(x => x.StatusChangedDt)
                                .Select(x => x.Status)
                                .FirstOrDefault();


                            claimm.ClaimStatusList?.RemoveAll(x => conditionList.Contains(x.Status) && x.Status != theLastOne);
                        }

                        claimm.ClaimStatusList?.RemoveAll(x => x.Remove == true);

                        if (claimm.ClaimStatusList != null)
                        {
                            var approved = claimm.ClaimStatusList.Where(x => x.Status == "Approved").FirstOrDefault();
                            var followedup = claimm.ClaimStatusList.Where(x => x.Status == "Followed-up").FirstOrDefault();

                            if (approved != null && followedup != null)
                            {

                                if (followedup.StatusChangedDt >= approved.StatusChangedDt)
                                {
                                    var shuffle = approved.Sort;
                                    approved.Sort = followedup.Sort;
                                    followedup.Sort = shuffle;

                                    claimm.ClaimStatusList = claimm.ClaimStatusList.OrderBy(x => x.Sort).ToList();
                                }
                            }
                            
                        }
                    }

                    MobileErrorLog($"ClaimList => {selectID} claimId => {claim.ClaimId}", $"changedList => Dt => {DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")}", "", httpContext?.HttpContext.Request.Path);

                    var progress = GetProgressAndContactHour(claim.TransactionDate.Value);
                    claimm.Progress = progress?.Percent; 
                    claimm.ClaimContactHours = progress?.Hours; 

                    claimList.Add(claimm);
                }


                var result = new PagedList<ClaimListRsp>(
                   source: claimList,
                   totalCount: count?.SelectCount ?? 0,
                   pageNumber: model.Page ?? 0,
                   pageSize: model.Size ?? 0);


                MobileErrorLog($"ClaimList => {selectID} response", JsonConvert.SerializeObject(result), "", httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<ClaimListRsp>>(ErrorCode.E0, result);

            }
            catch (Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<ClaimListRsp>>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<ClaimDetailRsp>> GetClaimDetail(Guid claimId)
        {
            try
            {
                var memberId = commonRepository.GetMemberIDFromToken();


                if (CheckAuthorization(memberId, null)?.Claim == false)
                    return new ResponseModel<ClaimDetailRsp> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var claim = unitOfWork.GetRepository<Entities.ClaimTran>()
                    .Query(x => x.ClaimId == claimId && x.AppMemberId == memberId).FirstOrDefault();

                if( claim != null )
                {
                    var claimStatusList = unitOfWork.GetRepository<Entities.ClaimsStatusUpdate>()
                        .Query(x => x.ClaimId == claim.ClaimId.ToString())
                        .ToList();


                    var changedStatusList = claimStatusList?
                            .GroupBy(x => new { x.NewStatusDesc })
                            .Select(group => group.OrderByDescending(g => g.CreatedDate).First())
                            .ToList()
                            .OrderByDescending(result => result.CreatedDate)
                            .ToList();


                    //var finalClaimStatus = changedStatusList?.Last()?.NewStatusDesc;


                    //var changedStatusList = unitOfWork.GetRepository<Entities.ClaimsStatusUpdate>()
                    //.Query(update => update.ClaimId == claim.ClaimId.ToString())
                    //.GroupBy(update => update.NewStatusDesc)
                    //.Select(group => new
                    //{
                    //    NewStatusDesc = group.Key,
                    //    CreatedDate = group.Max(update => update.CreatedDate)
                    //})
                    //.OrderByDescending(result => result.CreatedDate)
                    //.ToList();

                    var response = new ClaimDetailRsp();
                    response.ClaimStatus = claim.ClaimStatus;
                    response.ClaimType = claim.ClaimType;
                    response.ClaimTypeMm = claim.ClaimTypeMm;
                    response.ClaimTypeEnum = claim.ClaimFormType;
                    response.IsHoliday = IsHoliday();

                    if (!string.IsNullOrEmpty(changedStatusList?.FirstOrDefault()?.Reason))
                    {
                        response.Reason = changedStatusList?.FirstOrDefault()?.Reason;
                    }


                    if (!string.IsNullOrEmpty(changedStatusList?.FirstOrDefault()?.RemarkFromIL))
                    {
                        response.RemarkFromIL = changedStatusList?.FirstOrDefault()?.RemarkFromIL;
                    }
                    else
                    {
                        response.RemarkFromIL = response.Reason;
                    }

                                      

                    var claimContact = GetProgressAndContactHour(claim.TransactionDate.Value);

                    if (claimContact != null)
                    {
                        response.ClaimProgress = new ClaimProgress
                        {
                            Progress = claimContact.Percent,
                            ClaimContactHours = claimContact.Hours,
                        };

                        
                    }                    

                    if (changedStatusList?.Any()?? false)
                    {
                        var statusList = changedStatusList.Select(x => x.NewStatusDesc).ToList();

                        var matchedList = response.ClaimStatusList?
                            .Where(x => statusList.Contains(x.Status)).ToList();



                        matchedList?.ForEach(matched =>
                        {
                            matched.IsCompleted = true;
                            matched.Remove = false;
                            matched.StatusChangedDt = changedStatusList.Where(x => x.NewStatusDesc == matched.Status).Select(x => x.CreatedDate).FirstOrDefault();
                        });

                        //var conditionList = new List<string> { "Rejected", "Withdrawn", "Closed" };
                        //if (matchedList?.Where(x => conditionList.Contains(x.Status)).Any() ?? false)
                        //{
                        //    response.ClaimStatusList?.RemoveAll(x => x.Status == "Approved");
                        //}


                        var conditionList = new List<string> { "Approved", "Rejected", "Withdrawn", "Closed" };
                        if (matchedList?.Where(x => conditionList.Contains(x.Status)).Any() ?? false)
                        {
                            var theLastOne = matchedList
                                .Where(x => conditionList.Contains(x.Status))
                                .OrderByDescending(x => x.StatusChangedDt)
                                .Select(x => x.Status)
                                .FirstOrDefault();


                            response.ClaimStatusList?.RemoveAll(x => conditionList.Contains(x.Status) && x.Status != theLastOne);
                        }

                        response.ClaimStatusList?.RemoveAll(x => x.Remove == true);

                        if (response.ClaimStatusList != null)
                        {

                            var approved = response.ClaimStatusList.Where(x => x.Status == "Approved").FirstOrDefault();
                            var followedup = response.ClaimStatusList.Where(x => x.Status == "Followed-up").FirstOrDefault();

                            if (approved != null && followedup != null)
                            {

                                if (followedup.StatusChangedDt >= approved.StatusChangedDt)
                                {
                                    var shuffle = approved.Sort;
                                    approved.Sort = followedup.Sort;
                                    followedup.Sort = shuffle;

                                    response.ClaimStatusList = response.ClaimStatusList.OrderBy(x => x.Sort).ToList();
                                }
                            }

                        }

                        //////if (response.ClaimStatusList != null)
                        //////{
                        //////    finalClaimStatus = response.ClaimStatusList.Where(x => x.IsCompleted == true).OrderByDescending(x => x.Sort).Select(x => x.Status).FirstOrDefault();
                        //////    response.ClaimStatus = finalClaimStatus;
                        //////}

                        response.ClaimStatus = claim.ClaimStatus;

                        Console.WriteLine($"ClaimDetail => Original Last Updated Status {claim.ClaimStatus}");

                        #region #TMA complaint on 24-05-2024
                        var aiaClaimTblRecord = unitOfWork.GetRepository<Entities.Claim>().Query(x => x.ClaimId == claimId.ToString()).FirstOrDefault();
                        if (aiaClaimTblRecord != null && aiaClaimTblRecord.Status == "PN")
                        {
                            var lastCompletedRecord = response.ClaimStatusList.Where(x => x.IsCompleted == true)
                                .OrderByDescending(x => x.Sort)
                                .Select(x => x.Status)
                                .FirstOrDefault();

                            response.ClaimStatus = lastCompletedRecord;

                                Console.WriteLine($"ClaimDetail => Align Last Updated Status {response.ClaimStatus}");
                        }
                        #endregion
                    }

                    var policy = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => x.PolicyNo == claim.PolicyNo).FirstOrDefault();

                    if (policy != null)
                    {
                        var insured = unitOfWork.GetRepository<Entities.Client>()
                                        .Query(x => x.ClientNo == policy.InsuredPersonClientNo)
                                        .FirstOrDefault();

                        var insuredNrc = string.IsNullOrEmpty(insured?.Nrc)
                            ? (string.IsNullOrEmpty(insured?.PassportNo)
                            ? (insured?.Other)
                            : insured?.PassportNo)
                            : insured?.Nrc;

                        var owner = unitOfWork.GetRepository<Entities.Client>()
                                        .Query(x => x.ClientNo == policy.PolicyHolderClientNo)
                                        .Select(x => x.Name)
                                        .FirstOrDefault();

                        var product = unitOfWork.GetRepository<Entities.Product>()
                                        .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false)
                                        .Select(x => new { x.TitleEn, x.TitleMm })
                                        .FirstOrDefault();



                        response.ClaimRequest = new ClaimReq
                        {
                            ClaimDate = claim.TransactionDate,
                            PolicyNo = claim.PolicyNo,
                            InsurredId = policy.InsuredPersonClientNo,
                            InsurredName = insured?.Name,
                            InsurredNrc = insuredNrc,
                            Owner = owner,
                            ProductEn = product?.TitleEn,
                            ProductMm = product?.TitleMm,
                            ClaimStatus = response.ClaimStatus,
                            BenefitSubmittedDate = Utils.GetDefaultDate(),
                            BeneficiaryAmountPayable = changedStatusList?.Last()?.PayableAmountFromIL,
                        };
                    }


                    MobileErrorLog("ClaimDetails => response", "claimlist filter issue closed", JsonConvert.SerializeObject(response), httpContext?.HttpContext.Request.Path);


                    claim.ProgressAsPercent = claimContact?.Percent ?? claim.ProgressAsPercent;
                    claim.ProgressAsHours = claimContact?.Hours ?? claim.ProgressAsHours;
                    unitOfWork.SaveChanges();

                    return errorCodeProvider.GetResponseModel<ClaimDetailRsp>(ErrorCode.E0, response);
                }

                return errorCodeProvider.GetResponseModel<ClaimDetailRsp>(ErrorCode.E400);

            }
            catch (Exception ex)
            {
                MobileErrorLog("GetClaimDetail => Exception", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                //return errorCodeProvider.GetResponseModel<ClaimDetailRsp>(ErrorCode.E500);

                return new ResponseModel<ClaimDetailRsp> { Code = 500, Message = JsonConvert.SerializeObject(ex) };
            }
        }        

        public async Task CommonClaimService(ClaimNowRequest model, Entities.Policy policy, Guid mainClaimId, Guid claimId
            , string reasonCode = null, string componentCode = null, List<Entities.InOutPatientReasonBenefitCode>? benefitList = null, Guid? memberId = null)
        {

            MobileErrorLog(
                  $"CommonClaimServiceRequest => mainClaimId => {mainClaimId}, claimId => {claimId}, PolicyNo => {policy?.PolicyNo}, ReasonCode => {reasonCode}, ComponentCode => {componentCode}"
                , $"CommonClaimServiceRequest => model => {model}"
                , $"CommonClaimServiceRequest => inputBenefit => {JsonConvert.SerializeObject(benefitList)}"
                , "v1/claim/claim");

            try
            {
                //var memberId = GetMemberIDFromToken();

                


                var transactionDt = Utils.GetDefaultDate();

                var relationMm = "";
                var relationEn = "";
                var signatureImg = "";
                byte[]? pdfData = null;

                decimal? benefitAmount = 0;
                decimal? benefitTotalAmount = 0;
                DateTime? incurredDate = null;

                var productName = "";

                var claimFormType = unitOfWork.GetRepository<Entities.InsuranceBenefit>()
                                    .Query(x => x.BenefitFormType == model.BenefitFormType.ToString())
                                    .Select(x => new { x.BenefitNameEn, x.BenefitNameMm })
                                    .FirstOrDefault();

                var feRequest = JsonConvert.SerializeObject(model);
                var feRequestOn = Utils.GetDefaultDate();

                #region #Setting
                var holderNo = unitOfWork.GetRepository<Entities.MemberClient>().Query(x => x.MemberId == memberId)
                    .Select(x => x.ClientNo)
                    .FirstOrDefault();

                Diagnosis? diagnosis = null;
                if (model.TreatmentDetail?.DiagnosisId != null)
                {
                    diagnosis = unitOfWork.GetRepository<Entities.Diagnosis>()
                        .Query(x => x.IsDelete == false && x.IsActive == true && x.ID == Guid.Parse(model.TreatmentDetail.DiagnosisId)).FirstOrDefault();

                }

                Hospital? hospital = null;
                if (model.TreatmentDetail?.HospitalId != null)
                {
                    hospital = unitOfWork.GetRepository<Entities.Hospital>()
                        .Query(x => x.IsDelete == false && x.IsActive == true && x.ID == Guid.Parse(model.TreatmentDetail.HospitalId)).FirstOrDefault();

                }

                ClaimIncurredLocation? location = null;
                if (model.TreatmentDetail?.LocationId != null)
                {
                    location = unitOfWork.GetRepository<Entities.ClaimIncurredLocation>()
                        .Query(x => x.IsDelete == false && x.IsActive == true && x.ID == Guid.Parse(model.TreatmentDetail.LocationId)).FirstOrDefault();

                }

                Entities.Relationship? relation = null;

                if (model.ClaimantDetail != null
                    && model.ClaimantDetail.Relationship != null)
                {
                    relation = unitOfWork.GetRepository<Entities.Relationship>()
                        .Query(x => x.IsDelete == false && x.IsActive == true && x.ID == Guid.Parse(model.ClaimantDetail.Relationship))
                        .FirstOrDefault();

                    relationEn = relation?.Name;
                    relationMm = relation?.Name_MM;
                }

                Entities.Bank? bank = null;

                if (model.BankDetail != null
                    && model.BankDetail.BankCode != null)
                {
                    bank = unitOfWork.GetRepository<Entities.Bank>()
                        .Query(x => x.IsDelete == false && x.IsActive == true && x.BankCode == model.BankDetail.BankCode)
                        .FirstOrDefault();
                }

                #endregion

                var byNameEn = "";
                var byNameMm = "";
                var byCode = "";

                var byCIRiderCode = "";

                var CausedByName = "";

                if (model.CausedBy != null)
                {
                    #region #CausedBy   
                    if (model.CausedBy.ByType != null
                        && model.CausedBy.ByType == EnumCauseByType.PartialDisability
                        && model.CausedBy.ById != null)
                    {
                        var partialDisability = unitOfWork.GetRepository<Entities.PartialDisability>()
                            .Query(x => x.IsDelete == false && x.IsActive == true && x.ID == Guid.Parse(model.CausedBy.ById)).FirstOrDefault();
                        byNameEn = partialDisability?.Name;
                        byNameMm = partialDisability?.Name_MM;
                        byCode = partialDisability?.Code;

                        CausedByName = partialDisability?.Name;
                    }

                    if (model.CausedBy.ByType != null
                        && model.CausedBy.ByType == EnumCauseByType.PermanentDisability
                        && model.CausedBy.ById != null)
                    {
                        var permanentDisability = unitOfWork.GetRepository<Entities.PermanentDisability>()
                            .Query(x => x.IsDelete == false && x.IsActive == true && x.ID == Guid.Parse(model.CausedBy.ById)).FirstOrDefault();
                        byNameEn = permanentDisability?.Name;
                        byNameMm = permanentDisability?.Name_MM;
                        byCode = permanentDisability?.Code;

                        CausedByName = permanentDisability?.Name;
                    }

                    if (model.CausedBy.ByType != null
                        && model.CausedBy.ByType == EnumCauseByType.CriticalIllness
                        && model.CausedBy.ById != null)
                    {
                        var criticalIllness = unitOfWork.GetRepository<Entities.CriticalIllness>()
                            .Query(x => x.IsDelete == false && x.IsActive == true && x.ID == Guid.Parse(model.CausedBy.ById)).FirstOrDefault();
                        byNameEn = criticalIllness?.Name;
                        byNameMm = criticalIllness?.Name_MM;
                        byCode = criticalIllness?.Code;

                        CausedByName = criticalIllness?.Name;
                    }

                    if (model.CausedBy.ByType != null
                        && model.CausedBy.ByType == EnumCauseByType.Death
                        && model.CausedBy.ById != null)
                    {
                        var death = unitOfWork.GetRepository<Entities.Death>()
                            .Query(x => x.IsDelete == false && x.IsActive == true && x.ID == Guid.Parse(model.CausedBy.ById)).FirstOrDefault();
                        byNameEn = death?.Name;
                        byNameMm = death?.Name_MM;
                        byCode = death?.Code;

                        CausedByName = death?.Name;
                    }

                    #endregion
                }

                var customILCoastDate = GetILCoastClaimDate().Value;

                var diagnosisCode = diagnosis?.Code;


                var claimType = model.BenefitFormType.ToString();

                var benefitTypeEn = claimFormType?.BenefitNameEn;
                var benefitTypeMm = claimFormType?.BenefitNameMm;

                string eligibleComponents = componentCode;


                #region #ClaimTranInsert

                #region #Update ClaimStatusChange Tbl
                try
                {
                    unitOfWork.GetRepository<Entities.ClaimsStatusUpdate>().Add(new ClaimsStatusUpdate
                    {
                        Id = Guid.NewGuid(),
                        ClaimId = claimId.ToString(),
                        CreatedDate = Utils.GetDefaultDate(),
                        IsDone = true,
                        IsDeleted = false,
                        OldStatus = "RC",
                        NewStatus = "RC",
                        ChangedByAiaPlus = true,
                        NewStatusDesc = "Received",
                        NewStatusDescMm = "Received"
                    }
                    );
                    unitOfWork.SaveChanges();
                   
                }
                catch (Exception ex)
                {
                    MobileErrorLog("saving data in ClaimsStatusChange tbl exception", ex.Message
                            , JsonConvert.SerializeObject(ex), "v1/claim/claim");
                }
                #endregion


                var product1 = unitOfWork.GetRepository<Entities.Product>()
                            .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false)
                            .Select(x => x.TitleEn)
                            .FirstOrDefault();


                

                var entity = new Entities.ClaimTran();
                entity.CreatedDate = Utils.GetDefaultDate();
                entity.TransactionDate = Utils.GetDefaultDate();
                entity.MainId = mainClaimId;
                entity.PolicyNo = policy.PolicyNo;
                entity.HolderClientNo = holderNo;

                entity.Ferequest = feRequest;
                entity.FerequestOn = feRequestOn;

                entity.ClaimId = claimId;
                entity.InsuredClientNo = model.InsuredId;
                entity.ClaimFormType = model.BenefitFormType.ToString();
                entity.ClaimType = benefitTypeEn;
                entity.ClaimTypeMm = benefitTypeMm;
                entity.IndividualClaimType = claimType;

                entity.EligibleComponents = eligibleComponents;

                try
                {
                    (string? membertype, string? memberID, string? groupMemberId) clientInfo = GetClientInfo(memberId);

                    entity.IndividualMemberID = clientInfo.memberID;
                    entity.GroupMemberID = clientInfo.groupMemberId;
                    entity.MemberType = clientInfo.membertype;

                    var memberInfo = unitOfWork.GetRepository<Entities.Member>()
                    .Query(x => x.MemberId == memberId)
                    .Select(x => new { x.Name, x.Mobile })
                    .FirstOrDefault();

                    entity.MemberName = memberInfo?.Name;
                    entity.MemberPhone = memberInfo?.Mobile;

                }
                catch { }

                if (model.CausedBy != null
                && model.CausedBy.ByType != null
                && model.CausedBy.ById != null)
                {
                    entity.CausedByDate = model.CausedBy.ByDate;
                    entity.CausedById = Guid.Parse(model.CausedBy.ById);
                    entity.CausedByType = model.CausedBy.ByType.ToString();
                    entity.CausedByNameEn = byNameEn;
                    entity.CausedByNameMm = byNameMm;
                    entity.CausedByCode = byCode;
                }

                

                if (model.ClaimantDetail != null)
                {
                    entity.ClaimantAddress = ""; //model.ClaimantDetail.Address;
                    entity.ClaimantDob = model.ClaimantDetail.Dob;
                    entity.ClaimantPhone = model.ClaimantDetail.Phone;
                    entity.ClaimantEmail = model.ClaimantDetail.Email;
                    entity.ClaimantGender = model.ClaimantDetail.Gender;
                    entity.ClaimantName = model.ClaimantDetail.Name;
                    entity.ClaimantIdenType = model.ClaimantDetail.IdValue?.Type.ToString();
                    entity.ClaimantIdenValue = model.ClaimantDetail.IdValue?.Value;
                    entity.ClaimantRelationship = relationEn ?? model.ClaimantDetail.Relationship;
                    entity.ClaimantRelationshipMm = relationMm ?? model.ClaimantDetail.Relationship;
                }

               
                entity.DiagnosisId = diagnosis?.ID;
                entity.DiagnosisNameEn = diagnosis?.Name;
                entity.DiagnosisNameMm = diagnosis?.Name_MM;
                entity.DiagnosisCode = diagnosis?.Code;
                entity.HospitalId = hospital?.ID;
                entity.HospitalNameEn = hospital?.Name;
                entity.HospitalNameMm = hospital?.Name_MM;
                entity.HospitalCode = hospital?.Code;
                entity.LocationId = location?.ID;
                entity.LocationNameEn = location?.Name;
                entity.LocationNameMm = location?.Name_MM;
                entity.LocationCode = location?.Code;

                if (model.TreatmentDetail?.TreatmentDates != null && model.TreatmentDetail.TreatmentDates.Any())
                {
                    entity.TreatmentDates = string.Join(",", model.TreatmentDetail.TreatmentDates);
                }

                entity.TreatmentFromDate = model.TreatmentDetail?.TreatmentFromDate;
                entity.TreatmentToDate = model.TreatmentDetail?.TreatmentToDate;
                entity.TreatmentCount = model.TreatmentDetail?.TreatmentCount;
                entity.IncurredAmount = model.TreatmentDetail?.IncurredAmount;
                entity.IncidentSummary = model.IncidentSummary;
                entity.DoctorName = model.TreatmentDetail?.DoctorName;
                entity.ClaimForPolicyNo = model.TreatmentDetail?.PolicyNo; //TODO

                entity.BankCode = model.BankDetail?.BankCode;
                entity.BankAccountName = model.BankDetail?.AccountName;
                entity.BankAccountNumber = model.BankDetail?.AccountNumber;
                entity.BankAccHolderIdValue = model.BankDetail?.BankAccHolderIdValue;
                entity.BankAccHolderDob = model.BankDetail?.BankAccHolderDob;

                entity.BankNameEn = bank?.BankName;
                entity.BankNameMm = bank?.BankName;

                entity.IlerrorMessage = "";
                entity.Ilstatus = "";
                entity.RemainingTime = "";
                entity.SignatureImage = model.SignatureImage;
                entity.UpdatedOn = Utils.GetDefaultDate();                

                entity.DoctorId = AppSettingsHelper.GetSetting("AiaCommon:DoctorID");
                entity.DoctorName = model.TreatmentDetail?.DoctorName;
                entity.ProductType = policy.ProductType; 
                entity.ProductNameEn = product1 ?? ""; 
                entity.ClaimStatus = "Received"; 
                entity.ClaimStatusCode = "RC";
                //entity.EstimatedCompletedDate = GetProgressAndContactHour(Utils.GetDefaultDate())?.CompletedDate;


                entity.AppMemberId = memberId;

                unitOfWork.GetRepository<Entities.ClaimTran>().Add(entity);


                

                #region #Claim Benefits

                if (benefitList != null && benefitList.Any())
                {


                    foreach (var benefit in benefitList)
                    {

                        var claimBenefit = new Entities.ClaimBenefit()
                        {
                            Id = Guid.NewGuid(),
                            MainClaimId = mainClaimId,
                            ClaimId = claimId,
                            BenefitName = benefit.BenefitName,
                            BenefitFromDate = benefit.FromDate,
                            BenefitAmount = benefit.Amount,
                            BenefitToDate = benefit.ToDate,
                            TotalCalculatedAmount = benefit.TotalAmount,
                            BenefitCode = benefit.BenefitCode,
                        };


                        unitOfWork.GetRepository<Entities.ClaimBenefit>().Add(claimBenefit);


                    }
                }

                #endregion

                #region #Claim Docs

                if (model.ClaimDocuments != null && model.ClaimDocuments.Any())
                {
                    foreach (var docType in model.ClaimDocuments)
                    {
                        if (docType.DocIdList != null && docType.DocIdList.Any())
                        {
                            foreach (var docId in docType.DocIdList)
                            {
                                var claimDocument = new Entities.ClaimDocument
                                {
                                    DocTypeId = docType.DocTypeId,
                                    DocTypeName = docType.DocTypeName,
                                    MainClaimId = mainClaimId,
                                    ClaimId = claimId,
                                    Id = Guid.NewGuid(),
                                    UploadId = Guid.NewGuid(),
                                    DocName = docId,
                                    UploadStatus = "",
                                    DocName2 = $"{policy?.PolicyNo}_{docType.DocTypeId}_{docType.DocTypeName}_AIA_{(Utils.GetDefaultDate().ToString("yyyy-MM-dd_HH_mm_ss"))}_{docId}",
                                };

                                unitOfWork.GetRepository<Entities.ClaimDocument>().Add(claimDocument);
                            }
                        }


                    }
                }

                #endregion



                

                unitOfWork.SaveChanges();


                MobileErrorLog(
                  $"CommonClaimService => Saving Success"
                , $""
                , $""
                , "v1/claim/claim");

                #endregion

                #region #AiaILApi
                var claimModel = new CommonRegisterRequest();

                var apiType = EnumILClaimApi.Health;

                claimModel.componentID = componentCode;

                //var customILCoastDate = GetILCoastClaimDate().Value;

                var commonReasonCode = "";
                var benefitCode = "";

                #region #CommonReasonCode
                var COMPONENTS = policy.Components?.Trim().Split(",");

                var REASON_QUERY = unitOfWork.GetRepository<Entities.ReasonCode>()
                        .Query(x => x.ClaimType == claimType
                        && x.ProductCode == policy.ProductType
                        );
                REASON_QUERY = REASON_QUERY.Where(BuildSearchExpReasonCode(COMPONENTS.ToArray()));

                var REASON_CODE = REASON_QUERY
                        .Select(x => x.ReasonCode1)
                        .FirstOrDefault();



                commonReasonCode = REASON_CODE;
                #endregion

                MobileErrorLog(
                  $"CommonClaimService => ClaimType => {claimType}"
                , $""
                , $""
                , "v1/claim/claim");

                claimModel.ProductCode = policy.ProductType;
                claimModel.BenefitClaimType = claimType;

                if (claimType == EnumBenefitFormType.OutpatientAndAmbulatoryCare.ToString()
                || claimType == EnumBenefitFormType.Inpatient.ToString())
                {

                    MobileErrorLog(
                  $"CommonClaimService => Inpatient"
                , $""
                , $""
                , "v1/claim/claim");

                    commonReasonCode = reasonCode;
                    eligibleComponents = componentCode;


                    claimModel.claimId = claimId.ToString();
                    claimModel.policyNumber = policy?.PolicyNo;
                    var apiClaimType = "";
                    var dignosisCode = diagnosis?.Code;

                    if (claimType == EnumBenefitFormType.OutpatientAndAmbulatoryCare.ToString())
                    {
                        apiClaimType = "O";

                        //apiClaimType = "OP"; //Email Request //Comment out due to Ticket on 08/02/2024
                    }
                    else if (claimType == EnumBenefitFormType.Inpatient.ToString())
                    {
                        apiClaimType = "H";

                        //apiClaimType = "IP"; //Email Request //Comment out due to Ticket on 08/02/2024
                    }
                    else if (claimType == EnumBenefitFormType.MaternityCare.ToString()
                    || claimType == EnumBenefitFormType.DentalCare.ToString()
                    || claimType == EnumBenefitFormType.VisionCare.ToString()
                    || claimType == EnumBenefitFormType.Vaccination.ToString()
                    || claimType == EnumBenefitFormType.PhysicalCheckup.ToString())
                    {
                        apiClaimType = "M";


                    }


                    claimModel.diagnosisCode = dignosisCode;

                    claimModel.claimType = apiClaimType;
                    claimModel.componentID = eligibleComponents;
                    claimModel.receiveDate = customILCoastDate.ToString(DefaultConstants.AiaApiDateFormat);

                    claimModel.claimReason = commonReasonCode;
                    claimModel.paymentMethod = "B";
                    claimModel.claimLocation = location?.Code;

                    if (model.BankDetail != null)
                    {
                        claimModel.payee = new Payee //TODO
                        {
                            bankNameCode = model.BankDetail?.BankCode
                        ,
                            bankAccountNumber = model.BankDetail?.AccountNumber
                        ,
                            bankAccountName = model.BankDetail?.AccountName
                        };
                    }


                    claimModel.currencyCode = "MM";
                    claimModel.effectiveDate = customILCoastDate.ToString(DefaultConstants.AiaApiDateFormat);
                    //claimModel.sourceNotification = "M";
                    claimModel.sourceNotification = "A"; //Eric Requrest
                    claimModel.gratiaIndicator = "N";
                    claimModel.interimCoverage = "N";
                    claimModel.disallowITC = "N";
                    claimModel.soleProprietor = "N";
                    claimModel.purposeOrPersonal = "P";
                    claimModel.admitDate = customILCoastDate.ToString(DefaultConstants.AiaApiDateFormat);
                    claimModel.dischargeDate = customILCoastDate.ToString(DefaultConstants.AiaApiDateFormat);


                    if (model.TreatmentDetail != null && model.TreatmentDetail.TreatmentDates != null
                        && model.TreatmentDetail.TreatmentDates.Any())
                    {
                        var benefitDates = model.TreatmentDetail.TreatmentDates
                    .OrderBy(x => x).ToList();


                        claimModel.admitDate = benefitDates.First().ToString(DefaultConstants.AiaApiDateFormat);
                        claimModel.dischargeDate = benefitDates.Last().ToString(DefaultConstants.AiaApiDateFormat);
                    }


                    if (!string.IsNullOrEmpty(model.TreatmentDetail?.DoctorName))
                    {
                        claimModel.doctorID = AppSettingsHelper.GetSetting("AiaCommon:DoctorID");
                    }

                    claimModel.providerID = hospital?.Code;



                    #region #BenefitsCode                 





                    claimModel.benefits = new List<Benefit>();

                    var dateList = new List<DateTime>();

                    if (benefitList != null && benefitList.Any())
                    {


                        foreach (var benefitItem in benefitList)
                        {
                            var noOfDays = (benefitItem.FromDate.Value.Date == benefitItem.ToDate.Value.Date) ? 1 :
                                (benefitItem.ToDate.Value.Date - benefitItem.FromDate.Value.Date).Days;


                            #region #Outpatient#MER#Single
                            if (model.BenefitFormType == EnumBenefitFormType.OutpatientAndAmbulatoryCare && model.BenefitList == null
                                && model.TreatmentDetail != null && model.TreatmentDetail.TreatmentCount != null)
                            {
                                noOfDays = model.TreatmentDetail.TreatmentCount.Value;
                            }
                            #endregion


                            var benefit = new Benefit()
                            {
                                benefitName = benefitItem.BenefitName,
                                benefitCode = benefitItem.BenefitCode,
                                dateFrom = benefitItem.FromDate.Value.ToString(DefaultConstants.AiaApiDateFormat),
                                dateTo = benefitItem.ToDate.Value.ToString(DefaultConstants.AiaApiDateFormat),
                                noOfDays = noOfDays.ToString(),
                                incurredAmount = benefitItem.TotalAmount.ToString(),
                                totalAmount = benefitItem.TotalAmount.ToString(),
                            };


                            dateList.Add(benefitItem.FromDate.Value);
                            dateList.Add(benefitItem.ToDate.Value);

                            claimModel.benefits.Add(benefit);
                        }


                        var orderedDateList = dateList.OrderBy(x => x).ToList();

                        if (orderedDateList != null && orderedDateList.Any())
                        {
                            claimModel.incurredDate = orderedDateList.First().ToString(DefaultConstants.AiaApiDateFormat);
                            claimModel.admitDate = orderedDateList.First().ToString(DefaultConstants.AiaApiDateFormat);
                            claimModel.dischargeDate = orderedDateList.Last().ToString(DefaultConstants.AiaApiDateFormat);
                        }

                    }


                    #endregion

                    apiType = EnumILClaimApi.Health;

                }

                else if (
            claimType == EnumBenefitFormType.MaternityCare.ToString()
            || claimType == EnumBenefitFormType.DentalCare.ToString()
            || claimType == EnumBenefitFormType.VisionCare.ToString()
            || claimType == EnumBenefitFormType.Vaccination.ToString()
            || claimType == EnumBenefitFormType.PhysicalCheckup.ToString())
                {

                    MobileErrorLog(
                  $"CommonClaimService => MaternityCare"
                , $""
                , $""
                , "v1/claim/claim");




                    claimModel.claimId = claimId.ToString();
                    claimModel.policyNumber = policy?.PolicyNo;
                    var apiClaimType = "";
                    var dignosisCode = "";

                    apiClaimType = "M";



                    claimModel.claimType = apiClaimType;
                    claimModel.componentID = eligibleComponents;
                    claimModel.receiveDate = customILCoastDate.ToString(DefaultConstants.AiaApiDateFormat);



                    claimModel.claimReason = commonReasonCode;
                    claimModel.paymentMethod = "B";
                    claimModel.claimLocation = location?.Code;

                    if (model.BankDetail != null)
                    {
                        claimModel.payee = new Payee
                        {
                            bankNameCode = model.BankDetail?.BankCode
                        ,
                            bankAccountNumber = model.BankDetail?.AccountNumber
                        ,
                            bankAccountName = model.BankDetail?.AccountName
                        };
                    }


                    claimModel.currencyCode = "MM";
                    claimModel.effectiveDate = customILCoastDate.ToString(DefaultConstants.AiaApiDateFormat);
                    //claimModel.sourceNotification = "M";
                    claimModel.sourceNotification = "A"; //Eric Requrest
                    claimModel.gratiaIndicator = "N";
                    claimModel.interimCoverage = "N";
                    claimModel.disallowITC = "N";
                    claimModel.soleProprietor = "N";
                    claimModel.purposeOrPersonal = "P";
                    claimModel.admitDate = customILCoastDate.ToString(DefaultConstants.AiaApiDateFormat);
                    claimModel.dischargeDate = customILCoastDate.ToString(DefaultConstants.AiaApiDateFormat);


                    if (model.TreatmentDetail != null && model.TreatmentDetail.TreatmentDates != null
                        && model.TreatmentDetail.TreatmentDates.Any())
                    {
                        var benefitDates = model.TreatmentDetail.TreatmentDates
                    .OrderBy(x => x).ToList();

                        claimModel.admitDate = benefitDates.First().ToString(DefaultConstants.AiaApiDateFormat);
                        claimModel.dischargeDate = benefitDates.Last().ToString(DefaultConstants.AiaApiDateFormat);
                        claimModel.incurredDate = benefitDates.First().ToString(DefaultConstants.AiaApiDateFormat);
                    }
                    else if (model.TreatmentDetail != null && model.TreatmentDetail.TreatmentFromDate != null)
                    {
                        claimModel.incurredDate = model.TreatmentDetail.TreatmentFromDate.Value.ToString(DefaultConstants.AiaApiDateFormat);
                        claimModel.admitDate = model.TreatmentDetail.TreatmentFromDate.Value.ToString(DefaultConstants.AiaApiDateFormat);
                        claimModel.dischargeDate = model.TreatmentDetail.TreatmentFromDate.Value.ToString(DefaultConstants.AiaApiDateFormat);

                        if (model.TreatmentDetail != null && model.TreatmentDetail.TreatmentToDate != null)
                        {
                            claimModel.dischargeDate = model.TreatmentDetail.TreatmentToDate.Value.ToString(DefaultConstants.AiaApiDateFormat);
                        }

                    }

                    #region #DignosisCode
                    if (claimType == EnumBenefitFormType.MaternityCare.ToString())
                    {
                        claimModel.diagnosisCode = ClaimDignosisCode.MaternityCare;
                        diagnosisCode = ClaimDignosisCode.MaternityCare;
                    }
                    else if (claimType == EnumBenefitFormType.DentalCare.ToString())
                    {
                        claimModel.diagnosisCode = ClaimDignosisCode.DentalCare;
                        diagnosisCode = ClaimDignosisCode.DentalCare;
                    }
                    else if (claimType == EnumBenefitFormType.VisionCare.ToString())
                    {
                        claimModel.diagnosisCode = ClaimDignosisCode.VisionCare;
                        diagnosisCode = ClaimDignosisCode.VisionCare;
                    }
                    else if (claimType == EnumBenefitFormType.PhysicalCheckup.ToString())
                    {
                        claimModel.diagnosisCode = ClaimDignosisCode.PhysicalCheckup;
                        diagnosisCode = ClaimDignosisCode.PhysicalCheckup;
                    }
                    else if (claimType == EnumBenefitFormType.Vaccination.ToString())
                    {
                        claimModel.diagnosisCode = ClaimDignosisCode.Vaccination;
                        diagnosisCode = ClaimDignosisCode.Vaccination;
                    }
                    #endregion

                    if (!string.IsNullOrEmpty(model.TreatmentDetail?.DoctorName))
                    {
                        claimModel.doctorID = AppSettingsHelper.GetSetting("AiaCommon:DoctorID");
                    }

                    claimModel.providerID = hospital?.Code;



                    #region #BenefitsCode

                    claimModel.benefits = new List<Benefit>();

                    #region #All


                    if (benefitList != null && benefitList.Any())
                    {


                        foreach (var benefitItem in benefitList)
                        {
                            var noOfDays = (benefitItem.FromDate.Value.Date == benefitItem.ToDate.Value.Date) ? 1 :
                               (benefitItem.ToDate.Value.Date - benefitItem.FromDate.Value.Date).Days;

                            var benefit = new Benefit()
                            {
                                benefitName = benefitItem.BenefitName,
                                benefitCode = benefitItem.BenefitCode,
                                dateFrom = benefitItem.FromDate.Value.ToString(DefaultConstants.AiaApiDateFormat),
                                dateTo = benefitItem.ToDate.Value.ToString(DefaultConstants.AiaApiDateFormat),
                                incurredAmount = benefitItem.TotalAmount.ToString(),
                                totalAmount = benefitItem.TotalAmount.ToString(),
                                noOfDays = noOfDays.ToString(),
                            };


                            claimModel.benefits.Add(benefit);
                        }

                    }



                    #endregion

                    #endregion

                    apiType = EnumILClaimApi.Health;

                }
                else if (claimType == EnumBenefitFormType.PartialDisabilityAndInjury.ToString()
                        || claimType == EnumBenefitFormType.AcceleratedCancerBenefit.ToString()
                    )
                {
                    MobileErrorLog(
                  $"CommonClaimService => PartialDisabilityAndInjury"
                , $""
                , $""
                , "v1/claim/claim");

                    //var policyComponents = policy.Components?.Trim().Split(",");
                    string[] policyComponents = new string[] { componentCode };

                    var reasonCodeQuery = unitOfWork.GetRepository<Entities.ReasonCode>()
                            .Query(x => x.ClaimType == claimType
                            && x.ProductCode == policy.ProductType
                            );
                    reasonCodeQuery = reasonCodeQuery.Where(BuildSearchExpReasonCode(policyComponents.ToArray()));

                    var reasonCodeBenefitCode = reasonCodeQuery
                            .Select(x => x.ReasonCode1)
                            .FirstOrDefault();


                    claimModel.claimId = claimId.ToString();
                    claimModel.policyNumber = policy.PolicyNo;

                    if (claimType == EnumClaimType.AcceleratedCancerBenefit.ToString())
                    {
                        claimModel.claimType = "C";
                        commonReasonCode = reasonCodeBenefitCode;
                    }
                    else if (claimType == EnumClaimType.PartialDisabilityAndInjury.ToString())
                    {
                        commonReasonCode = byCode;
                        claimModel.claimType = "P";
                        claimModel.incurredDate = model.CausedBy?.ByDate.Value.ToString(DefaultConstants.AiaApiDateFormat);
                    }

                    claimModel.componentID = eligibleComponents;
                    claimModel.receiveDate = customILCoastDate.ToString(DefaultConstants.AiaApiDateFormat);

                    if (model.TreatmentDetail != null
                        && model.TreatmentDetail?.TreatmentFromDate != null) //TODO
                    {
                        claimModel.incurredDate = model.TreatmentDetail.TreatmentFromDate.Value.ToString(DefaultConstants.AiaApiDateFormat);
                    }
                    else if (model.TreatmentDetail != null
                        && model.TreatmentDetail?.TreatmentDates != null
                        && model.TreatmentDetail.TreatmentDates.Any())
                    {
                        claimModel.incurredDate = model.TreatmentDetail.TreatmentDates.First().ToString(DefaultConstants.AiaApiDateFormat);
                    }

                    claimModel.claimReason = commonReasonCode;

                    claimModel.claimLocation = location?.Code; //TODO

                    if (model.BankDetail != null)
                    {
                        claimModel.payee = new Payee
                        {
                            bankNameCode = model.BankDetail?.BankCode
                        ,
                            bankAccountNumber = model.BankDetail?.AccountNumber
                        ,
                            bankAccountName = model.BankDetail?.AccountName
                        };
                    }

                    //claimModel.sourceNotification = "M";
                    claimModel.sourceNotification = "A"; //Eric Requrest
                    claimModel.gratiaIndicator = "N";
                    claimModel.interimCoverage = "N";
                    claimModel.disallowITC = "N";
                    claimModel.soleProprietor = "N";
                    claimModel.purposeOrPersonal = "P";
                    //claimModel.claimentOwnerAccount = ""; //TODO


                    apiType = EnumILClaimApi.NonHealth;
                }
                else if (claimType == EnumBenefitFormType.DeathAndAccidentalDeath.ToString())
                {

                    claimModel.claimReason = commonReasonCode; //TODO
                    claimModel.claimId = claimId.ToString();
                    claimModel.policyNumber = model.TreatmentDetail?.PolicyNo;
                    claimModel.effectiveDate = customILCoastDate.ToString(DefaultConstants.AiaApiDateFormat);

                    if (model.CausedBy != null && model.CausedBy.ByDate != null)
                    {
                        claimModel.dateOfDisability = model.CausedBy.ByDate.Value.ToString(DefaultConstants.AiaApiDateFormat);
                    }

                    claimModel.causeOfDisability = byCode;
                    //claimModel.sourceNotification = "M";
                    claimModel.sourceNotification = "A"; //Eric Requrest
                    claimModel.gratiaIndicator = "N";
                    claimModel.interimCoverage = "N";
                    claimModel.disallowITC = "N";
                    claimModel.soleProprietor = "N";
                    claimModel.purposeOrPersonal = "P";
                    claimModel.relationship = relation?.Code; //TODO
                    claimModel.claimLocation = location?.Code; //TODO
                    claimModel.issuAgeVerified = "Y"; //TODO

                    claimModel.componentID = eligibleComponents;

                    Entities.Policy? deathPolicy = null;
                    if (!string.IsNullOrEmpty(model.TreatmentDetail?.PolicyNo))
                    {
                        deathPolicy = unitOfWork.GetRepository<Entities.Policy>()
                           .Query(x => x.PolicyNo == model.TreatmentDetail.PolicyNo)
                           .FirstOrDefault();
                    }
                    if (model.ClaimantDetail != null)
                    {
                        var claiment = new Claiment();
                        claiment.clientNumber = deathPolicy?.InsuredPersonClientNo;
                        claiment.name = model.ClaimantDetail.Name;
                        if (model.ClaimantDetail.Dob != null)
                            claiment.dob = model.ClaimantDetail.Dob.Value.ToString(DefaultConstants.AiaApiDateFormat);
                        claiment.gender = model.ClaimantDetail.Gender;
                        claiment.email = model.ClaimantDetail.Email;
                        claiment.phone = model.ClaimantDetail.Phone;
                        claiment.idnumber = model.ClaimantDetail.IdValue?.Value;


                        if ((model.ClaimantDetail.IdValue != null && model.ClaimantDetail.IdValue?.Type != null))
                        {
                            if (model.ClaimantDetail.IdValue?.Type == EnumIdenType.Nrc)
                            {
                                claiment.idtype = "N";
                            }
                            else if (model.ClaimantDetail.IdValue?.Type == EnumIdenType.Passport)
                            {
                                claiment.idtype = "X";
                            }
                            else if (model.ClaimantDetail.IdValue?.Type == EnumIdenType.Others)
                            {
                                claiment.idtype = "O";
                            }
                        }

                        claiment.address1 = model.ClaimantDetail?.Address?.BuildingOrUnitNo?? "";
                        claiment.address2 = model.ClaimantDetail?.Address?.StreetName ?? "";
                        claiment.address3 = model.ClaimantDetail?.Address?.TownshipName ?? "";
                        claiment.address4 = model.ClaimantDetail?.Address?.DistrictName ?? "";
                        claiment.address5 = model.ClaimantDetail?.Address?.ProvinceOrCityName ?? "";


                        claiment.country = model.ClaimantDetail?.Address?.CountryCode;
                        claiment.townshipCode = model.ClaimantDetail?.Address?.TownshipCode;

                        claimModel.claiment = claiment;
                    }


                    apiType = EnumILClaimApi.Death;
                }
                else if (claimType == EnumBenefitFormType.TotalPermanentDisability.ToString())
                {
                    MobileErrorLog(
                  $"CommonClaimService => TotalPermanentDisability"
                , $""
                , $""
                , "v1/claim/claim");

                    claimModel.claimReason = byCode; //TODO
                    claimModel.claimId = claimId.ToString();
                    claimModel.policyNumber = policy?.PolicyNo;
                    claimModel.effectiveDate = customILCoastDate.ToString(DefaultConstants.AiaApiDateFormat);

                    if (model.CausedBy != null && model.CausedBy.ByDate != null)
                    {
                        claimModel.dateOfDisability = model.CausedBy.ByDate.Value.ToString(DefaultConstants.AiaApiDateFormat);
                    }

                    claimModel.causeOfDisability = byCode;
                    //claimModel.sourceNotification = "M";
                    claimModel.sourceNotification = "A"; //Eric Requrest
                    claimModel.gratiaIndicator = "N";
                    claimModel.interimCoverage = "N";
                    claimModel.disallowITC = "N";
                    claimModel.soleProprietor = "N";
                    claimModel.purposeOrPersonal = "P";
                    claimModel.relationship = relation?.Code; //TODO
                    claimModel.claimLocation = location?.Code; //TODO
                    claimModel.issuAgeVerified = "Y"; //TODO

                    claimModel.componentID = eligibleComponents;

                    var policyHolder = unitOfWork.GetRepository<Entities.Client>()
                        .Query(x => x.ClientNo == entity.HolderClientNo)
                        .FirstOrDefault();

                    MobileErrorLog("", "IamHere -> TotalPermanentDisability 123", "", "v1/claim/claim");


                    var policyBy_IL_Coast = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => x.PolicyNo == policy.PolicyNo).FirstOrDefault();



                    if (policyHolder != null)
                    {
                        var claiment = new Claiment();
                        claiment.clientNumber = policyBy_IL_Coast?.PolicyHolderClientNo;

                        claiment.name = policyHolder?.Name;

                        if (policyHolder.Dob != null)
                        {
                            claiment.dob = policyHolder.Dob.ToString(DefaultConstants.AiaApiDateFormat);
                        }

                        claiment.gender = policyHolder?.Gender;
                        claiment.email = policyHolder?.Email;
                        claiment.phone = policyHolder?.PhoneNo;
                        claiment.country = "MMR";

                        var idValue = "";
                        var idType = "";

                        if (!string.IsNullOrEmpty(policyHolder.Nrc))
                        {
                            idType = DefaultConstants.IdTypeNrc;
                            idValue = policyHolder.Nrc;
                        }
                        else if (!string.IsNullOrEmpty(policyHolder.PassportNo))
                        {
                            idType = DefaultConstants.IdTypePassport;
                            idValue = policyHolder.PassportNo;
                        }
                        else if (!string.IsNullOrEmpty(policyHolder.Other))
                        {
                            idType = DefaultConstants.IdTypeOther;
                            idValue = policyHolder.Other;
                        }

                        claiment.idnumber = idValue;
                        claiment.idtype = idType;
                        claiment.address1 = ""; //model.ClaimantDetail?.Address;

                        claiment.country = "MMR";

                        claimModel.claiment = claiment;
                    }



                    apiType = EnumILClaimApi.TPD;
                }
                else if (claimType == EnumBenefitFormType.CriticalIllnessBenefit.ToString())
                {
                    MobileErrorLog(
                  $"CommonClaimService => CriticalIllnessBenefit"
                , $""
                , $""
                , "v1/claim/claim");

                    claimModel.claimId = claimId.ToString();
                    claimModel.policyNumber = policy.PolicyNo;
                    claimModel.gratiaIndicator = "N";
                    claimModel.soleProprietor = "N";
                    claimModel.purposeOrPersonal = "P";


                    if (eligibleComponents.Contains("CII0") )
                    {
                        claimModel.ciOrNonhealth = "CI";
                        claimModel.effectiveDate = customILCoastDate.ToString(DefaultConstants.AiaApiDateFormat);

                        if (model.CausedBy != null && model.CausedBy.ByDate != null)
                        {
                            claimModel.dateOfDisability = model.CausedBy.ByDate.Value.ToString(DefaultConstants.AiaApiDateFormat);
                        }

                        claimModel.causeOfDisability = byCode;
                        claimModel.interimCoverage = "N";
                        claimModel.disallowITC = "N";
                        //claimModel.sourceNotification = "M"; //changed A to M
                        claimModel.sourceNotification = "A"; //changed M to A Back
                        claimModel.issuAgeVerified = "Y";

                    }
                    else
                    {
                        claimModel.ciOrNonhealth = "NH";
                        claimModel.claimType = "C";
                        claimModel.componentID = eligibleComponents;
                        claimModel.receiveDate = "";
                        claimModel.incurredDate = "";
                        claimModel.claimReason = byCode; 
                        //claimModel.sourceNotification = "M"; //changed A to M
                        claimModel.sourceNotification = "A"; //changed M to A Back
                        claimModel.interimCoverage = "N";
                        claimModel.disallowITC = "N";
                        claimModel.payee = new Payee
                        {
                            bankNameCode = model.BankDetail?.BankCode
                            ,
                            bankAccountNumber = model.BankDetail?.AccountNumber
                            ,
                            bankAccountName = model.BankDetail?.AccountName
                        };

                        claimModel.receiveDate = customILCoastDate.ToString(DefaultConstants.AiaApiDateFormat);
                        claimModel.incurredDate = model.CausedBy?.ByDate.Value.ToString(DefaultConstants.AiaApiDateFormat);
                    }




                    claimModel.claimLocation = location?.Code; //TODO

                    apiType = EnumILClaimApi.CI;
                }

                MobileErrorLog(
                  $"CommonClaimService => After Created ILCommonClaimRequest"
                , $""
                , $""
                , "v1/claim/claim");

                #region #UpdateClaimTran
                try
                {
                    if (diagnosis != null)
                    {
                        entity.DiagnosisCode = diagnosis?.Code;
                    }
                    else
                    {
                        entity.DiagnosisCode = diagnosisCode;
                    }

                    entity.Ilrequest = JsonConvert.SerializeObject(claimModel);
                    entity.IlrequestOn = Utils.GetDefaultDate();
                    unitOfWork.GetRepository<Entities.ClaimTran>().Update(entity);
                    unitOfWork.SaveChanges();
                }
                catch (Exception ex)
                {
                    MobileErrorLog("update claim tran ex before IL call", ex.Message, JsonConvert.SerializeObject(ex), "v1/claim/claim");
                }
                #endregion

                var systemError = "";
                var SerializeModel = "";

                claimModel.doctorID = AppSettingsHelper.GetSetting("AiaCommon:DoctorID");

                MobileErrorLog("", "BeforeILCommonRegister", "", "v1/claim/claim");

                MobileErrorLog(
                  $"CommonClaimService => Before ILCommonRegister"
                , $""
                , $""
                , "v1/claim/claim");


                Console.WriteLine($"{policy.ProductType}, {policy.PolicyNo}, {Utils.GetDefaultDate().ToString("yyyy-MM-dd HH:mm:ss")}");

                

                var result = aiaILApiService.CommonRegister(claimModel, apiType, out systemError, out SerializeModel);

                #region #UpdateClaimTran
                try
                {
                    entity.Ilrequest = SerializeModel ?? entity.Ilrequest;
                    entity.Ilstatus = result?.data?.status ?? systemError;
                    entity.IlerrorMessage = result?.data?.errorMessage ?? systemError;
                    entity.Ilresponse = result != null ? JsonConvert.SerializeObject(result) : systemError;
                    entity.IlresponseOn = Utils.GetDefaultDate();
                    unitOfWork.GetRepository<Entities.ClaimTran>().Update(entity);
                    unitOfWork.SaveChanges();


                    MobileErrorLog("result?.data?.status", $"{result?.data?.status}"
                                , $"{JsonConvert.SerializeObject(result)}", "v1/claim/claim");
                }
                catch (Exception ex)
                {
                    MobileErrorLog("update claim tran ex after IL call", ex.Message, JsonConvert.SerializeObject(ex), "v1/claim/claim");
                }
                #endregion



                MobileErrorLog(
                  $"CommonClaimService => Before CRM"
                , $""
                , $""
                , "v1/claim/claim");
                #region #CRM API

                try
                {
                    var insurredPerson = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == model.InsuredId).FirstOrDefault();
                    var claimCode = unitOfWork.GetRepository<Entities.CrmClaimCode>().Query(x => x.ClaimType == model.BenefitFormType.ToString()).FirstOrDefault();

                    CaseRequest crmModel = new CaseRequest();
                    crmModel.CustomerInfo = new CustomerInfo();
                    crmModel.CustomerInfo.ClientNumber = model.InsuredId;
                    crmModel.CustomerInfo.FirstName = insurredPerson?.Name;
                    crmModel.CustomerInfo.LastName = insurredPerson?.Name;
                    crmModel.CustomerInfo.Email = insurredPerson?.Email;

                    crmModel.PolicyInfo = new aia_core.Model.AiaCrm.PolicyInfo();
                    crmModel.PolicyInfo.PolicyNumber = policy.PolicyNo;

                    crmModel.RequestInfo = new aia_core.Model.AiaCrm.Request();
                    crmModel.RequestInfo.CaseCategory = claimCode?.ClaimCode ?? "CC034";
                    crmModel.RequestInfo.Channel = "100005"; //"100004";
                    crmModel.RequestInfo.ClaimId = claimId.ToString();
                    crmModel.RequestInfo.CaseType = "Claim";
                    crmModel.RequestInfo.RequestId = claimId.ToString();

                    if(model.BankDetail != null )
                    {
                        crmModel.RequestInfo.BankName = model.BankDetail.BankCode;
                        crmModel.RequestInfo.BankAccountNo = model.BankDetail.AccountNumber;
                        crmModel.RequestInfo.BankAccountName = model.BankDetail.AccountName;
                    }

                    entity.CrmRequestOn = Utils.GetDefaultDate();

                    MobileErrorLog("aiaCrmApiService => CreateCase", "Request"
                        , JsonConvert.SerializeObject(crmModel), "v1/claim/claim");

                    var crmResponse = aiaCrmApiService.CreateCase(crmModel).Result;


                    MobileErrorLog("aiaCrmApiService => CreateCase", "Response"
                        , JsonConvert.SerializeObject(crmResponse), "v1/claim/claim");

                    #region #UpdateClaimTran
                    try
                    {
                        entity.CrmRequest = JsonConvert.SerializeObject(crmModel);
                        entity.CrmResponse = JsonConvert.SerializeObject(crmResponse);
                        entity.CrmResponseOn = Utils.GetDefaultDate();

                        unitOfWork.GetRepository<Entities.ClaimTran>().Update(entity);
                    }
                    catch (Exception ex)
                    {
                        MobileErrorLog("update claim tran ex after Crm call", ex.Message
                            , JsonConvert.SerializeObject(ex), "v1/claim/claim");
                    }
                    #endregion

                }
                catch (Exception ex)
                {
                    MobileErrorLog($"ClaimNow => CrmApi Ex", ex.Message, JsonConvert.SerializeObject(ex), "v1/claim/claim");

                }


                #endregion

                if (result != null && result.data?.status == DefaultConstants.AiaILApiSuccessCode)
                {
                    //DO NOTHING.
                }

                #region #Send Email

                var holder = unitOfWork.GetRepository<Entities.Member>()
                    .Query(x => x.MemberId == memberId && x.IsActive == true && x.IsVerified == true)
                    .FirstOrDefault();

                //var holder = unitOfWork.GetRepository<Entities.Client>()
                //            .Query(x => x.ClientNo == policy.PolicyHolderClientNo)
                //            .FirstOrDefault();

                var holderNrc = string.IsNullOrEmpty(holder?.Nrc)
                                ? (string.IsNullOrEmpty(holder?.Passport)
                                ? (holder?.Others)
                                : holder?.Passport)
                                : holder?.Nrc;

                var insurred = unitOfWork.GetRepository<Entities.Client>()
                    .Query(x => x.ClientNo == policy.InsuredPersonClientNo)
                .FirstOrDefault();

                var insuredNrc = string.IsNullOrEmpty(insurred?.Nrc)
                                ? (string.IsNullOrEmpty(insurred?.PassportNo)
                                ? (insurred?.Other)
                                : insurred?.PassportNo)
                                : insurred?.Nrc;

                var environment = AppSettingsHelper.GetSetting("Deploy:Environment") == "uat" ? "UAT-" : "";

                var product = unitOfWork.GetRepository<Entities.Product>()
                            .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false)
                            .Select(x => x.TitleEn)
                            .FirstOrDefault();

                productName = product ?? "-";

                #region #ClaimRequestEmail
                try
                {
                    var templateName = "claim.html";

                    if (model.BenefitFormType == EnumBenefitFormType.OutpatientAndAmbulatoryCare)
                    {
                        if (policy.ProductType == "MER")
                        {
                            templateName = "claim_OP_MER.html";
                        }
                        else if (policy.ProductType == "OHI")
                        {
                            templateName = "claim_OP_OHI.html";
                        }
                        else if (policy.ProductType == "OHG")
                        {
                            templateName = "claim_OP_OHG.html";
                        }
                    }
                    else if (model.BenefitFormType == EnumBenefitFormType.DentalCare
                        || model.BenefitFormType == EnumBenefitFormType.VisionCare
                        || model.BenefitFormType == EnumBenefitFormType.Vaccination
                        || model.BenefitFormType == EnumBenefitFormType.PhysicalCheckup)
                    {
                        templateName = "claim_Dental-Vision-Vaci-Phy.html";
                    }

                    else if (model.BenefitFormType == EnumBenefitFormType.Inpatient)
                    {
                        if (policy.ProductType == "MER")
                        {
                            templateName = "claim_IP_MER.html";
                        }
                        else if (policy.ProductType == "OHI" || policy.ProductType == "OHG")
                        {
                            templateName = "claim_IP_OHI-OHG.html";
                        }
                    }
                    else if (model.BenefitFormType == EnumBenefitFormType.AcceleratedCancerBenefit)
                    {
                        templateName = "claim_Cancer.html";
                    }
                    else if (model.BenefitFormType == EnumBenefitFormType.DeathAndAccidentalDeath)
                    {
                        templateName = "claim_Death.html";
                    }
                    else if (model.BenefitFormType == EnumBenefitFormType.TotalPermanentDisability
                        || model.BenefitFormType == EnumBenefitFormType.PartialDisabilityAndInjury
                        || model.BenefitFormType == EnumBenefitFormType.CriticalIllnessBenefit)
                    {
                        templateName = "claim_TPD-PD-CI.html";
                    }

                    var path = Path.Combine(
                    this.environment.ContentRootPath, "email_templates/", templateName);

                    var htmlData = File.ReadAllText(path);

                    #region #Sub
                    if (!string.IsNullOrEmpty(htmlData))
                    {
                        
                        try
                        {

                            //var holderAppMember = unitOfWork.GetRepository<Entities.MemberClient>()
                            //    .Query(x => x.ClientNo == policy.PolicyHolderClientNo
                            //    || (x.Member.Nrc == holderNrc || x.Member.Passport == holderNrc || x.Member.Others == holderNrc))
                            //    .Include(x => x.Member)
                            //    .Select(x => new { x.Member.Email, x.Member.Mobile })
                            //    .FirstOrDefault();


                            
                        }
                        catch { }

                        //Holder
                        htmlData = htmlData.Replace("{HolderName}", holder?.Name);
                        htmlData = htmlData.Replace("{HolderEmail}", holder?.Email ?? "-");
                        htmlData = htmlData.Replace("{HolderPhone}", holder?.Mobile ?? "-");
                        if (holder?.Dob != null)
                        {
                            htmlData = htmlData.Replace("{HolderDob}", holder?.Dob.Value.ToString("yyyy-MM-dd"));

                        }


                        
                        htmlData = htmlData.Replace("{HolderGender}", holder?.Gender);
                        htmlData = htmlData.Replace("{HolderNrc}", holderNrc);
                        

                        //Claim



                        htmlData = htmlData.Replace("{PolicyNo}", policy.PolicyNo ?? "-");
                        htmlData = htmlData.Replace("{ProductName}", product ?? "-");
                        htmlData = htmlData.Replace("{ClaimType}", claimFormType?.BenefitNameEn ?? "-");
                        htmlData = htmlData.Replace("{BankName}", bank?.BankName ?? "-");
                        htmlData = htmlData.Replace("{AccountNo}", model.BankDetail?.AccountNumber ?? "-");
                        htmlData = htmlData.Replace("{AccountName}", model.BankDetail?.AccountName ?? "-");

                        htmlData = htmlData.Replace("{DiagnosisName}", diagnosis?.Name);

                        #region #CI,TPD,PD
                        if (model.BenefitFormType == EnumBenefitFormType.TotalPermanentDisability
                        || model.BenefitFormType == EnumBenefitFormType.PartialDisabilityAndInjury
                        || model.BenefitFormType == EnumBenefitFormType.CriticalIllnessBenefit)
                        {
                            if (model.CausedBy != null && model.CausedBy.ByType != null && model.CausedBy.ByDate != null) //CI, Death/ TPD, PD
                            {
                                var diagnosisNameLbl = "";
                                var treatmentDateLbl = "";

                                if (model.CausedBy.ByType == EnumCauseByType.PartialDisability)
                                {
                                    diagnosisNameLbl = "Cause of injury";
                                    treatmentDateLbl = "Date of injury";

                                    var diagnosisName = unitOfWork.GetRepository<Entities.PartialDisability>()
                                        .Query(x => x.IsDelete == false && x.IsActive == true && x.ID == Guid.Parse(model.CausedBy.ById))
                                        .Select(x => x.Name)
                                        .FirstOrDefault();

                                    htmlData = htmlData.Replace("{DiagnosisNameValue}", diagnosisName ?? "");
                                }
                                else if (model.CausedBy.ByType == EnumCauseByType.PermanentDisability)
                                {
                                    diagnosisNameLbl = "Cause of disability";
                                    treatmentDateLbl = "Date of disability";

                                    var diagnosisName = unitOfWork.GetRepository<Entities.PermanentDisability>()
                                        .Query(x => x.IsDelete == false && x.IsActive == true && x.ID == Guid.Parse(model.CausedBy.ById))
                                        .Select(x => x.Name)
                                        .FirstOrDefault();

                                    htmlData = htmlData.Replace("{DiagnosisNameValue}", diagnosisName ?? "");
                                }
                                else if (model.CausedBy.ByType == EnumCauseByType.CriticalIllness)
                                {
                                    diagnosisNameLbl = "Cause of criticle illness";
                                    treatmentDateLbl = "Date of criticle illness";

                                    var diagnosisName = unitOfWork.GetRepository<Entities.CriticalIllness>()
                                        .Query(x => x.IsDelete == false && x.IsActive == true && x.ID == Guid.Parse(model.CausedBy.ById))
                                        .Select(x => x.Name)
                                        .FirstOrDefault();

                                    htmlData = htmlData.Replace("{DiagnosisNameValue}", diagnosisName ?? "");
                                }

                                htmlData = htmlData.Replace("{DiagnosisNameText}", diagnosisNameLbl);
                                htmlData = htmlData.Replace("{TreatmentDateText}", treatmentDateLbl);

                                htmlData = htmlData.Replace("{DiagnosisNameValue}", byNameEn?? entity?.CausedByNameEn);
                                htmlData = htmlData.Replace("{TreatmentDateValue}", model.CausedBy.ByDate.Value.ToString(DefaultConstants.AiaApiDateFormat));


                                
                            }
                        }
                        
                        #endregion
                        #region #Cancer
                        else if (model.BenefitFormType == EnumBenefitFormType.AcceleratedCancerBenefit)
                        {
                            

                            if (model.TreatmentDetail != null && model.TreatmentDetail.TreatmentDates != null && model.TreatmentDetail.TreatmentDates.Any())
                            {

                                htmlData = htmlData.Replace("{TreatmentDate}", model.TreatmentDetail?.TreatmentDates.First().ToString(DefaultConstants.AiaApiDateFormat));
                            }
                        }
                        #endregion
                        #region #MaternityCare
                        else if (model.BenefitFormType == EnumBenefitFormType.MaternityCare)
                        {
                            if (model.TreatmentDetail != null && model.TreatmentDetail.TreatmentFromDate != null && model.TreatmentDetail.TreatmentToDate != null)
                            {
                                var fromDt = model.TreatmentDetail.TreatmentFromDate.Value.ToString(DefaultConstants.AiaApiDateFormat);
                                var toDt = model.TreatmentDetail.TreatmentToDate.Value.ToString(DefaultConstants.AiaApiDateFormat);

                                htmlData = htmlData.Replace("{TreatmentDate}", $"{fromDt} to {toDt}");
                            }

                            htmlData = htmlData.Replace("{TotalIncurredAmount}", $"{string.Format("{0:N0}", model.TreatmentDetail?.IncurredAmount ?? 0)} MMK");
                        }
                        #endregion
                        #region #DentalCare,VisionCare,Vaccination,PhysicalCheckup
                        else if (model.BenefitFormType == EnumBenefitFormType.DentalCare
                            || model.BenefitFormType == EnumBenefitFormType.VisionCare
                            || model.BenefitFormType == EnumBenefitFormType.Vaccination
                            || model.BenefitFormType == EnumBenefitFormType.PhysicalCheckup)
                        {
                            if (model.TreatmentDetail != null && model.TreatmentDetail.TreatmentDates != null
                            && model.TreatmentDetail.TreatmentDates.Any())
                            {
                                var strdates = model.TreatmentDetail.TreatmentDates.Order().Select(x => x.ToString(DefaultConstants.AiaApiDateFormat)).ToList();

                                htmlData = htmlData.Replace("{TreatmentDate}", string.Join(", ", strdates));
                            }

                            if (model.TreatmentDetail?.IncurredAmount != null)
                            {
                                var amount = $"{string.Format("{0:N0}", model.TreatmentDetail?.IncurredAmount)} MMK";
                                htmlData = htmlData.Replace("{TotalIncurredAmount}", amount ?? "-");
                            }
                        }
                        else if (model.BenefitFormType == EnumBenefitFormType.OutpatientAndAmbulatoryCare)
                        {
                            htmlData = htmlData.Replace("{DiagnosisName}", diagnosis?.Name);

                            if (policy.ProductType == "MER")
                            {
                                htmlData = htmlData.Replace("{TreatmentCount}", (model.TreatmentDetail?.TreatmentCount ?? 0).ToString());

                                if (model.TreatmentDetail != null && model.TreatmentDetail.TreatmentDates != null
                            && model.TreatmentDetail.TreatmentDates.Any())
                                {
                                    var strdates = model.TreatmentDetail.TreatmentDates.Order().Select(x => x.ToString(DefaultConstants.AiaApiDateFormat)).ToList();


                                    htmlData = htmlData.Replace("{TreatmentDate}", string.Join(", ", strdates));
                                }
                            }
                            else if (policy.ProductType == "OHI")
                            {
                                if (model.TreatmentDetail != null && model.TreatmentDetail.TreatmentFromDate != null)
                                {
                                    htmlData = htmlData.Replace("{TreatmentDate}", model.TreatmentDetail.TreatmentFromDate.Value.ToString(DefaultConstants.AiaApiDateFormat));
                                }

                                htmlData = htmlData.Replace("{TotalIncurredAmount}", $"{string.Format("{0:N0}", model.TreatmentDetail?.IncurredAmount ?? 0)} MMK");
                            }
                            else if (policy.ProductType == "OHG" && benefitList != null && benefitList.Any())
                            {
                                foreach(var benefit  in benefitList) 
                                {
                                    var formatted = $"{benefit?.FromDate?.ToString(DefaultConstants.AiaApiDateFormat)} to {benefit?.ToDate?.ToString(DefaultConstants.AiaApiDateFormat)}";
                                    formatted = $"{formatted} ({string.Format("{0:N0}", benefit.TotalAmount ?? 0)} MMK)";

                                    htmlData = htmlData.Replace("{" + benefit.BenefitName + "}", formatted);

                                }

                                #region #Remove Uncontained Benefit PlaceHolder
                                if (htmlData.Contains("{Out-Patient Diagnostics/Lab}"))
                                {
                                    htmlData = htmlData.Replace("{Hide1}", "hidden-row");
                                } 
                                if (htmlData.Contains("{Out-Patient Local Specialist Practitioner}"))
                                {
                                    htmlData = htmlData.Replace("{Hide2}", "hidden-row");
                                }
                                if (htmlData.Contains("{Out-Patient Overseas Specialist Practitioner}"))
                                {
                                    htmlData = htmlData.Replace("{Hide3}", "hidden-row");
                                }
                                if (htmlData.Contains("{Out-Patient Local General Practitioner}"))
                                {
                                    htmlData = htmlData.Replace("{Hide4}", "hidden-row");
                                }
                                if (htmlData.Contains("{Out-Patient Overseas General Practitioner}"))
                                {
                                    htmlData = htmlData.Replace("{Hide5}", "hidden-row");
                                }
                                if (htmlData.Contains("{Ambulatory Care}"))
                                {
                                    htmlData = htmlData.Replace("{Hide6}", "hidden-row");
                                }
                                
                                #endregion
                            }
                        }
                        else if (model.BenefitFormType == EnumBenefitFormType.Inpatient)
                        {
                            htmlData = htmlData.Replace("{DiagnosisName}", diagnosis?.Name);

                            if (policy.ProductType == "MER" && benefitList != null && benefitList.Any())
                            {
                                foreach (var benefit in benefitList)
                                {
                                    var formatted = $"{benefit?.FromDate?.ToString(DefaultConstants.AiaApiDateFormat)} to {benefit?.ToDate?.ToString(DefaultConstants.AiaApiDateFormat)}";
                                    

                                    //formatted = $"{formatted} ({string.Format("{0:N0}", benefit.TotalAmount ?? 0)} MMK)";

                                    htmlData = htmlData.Replace("{" + benefit.BenefitName + "}", formatted);
                                }

                                #region #Remove Uncontained Benefit PlaceHolder
                                if (htmlData.Contains("{Hospitalization for medical treatment}"))
                                {
                                    htmlData = htmlData.Replace("{Hide1}", "hidden-row");
                                }
                                if (htmlData.Contains("{Miscarriage Expenses}"))
                                {
                                    htmlData = htmlData.Replace("{Hide2}", "hidden-row");
                                }
                                if (htmlData.Contains("{Surgical Expenses}"))
                                {
                                    htmlData = htmlData.Replace("{Hide3}", "hidden-row");
                                }
                                #endregion
                            }
                            else if (policy.ProductType == "OHI" || policy.ProductType == "OHG")
                            {
                                foreach (var benefit in benefitList)
                                {
                                    var formatted = $"{benefit?.FromDate?.ToString(DefaultConstants.AiaApiDateFormat)} to {benefit?.ToDate?.ToString(DefaultConstants.AiaApiDateFormat)}";
                                    
                                    formatted = $"{formatted} ({string.Format("{0:N0}", benefit.TotalAmount ?? 0)} MMK)";

                                    htmlData = htmlData.Replace("{" + benefit.BenefitName + "}", formatted);
                                }

                                #region #Remove Uncontained Benefit PlaceHolder
                                if (htmlData.Contains("{Room & Board}"))
                                {
                                    htmlData = htmlData.Replace("{Hide1}", "hidden-row");
                                }
                                if (htmlData.Contains("{Other hospitalization related fees}"))
                                {
                                    htmlData = htmlData.Replace("{Hide2}", "hidden-row");
                                }
                                #endregion
                            }
                        }
                        
                        
                        else if (model.BenefitFormType == EnumBenefitFormType.DeathAndAccidentalDeath)
                        {
                            htmlData = htmlData.Replace("{ClaimantName}", model.ClaimantDetail?.Name);
                            htmlData = htmlData.Replace("{ClaimantDob}", model.ClaimantDetail?.Dob != null ?
                                model.ClaimantDetail?.Dob.Value.ToString(DefaultConstants.AiaApiDateFormat) : "");
                            
                            htmlData = htmlData.Replace("{ClaimantGender}", model.ClaimantDetail?.Gender?? "");
                            htmlData = htmlData.Replace("{ClaimantIden}", model.ClaimantDetail?.IdValue?.Value?? "");
                            htmlData = htmlData.Replace("{ClaimantPh}", model.ClaimantDetail?.Phone ?? "");
                            htmlData = htmlData.Replace("{ClaimantEmail}", model.ClaimantDetail?.Email?? "");
                            htmlData = htmlData.Replace("{ClaimantAddress}",  "");
                            
                            htmlData = htmlData.Replace("{ClaimantProvince}", model.ClaimantDetail?.Address?.ProvinceOrCityName ?? "");
                            htmlData = htmlData.Replace("{ClaimantDistinct}", model.ClaimantDetail?.Address?.DistrictName ?? "");
                            htmlData = htmlData.Replace("{ClaimantTsp}", model.ClaimantDetail?.Address?.TownshipName ?? "");
                            htmlData = htmlData.Replace("{ClaimantBuilding}", model.ClaimantDetail?.Address?.BuildingOrUnitNo ?? "");
                            htmlData = htmlData.Replace("{ClaimantStreet}", model.ClaimantDetail?.Address?.StreetName ?? "");

                            
                            if (!string.IsNullOrEmpty(model.ClaimantDetail?.Address?.CountryCode))
                            {
                                var countryName = unitOfWork.GetRepository<Entities.Country>()
                                    .Query(x => x.code == model.ClaimantDetail.Address.CountryCode)
                                    .Select(x => x.description)
                                    .FirstOrDefault();

                                htmlData = htmlData.Replace("{ClaimantCountry}", countryName ?? model.ClaimantDetail?.Address?.CountryCode);
                            }


                            

                            if (!string.IsNullOrEmpty(model.CausedBy?.ById))
                            {

                                var deathName = unitOfWork.GetRepository<Entities.Death>()
                                    .Query(x => x.IsDelete == false && x.IsActive == true && x.ID == Guid.Parse(model.CausedBy.ById))
                                    .Select(x => x.Name)
                                    .FirstOrDefault();

                                MobileErrorLog("Death claim", $"deathName => {deathName}"
                                , $"", "v1/claim/claim");

                                htmlData = htmlData.Replace("{{CausedOfDeath}}", deathName ?? model.CausedBy?.ById);
                            }
                            

                            if (!string.IsNullOrEmpty(model.ClaimantDetail?.Relationship))
                            {
                                var rsName = unitOfWork.GetRepository<Entities.Relationship>()
                                    .Query(x => x.IsDelete == false && x.IsActive == true && x.ID == new Guid(model.ClaimantDetail.Relationship))
                                    .Select(x => x.Name)
                                    .FirstOrDefault();

                                htmlData = htmlData.Replace("{ClaimantRs}", rsName ?? model.ClaimantDetail?.Relationship);
                            }
                            

                            var deathDate = model.TreatmentDetail?.TreatmentDates?.First() ??
                                model.TreatmentDetail?.TreatmentFromDate ?? Utils.GetDefaultDate();

                            htmlData = htmlData.Replace("{TreatmentDate}", deathDate != null ? deathDate.ToString(DefaultConstants.AiaApiDateFormat) : "");
                        }
                        #endregion



                        

                        //Insurred
                        htmlData = htmlData.Replace("{PolicyNo}", policy.PolicyNo ?? "-");
                        htmlData = htmlData.Replace("{InsurredNo}", policy.InsuredPersonClientNo ?? "-");
                        htmlData = htmlData.Replace("{InsurredName}", insurred?.Name ?? "-");
                        htmlData = htmlData.Replace("{InsurredGender}", Utils.GetGender(insurred?.Gender) ?? "-");
                        htmlData = htmlData.Replace("{InsurredNrc}", insuredNrc ?? "-");
                        htmlData = htmlData.Replace("{InsurredDob}", insurred?.Dob.ToString("yyyy-MM-dd") ?? "-");
                        htmlData = htmlData.Replace("{InsurredEmail}", insurred?.Email ?? "-");
                        htmlData = htmlData.Replace("{InsurredPhone}", insurred?.PhoneNo ?? "-");
                        htmlData = htmlData.Replace("{InsurredFather}", insurred?.FatherName ?? "-");
                        htmlData = htmlData.Replace("{InsurredAddress}", $"{insurred?.Address1},{insurred?.Address2},{insurred?.Address3},{insurred?.Address4},{insurred?.Address5}");

                        string signatureBase64 = azureStorage.GetBase64ByFileName(entity.SignatureImage).Result;
                        string signatureMimeType = "image/jpg";
                        string signatureDataUrl = $"data:{signatureMimeType};base64,{signatureBase64}";
                        htmlData = htmlData.Replace("{signDataUrl}", signatureDataUrl ?? "");


                        #region #ClearThePalceHolder
                        if (htmlData.Contains("{TreatmentDate}"))
                        {
                            htmlData = htmlData.Replace("{TreatmentDate}", "");
                        }
                        if (htmlData.Contains("{TreatmentCount}"))
                        {
                            htmlData = htmlData.Replace("{TreatmentCount}", "");
                        }
                        if (htmlData.Contains("{TotalIncurredAmount}"))
                        {
                            htmlData = htmlData.Replace("{TotalIncurredAmount}", "");
                        }
                        #endregion

                        MobileErrorLog("Generated claim html", ""
                            , htmlData, "v1/claim/claim");

                        try
                        {
                            var htmlToPdfDocument = new HtmlToPdfDocument()
                            {
                                GlobalSettings = {
                                ColorMode = ColorMode.Color,
                                Orientation = Orientation.Portrait,
                                PaperSize = PaperKind.A4Extra,
                            },
                                Objects = {
                                    new ObjectSettings()
                                    {
                                        PagesCount = true,
                                        HtmlContent = htmlData  ,
                                        WebSettings = { DefaultEncoding = "utf-8" },
                                        HeaderSettings =
                                        { FontSize = 9, Right = "Page [page] of [toPage]", Line = true, Spacing = 2.812 }

                                    }

                                }
                            };

                            pdfData = this.converter.Convert(htmlToPdfDocument);

                            MobileErrorLog("Dink2Pdf Convert", "pdfData?.Length"
                                , $"{pdfData?.Length}", "v1/claim/claim");
                        }
                        catch (Exception ex)
                        {
                            MobileErrorLog("Dink2Pdf exception", ex.Message
                                , JsonConvert.SerializeObject(ex), "v1/claim/claim");
                        }

                        var pdfFileName = $"{environment}-{policy.PolicyNo}-{claimType}-{holder?.Name}-{policy.PolicyHolderClientNo}.pdf";
                        if (pdfData != null && pdfData.Any())
                        {
                            var attachments = new List<EmailAttachment>();
                            var attachment = new EmailAttachment
                            {
                                Data = pdfData,
                                FileName = pdfFileName,
                            };

                            attachments.Add(attachment);

                            var claimEmail = unitOfWork.GetRepository<Entities.AppConfig>()
                                .Query()
                                .Select(x => x.ClaimEmail)
                                .FirstOrDefault();

                            var memberType = GetMemberType();
                            var emailSubject = "";

                            
                            var bodyText = $"";
                            bodyText += $"<p><strong>IL Status:</strong> {entity.Ilstatus}</p>";
                            bodyText += $"<p><strong>IL Error Message:</strong> {(entity.Ilstatus != "success" ? entity.IlerrorMessage : "")}</p>";
                            bodyText += $"<p><strong>Unique ID:</strong> {claimId}</p>";
                            bodyText += $"<p><strong>Bank Account Holder Name:</strong> {model.BankDetail?.AccountName}</p>";
                            bodyText += $"<p><strong>Bank Account No:</strong> {model.BankDetail?.AccountNumber}</p>";
                            bodyText += $"<p><strong>Bank Name:</strong> {bank?.BankName}</p>";

                            bodyText += "<br><br><br><br><br>"; 

                            if (memberType == EnumIndividualMemberType.Ruby)
                            {
                                emailSubject = $"{policy.PolicyNo} / {claimType} / {insurred?.Name} / {holder?.Name}";

                                var paragraph = $"<p><strong>{emailSubject}</strong></p>";
                                var emailBody = $"<html><body>{paragraph}{bodyText}</body></html>";

                                Utils.SendClaimEmail(claimEmail, emailSubject, emailBody
                                    , attachments);
                            }
                            else
                            {
                                emailSubject = $"{policy.PolicyNo} / {claimType} / {insurred?.Name} / {holder?.Name}";

                                var paragraph = $"<p><strong>{emailSubject}</strong></p>";
                                var emailBody = $"<html><body>{paragraph}{bodyText}</body></html>";

                                Utils.SendClaimEmail(claimEmail, emailSubject, emailBody
                                    , attachments);

                            }

                        }
                    }
                    #endregion


                }
                catch (Exception ex)
                {
                    MobileErrorLog("SendClaimEmail exception", ex.Message
                            , JsonConvert.SerializeObject(ex), "v1/claim/claim");
                }
                #endregion

                #endregion



                #region #UpdateClaimTran
                try
                {
                    entity.ClaimStatus = "Received"; //"RC";
                    entity.ClaimStatusCode = "RC";

                    var claimContact = GetProgressAndContactHour(entity.TransactionDate.Value);
                    entity.ProgressAsHours = claimContact?.Hours;
                    entity.ProgressAsPercent = claimContact?.Percent;
                    
                    unitOfWork.GetRepository<Entities.ClaimTran>().Update(entity);
                }
                catch (Exception ex)
                {
                    MobileErrorLog("update claim tran ex after Crm call", ex.Message
                        , JsonConvert.SerializeObject(ex), "v1/claim/claim");
                }
                #endregion

                var docList = unitOfWork.GetRepository<Entities.ClaimDocument>().Query(x => x.MainClaimId == mainClaimId && x.ClaimId == claimId).ToList();

                MobileErrorLog(
                  $"CommonClaimService => Before CMS"
                , $""
                , $""
                , "v1/claim/claim");

                #region #SendPdfToCMS
                if (pdfData != null && pdfData.Any())
                {

                    try
                    {
                        #region #Upload to Storage

                        //G001742H04-CLMF001-AIA+_7080504301_2025-09-03_09_20_47.pdf
                        var docName2 = $"{policy?.PolicyNo}_{DefaultConstants.DocTypeIdForEmailPdf}_AIA+_{policy.PolicyHolderClientNo}_{(Utils.GetDefaultDate().ToString("yyyy-MM-dd_HH_mm_ss"))}.pdf";

                        //var pdfUpload = azureStorage.UploadBase64Async(docName, pdfData).Result; //TODO
                        var pdfUpload = azureStorage.UploadBase64Async(docName2, pdfData).Result; //TODO

                        if (pdfUpload != null && pdfUpload.Code == 200)
                        {
                            var format = ".pdf";
                            var upload = new UploadBase64Request();
                            upload.docTypeId = $"{DefaultConstants.DocTypeIdForEmailPdf}";
                            upload.PolicyNo = policy.PolicyNo.Substring(0, 10);
                            //upload.FileName = fileName;
                            upload.format = format;
                            upload.templateId = AppSettingsHelper.GetSetting("AiaCmsApi:TemplateId");

                            string base64String = Convert.ToBase64String(pdfData);

                            var dataUrl = "";
                            if (!base64String.StartsWith("data:application/pdf;base64,"))
                            {
                                dataUrl = $"data:application/pdf;base64,{base64String}";
                            }
                            else
                            {
                                dataUrl = base64String;
                            }

                            upload.file = dataUrl;
                            upload.fileName = docName2; //TODO

                            var pdfClaimDoc = new Entities.ClaimDocument
                            {
                                Id = Guid.NewGuid(),
                                ClaimId = claimId,
                                MainClaimId = mainClaimId,
                                UploadId = Guid.NewGuid(),
                                DocName = docName2,
                                DocName2 = docName2, //TODO
                                DocTypeId = DefaultConstants.DocTypeIdForEmailPdf,
                                
                                CmsRequestOn = Utils.GetDefaultDate(),
                                DocTypeName = "Claim Request Form",
                            };

                            upload.membershipId = policy.PolicyHolderClientNo;
                            upload.claimId = claimId;

                            unitOfWork.GetRepository<Entities.ClaimDocument>().Add(pdfClaimDoc);
                            unitOfWork.SaveChanges();

                            var uploadResult = aiaCmsApiService.UploadBase64(upload).Result;

                            pdfClaimDoc.CmsResponse = upload != null ? JsonConvert.SerializeObject(uploadResult) : "";
                            pdfClaimDoc.CmsResponseOn = Utils.GetDefaultDate();
                            pdfClaimDoc.UploadStatus = uploadResult?.msg;

                            unitOfWork.SaveChanges();

                            MobileErrorLog($"ClaimNow => SendPdfToCMS Success", "", ""
                            , "v1/claim/claim");
                        }
                        #endregion

                        

                    }
                    catch (Exception ex)
                    {
                        MobileErrorLog($"ClaimNow => SendPdfToCMS Exception", "", JsonConvert.SerializeObject(ex)
                            , "v1/claim/claim");
                    }
                }
                #endregion



                foreach (var doc in docList)
                {

                    try
                    {
                        var fileName = doc.DocName;
                        var format = ".pdf";

                        #region #GetFileExt
                        // Use Path.GetExtension to get the file extension
                        string fileExtension = Path.GetExtension(fileName);

                        // Remove the leading dot (.) from the extension
                        if (!string.IsNullOrEmpty(fileExtension))
                        {
                            fileExtension = fileExtension.TrimStart('.');
                        }
                        #endregion

                        format = fileExtension;

                        var upload = new UploadBase64Request();
                        upload.docTypeId = doc.DocTypeId;

                        upload.PolicyNo = policy.PolicyNo.Substring(0, 10);

                        //upload.FileName = fileName; 
                        //G001742H04-CLMF001-AIA+_7080504301_2025-09-03_09_20_47.png
                        fileName = $"{policy?.PolicyNo}_{doc.DocTypeId}_AIA+_{policy.PolicyHolderClientNo}_{(Utils.GetDefaultDate().ToString("yyyy-MM-dd_HH_mm_ss"))}.{fileExtension}";
                        upload.fileName = fileName;

                        upload.format = format;
                        upload.templateId = AppSettingsHelper.GetSetting("AiaCmsApi:TemplateId");

                        MobileErrorLog($"CommonClaimService doc.DocName", doc.DocName, doc.DocName2, "v1/claim/claim");

                        string base64EncodedPDF = azureStorage.GetBase64ByFileName(doc.DocName).Result;

                        
                        MobileErrorLog($"CommonClaimService base64EncodedPDF", base64EncodedPDF, "", "v1/claim/claim");

                        // Construct the data URI



                        // Construct the data URI
                        string dataURI = "";
                        //string dataURI = $"data:application/pdf;base64,{base64EncodedPDF}";
                        if (format == "jpeg" || format == "jpg")
                        {
                            dataURI = $"data:image/jpeg;base64,{base64EncodedPDF}";
                        }
                        else if (format == "png")
                        {
                            dataURI = $"data:image/png;base64,{base64EncodedPDF}";
                        }
                        else if (format == "pdf")
                        {
                            dataURI = $"data:application/pdf;base64,{base64EncodedPDF}";
                        }

                        

                        // Output the data URI
                        Console.WriteLine(dataURI);

                        upload.file = dataURI;
                        upload.membershipId = policy.PolicyHolderClientNo;
                        upload.claimId = claimId;

                        doc.CmsRequestOn = Utils.GetDefaultDate();
                        
                        doc.DocName2 = fileName;

                        unitOfWork.GetRepository<Entities.ClaimDocument>().Update(doc);
                        unitOfWork.SaveChanges();

                        var uploadResult = aiaCmsApiService.UploadBase64(upload).Result;

                        doc.UploadStatus = uploadResult?.msg;
                        doc.CmsResponseOn = Utils.GetDefaultDate();
                        doc.CmsResponse = uploadResult != null ? JsonConvert.SerializeObject(uploadResult) : null;



                        Console.WriteLine($"Cms API => doc.DocName => {doc.DocName}");
                        Console.WriteLine($"Cms API => doc.DocName => {doc.DocName}");
                        unitOfWork.GetRepository<Entities.ClaimDocument>().Update(doc);
                        unitOfWork.SaveChanges();


                        //#region #Change the file name in Drive
                        //try
                        //{
                        //    azureStorage.RenameFile(doc.DocName, doc.DocName2);
                        //}
                        //catch (Exception ex) 
                        //{
                        //    MobileErrorLog($"RenameFile Exception", ex.Message, JsonConvert.SerializeObject(ex), "v1/claim/claim");

                        //}



                        //#endregion

                    }
                    catch (Exception ex)
                    {
                        MobileErrorLog($"ClaimNow => UploadBase64 Ex {mainClaimId}{claimId}{doc.UploadId}", ex.Message, JsonConvert.SerializeObject(ex), "v1/claim/claim");
                    }

                }

                
                


                MobileErrorLog(
                  $"CommonClaimService => SendClaimNoti"
                , $""
                , $""
                , "v1/claim/claim");

                try 
                {

                    notificationService.SendClaimNoti(memberId.Value, claimId, EnumClaimStatus.RC, claimFormType?.BenefitNameEn);

                    

                    

                }
                catch (Exception ex) {
                    MobileErrorLog($"SendClaimNoti => Exc {mainClaimId}{claimId}", ex.Message, JsonConvert.SerializeObject(ex), "v1/claim/claim");
                }


                #region #AiaOcrApi

                try
                {
                    var medicalBillList = docList
                                            .Where(doc => doc.DocTypeId == DefaultConstants.CLAIM_MEDICAL_BILL_DOCTYPEID
                                            || doc.DocTypeId == DefaultConstants.CLAIM_MEDICAL_RECORD_DOCTYPEID)
                                            .ToList();

                    if (medicalBillList?.Any() == true)
                    {
                        var oCRFileModels = new List<OCRFileModel>();
                        foreach (var doc in medicalBillList)
                        {

                            string fileContent = await azureStorage.GetBase64ByFileName(doc.DocName);

                            // Convert base64 to byte array
                            byte[] imageBytes = Convert.FromBase64String(fileContent);

                            Console.WriteLine($"OCR => doc.DocName => {doc.DocName}");
                            Console.WriteLine($"OCR => doc.DocName => {doc.DocName}, Length => {imageBytes.Length}");


                            //streamDictionary.Add(doc.DocName, imageBytes);

                            oCRFileModels.Add(new OCRFileModel
                            {
                                FileName = doc.DocName,
                                FileType = doc.DocTypeId.ToString(),
                                FileContent = imageBytes,
                            });
                        }

                        var ocr = ocrApiService.SendDocs(claimId, hospital.Name, oCRFileModels);
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AiaOcrApi error => {claimId} {ex.Message} {JsonConvert.SerializeObject(ex)}");
                }
                #endregion




                #endregion


            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
                try
                {
                    var entity = unitOfWork.GetRepository<Entities.ClaimTran>().Query(x => x.ClaimId == claimId).FirstOrDefault();
                    if (entity != null)
                    {
                        entity.Ilresponse = JsonConvert.SerializeObject(ex);
                        entity.IlresponseOn = Utils.GetDefaultDate();
                        entity.Ilstatus = ex.Message;
                        unitOfWork.SaveChanges();
                    }
                }
                catch { }


                Console.WriteLine($"CommonClaimService => Exception => {ex?.Message}, ex => {JsonConvert.SerializeObject(ex)}");

                
                MobileErrorLog(
                  $"CommonClaimService => Exception => {ex?.Message}"
                , $"CommonClaimService => ex => {JsonConvert.SerializeObject(ex)}"
                , $""
                , "v1/claim/claim");
            }

        }

        public static string ConvertGuidToUserCode(Guid guid)
        {
            // Convert the Guid to a base64-encoded string
            string base64String = Convert.ToBase64String(guid.ToByteArray());

            // Remove non-alphanumeric characters
            string alphanumericString = base64String.Replace("+", "").Replace("/", "");

            // Truncate the string to 10 characters
            return alphanumericString.Substring(0, 10);
        }        


        public static List<Benefit> CreateBenefit(EnumBenefitFormType claimType, ClaimNowRequest model)
        {
            var benefitList = new List<Benefit>();

            if (claimType == EnumBenefitFormType.DentalCare)
            {

                var benefit = new Benefit();
                benefit.benefitCode = ClaimBenefitCode.DentalCare;

                if (model.TreatmentDetail != null
                    && model.TreatmentDetail.TreatmentDates != null
                    && model.TreatmentDetail.TreatmentDates.Any())
                {
                    var benefitDates = model.TreatmentDetail.TreatmentDates
                        .OrderBy(x => x).ToList();

                    if (benefitDates != null)
                    {
                        benefit.dateFromDt = benefitDates.First();
                        benefit.dateToDt = benefitDates.Last();                        
                        benefit.Amount = model.TreatmentDetail?.IncurredAmount ?? 0;
                    }
                }

                benefitList.Add(benefit);

            }
            else if (claimType == EnumBenefitFormType.VisionCare)
            {
                var benefit = new Benefit();
                benefit.benefitCode = ClaimBenefitCode.VisionCare;

                if (model.TreatmentDetail != null
                    && model.TreatmentDetail.TreatmentDates != null
                    && model.TreatmentDetail.TreatmentDates.Any())
                {
                    var benefitDates = model.TreatmentDetail.TreatmentDates
                        .OrderBy(x => x).ToList();

                    if (benefitDates != null)
                    {
                        benefit.dateFromDt = benefitDates.First();
                        benefit.dateToDt = benefitDates.Last();
                        benefit.Amount = model.TreatmentDetail?.IncurredAmount ?? 0;
                    }
                }

                benefitList.Add(benefit);
            }
            else if (claimType == EnumBenefitFormType.MaternityCare)
            {
                var dateFrom = (model.TreatmentDetail != null && model.TreatmentDetail.TreatmentFromDate != null) ? model.TreatmentDetail.TreatmentFromDate.Value : Utils.GetDefaultDate();
                var dateTo = (model.TreatmentDetail != null && model.TreatmentDetail.TreatmentToDate != null) ? model.TreatmentDetail.TreatmentToDate.Value : Utils.GetDefaultDate();

                benefitList.Add(new Benefit
                {
                    benefitCode = ClaimBenefitCode.MaternityCare,

                    dateFromDt = dateFrom,
                    dateToDt = dateTo,
                    Amount = model.TreatmentDetail?.IncurredAmount ?? 0,
                });

            }
            else if (claimType == EnumBenefitFormType.PhysicalCheckup)
            {
                var benefit = new Benefit();
                benefit.benefitCode = ClaimBenefitCode.PhysicalCheckup;

                if (model.TreatmentDetail != null
                    && model.TreatmentDetail.TreatmentDates != null
                    && model.TreatmentDetail.TreatmentDates.Any())
                {
                    var benefitDates = model.TreatmentDetail.TreatmentDates
                        .OrderBy(x => x).ToList();

                    if (benefitDates != null)
                    {
                        benefit.dateFromDt = benefitDates.First();
                        benefit.dateToDt = benefitDates.Last();
                        benefit.Amount = model.TreatmentDetail?.IncurredAmount ?? 0;
                    }
                }

                benefitList.Add(benefit);
            }
            else if (claimType == EnumBenefitFormType.Vaccination)
            {
                var benefit = new Benefit();
                benefit.benefitCode = ClaimBenefitCode.Vaccination;

                if (model.TreatmentDetail != null
                    && model.TreatmentDetail.TreatmentDates != null
                    && model.TreatmentDetail.TreatmentDates.Any())
                {
                    var benefitDates = model.TreatmentDetail.TreatmentDates
                        .OrderBy(x => x).ToList();

                    if (benefitDates != null)
                    {
                        benefit.dateFromDt = benefitDates.First();
                        benefit.dateToDt = benefitDates.Last();
                        benefit.Amount = model.TreatmentDetail?.IncurredAmount ?? 0;
                    }
                }

                benefitList.Add(benefit);
            }


            return benefitList;
        }

        public async Task<ResponseModel<string>> FollowupClaim(FollowupClaimRequest model)
        {
            try
            {

                var memberId = commonRepository.GetMemberIDFromToken();

                if (CheckAuthorization(memberId, null)?.Claim == false)
                    return new ResponseModel<string> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };


                var claimTran = unitOfWork.GetRepository<Entities.ClaimTran>()
                    .Query(x => x.ClaimId == model.ClaimId)
                    .FirstOrDefault();

                if(claimTran  == null) return new ResponseModel<string> { Code = 400, Message = "No ClaimId found!" };

                foreach (var doc in model.FollowupDoc.DocIdList)
                {
                    var fileName2 = $"{claimTran.PolicyNo}-{DefaultConstants.FollowupDocTypeId}_Followed-up_AIA_{(Utils.GetDefaultDate().ToString("yyyy-MM-dd_HH_mm_ss"))}-{doc}";

                    var followup = new Entities.ClaimFollowup();

                    #region #UploadDoc
                    try
                    {
                        var fileName = doc;
                        var format = ".pdf";

                        #region #GetFileExt
                        // Use Path.GetExtension to get the file extension
                        string fileExtension = Path.GetExtension(fileName);

                        // Remove the leading dot (.) from the extension
                        if (!string.IsNullOrEmpty(fileExtension))
                        {
                            fileExtension = fileExtension.TrimStart('.');
                        }
                        #endregion

                        format = fileExtension;

                        var upload = new UploadBase64Request();
                        upload.docTypeId = DefaultConstants.FollowupDocTypeId; 

                        upload.PolicyNo = claimTran.PolicyNo.Substring(0, 10);


                        upload.fileName = fileName2;
                        upload.format = format;
                        upload.templateId = AppSettingsHelper.GetSetting("AiaCmsApi:TemplateId");                     

                        string base64EncodedPDF = azureStorage.GetBase64ByFileName(doc).Result;

                        

                        // Construct the data URI
                        string dataURI = "";
                        //string dataURI = $"data:application/pdf;base64,{base64EncodedPDF}";
                        if (format == "jpeg" || format == "jpg")
                        {
                            dataURI = $"data:image/jpeg;base64,{base64EncodedPDF}";
                        }
                        else if (format == "png")
                        {
                            dataURI = $"data:image/png;base64,{base64EncodedPDF}";
                        }
                        else if (format == "pdf")
                        {
                            dataURI = $"data:application/pdf;base64,{base64EncodedPDF}";
                        }

                        Console.WriteLine(dataURI);

                        upload.file = dataURI;
                        upload.membershipId = claimTran.HolderClientNo;

                        followup.CmsRequest = JsonConvert.SerializeObject(upload);
                        followup.CmsRequestOn = Utils.GetDefaultDate();

                        var uploadResult = aiaCmsApiService.UploadBase64(upload).Result;

                        followup.CmsStatus = uploadResult?.msg ?? "failed";

                        followup.CmsResponseOn = Utils.GetDefaultDate();
                        followup.CmsResponse = uploadResult != null ? JsonConvert.SerializeObject(uploadResult) : null;                        

                    }
                    catch (Exception ex)
                    {
                        MobileErrorLog($"FollowupClaim => UploadBase64 Ex {model.ClaimId}{doc}", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                    }
                    #endregion

                    
                    followup.RequiredInfo = model.RequiredInfo;
                    followup.Id = Guid.NewGuid();
                    followup.ClaimId = model.ClaimId;
                    followup.DocId = doc;
                    followup.DocName = doc;
                    followup.DocName2 = fileName2;
                    followup.DocTypeName = "";

                    unitOfWork.GetRepository<Entities.ClaimFollowup>().Add(followup);
                    
                }

                unitOfWork.SaveChanges();

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0, "Submitted the follow up information.");

            }
            catch (Exception ex)
            {
                MobileErrorLog("FollowupClaim => Ex", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500);
            }
        }

        private QueryStrings PrepareClaimStatusListQuery(ClaimStatusListRequest model)
        {
            #region #CountQuery
            var countQuery = @"SELECT COUNT(ClaimTran.ClaimId) AS SelectCount ";
            var asQuery = @"";
            #endregion

            

            #region #DataQuery
            var dataQuery = $@"SELECT
                                ClaimTran.TransactionDate,
                                ClaimTran.ClaimId,
	                            ClaimTran.ClaimType,
                                ClaimTran.ClaimTypeMm,
                                ClaimTran.ClaimFormType,
                                ClaimTran.ClaimStatusCode AS ClaimStatusCode,
                                ClaimTran.ClaimStatus AS ClaimStatus,
                                ClaimTran.ProgressAsPercent,
                                ClaimTran.ProgressAsHours ";
            #endregion

            #region #FromQuery
            var fromQuery = @"FROM ClaimTran ";
            #endregion

            #region #GroupQuery

            var groupQuery = @"";
            #endregion

            #region #OrderQuery
            var orderQuery = @"Order by ClaimTran.TransactionDate desc ";
            #endregion



            #region #FilterQuery

            var filterQuery = $@"where ClaimTran.AppMemberId = '{model.AppMemberId}' ";            

            if (model.ClaimType != null)
            {
                filterQuery += "AND ClaimTran.ClaimFormType = '" + model.ClaimType + "' ";
            }

            if (!string.IsNullOrEmpty(model.ClaimStatus))            
            {
                
                filterQuery += "AND ClaimTran.ClaimStatus = '" + model.ClaimStatus + "' ";
            }

            if (model.HolderClientNoList != null && model.HolderClientNoList.Any())
            {
                filterQuery += "AND ClaimTran.HolderClientNo IN ('" + string.Join("', '", model.HolderClientNoList) + "') ";
            }


            #endregion

            #region #OffsetQuery
            var offsetQuery = "";
            offsetQuery = $"OFFSET {(model.Page - 1) * model.Size} ROWS FETCH NEXT {model.Size} ROWS ONLY ";
            #endregion


            countQuery = $"{countQuery}{fromQuery}{filterQuery}{groupQuery}{asQuery}";
            var listQuery = $"{dataQuery}{fromQuery}{filterQuery}{groupQuery}{orderQuery}{offsetQuery}";

            return new QueryStrings { CountQuery = countQuery, ListQuery = listQuery };
        }

        public async Task<ResponseModel<ValidationResult>> ValidateClaim(ClaimValidationRequest model)
        {
            try
            {
                var memberId = commonRepository.GetMemberIDFromToken();

                if (CheckAuthorization(memberId, null)?.Claim == false)
                    return new ResponseModel<ValidationResult> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };


                var holderList = GetClientNoListByIdValue(memberId);


                var member = unitOfWork.GetRepository<Entities.Member>()
                    .Query(x => x.MemberId == memberId && x.IsVerified == true && x.IsActive == true)
                    .Include(x => x.MemberClients)
                    .FirstOrDefault();

                var insuranceBenefit = unitOfWork.GetRepository<Entities.InsuranceBenefit>()
                    .Query(x => x.BenefitFormType == model.BenefitFormType.ToString())
                    .FirstOrDefault();

                List<Entities.Policy>? policies = null;

                if (model.BenefitFormType == EnumBenefitFormType.DeathAndAccidentalDeath)
                {
                    
                    if (model.TreatmentDetail != null && !string.IsNullOrEmpty(model.TreatmentDetail.PolicyNo))
                    {
                        policies = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => x.PolicyNo == model.TreatmentDetail.PolicyNo && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus)
                        && holderList.Contains(x.PolicyHolderClientNo))
                        .ToList();

                        if (policies != null && policies.Any())
                        {

                            var components = policies.First().Components?.Trim().Split(",");
                            var query = unitOfWork.GetRepository<Entities.InsuranceMapping>().Query();                            
                            query = query.Where(BuildSearchExpression(components.ToArray()));
                            query = query.Where(x => x.ProductCode == policies.First().ProductType);
                            query = query.Where(x => x.Benefit.BenefitFormType == EnumBenefitFormType.DeathAndAccidentalDeath.ToString());

                            var insuranceMappings = query
                                .Include(x => x.Benefit)
                                .ToList();

                            if (insuranceMappings == null || !insuranceMappings.Any())
                            {
                                var message = "Policy number isn't eligible for Death/Accidental Death claim.";


                                #region #SavedLog
                                try
                                {
                                    unitOfWork.GetRepository<Entities.ClaimValidateMessage>().Add(new ClaimValidateMessage
                                    {
                                        Id = Guid.NewGuid(),
                                        Date = Utils.GetDefaultDate(),
                                        ClaimFormType = model.BenefitFormType?.ToString(),
                                        ClaimType = insuranceBenefit?.BenefitNameEn,
                                        PolicyNumber = model.TreatmentDetail.PolicyNo,
                                        MemberId = member?.MemberClients?.First()?.ClientNo,
                                        MemberName = member?.Name,
                                        MemberPhone = member?.Mobile,
                                        Message = message,
                                    }
                                );
                                    unitOfWork.SaveChanges();
                                }
                                catch { }
                                
                                #endregion

                                return errorCodeProvider.GetResponseModel<ValidationResult>(ErrorCode.E0,
                                        new ValidationResult { IsValid = false, ValidationMessageList = new List<string> { message } });
                            }

                        }
                        else
                        {
                            var contactNumber = unitOfWork.GetRepository<Entities.AppConfig>().Query().Select(x => x.SherContactNumber).FirstOrDefault();

                            var contactMessage = "";
                            if (!string.IsNullOrEmpty(contactNumber))
                            {
                                contactMessage = $"Please reach out to SHER at {contactNumber}.";
                            }

                            return errorCodeProvider.GetResponseModel<ValidationResult>(ErrorCode.E0,
                                    new ValidationResult { IsValid = false, ValidationMessageList = new List<string> { $"The policy number is invalid. {contactMessage}" } });
                        }

                        
                    }
                    
                }
                else
                {
                    policies = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => x.InsuredPersonClientNo == model.InsuredId
                        && holderList.Contains(x.PolicyHolderClientNo)
                        && model.ProductCodes.Contains(x.ProductType)
                        && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                        .ToList();
                }

                #region #Dummy
                ////////var policies = new List<Entities.Policy>();
                ////////var mer = new Entities.Policy()
                ////////{
                ////////    ProductType = "MER",
                ////////    Components = "HL21",
                ////////    PolicyNo = "H012119001",
                ////////};
                ////////var ohi = new Entities.Policy()
                ////////{
                ////////    ProductType = "OHI",
                ////////    Components = "OHI1",
                ////////    PolicyNo = "H012135909",
                ////////};
                ////////var ohg = new Entities.Policy()
                ////////{
                ////////    ProductType = "OHG",
                ////////    Components = "DCB1",
                ////////    PolicyNo = "H012127602",
                ////////};

                ////////policies.Add(mer);
                ////////policies.Add(ohg);
                ////////policies.Add(ohi);

                #endregion

                var validationList = new List<string>();

                policies.ForEach(policy =>
                {
                    MobileErrorLog("ValidateClaim => ", $"Policy No => {policy.PolicyNo}, ProductCode => {policy.ProductType}", JsonConvert.SerializeObject(model), httpContext?.HttpContext.Request.Path);
                    var validationResult = ClaimValidation(model, policy);
                    if (validationResult != null && validationResult.Any())
                    {
                        validationList.AddRange(validationResult);

                        #region #SavedLog
                        try
                        {
                            unitOfWork.GetRepository<Entities.ClaimValidateMessage>().Add(new ClaimValidateMessage
                            {
                                Id = Guid.NewGuid(),
                                Date = Utils.GetDefaultDate(),
                                ClaimFormType = model.BenefitFormType?.ToString(),
                                ClaimType = insuranceBenefit?.BenefitNameEn,
                                PolicyNumber = policy.PolicyNo,
                                MemberId = member?.MemberClients?.First()?.ClientNo,
                                MemberName = member?.Name,
                                MemberPhone = member?.Mobile,
                                Message = string.Join(", ", validationList),
                            }
                        );
                            unitOfWork.SaveChanges();
                        }
                        catch { }

                        #endregion
                    }
                }
                );


                if (validationList != null && validationList.Any())
                {
                    validationList = validationList.Distinct().ToList();

                }

                return errorCodeProvider.GetResponseModel<ValidationResult>(ErrorCode.E0, new ValidationResult { IsValid = !validationList.Any(), ValidationMessageList = validationList });

            }
            catch (Exception ex)
            {
                MobileErrorLog("FollowupClaim => Ex", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<ValidationResult>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<ClaimNowResponse>> ClaimNowAsync(ClaimNowRequest model)
        {
            MobileErrorLog("ClaimNowAsync", "ClaimNowRequest", JsonConvert.SerializeObject(model), httpContext?.HttpContext.Request.Path);

            try
            {

                var memberId = commonRepository.GetMemberIDFromToken();

                if (CheckAuthorization(memberId, null)?.Claim == false)
                    return new ResponseModel<ClaimNowResponse> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                if (string.IsNullOrEmpty(model.ClaimOtp.OtpCode) && !model.IsSkipOtpValidation)
                    return new ResponseModel<ClaimNowResponse> { Code = 401, Message = "OtpCode required." };


                if (AppSettingsHelper.GetSetting("Env").ToLower() != "uat")
                {
                    if (model.ClaimOtp.OtpCode == "111111")
                    {
                        return new ResponseModel<ClaimNowResponse> { Code = 401, Message = "Invalid OtpCode or expired." };
                    }
                }

                if (model.ClaimOtp.OtpCode == "111111" || model.IsSkipOtpValidation)
                {
                    goto Lbl111111;
                }

                var referenceNumber = Utils.ReferenceNumber(model.ClaimOtp.ReferenceNo);

                var claimOtp = unitOfWork.GetRepository<Entities.CommonOtp>()
                    .Query(x => x.OtpType == EnumOtpType.claim.ToString() && x.OtpTo == referenceNumber && x.MemberId == memberId && x.OtpCode == model.ClaimOtp.OtpCode)
                    .OrderByDescending(x => x.CreatedOn)
                    .FirstOrDefault();

                // IsUsed
                if (claimOtp != null && claimOtp.OtpCode == model.ClaimOtp.OtpCode)
                {
                    claimOtp.IsUsed = true;
                    claimOtp.UsedOn = Utils.GetDefaultDate();
                    unitOfWork.SaveChanges();
                }

                var defaultDate = Utils.GetDefaultDate();
                if (claimOtp?.OtpCode != model.ClaimOtp.OtpCode
                    || claimOtp?.OtpExpiry < defaultDate)
                {
                    try
                    {
                        MobileErrorLog("ClaimNow VerifyOtp", $"{claimOtp?.OtpCode} != {model.ClaimOtp.OtpCode} " +
                        $"|| {claimOtp?.OtpExpiry.Value.ToString("yyyy-MM-ddTHH:mm:ss")} < {defaultDate.ToString("yyyy-MM-ddTHH:mm:ss")}"
                        , "Invalid OtpCode or expired.", httpContext?.HttpContext.Request.Path);
                    }
                    catch { }

                    return new ResponseModel<ClaimNowResponse> { Code = 401, Message = "Invalid OtpCode or expired." };
                }

                
                


            Lbl111111:

                var holderList = GetClientNoListByIdValue(memberId);
                var insuredList = GetAllClientNoListByClientNo(model.InsuredId);




                List<Entities.Policy>? policies = null;

                if (model.BenefitFormType == EnumBenefitFormType.DeathAndAccidentalDeath)
                {
                    if (model.TreatmentDetail != null && !string.IsNullOrEmpty(model.TreatmentDetail.PolicyNo))
                    {
                        policies = unitOfWork.GetRepository<Entities.Policy>()
                           .Query(x => x.PolicyNo == model.TreatmentDetail.PolicyNo)
                           .ToList();
                    }

                }
                if (model.BenefitFormType == EnumBenefitFormType.CriticalIllnessBenefit)
                {
                    //model.CausedBy.ById

                    var ciProductIdList= unitOfWork.GetRepository<Entities.CI_Product>()
                        .Query(x => x.DisabiltiyId == new Guid(model.CausedBy.ById))
                        .Select(x => x.ProductId)
                        .ToList(); //ProductId of OHI,ULI << MH << GENERALIZED TETANUS

                    if (ciProductIdList?.Any() == true)
                    {
                        var ciProductCodeList = unitOfWork.GetRepository<Entities.Product>()
                        .Query(x => ciProductIdList.Contains(x.ProductId) && x.IsActive == true && x.IsDelete == false
                        && model.ProductCodes.Contains(x.ProductTypeShort))
                        .Select(x => x.ProductTypeShort)
                        .ToList(); //OHI,ULI

                        if(ciProductCodeList?.Any() == true)
                        {
                            policies = unitOfWork.GetRepository<Entities.Policy>()
                            .Query(x => insuredList.Contains(x.InsuredPersonClientNo)
                            && holderList.Contains(x.PolicyHolderClientNo)
                            && ciProductCodeList.Contains(x.ProductType)
                            && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                            .ToList();
                        }

                        

                    }

                    

                }
                else
                {
                    policies = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => insuredList.Contains(x.InsuredPersonClientNo)
                        && holderList.Contains(x.PolicyHolderClientNo)
                        && model.ProductCodes.Contains(x.ProductType)
                        && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                        .ToList();
                }

                #region #Dummy
                //var policies = new List<Entities.Policy>();
                //var mer = new Entities.Policy()
                //{
                //    ProductType = "MER",
                //    Components = "MER1",
                //    PolicyNo = "H012119001",
                //};
                //var ohi = new Entities.Policy()
                //{
                //    ProductType = "OHI",
                //    Components = "OHI1",
                //    PolicyNo = "H012135909",
                //};
                //var ohg = new Entities.Policy()
                //{
                //    ProductType = "OHG",
                //    Components = "OHG1,OPB2",
                //    PolicyNo = "P1234567",
                //};

                //policies.Add(mer);

                //policies.Add(ohi);

                //policies.Add(ohg);

                #endregion


                if (policies != null && policies.Any())
                {
                    Task.Run(() =>
                    {
                        using (var scope = serviceProvider.CreateScope())
                        {
                            var claimRepository = scope.ServiceProvider.GetRequiredService<IClaimRepository>();

                            _ = claimRepository.CommonClaimServiceCaller(model, memberId, policies);
                        }
                    });
                }
                


                var response = errorCodeProvider.GetResponseModel<ClaimNowResponse>(ErrorCode.E0);

                MobileErrorLog("ClaimNowAsync", "ClaimNowResponse", JsonConvert.SerializeObject(response), httpContext?.HttpContext.Request.Path);




                return response;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"ClaimNowAsync => {ex?.Message}, {JsonConvert.SerializeObject(ex)}");

                var response = errorCodeProvider.GetResponseModel<ClaimNowResponse>(ErrorCode.E500);

                MobileErrorLog("ClaimNowAsync", "ClaimNowResponse", JsonConvert.SerializeObject(response), httpContext?.HttpContext.Request.Path);


                return errorCodeProvider.GetResponseModel<ClaimNowResponse>(ErrorCode.E500);
            }
        }

        public async Task CommonClaimServiceCaller(ClaimNowRequest model, Guid? memberId, List<Entities.Policy>? policies)
        {
            try
            {
                var mainClaimId = Guid.NewGuid();

                

                if (model.BenefitFormType == EnumBenefitFormType.Inpatient || model.BenefitFormType == EnumBenefitFormType.OutpatientAndAmbulatoryCare)
                {

                    if (model.BenefitFormType == EnumBenefitFormType.OutpatientAndAmbulatoryCare && model.BenefitList == null)
                    {
                        foreach (var policy in policies)
                        {
                            var policyComponents = policy.Components?.Trim().Split(",");

                            var query = unitOfWork.GetRepository<Entities.InOutPatientReasonBenefitCode>()
                            .Query(x => x.ClaimType == model.BenefitFormType.ToString()
                            && x.BenefitName == DefaultConstants.NoBenefitForm
                            && x.ProductCode == policy.ProductType);

                            var reasonBenefitCode = query.Where(BuildSearchExpReasonAndBenefitCodeForInOutPatient(policyComponents.ToArray()))
                                .FirstOrDefault();

                            if (reasonBenefitCode != null)
                            {
                                using (var scope = serviceProvider.CreateScope())
                                {
                                    var claimRepository = scope.ServiceProvider.GetRequiredService<IClaimRepository>();

                                    var claimId = Guid.NewGuid();

                                    string[] componentCodeList = reasonBenefitCode.ComponentCode.Split(',');
                                    var commonElements = componentCodeList.Intersect(policyComponents);
                                    string eligibleComponents = string.Join(",", commonElements);


                                    if (model.TreatmentDetail != null)
                                    {
                                        if (model.TreatmentDetail.TreatmentDates != null && model.TreatmentDetail.TreatmentDates.Any())
                                        {
                                            reasonBenefitCode.FromDate = model.TreatmentDetail.TreatmentDates.Order().ToList().First();
                                            reasonBenefitCode.ToDate = model.TreatmentDetail.TreatmentDates.Order().ToList().Last();
                                        }

                                        if (policy.ProductType == "OHI")
                                        {
                                            if (model.TreatmentDetail != null && model.TreatmentDetail.TreatmentFromDate != null)
                                            {
                                                reasonBenefitCode.FromDate = model.TreatmentDetail.TreatmentFromDate;
                                                reasonBenefitCode.ToDate = model.TreatmentDetail.TreatmentFromDate;
                                            }
                                            reasonBenefitCode.TotalAmount = model.TreatmentDetail.IncurredAmount ?? 0;
                                        }
                                        else if (policy.ProductType == "MER")
                                        {
                                            reasonBenefitCode.TotalAmount = 0;
                                        }


                                    }


                                    var result = claimRepository.CommonClaimService(model, policy, mainClaimId, claimId,
                                            reasonBenefitCode.ReasonCode, eligibleComponents, new List<InOutPatientReasonBenefitCode> { reasonBenefitCode }, memberId);
                                }
                            }


                        }
                    }
                    else if ((model.BenefitFormType == EnumBenefitFormType.OutpatientAndAmbulatoryCare
                                || model.BenefitFormType == EnumBenefitFormType.Inpatient) && model.BenefitList != null && model.BenefitList.Any())
                    {
                        var benefitNames = model.BenefitList.Select(x => x.Name).ToList();

                        if (benefitNames != null && benefitNames.Any())
                        {
                            benefitNames.Add(DefaultConstants.NoBenefitForm);
                        }


                        var inOutPatientReasonBenefitCodes = new List<InOutPatientReasonBenefitCode>();
                        foreach (var policy in policies)
                        {
                            
                            var policyComponents = policy.Components?.Trim().Split(",");

                            var query = unitOfWork.GetRepository<Entities.InOutPatientReasonBenefitCode>()
                            .Query(x => x.ClaimType == model.BenefitFormType.ToString()
                            && benefitNames.Contains(x.BenefitName)
                            && x.ProductCode == policy.ProductType);

                            var reasonBenefitCodeList = query.Where(BuildSearchExpReasonAndBenefitCodeForInOutPatient(policyComponents.ToArray())).ToList(); // 6 rows

                            if (reasonBenefitCodeList != null && reasonBenefitCodeList.Any())
                            {
                                foreach (var dbBenefit in reasonBenefitCodeList)
                                {

                                    if (dbBenefit.BenefitName == DefaultConstants.NoBenefitForm)
                                    {
                                        if (dbBenefit.BenefitName == DefaultConstants.NoBenefitForm
                                        && model.TreatmentDetail != null)
                                        {

                                            if (policy.ProductType == "OHI")
                                            {
                                                if (model.TreatmentDetail != null && model.TreatmentDetail.TreatmentFromDate != null)
                                                {
                                                    dbBenefit.FromDate = model.TreatmentDetail.TreatmentFromDate;
                                                    dbBenefit.ToDate = model.TreatmentDetail.TreatmentFromDate;
                                                }
                                                dbBenefit.TotalAmount = model.TreatmentDetail.IncurredAmount ?? 0;
                                            }
                                            else if (policy.ProductType == "MER")
                                            {

                                                if (model.TreatmentDetail.TreatmentDates != null && model.TreatmentDetail.TreatmentDates.Any())
                                                {
                                                    dbBenefit.FromDate = model.TreatmentDetail.TreatmentDates.Order().ToList().First();
                                                    dbBenefit.ToDate = model.TreatmentDetail.TreatmentDates.Order().ToList().Last();
                                                }

                                                dbBenefit.TotalAmount = 0;
                                            }

                                        }
                                    }
                                    else
                                    {
                                        var matched = model.BenefitList.Where(x => x.Name == dbBenefit.BenefitName).FirstOrDefault();

                                        DateTime fromDate = (matched != null && matched.FromDate != null) ? matched.FromDate.Value : Utils.GetDefaultDate();
                                        DateTime toDate = (matched != null && matched.ToDate != null) ? matched.ToDate.Value : fromDate;
                                        decimal amount = (matched != null && matched.Amount != null) ? matched.Amount.Value : 0;
                                        decimal totalAmount = (matched != null && matched.TotalCalculatedAmount != null) ? matched.TotalCalculatedAmount.Value : 0;

                                        dbBenefit.FromDate = fromDate;
                                        dbBenefit.ToDate = toDate;
                                        dbBenefit.TotalAmount = totalAmount;
                                    }




                                }

                                var groupBenefitList = reasonBenefitCodeList.GroupBy(x => x.ReasonCode).ToList();
                                // (list list) 2 rows (5 same ReasonCode Benfit list inside 2 group by list)


                                var grpByReasonCodeList = new List<BenefitListGrpByReasonCode>();
                                foreach (var groupBenefit in groupBenefitList)
                                {
                                    var grpByReasonCode = new BenefitListGrpByReasonCode();
                                    grpByReasonCode.ComponentCode = groupBenefit.First().ComponentCode;
                                    grpByReasonCode.ReasonCode = groupBenefit.First().ReasonCode;
                                    grpByReasonCode.BenefitList = groupBenefit.ToList();

                                    grpByReasonCodeList.Add(grpByReasonCode);
                                }

                                foreach (var groupBenefit in grpByReasonCodeList)
                                {
                                    using (var scope = serviceProvider.CreateScope())
                                    {
                                        var claimRepository = scope.ServiceProvider.GetRequiredService<IClaimRepository>();

                                        var claimId = Guid.NewGuid();

                                        string[] componentCodeList = groupBenefit.ComponentCode.Split(',');
                                        var commonElements = componentCodeList.Intersect(policyComponents);
                                        string eligibleComponents = string.Join(",", commonElements);

                                        var result = claimRepository.CommonClaimService(model, policy, mainClaimId, claimId,
                                            groupBenefit.ReasonCode, eligibleComponents, groupBenefit.BenefitList, memberId);
                                    }
                                }
                            }


                        }


                    }
                }
                else
                {
                    foreach (var policy in policies)
                    {

                        var components = policy.Components?.Trim().Split(",");

                        var query = unitOfWork.GetRepository<Entities.InsuranceMapping>().Query();
                        query = query.Where(BuildSearchExpression(components.ToArray()));

                        var mapping = query.Where(x => x.ProductCode == policy.ProductType && x.Benefit.BenefitFormType == model.BenefitFormType.ToString())
                            .Include(x => x.Benefit)
                            .FirstOrDefault();

                        if (mapping != null)
                        {
                            string[] componentCodeList = mapping.ComponentCode.Split(',');
                            var commonElements = componentCodeList.Intersect(components);
                            string eligibleComponents = string.Join(",", commonElements);


                            using (var scope = serviceProvider.CreateScope())
                            {

                                if (model.BenefitFormType == EnumBenefitFormType.MaternityCare
                                    || model.BenefitFormType == EnumBenefitFormType.DentalCare
                                    || model.BenefitFormType == EnumBenefitFormType.VisionCare
                                    || model.BenefitFormType == EnumBenefitFormType.Vaccination
                                    || model.BenefitFormType == EnumBenefitFormType.PhysicalCheckup)
                                {
                                    var createdBenefit = CreateBenefit(model.BenefitFormType.Value, model);

                                    if (createdBenefit != null && createdBenefit.Any())
                                    {
                                        var customizedBenefit = new InOutPatientReasonBenefitCode()
                                        {
                                            BenefitName = model.BenefitFormType.ToString(),
                                            BenefitCode = createdBenefit[0].benefitCode,
                                            FromDate = createdBenefit[0].dateFromDt,
                                            ToDate = createdBenefit[0].dateToDt,
                                            TotalAmount = createdBenefit[0].Amount,
                                        };

                                        var claimRepository = scope.ServiceProvider.GetRequiredService<IClaimRepository>();

                                        var claimId = Guid.NewGuid();

                                        var result = claimRepository.CommonClaimService(model, policy, mainClaimId, claimId, null, eligibleComponents, new List<InOutPatientReasonBenefitCode> { customizedBenefit }, memberId);
                                    }
                                }
                                else
                                {


                                    #region #Dedicated 3 Riders Claim
									if ((model.BenefitFormType == EnumBenefitFormType.AcceleratedCancerBenefit
										|| model.BenefitFormType == EnumBenefitFormType.DeathAndAccidentalDeath
										|| model.BenefitFormType == EnumBenefitFormType.PartialDisabilityAndInjury
										|| model.BenefitFormType == EnumBenefitFormType.TotalPermanentDisability)
										&& (policy.ProductType == "ULI" || policy.ProductType == "OHI"))
									{
										var componentCodeString = unitOfWork.GetRepository<Entities.InsuranceMapping>()
											.Query(x => x.ProductCode == policy.ProductType && x.Benefit.BenefitFormType == model.BenefitFormType.ToString())   
											.Select(x => x.ComponentCode)
											.FirstOrDefault();


										if (!string.IsNullOrEmpty(componentCodeString))
										{
											List<string> componentCodesFromMapping = componentCodeString.Split(",").ToList();
											List<string> componentCodesFromPolicy = policy.Components.Split(",").ToList();

											if (componentCodesFromMapping?.Any() == true && componentCodesFromPolicy?.Any() == true)
											{
												List<string> matchedComponentCodes = componentCodesFromMapping
																						.Intersect(componentCodesFromPolicy)
																						.ToList();


												matchedComponentCodes?.ForEach(matchedComponentCode =>
												{
													var claimRepository = scope.ServiceProvider.GetRequiredService<IClaimRepository>();

													var claimId = Guid.NewGuid();

													var result = claimRepository.CommonClaimService(model, policy, mainClaimId, claimId, null, matchedComponentCode, null, memberId);
												});
											}
											
										}

										
									}
									#endregion
									else
									{
										var claimRepository = scope.ServiceProvider.GetRequiredService<IClaimRepository>();

										var claimId = Guid.NewGuid();

										var result = claimRepository.CommonClaimService(model, policy, mainClaimId, claimId, null, eligibleComponents, null, memberId);
									}
                                    
                                }


                            }
                        }


                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CommonClaimServiceCaller Exception => {ex.Message}, {JsonConvert.SerializeObject(ex)}");
            }
        }

        public async Task<ResponseModel<ValidationResult>> ValidateDeathClaim(EnumBenefitFormType? claimType, string policyNo)
        {
            try
            {
                var memberId = commonRepository.GetMemberIDFromToken();

                if (CheckAuthorization(memberId, null)?.Claim == false)
                    return new ResponseModel<ValidationResult> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var holderList = GetClientNoListByIdValue(memberId);

                

                var policy = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => x.PolicyNo == policyNo)
                    .FirstOrDefault();


                var member = unitOfWork.GetRepository<Entities.Member>()
                    .Query(x => x.MemberId == memberId && x.IsVerified == true && x.IsActive == true)
                    .Include(x => x.MemberClients)
                    .FirstOrDefault();

                

                if (policy == null)
                {
                    var message = "Policy does't exist.";

                    #region #SavedLog
                    try
                    {
                        unitOfWork.GetRepository<Entities.ClaimValidateMessage>().Add(new ClaimValidateMessage
                        {
                            Id = Guid.NewGuid(),
                            Date = Utils.GetDefaultDate(),
                            ClaimFormType = claimType?.ToString(),
                            ClaimType = "Death/Accidental Death",
                            PolicyNumber = policyNo,
                            MemberId = member?.MemberClients?.First()?.ClientNo,
                            MemberName = member?.Name,
                            MemberPhone = member?.Mobile,
                            Message = message,
                        }
                                            );
                        unitOfWork.SaveChanges();
                    }
                    catch { }
                    
                    #endregion

                    return errorCodeProvider.GetResponseModel<ValidationResult>(ErrorCode.E0,
                        new ValidationResult { IsValid = false, ValidationMessageList = new List<string> { message } });
                }

                var components = policy.Components?.Trim().Split(",");
                var query = unitOfWork.GetRepository<Entities.InsuranceMapping>().Query();
                query = query.Where(BuildSearchExpression(components.ToArray()));
                query = query.Where(x => x.ProductCode == policy.ProductType);
                query = query.Where(x => x.Benefit.BenefitFormType == EnumBenefitFormType.DeathAndAccidentalDeath.ToString());

                var insuranceMappings = query
                    .Include(x => x.Benefit)
                    .ToList();

                if (insuranceMappings == null || !insuranceMappings.Any())
                {
                    var message = "Policy number isn't eligible for Death/Accidental Death claim.";

                    #region #SavedLog
                    try
                    {
                        unitOfWork.GetRepository<Entities.ClaimValidateMessage>().Add(new ClaimValidateMessage
                        {
                            Id = Guid.NewGuid(),
                            Date = Utils.GetDefaultDate(),
                            ClaimFormType = claimType?.ToString(),
                            ClaimType = "Death/Accidental Death",
                            PolicyNumber = policyNo,
                            MemberId = member?.MemberClients?.First()?.ClientNo,
                            MemberName = member?.Name,
                            MemberPhone = member?.Mobile,
                            Message = message,
                        }
                    );
                        unitOfWork.SaveChanges();
                    }
                    catch { }
                    
                    #endregion

                    return errorCodeProvider.GetResponseModel<ValidationResult>(ErrorCode.E0,
                            new ValidationResult { IsValid = false, ValidationMessageList = new List<string> { message } });
                }


                return errorCodeProvider.GetResponseModel<ValidationResult>(ErrorCode.E0, new ValidationResult { IsValid = true });

            }
            catch (Exception ex)
            {
                MobileErrorLog("ValidateDeathClaim => Ex", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<ValidationResult>(ErrorCode.E500);
            }
        }

        #region #Test
        public ResponseModel<ClaimContact> Get72HoursTest(DateTime dt)
        {
            return errorCodeProvider.GetResponseModel<ClaimContact>(ErrorCode.E0, GetProgressAndContactHour(dt)); 
        }

        public async Task<ResponseModel<List<InsuranceTypeResponse>>> GetInsuranceTypeListTest(Guid? memberId, string InsuredId)
        {
            try
            {
                //var memberId = commonRepository.GetMemberIDFromToken();
                if (CheckAuthorization(memberId, null)?.Claim == false)
                    return new ResponseModel<List<InsuranceTypeResponse>> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var insuranceMappingList = new List<InsuranceTypeResponse>();
                var insuranceBenefitList = new List<InsuranceTypeResponse>();
                var insuranceTypeList = new List<InsuranceTypeResponse>();

                var holderList = GetClientNoListByIdValue(memberId);


                Console.WriteLine($"GetInsuranceTypeListTest > HolderClientNoList >{string.Join(",", holderList)}");

                try
                {
                    var itemList = "";
                    holderList?.ForEach(item =>
                    {
                        itemList += $"{item} \n";
                    });

                    Console.WriteLine($"GetInsuranceTypeListTest > TLS HolderClientNoList => {itemList}");

                }
                catch { }


                //var insuredNrc = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == InsuredId)
                //    .Select(x => new { x.Nrc, x.PassportNo, x.Other })
                //    .FirstOrDefault();

                List<string>? insuredClientNoList = new List<string>();
                //if (insuredNrc != null)
                //{

                //    insuredClientNoList = unitOfWork.GetRepository<Entities.Client>()
                //        .Query(x => (!string.IsNullOrEmpty(x.Nrc) && (x.Nrc == insuredNrc.Nrc))
                //        || (!string.IsNullOrEmpty(x.PassportNo) && x.PassportNo == insuredNrc.PassportNo)
                //        || (!string.IsNullOrEmpty(x.Other) && x.Other == insuredNrc.Other))
                //        .Select(x => x.ClientNo).ToList();
                //}


                insuredClientNoList = GetAllClientNoListByClientNo(InsuredId);

                var policies = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x =>
                    insuredClientNoList.Contains(x.InsuredPersonClientNo)
                    && holderList.Contains(x.PolicyHolderClientNo)
                    && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus)
                    )
                    .Select(x => new { x.ProductType, x.Components, x.PolicyNo, x.PolicyStatus })
                    .ToList();

                Console.WriteLine($"GetInsuranceTypeListTest > policies >{string.Join(",", policies.Select(x => x.PolicyNo))}");

                #region #Dummy
                //////var policies = new List<Entities.Policy>();
                //////var mer = new Entities.Policy()
                //////{
                //////    ProductType = "MER",
                //////    Components = "HL21,HL01,HL11,MER1",
                //////    PolicyNo = "H012119001",
                //////};
                //////var ohi = new Entities.Policy()
                //////{
                //////    ProductType = "OHI",
                //////    Components = "OHI1",
                //////    PolicyNo = "H012135909",
                //////};
                //////var ohg = new Entities.Policy()
                //////{
                //////    ProductType = "OHG",
                //////    Components = "DCB1,OHG1,OPB1",
                //////    PolicyNo = "H012127602",
                //////};

                //////policies.Add(mer);
                //////policies.Add(ohg);
                //////policies.Add(ohi);


                #endregion

                foreach (var policy in policies)
                {

                    //MobileErrorLog($"GetInsuranceTypeListTest memberId => {memberId}", $"Policy {policy.PolicyNo} ProductCode {policy.ProductType} Status {policy.PolicyStatus}"
                    //    , $"Components {policy.Components}"
                    //    , httpContext?.HttpContext.Request.Path);

                    var components = policy.Components?.Trim().Split(",");
                    var query = unitOfWork.GetRepository<Entities.InsuranceMapping>().Query();

                    query = query.Where(BuildSearchExpression(components.ToArray()));
                    query = query.Where(x => x.ProductCode == policy.ProductType.Trim());

                    var insuranceMappings = query
                        .Include(x => x.Benefit).ThenInclude(x => x.InsuranceType)
                        .ToList();


                    #region #BenefitForm
                    try
                    {
                        insuranceMappings?.ForEach(mapp =>
                        {
                            Console.WriteLine($"GetInsuranceTypeListTest > TLS BenefitForm => {mapp.Benefit.BenefitFormType}");
                        });
                    }
                    catch
                    { }
                    #endregion


                    foreach (var insuranceMapping in insuranceMappings)
                    {
                        if (insuranceMapping.Benefit.BenefitNameEn != "Death/Accidental Death") // hide Death claim
                        {
                            var insuranceMapp = new InsuranceTypeResponse();
                            insuranceMapp.InsuranceTypeId = insuranceMapping.Benefit.InsuranceType.InsuranceTypeId;
                            insuranceMapp.InsuranceTypeEn = insuranceMapping.Benefit.InsuranceType.InsuranceTypeEn;
                            insuranceMapp.InsuranceTypeMm = insuranceMapping.Benefit.InsuranceType.InsuranceTypeMm;
                            insuranceMapp.InsuranceTypeImage = insuranceMapping.Benefit.InsuranceType.InsuranceTypeImage;

                            insuranceMapp.BenefitId = insuranceMapping.ClaimId;
                            insuranceMapp.EligileBenefitNameListEn = insuranceMapping.Benefit.BenefitNameEn;
                            insuranceMapp.EligileBenefitNameListMm = insuranceMapping.Benefit.BenefitNameMm;
                            insuranceMapp.ClaimNameEn = insuranceMapping.Benefit.ClaimNameEn;
                            insuranceMapp.BenefitImage = insuranceMapping.Benefit.BenefitImage;
                            insuranceMapp.BenefitFormType = insuranceMapping.Benefit.BenefitFormType;
                            insuranceMapp.ProductCode = insuranceMapping.ProductCode;
                            insuranceMapp.PolicyNumber = policy.PolicyNo;
                            insuranceMapp.Components = insuranceMapping.ComponentCode;
                            insuranceMappingList.Add(insuranceMapp);
                        }

                    }
                }

                if (AppSettingsHelper.GetSetting("Deploy:Environment") == "stag")
                {
                    #region #Temp
                    insuranceMappingList.Clear();
                    var query = unitOfWork.GetRepository<Entities.InsuranceMapping>().Query();

                    var insuranceMappings = query
                        .Include(x => x.Benefit).ThenInclude(x => x.InsuranceType)
                        .ToList();


                    foreach (var insuranceMapping in insuranceMappings)
                    {
                        var insuranceMapp = new InsuranceTypeResponse();
                        insuranceMapp.InsuranceTypeId = insuranceMapping.Benefit.InsuranceType.InsuranceTypeId;
                        insuranceMapp.InsuranceTypeEn = insuranceMapping.Benefit.InsuranceType.InsuranceTypeEn;
                        insuranceMapp.InsuranceTypeMm = insuranceMapping.Benefit.InsuranceType.InsuranceTypeMm;
                        insuranceMapp.InsuranceTypeImage = insuranceMapping.Benefit.InsuranceType.InsuranceTypeImage;

                        insuranceMapp.BenefitId = insuranceMapping.ClaimId;
                        insuranceMapp.EligileBenefitNameListEn = insuranceMapping.Benefit.BenefitNameEn;
                        insuranceMapp.EligileBenefitNameListMm = insuranceMapping.Benefit.BenefitNameMm;
                        insuranceMapp.ClaimNameEn = insuranceMapping.Benefit.ClaimNameEn;
                        insuranceMapp.BenefitImage = insuranceMapping.Benefit.BenefitImage;
                        insuranceMapp.BenefitFormType = insuranceMapping.Benefit.BenefitFormType;
                        insuranceMapp.ProductCode = insuranceMapping.ProductCode;
                        //insuranceMapp.PolicyNumber = policy.PolicyNo;
                        insuranceMapp.Components = insuranceMapping.ComponentCode;
                        insuranceMappingList.Add(insuranceMapp);
                    }
                    #endregion
                }

                Console.WriteLine($"GetInsuranceTypeListTest > TLS insuranceMappingList => {JsonConvert.SerializeObject(insuranceMappingList)}");

                var listGrpByBenefit = insuranceMappingList.OrderBy(x => x.EligileBenefitNameListEn).GroupBy(x => x.BenefitFormType).ToList();

                foreach (var grpByBenefit in listGrpByBenefit)
                {
                    var insuranceBenefit = new InsuranceTypeResponse();
                    insuranceBenefit.InsuranceTypeId = grpByBenefit.First().InsuranceTypeId;
                    insuranceBenefit.InsuranceTypeEn = grpByBenefit.First().InsuranceTypeEn;
                    insuranceBenefit.InsuranceTypeMm = grpByBenefit.First().InsuranceTypeMm;
                    insuranceBenefit.InsuranceTypeImage = grpByBenefit.First().InsuranceTypeImage;

                    insuranceBenefit.BenefitId = grpByBenefit.First().BenefitId;
                    insuranceBenefit.EligileBenefitNameListEn = grpByBenefit.First().EligileBenefitNameListEn;
                    insuranceBenefit.EligileBenefitNameListMm = grpByBenefit.First().EligileBenefitNameListMm;
                    insuranceBenefit.BenefitImage = grpByBenefit.First().BenefitImage;
                    insuranceBenefit.BenefitFormType = grpByBenefit.First().BenefitFormType;
                    insuranceBenefit.ProductCode = string.Join(",", grpByBenefit.Select(x => x.ProductCode).ToArray());
                    insuranceBenefit.PolicyList = grpByBenefit.Select(x => x.PolicyNumber).ToArray();
                    insuranceBenefit.ClaimTypeList = grpByBenefit.Select(x => x.ClaimNameEn).ToArray();

                    insuranceBenefitList.Add(insuranceBenefit);
                }

                var listGrpByInsuranceType = insuranceBenefitList.OrderBy(x => x.InsuranceTypeEn).GroupBy(x => x.InsuranceTypeId).ToList();

                foreach (var grpByInsuranceType in listGrpByInsuranceType)
                {
                    var insuranceType = new InsuranceTypeResponse();
                    insuranceType.InsuranceTypeId = grpByInsuranceType.First().InsuranceTypeId;
                    insuranceType.InsuranceTypeEn = grpByInsuranceType.First().InsuranceTypeEn;
                    insuranceType.InsuranceTypeMm = grpByInsuranceType.First().InsuranceTypeMm;
                    insuranceType.InsuranceTypeImage = grpByInsuranceType.First().InsuranceTypeImage;

                    insuranceType.EligileBenefitNameListEn = string.Join(", ", grpByInsuranceType.Select(x => x.EligileBenefitNameListEn).ToArray());
                    insuranceType.EligileBenefitNameListMm = string.Join("၊ ", grpByInsuranceType.Select(x => x.EligileBenefitNameListMm).ToArray());
                    insuranceType.BenefitFormType = grpByInsuranceType.First().BenefitFormType;

                    insuranceType.Benefits = grpByInsuranceType.Select(x =>
                    new BenefitResponse()
                    {
                        InsuredId = InsuredId,
                        BenefitId = x.BenefitId,
                        BenefitNameEn = x.EligileBenefitNameListEn,
                        BenefitNameMm = x.EligileBenefitNameListMm,
                        BenefitImage = x.BenefitImage,
                        ProductCode = grpByInsuranceType.Select(x => x.ProductCode).Distinct().ToArray(),
                        BenefitFormType = (EnumBenefitFormType?)Enum.Parse(typeof(EnumBenefitFormType), x.BenefitFormType),

                    })
                        .ToList();

                    insuranceTypeList.Add(insuranceType);
                }

                Console.WriteLine($"GetInsuranceTypeListTest > TLS insuranceTypeList => {JsonConvert.SerializeObject(insuranceTypeList)}");


                foreach (var item in insuranceTypeList)
                {
                    foreach (var benefit in item.Benefits)
                    {
                        var policyProdCodeList = unitOfWork.GetRepository<Entities.Policy>()
                                    .Query(x => insuredClientNoList.Contains(x.InsuredPersonClientNo) && holderList.Contains(x.PolicyHolderClientNo)
                                    && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                                    .Select(x => x.ProductType)
                                    .ToArray();

                        var prodCodeList = unitOfWork.GetRepository<Entities.InsuranceMapping>()
                            .Query(x => x.Benefit.BenefitFormType == benefit.BenefitFormType.ToString()
                            && policyProdCodeList.Contains(x.ProductCode))
                            .Select(x => x.ProductCode)
                            .ToArray();

                        benefit.ProductCode = prodCodeList.Distinct().ToArray();


                        try
                        {
                            // 
                            // ComponentCode
                            #region #Dummy
                            //////var policyComponents = policies
                            //////            .Select(x => x.Components)
                            //////            .ToArray();
                            #endregion

                            var policyComponents = unitOfWork.GetRepository<Entities.Policy>()
                                        //.Query(x => x.InsuredPersonClientNo == InsuredId
                                        .Query(x => insuredClientNoList.Contains(x.InsuredPersonClientNo) && holderList.Contains(x.PolicyHolderClientNo)
                                        && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                                        .Select(x => x.Components)
                                        .ToList();

                            // MER1,HL01,HL21,OHG1,OHI7,OPB2
                            // MER1,HL01,HL21,OHG1,OHI7,OPB2
                            // MER1,HL01,HL21,OHG1,OHI7,OPB2

                            var aggregateString = string.Join(",", policyComponents); //MER1,HL01,HL21,OHG1,OHI7,OPB2,MER1,HL01,HL21,OHG1,OHI7,OPB2,MER1,HL01,HL21,OHG1,OHI7,OPB2
                            var aggregateList = aggregateString.Split(','); //["","","" ,""] Duplicates
                                                                            //aggregateList = aggregateList.Distinct().ToArray(); //["","","" ,""] Removed duplicates

                            var compoQuery = unitOfWork.GetRepository<Entities.InsuranceMapping>()
                                .Query(x => x.Benefit.BenefitFormType == benefit.BenefitFormType.ToString());

                            var permissionComponents = compoQuery.Where(BuildSearchExpression(aggregateList))
                                            .Select(x => x.ComponentCode)
                                            .ToList();

                            // MER1,HL01,HL21,OHG1,OHI7,OPB2
                            // MER1,HL01,HL21,OHG1,OHI7,OPB2
                            // MER1,HL01,HL21,OHG1,OHI7,OPB2

                            var permAggregateString = string.Join(",", permissionComponents);
                            var permAggregateList = permAggregateString.Split(',');
                            //permAggregateList = permAggregateList.Distinct().ToArray(); //["","","" ,""] Removed duplicates

                            var commonElements = permAggregateList.Intersect(aggregateList);

                            benefit.ComponentCodes = commonElements.Distinct().ToArray();

                            #region #08-02-2024
                            var filterProductCodeQuery = unitOfWork.GetRepository<Entities.InsuranceMapping>()
                                .Query(x => x.Benefit.BenefitFormType == benefit.BenefitFormType.ToString());

                            var filterProductCodeList = filterProductCodeQuery.Where(BuildSearchExpression(benefit.ComponentCodes))
                                            .Select(x => x.ProductCode)
                                            .ToList();

                            benefit.ProductCode = filterProductCodeList.Distinct().ToArray();

                            #endregion

                            try
                            {
                                MobileErrorLog("insuranceTypeList => Test",
                                    $"benefit.BenefitFormType => {benefit.BenefitFormType.ToString()} aggregateString => {aggregateString}" +
                                    $" permAggregateString => {permAggregateString} commonElements => {JsonConvert.SerializeObject(commonElements)}" +
                                    $" prodCodeList => {JsonConvert.SerializeObject(prodCodeList)}"
                            , "", httpContext?.HttpContext.Request.Path);
                            }
                            catch { }
                        }
                        catch (Exception ex)
                        {
                            MobileErrorLog("insuranceTypeList => ComponentCode Ex", $""
                        , JsonConvert.SerializeObject(insuranceTypeList), httpContext?.HttpContext.Request.Path);
                        }


                    }
                }

                insuranceTypeList = insuranceTypeList.OrderBy(x => x.InsuranceTypeEn).ToList();

                MobileErrorLog("insuranceTypeList", $"insuranceTypeListResponse"
                    , JsonConvert.SerializeObject(insuranceTypeList), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<InsuranceTypeResponse>>(ErrorCode.E0, insuranceTypeList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetInsuranceTypeListTest > Ex > {JsonConvert.SerializeObject(ex)}");


                return errorCodeProvider.GetResponseModel<List<InsuranceTypeResponse>>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<UploadDocResponseModel>> ValidateMedicalBillDoc(IFormFile doc)
        {

            var clientNoList = GetClientNoListByIdValue(GetMemberIDFromToken());
            var holderClientNoList = unitOfWork.GetRepository<Entities.Policy>()
                .Query(x => clientNoList.Contains(x.PolicyHolderClientNo) && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                .Select(x => x.PolicyHolderClientNo)
                .ToList();

            var masterClientNo = unitOfWork.GetRepository<Entities.Client>()
                .Query(x => holderClientNoList.Contains(x.ClientNo))
                .Select(x => x.MasterClientNo)
                .FirstOrDefault();

            
            var result = await ocrApiService.ValidateDoc(doc, masterClientNo);

            if (result?.pages?.Any() == true)
            {
                var isBlurWarning = result.pages.Any(x => x.blur_warning == true);
                var isNotDetectionPassed = result.pages.Any(x => x.detection_passed == false);

                return errorCodeProvider.GetResponseModel<UploadDocResponseModel>(ErrorCode.E0,
                    new UploadDocResponseModel
                    {
                        blur_warning = isBlurWarning,
                        detection_passed = !isNotDetectionPassed,
                    });
            }

            return errorCodeProvider.GetResponseModel<UploadDocResponseModel>(ErrorCode.E0);
        }
        #endregion
    }

    public static class ExpressionExtensions
    {
        public static Expression<Func<T, bool>> OrElse<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>(Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
        }
    }

    public class CommonClaim
    {
        //public string relationMm { get; set; }
        //public string relationMm { get; set; }
        //public string relationMm { get; set; }
        //public string relationMm { get; set; }
    }
}
