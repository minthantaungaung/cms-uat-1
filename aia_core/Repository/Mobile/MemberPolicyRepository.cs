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
using DocumentFormat.OpenXml.Office.Word;
using aia_core.Model.Mobile.Request;
using DocumentFormat.OpenXml.Bibliography;
using aia_core.Model.Mobile.Response.DocConfig;

namespace aia_core.Repository.Mobile
{
    public enum StatusType
    {
        PolicyStatus,
        PremiumStatus,
        ClaimStatus
    }
    public interface IMemberPolicyRepository
    {
        Task<ResponseModel<MemberPolicyResponse>> GetPolicies();
        Task<ResponseModel<PolicyDetailResponse>> GetPolicyDetail(string policyNumber);
        Task<ResponseModel<List<PolicyCoveragesResponse>>> GetCoverages(string insuredId, bool active);

        Task<ResponseModel<List<UpcomingPremiumList>>> GetUpcomingPremiums(bool? isAuthPassed);

        Task<ResponseModel<UpcomingPremiumDetail>> GetUpcomingPremiumDetail(string policyNumber);

        Task<ResponseModel<UpcomingPremiumDetail>> TestGetUpcomingPremiumDetail(string policyNumber, Guid? memberId, string otp);

        Task<ResponseModel<List<PolicyCoveragesResponse>>> TestGetCoverages(string insuredId, bool active, string nIRC, string otp);
    }

    public class MemberPolicyRepository : BaseRepository, IMemberPolicyRepository
    {
        private readonly ICommonRepository commonRepository;
        private readonly ITemplateLoader templateLoader;

        public MemberPolicyRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider, IUnitOfWork<Context> unitOfWork, ICommonRepository commonRepository, ITemplateLoader templateLoader)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.commonRepository = commonRepository;
            this.templateLoader = templateLoader;
        }

        public async Task<ResponseModel<MemberPolicyResponse>> GetPolicies()
        {
            try
            {
                var memberGuid = GetMemberIDFromToken();
                var memberId = memberGuid?.ToString();

                if (CheckAuthorization(memberGuid, null)?.ViewMyPolicies == false)
                    return new ResponseModel<MemberPolicyResponse> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var activePolicies = new List<Policies>();
                var inactivePolicies = new List<Policies>();

                var memberClients = GetClientNoListByIdValue(memberGuid);

                if (!memberClients.Any())
                {
                    return errorCodeProvider.GetResponseModel<MemberPolicyResponse>(ErrorCode.E700);
                }

                var includedPolicyStatusList = new string[] { "IF", "LA", "SU" };
                //IF  In Force
                //LA  Contract Lapsed
                //SU  Contract Surrendered                

                var query = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => (memberClients.Contains(x.PolicyHolderClientNo) || memberClients.Contains(x.InsuredPersonClientNo))
                    && includedPolicyStatusList.Contains(x.PolicyStatus) == true
                    );

                var excludedProductCodeList = unitOfWork
                         .GetRepository<PolicyExcludedList>()
                         .Query()
                         .Select(x => x.ProductCode)
                         .ToList();

                if (excludedProductCodeList != null && excludedProductCodeList.Any())
                {
                    query = query.Where(x => excludedProductCodeList.Contains(x.ProductType) == false);
                }


                var policies = query
                    .ToList();
                
                // TODO 

                if (policies.Any())
                {

                    var insuredPolicies = new List<InsuredPolicies>();
                    foreach (var policy in policies)
                    {
                        var insuredPolicy = new InsuredPolicies();

                        var insuredPerson = unitOfWork.GetRepository<Client>().Query(x => x.ClientNo == policy.InsuredPersonClientNo).FirstOrDefault();

                        var product = unitOfWork.GetRepository<Entities.Product>()
                            .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false).FirstOrDefault();

                        var IdentityPerson = insuredPerson;
                        var insuredNrc = string.IsNullOrEmpty(IdentityPerson?.Nrc) 
                            ? (string.IsNullOrEmpty(IdentityPerson?.PassportNo) 
                            ? (IdentityPerson?.Other) 
                            : IdentityPerson?.PassportNo) 
                            : IdentityPerson?.Nrc;


                        var isCoastPolicy = policy.PolicyNo.Length > DefaultConstants.IndividualPolicyNoLength;
                        insuredPolicy.IsCOASTPolicy = isCoastPolicy;

                        insuredPolicy.InsuredName = insuredPerson?.Name;
                        insuredPolicy.InsuredId = policy.InsuredPersonClientNo;
                        insuredPolicy.InsuredNrc = insuredNrc;
                        insuredPolicy.PolicyDate = policy.PolicyIssueDate;
                        insuredPolicy.PolicyNumber = policy.PolicyNo;
                        insuredPolicy.PolicyUnits = policy.NumberOfUnit;
                        insuredPolicy.SumAssured = Convert.ToDouble(policy.SumAssured);
                        insuredPolicy.PremiumDue = !isCoastPolicy ? Convert.ToDouble(policy.PremiumDue) : null;
                        insuredPolicy.ProductName = product?.TitleEn;
                        insuredPolicy.ProductNameMM = product?.TitleMm;
                        insuredPolicy.PolicyStatus = policy.PolicyStatus;

                        insuredPolicy.ProductCode = policy.ProductType;
                        insuredPolicy.PremiumDueDate = policy.PaidToDate;
                        insuredPolicy.Premium = $"{String.Format("{0:N0}", policy.InstallmentPremium)} MMK";
                        insuredPolicy.PlanName = GetPolicyPlanName(policy.ProductType, policy.Components, policy.PolicyNo); ;

                        if (!string.IsNullOrEmpty(policy.NumberOfUnit)
                            && Convert.ToInt32(policy.NumberOfUnit) > 0)
                        {
                            insuredPolicy.SumAssuredByUnitOrAmt = $"{policy.NumberOfUnit} unit(s)";
                        }
                        else if (policy.SumAssured != null)
                        {
                            insuredPolicy.SumAssuredByUnitOrAmt = $"{String.Format("{0:N0}", policy.SumAssured)} MMK";
                        }
                        

                        if (policy.PaidToDate != null 
                            && ((policy.PolicyStatus == "IF" && policy.PremiumStatus != "PU") || (policy.PolicyStatus == "LA"))
                            && policy.ProductType != "PAI" /*Mya Kyay Hmone Request 12/03/2024*/
                            && policy.AcpModeFlag != "1" /*MKM & TMA Request 02/07/2024 ms team meeting */
                            ) 
                        {
                            int numberOfDaysForDue = Utils.GetNumberOfDaysForPolicyDue(policy.PaidToDate.Value);
                            

                            if (numberOfDaysForDue < 0 && numberOfDaysForDue >= ((-1) * DefaultConstants.LimitDaysForUpcomingAndOverdue))
                                insuredPolicy.IsDued = true;
                            else
                                insuredPolicy.IsDued = false;

                            if (numberOfDaysForDue >= 0 && numberOfDaysForDue <= DefaultConstants.LimitDaysForUpcomingAndOverdue)
                                insuredPolicy.IsUpcoming = true;
                            else
                                insuredPolicy.IsUpcoming = false;

                            insuredPolicy.NumberOfDaysForDue = (numberOfDaysForDue < 0) ? ((-1) * numberOfDaysForDue) : numberOfDaysForDue;

                            var policyAdditionalAmt = unitOfWork.GetRepository<Entities.PolicyAdditionalAmt>().Query(x => x.PolicyNo == policy.PolicyNo).FirstOrDefault();
                            if (insuredPolicy.IsUpcoming == true && policyAdditionalAmt?.PremiumDueAmount > 0)
                            {
                                
                                var message = $"{policyAdditionalAmt?.PremiumDueAmount:N0} MMK due in next {insuredPolicy.NumberOfDaysForDue} day(s)";

                                if (message.Contains("next 0 day(s)"))
                                {
                                    message = message.Replace("next 0 day(s)", "today");
                                }

                                insuredPolicy.productStatusMM = message;
                                insuredPolicy.ProductStatus = message;
                            }
                            else if (insuredPolicy.IsDued == true && policyAdditionalAmt?.PremiumDueAmount > 0)
                            {
                                //overdued
                                var message = $"{policyAdditionalAmt?.PremiumDueAmount:N0} MMK overdued {insuredPolicy.NumberOfDaysForDue} day(s)";

                                if (message.Contains("0 day(s)"))
                                {
                                    message = message.Replace("0 day(s)", "today");
                                }
                                insuredPolicy.productStatusMM = message;
                                insuredPolicy.ProductStatus = message;
                            }
                        }
                            
                        
                        insuredPolicy.IsPolicyActive = Utils.GetActivePolicyStatus().Contains(policy.PolicyStatus) ? true : false; 


                        if (product != null && product.LogoImage != null)
                        {
                            insuredPolicy.ProductLogo = commonRepository.GetFileFullUrl(EnumFileType.Product, product.LogoImage);
                        }

                        //AyeMyatMin request on 16/07/2024
                        if(insuredPolicy.PolicyUnits == "0")
                        {
                            insuredPolicy.PolicyUnits = "";
                        }

                        insuredPolicies.Add(insuredPolicy);

                    }

                    activePolicies = insuredPolicies.Where(x => x.IsPolicyActive == true).GroupBy(
                    p => p.InsuredId,
                    (key, g) => new Policies
                    {
                        InsuredName = g.Select(x => x.InsuredName).FirstOrDefault()
                    ,
                        InsuredId = g.Select(x => x.InsuredId).FirstOrDefault()
                    ,
                        InsuredNrc = g.Select(x => x.InsuredNrc).FirstOrDefault()
                    ,
                        InsuredPolicies = g.ToList()
                    })
                        .ToList();

                    

                    inactivePolicies = insuredPolicies.Where(x => x.IsPolicyActive == false).GroupBy(
                    p => p.InsuredId,
                    (key, g) => new Policies
                    {
                        InsuredName = g.Select(x => x.InsuredName).FirstOrDefault()
                    ,
                        InsuredId = g.Select(x => x.InsuredId).FirstOrDefault()
                    ,
                        InsuredNrc = g.Select(x => x.InsuredNrc).FirstOrDefault()
                    ,
                        InsuredPolicies = g.ToList()
                    })
                        .ToList();
                }
                else
                {
                    return errorCodeProvider.GetResponseModel<MemberPolicyResponse>(ErrorCode.E701);
                }

                return errorCodeProvider.GetResponseModel<MemberPolicyResponse>(ErrorCode.E0, new MemberPolicyResponse()
                {
                    ActivePolicies = activePolicies,
                    InactivePolicies = inactivePolicies,
                });

            }
            catch (Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<MemberPolicyResponse>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<List<PolicyCoveragesResponse>>> GetCoverages(string insuredId, bool active)
        {
            var coveragesResponse = new List<PolicyCoveragesResponse>();

            try
            {

                var memberGuid = GetMemberIDFromToken();
                if (CheckAuthorization(memberGuid, null)?.ViewMyPolicies == false)
                    return new ResponseModel<List<PolicyCoveragesResponse>> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var checkInsured = unitOfWork.GetRepository<Entities.Client>()
                    .Query(x => x.ClientNo == insuredId).Any();

                if (!checkInsured)
                {
                    return errorCodeProvider.GetResponseModel <List<PolicyCoveragesResponse>>(ErrorCode.E700);
                }


                var clientNoList = GetClientNoListByIdValue(memberGuid);
                List<Entities.Policy>? policies = null;

                MobileErrorLog("GetCoverages"
                            , $"active => {active}"
                            , $""
                            , httpContext?.HttpContext.Request.Path);

                if (active)
                {
                    policies = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => x.InsuredPersonClientNo == insuredId 
                        && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                        .ToList();
                }
                else
                {
                    policies = unitOfWork.GetRepository<Entities.Policy>()
                            .Query(x => x.InsuredPersonClientNo == insuredId
                            && Utils.GetPolicyStatus().Contains(x.PolicyStatus)
                            && !Utils.GetActivePolicyStatus().Contains(x.PolicyStatus)
                            
                            )
                            .ToList();
                }
                

                var coverages = await unitOfWork.GetRepository<Entities.Coverage>()
                    .Query(x => x.IsDelete == false && x.IsActive == true)
                    .Include(x => x.ProductCoverages).ThenInclude(x => x.Product)
                    .ToListAsync();               

                foreach (var coverage in coverages)
                {


                    var policyCoverage = new PolicyCoveragesResponse()
                    {
                        CoverageId = coverage.CoverageId,
                        CoverageNameEn = coverage.CoverageNameEn,
                        CoverageNameMM = coverage.CoverageNameMm,
                    };

                    if (coverage.CoverageIcon != null)
                    {
                        policyCoverage.CoverageIcon = commonRepository.GetFileFullUrl(EnumFileType.Product, coverage.CoverageIcon);
                    }

                    MobileErrorLog("GetCoverages"
                            , $"Policy count => {policies?.Count ?? 0}"
                            , $""
                            , httpContext?.HttpContext.Request.Path);

                    if (policies != null && policies.Any())
                    {

                        
                        var productCodeListByCoverage = coverage.ProductCoverages.Select(x => x.Product.ProductTypeShort).ToList();   
                        var productCodeListByPolicy = policies.Select(x => x.ProductType).ToList();

                        MobileErrorLog("productCodeListByCoverage"
                            , $"CoverageName => {coverage.CoverageNameEn}"
                            , $"ProductCode => {string.Join(",", productCodeListByCoverage)}"
                            , httpContext?.HttpContext.Request.Path);

                        MobileErrorLog("productCodeListByPolicy"
                            , $"CoverageName => {coverage.CoverageNameEn}"
                            , $"ProductCode => {string.Join(",", productCodeListByPolicy)}"
                            , httpContext?.HttpContext.Request.Path);

                        policyCoverage.IsCovered = productCodeListByCoverage.Intersect(productCodeListByPolicy).Any();

                        MobileErrorLog("productCodeListBy"
                            , $"CoverageName => {coverage.CoverageNameEn}"
                            , $"IsCovered => {policyCoverage.IsCovered}"
                            , httpContext?.HttpContext.Request.Path);
                    }
                    else
                    {
                        policyCoverage.IsCovered = false;
                    }


                    coveragesResponse.Add(policyCoverage);
                }

                return errorCodeProvider.GetResponseModel<List<PolicyCoveragesResponse>>(ErrorCode.E0, coveragesResponse);

            }
            catch (Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<List<PolicyCoveragesResponse>>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<PolicyDetailResponse>> GetPolicyDetail(string policyNumber)
        {
            try
            {
                var memberGuid = GetMemberIDFromToken();
                if (CheckAuthorization(memberGuid, null)?.ViewMyPolicies == false)
                    return new ResponseModel<PolicyDetailResponse> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };


                

                var response = new PolicyDetailResponse();

                var query = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => x.PolicyNo == policyNumber);

                var excludedProductCodeList = unitOfWork
                     .GetRepository<PolicyExcludedList>()
                     .Query()
                     .Select(x => x.ProductCode)
                     .ToList();

                if (excludedProductCodeList != null && excludedProductCodeList.Any())
                {
                    query = query.Where(x => excludedProductCodeList.Contains(x.ProductType) == false);
                }

                var insuredPolicy = await query
                    .FirstOrDefaultAsync();

                var clientNoList = GetClientNoListByIdValue(memberGuid);

                if (insuredPolicy != null && clientNoList != null)
                {
                    if (clientNoList.Contains(insuredPolicy.PolicyHolderClientNo) == false && clientNoList.Contains(insuredPolicy.InsuredPersonClientNo) == false)
                    {
                        return errorCodeProvider.GetResponseModel<PolicyDetailResponse>(ErrorCode.E403);
                    }
                }

                if (insuredPolicy == null)
                    return errorCodeProvider.GetResponseModel<PolicyDetailResponse>(ErrorCode.E701);

                var isCoastPolicy = insuredPolicy.PolicyNo.Length > DefaultConstants.IndividualPolicyNoLength;

                var policyHolder = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == insuredPolicy.PolicyHolderClientNo).FirstOrDefault();
                var policyAgent = unitOfWork.GetRepository<Entities.Client>().Query(x => x.AgentCode == insuredPolicy.AgentCode).FirstOrDefault();
                var policyInsured = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == insuredPolicy.InsuredPersonClientNo).FirstOrDefault();

                var product = unitOfWork.GetRepository<Entities.Product>()
                    .Query(x => x.ProductTypeShort == insuredPolicy.ProductType && x.IsActive == true && x.IsDelete == false)
                    .FirstOrDefault();

                var policyStatus = unitOfWork.GetRepository<Entities.PolicyStatus>().Query(x => x.ShortDesc == insuredPolicy.PolicyStatus)
                        .FirstOrDefault()?.LongDesc;

                if (insuredPolicy.PolicyStatus == EnumPolicyStatus.IF.ToString() 
                    && insuredPolicy.PremiumStatus == EnumPremiumStatus.PU.ToString())
                    policyStatus = "Paid Up";

                response.insuredName = policyInsured?.Name;
                response.policyNumber = insuredPolicy?.PolicyNo;
                response.policyName = product?.TitleEn;
                response.policyNameMM = product?.TitleMm;
                response.PolicyStatus = policyStatus;

                response.PolicyUnits = insuredPolicy?.NumberOfUnit;
                response.SumAssured = insuredPolicy?.SumAssured != null ? Convert.ToDouble(insuredPolicy.SumAssured) : 0;


                if (!string.IsNullOrEmpty(insuredPolicy?.NumberOfUnit)
                            && Convert.ToInt32(insuredPolicy?.NumberOfUnit) > 0)
                {
                    response.SumAssuredByUnitOrAmt = $"{insuredPolicy?.NumberOfUnit} unit(s)";
                }
                else if (insuredPolicy?.SumAssured != null)
                {
                    response.SumAssuredByUnitOrAmt = $"{String.Format("{0:N0}", insuredPolicy.SumAssured)} MMK";
                }
                else if (!(!string.IsNullOrEmpty(insuredPolicy?.NumberOfUnit)
                            && Convert.ToInt32(insuredPolicy?.NumberOfUnit) > 0)
                            && (insuredPolicy?.SumAssured == null))
                {
                    response.SumAssuredByUnitOrAmt = $"{String.Format("{0:N0}", 0)} MMK";
                }

                var policyInsuredNrc = string.IsNullOrEmpty(policyInsured?.Nrc)
                    ? (string.IsNullOrEmpty(policyInsured?.PassportNo)
                    ? (policyInsured?.Other)
                    : policyInsured?.PassportNo)
                    : policyInsured?.Nrc;

                response.InsuredNrc = policyInsuredNrc;

                MobileErrorLog($"GetPolicyDetail {response.InsuredNrc}", "", "", httpContext?.HttpContext.Request.Path);

                if (insuredPolicy.PremiumDue != null)
                {
                    response.PremiumDue = Convert.ToDouble(insuredPolicy.PremiumDue);
                }
                

                if (insuredPolicy.PaidToDate != null && insuredPolicy.ProductType != "PAI" /*Mya Kyay Hmone Request 12/03/2024*/
                    /*&& insuredPolicy.AcpModeFlag != "1"*/ /*MKM & TMA Request 02/07/2024 ms team meeting */  /*MKM asked to use to move this checking on 30 / 05 / 2025 Friday!*/
                    )
                {
                    int numberOfDaysForDue = Utils.GetNumberOfDaysForPolicyDue(insuredPolicy.PaidToDate.Value);


                    if (numberOfDaysForDue < 0 && numberOfDaysForDue >= ((-1) * DefaultConstants.LimitDaysForUpcomingAndOverdue))
                        response.IsDued = true;
                    else
                        response.IsDued = false;

                    if (numberOfDaysForDue > 0 && numberOfDaysForDue <= DefaultConstants.LimitDaysForUpcomingAndOverdue)
                        response.IsUpcoming = true;
                    else
                        response.IsUpcoming = false;

                    response.NumberOfDaysForDue = (numberOfDaysForDue < 0) ? ((-1) * numberOfDaysForDue) : numberOfDaysForDue;
                }

                if (product != null && product.LogoImage != null)
                {
                    response.ProductLogo = commonRepository.GetFileFullUrl(EnumFileType.Product, product.LogoImage);
                }

                var policyAgentNrc = string.IsNullOrEmpty(policyAgent?.Nrc)
                    ? (string.IsNullOrEmpty(policyAgent?.PassportNo)
                    ? (policyAgent?.Other)
                    : policyAgent?.PassportNo)
                    : policyAgent?.Nrc;

                var policyHolderNrc = string.IsNullOrEmpty(policyHolder?.Nrc)
                    ? (string.IsNullOrEmpty(policyHolder?.PassportNo)
                    ? (policyHolder?.Other)
                    : policyHolder?.PassportNo)
                    : policyHolder?.Nrc;

               

                #region PolicyInfo

                var aCP = false;
                //var policyStatus = "";

                if (insuredPolicy != null)
                {
                    aCP = insuredPolicy.AcpModeFlag == "1" ? true : false;

                    policyStatus = unitOfWork.GetRepository<Entities.PolicyStatus>().Query(x => x.ShortDesc == insuredPolicy.PolicyStatus)
                        .FirstOrDefault()?.LongDesc;
                }

                var agentOccupation = unitOfWork.GetRepository<Entities.Occupation>()
                    .Query(x => policyAgent != null ? (x.Code == policyAgent.Occupation) : (x.Code == string.Empty))
                    .FirstOrDefault()?.Description;


                
                var agentInfo = new PolicyAgentInfo()
                {
                    AgentNrc = policyAgentNrc,
                    //AgentPhone = policyAgent?.PhoneNo,
                    AgentPhone = NormalizeMyanmarPhoneNumber(policyAgent?.PhoneNo),
                    AgentEmail = policyAgent?.Email,
                    AgentName = policyAgent?.Name,
                    AgentFather = policyAgent?.FatherName,
                    AgentGender = Utils.GetGender(policyAgent?.Gender),
                    AgentMarriedStatus = Utils.GetMaritalStatus(policyAgent?.MaritalStatus),
                    AgentOccupation = agentOccupation,
                    AgentAddress = policyAgent?.Address1
                                + ", " + policyAgent?.Address2
                                + ", " + policyAgent?.Address3
                                + ", " + policyAgent?.Address4
                                + ", " + policyAgent?.Address5
                        ,
                };

                double sumAssured = 0;
                double OutstandingInterest = 0;
                double OutstandingPremium = 0;
                if (insuredPolicy?.SumAssured != null)
                {
                    sumAssured = Convert.ToDouble(insuredPolicy.SumAssured);
                }
                if (insuredPolicy?.OutstandingInterest != null)
                {
                    OutstandingInterest = Convert.ToDouble(insuredPolicy.OutstandingInterest);
                }
                if (insuredPolicy?.OutstandingPremium != null)
                {
                    OutstandingPremium = Convert.ToDouble(insuredPolicy.OutstandingPremium);
                }
                var policyInfo = new PolicyInfo
                {
                    PolicyUnits = insuredPolicy?.NumberOfUnit,
                    PolicyNumber = insuredPolicy?.PolicyNo,
                    SumAssured = sumAssured,
                    PaymentFrequency = Utils.GetPaymentFrequency(insuredPolicy?.PaymentFrequency),
                    PolicyACP = aCP,
                    OutstandingInterest = OutstandingInterest,
                    OutstandingPremium = OutstandingPremium,
                    PolicyHolder = policyHolder?.Name,
                    agentInfo = policyAgent != null ? agentInfo : null,
                    PremiumDueDate = insuredPolicy?.PaidToDate,
                    PolicyDate = insuredPolicy?.PolicyIssueDate,

                    PolicyStatus = policyStatus,
                    PlanName = "",
                    ProductCode = insuredPolicy?.ProductType,
                };

                policyInfo.PlanName = GetPolicyPlanName(insuredPolicy.ProductType, insuredPolicy.Components, policyNumber);
                response.PolicyInfo = policyInfo;

                response.DBDataLog = $"Policy Info: <<PolicyNo => {insuredPolicy.PolicyNo}, ProductType => {insuredPolicy.ProductType}" +
                    $", PolicyStatus => {insuredPolicy.PolicyStatus}, PremiumStatus => {insuredPolicy.PremiumStatus} " +
                    $", Issued Date => {insuredPolicy.PolicyIssueDate}, Lapsed Date => {insuredPolicy.PolicyLapsedDate}, PaidToDate => {insuredPolicy.PaidToDate}" +
                    $", AcpModeFlag => {insuredPolicy.AcpModeFlag}>>";

                var eligibleStatuses = new string[] { "IF", "LA" };
                var eligiblProducts = new string[] { "END", "IED" };

                #region #Loan
                var loanPolicy = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => x.PolicyNo == policyNumber && eligibleStatuses.Contains(x.PolicyStatus) && eligiblProducts.Contains(x.ProductType))
                    .FirstOrDefault();

                if(loanPolicy != null)
                {
                    var policyAdditionalAmt = unitOfWork.GetRepository<Entities.PolicyAdditionalAmt>()
                        .Query(x => x.PolicyNo == policyNumber)
                        .FirstOrDefault();

                    response.DBDataLog = response.DBDataLog + $"||" +
                        $"#Loan Info: <<LoanPrincipalAmount => {policyAdditionalAmt?.LoanPrincipalAmount}, LoanInterestAmount => {policyAdditionalAmt?.LoanInterestAmount}>>";

                    if (policyAdditionalAmt != null 
                        && ((policyAdditionalAmt.LoanPrincipalAmount != null && policyAdditionalAmt.LoanPrincipalAmount > 0) 
                        || (policyAdditionalAmt.LoanInterestAmount != null && policyAdditionalAmt.LoanInterestAmount > 0)))
                    {


                        var infoText = "Your policy has outstanding policy loan and interest.";
                        var infoTextMm = "Your policy has outstanding policy loan and interest.";

                        if(loanPolicy.PolicyStatus == "LA" && loanPolicy.PolicyLapsedDate != null)
                        {
                            
                            infoText = $"Your policy has outstanding policy loan and interest. Amount is calculated up to {loanPolicy.PolicyLapsedDate.Value.ToString(DefaultConstants.AiaApiDateFormat)}";
                            infoTextMm = $"Your policy has outstanding policy loan and interest. Amount is calculated up to {loanPolicy.PolicyLapsedDate.Value.ToString(DefaultConstants.AiaApiDateFormat)}";
                        }
                        response.PolicyInfo.PolicyLoanDetail = new PolicyLoanDetail
                        {
                            OutstandingPolicyLoan = $"MMK {policyAdditionalAmt.LoanPrincipalAmount:N0}",
                            LoanInterest = $"MMK {policyAdditionalAmt.LoanInterestAmount:N0}",
                            InfoText = infoText,
                            InfoTextMm = infoTextMm,
                        };
                    }
                }

                #endregion

                var isAllowedReinstate = false;

                #region #LapsedReinstatement
                eligibleStatuses = new string[] { "LA" };
                eligiblProducts = new string[] { "END", "IED", "OHI", "ULI", "CIS", "TLS" };

                var lapsedPolicy = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => x.PolicyNo == policyNumber && eligibleStatuses.Contains(x.PolicyStatus) && eligiblProducts.Contains(x.ProductType))
                    .FirstOrDefault();

                if (lapsedPolicy != null)
                {
                    var policyAdditionalAmt = unitOfWork.GetRepository<Entities.PolicyAdditionalAmt>()
                        .Query(x => x.PolicyNo == policyNumber)
                        .FirstOrDefault();

                    response.DBDataLog = response.DBDataLog + $"||" +
                        $"#LapsedReinstatement Info: <<ReinstatementPremiumAmount => {policyAdditionalAmt?.ReinstatementPremiumAmount}, ReinstatementInterestAmount => {policyAdditionalAmt?.ReinstatementInterestAmount}>>";

                    if (lapsedPolicy.PolicyLapsedDate != null)
                    {
                        var allowed = true;

                        

                        if (lapsedPolicy.ProductType == "END" || lapsedPolicy.ProductType == "IED")
                        {
                            var lapsedDate = lapsedPolicy.PolicyLapsedDate.Value.AddYears(1);

                            if (Utils.GetDefaultDate() > lapsedDate)
                            {
                                allowed = false;
                            }
                        }
                        else if (lapsedPolicy.ProductType == "OHI" || lapsedPolicy.ProductType == "ULI" || lapsedPolicy.ProductType == "CIS"
                            || lapsedPolicy.ProductType == "TLS")
                        {
                            var lapsedDate = lapsedPolicy.PolicyLapsedDate.Value.AddYears(2);

                            if (Utils.GetDefaultDate() > lapsedDate)
                            {
                                allowed = false;
                            }
                        }

                        Console.WriteLine($"LapsedReinstatement => {allowed}");

                        var productStatus = "";
                        var productStatusMm = "";
                        var infoText = "";
                        var infoTextMm = "";

                        if (allowed)
                        {
                            isAllowedReinstate = true;

                            if (policyAdditionalAmt != null
                        && ((policyAdditionalAmt.ReinstatementPremiumAmount != null && policyAdditionalAmt.ReinstatementPremiumAmount > 0)
                        || (policyAdditionalAmt.ReinstatementInterestAmount != null && policyAdditionalAmt.ReinstatementInterestAmount > 0)))
                            {

                                //infoText = $"Please remit the premiums and interests (if any) to restatement your policy. Amount is calculated up to {lapsedPolicy.PolicyLapsedDate.Value.ToString(DefaultConstants.AiaApiDateFormat)}.";
                                //infoTextMm = $"Please remit the premiums and interests (if any) to restatement your policy. Amount is calculated up to {lapsedPolicy.PolicyLapsedDate.Value.ToString(DefaultConstants.AiaApiDateFormat)}.";


                                // AIA requested not to show LapseReinstatementDetail

                                response.PolicyInfo.LapseReinstatementDetail = new LapseReinstatementDetail
                                {
                                    PremiumDue = $"MMK {policyAdditionalAmt.ReinstatementPremiumAmount:N0}",
                                    Interest = $"MMK {policyAdditionalAmt.ReinstatementInterestAmount:N0}",
                                };
                            }

                                
                           
                        }
                        else
                        {
                            isAllowedReinstate = false;

                            //infoText = $"Amount is calculated up to {lapsedPolicy.PolicyLapsedDate.Value.ToString(DefaultConstants.AiaApiDateFormat)}.";
                            //infoTextMm = $"Amount is calculated up to {lapsedPolicy.PolicyLapsedDate.Value.ToString(DefaultConstants.AiaApiDateFormat)}.";                            

                            response.PolicyInfo.LapseReinstatementDetail = null;

                            response.productStatusMM = "Policy cannot reinstate anymore.";
                            response.ProductStatus = "Policy cannot reinstate anymore.";
                        }

                        
                    }
                    
                }

                #endregion

                #region #ACP
                eligibleStatuses = new string[] { "IF", "LA" };
                eligiblProducts = new string[] { "END", "IED" };

                var acpPolicy = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => x.PolicyNo == policyNumber && eligibleStatuses.Contains(x.PolicyStatus) 
                    && eligiblProducts.Contains(x.ProductType) && x.AcpModeFlag == "1")
                    .FirstOrDefault();

                if (acpPolicy != null)
                {
                    var policyAdditionalAmt = unitOfWork.GetRepository<Entities.PolicyAdditionalAmt>()
                        .Query(x => x.PolicyNo == policyNumber)
                        .FirstOrDefault();

                    response.DBDataLog = response.DBDataLog + $"||" +
                        $"#ACP Info: <<AcpPrincipalAmount => {policyAdditionalAmt?.AcpPrincipalAmount}, AcpInterestAmount => {policyAdditionalAmt?.AcpInterestAmount}>>";


                    if (policyAdditionalAmt != null 
                        && ((policyAdditionalAmt.AcpPrincipalAmount != null && policyAdditionalAmt.AcpPrincipalAmount > 0) 
                        || (policyAdditionalAmt.AcpInterestAmount != null && policyAdditionalAmt.AcpInterestAmount > 0)))
                    {
                        var productStatus = "";
                        var productStatusMm = "";
                        var infoText = "Your policy is currently on Automatic Continuation of Policy (ACP) mode.";
                        var infoTextMm = "Your policy is currently on Automatic Continuation of Policy (ACP) mode.";

                        if (acpPolicy.PolicyStatus == "LA" && acpPolicy.PolicyLapsedDate != null)
                        {
                            infoText = $"Amount is calculated up to {acpPolicy.PolicyLapsedDate.Value.ToString(DefaultConstants.AiaApiDateFormat)}.";
                            infoTextMm = $"Amount is calculated up to {acpPolicy.PolicyLapsedDate.Value.ToString(DefaultConstants.AiaApiDateFormat)}.";
                        }

                        response.PolicyInfo.ACPDetail = new ACPDetail
                        {
                            InfoText = infoText,
                            InfoTextMm = infoTextMm,
                            OutstandingACPPremium = $"MMK {policyAdditionalAmt.AcpPrincipalAmount:N0}",
                            OutstandingACPInterest = $"MMK {policyAdditionalAmt.AcpInterestAmount:N0}",
                        };

                        //response.productStatusMM = productStatusMm;
                        //response.ProductStatus = productStatus;
                    }

                }

                #endregion

                #region #Renew

                

                eligiblProducts = new string[] { "MER", "OHI", "CII", "CIS", "TLS" };

                var eligiblePolicy = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => x.PolicyNo == policyNumber && eligiblProducts.Contains(x.ProductType))
                    .FirstOrDefault();


                response.DBDataLog = response.DBDataLog + $"||" +
                        $"#Renew Info: <<eligiblePolicy?.PolicyStatus => {eligiblePolicy?.PolicyStatus}>>";

                

                

                if (eligiblePolicy != null)
                {

                    if(eligiblePolicy.PolicyStatus == "EX")
                    {
                        response.productStatusMM = "Policy cannot renew anymore.";
                        response.ProductStatus = "Policy cannot renew anymore.";
                    }
                    else if(Utils.GetActivePolicyStatus().Contains(eligiblePolicy.PolicyStatus))
                    {
                        var infoText = "";
                        var infoTextMm = "";

                        var policyAdditionalAmt = unitOfWork.GetRepository<Entities.PolicyAdditionalAmt>()
                            .Query(x => x.PolicyNo == policyNumber).FirstOrDefault();

                        if (policyAdditionalAmt?.HealthRenewalAmount > 0)
                        {
                            

                            if (eligiblePolicy.PolicyIssueDate != null)
                            {
                                DateTime nextAnniversary = Utils.GetNextAnniversary(eligiblePolicy.PolicyIssueDate.Value);

                                // Calculate the reminder date (30 days before the anniversary)
                                DateTime reminderDate = nextAnniversary.AddDays(-30);

                                response.DBDataLog = response.DBDataLog + $"||" +
                            $", PolicyIssueDate(I) => {eligiblePolicy.PolicyIssueDate}, AnniversaryDate(T) => {nextAnniversary}, (T-30days) => {reminderDate}";

                                Console.WriteLine($"RenewalSection {policyNumber} " +
                                    $"issued_date {eligiblePolicy.PolicyIssueDate} " +
                                    $"nextAnniversary => {nextAnniversary}  " +
                                    $"reminderDate => {reminderDate} ");

                                if (reminderDate < Utils.GetDefaultDate())
                                {
                                    Console.WriteLine($"RenewalSection {policyNumber} reminderDate >= Utils.GetDefaultDate().AddDays(-30) {Utils.GetDefaultDate().AddDays(-30)}");

                                    if (reminderDate >= Utils.GetDefaultDate().Date.AddDays(-30))
                                    {
                                        Console.WriteLine($"RenewalSection => Please remit premium to renew your policy.");
                                        infoText = "Please remit premium to renew your policy.";
                                        infoTextMm = "Please remit premium to renew your policy.";
                                    }
                                }
                            }

                            response.PolicyInfo.RenewalDetail = new RenewalDetail
                            {
                                InfoText = infoText,
                                InfoTextMm = infoTextMm,
                                RenewalAmount = $"MMK {policyAdditionalAmt?.HealthRenewalAmount:N0}",
                            };
                        }
                    }
                }

                #endregion

                response.PolicyInfo.IsCoastPolicy = isCoastPolicy;

                if (!isCoastPolicy)
                {

                    #region #PremiumDue

                    var renewalProducts = new string[] { "MER", "OHI", "CII", "CIS", "TLS" };

                    var renewalPolicy = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => x.PolicyNo == policyNumber && renewalProducts.Contains(x.ProductType)
                        && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                        .FirstOrDefault();

                    if (renewalPolicy == null)
                    {
                        var premiumDuePolicy = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => x.PolicyNo == policyNumber && ((x.PolicyStatus == "IF" && x.PremiumStatus != "PU") || (x.PolicyStatus == "LA")))
                        .FirstOrDefault();

                        if (premiumDuePolicy != null)
                        {
                            var policyAdditionalAmt = unitOfWork.GetRepository<Entities.PolicyAdditionalAmt>().Query(x => x.PolicyNo == policyNumber).FirstOrDefault();

                            response.DBDataLog = response.DBDataLog + $"||" +
                                $"#PremiumDue Info: <<PaidToDate => {premiumDuePolicy.PaidToDate}, PremiumDueAmount => {policyAdditionalAmt?.PremiumDueAmount}";


                            if (policyAdditionalAmt != null && policyAdditionalAmt.PremiumDueAmount != null && policyAdditionalAmt.PremiumDueAmount > 0)
                            {

                                var productStatus = "";
                                var productStatusMm = "";
                                var infoText = "";
                                var infoTextMm = "";

                            response.PolicyInfo.PremiumDueAmountDetail = new PremiumDueAmountDetail
                            {
                                InfoText = infoText,
                                InfoTextMm = infoTextMm,
                                PremiumDue = $"MMK {policyAdditionalAmt?.PremiumDueAmount:N0}",
                            };

                            }
                        }
                    }



                    #endregion
                }

                #endregion


                #region PolicyHolderInfo

                var policyHolderOccupation = unitOfWork.GetRepository<Entities.Occupation>()
                    .Query(x => policyHolder != null ? (x.Code == policyHolder.Occupation) : (x.Code == string.Empty))
                    .FirstOrDefault()?.Description;


                

                var policyHolderInfo = new PolicyHolderInfo()
                {
                    PolicyHolder = policyHolder?.Name,
                    PolicyHolderNrc = policyHolderNrc,
                    PolicyHolderMarriedStatus = Utils.GetMaritalStatus(policyHolder?.MaritalStatus),
                    PolicyHolderGender = Utils.GetGender(policyHolder?.Gender),
                    PolicyHolderFather = policyHolder?.FatherName,
                    PolicyHolderOccupation = policyHolderOccupation,
                    PolicyHolderEmail = policyHolder?.Email,
                    PolicyHolderPhone = policyHolder?.PhoneNo,
                    PolicyHolderAddress =
                    policyHolder?.Address1
                    + ", " + policyHolder?.Address2
                    + ", " + policyHolder?.Address3
                    + ", " + policyHolder?.Address4
                    + ", " + policyHolder?.Address5
                    ,
                };

                response.PolicyHolderInfo = policyHolderInfo;
                #endregion

                #region InsuredInfo

                var insuredOccupation = unitOfWork.GetRepository<Entities.Occupation>()
                    .Query(x => policyInsured != null ? (x.Code == policyInsured.Occupation) : (x.Code == string.Empty))
                    .FirstOrDefault()?.Description;

                var insuredInfo = new InsuredInfo()
                {

                    InsuredNrc = policyInsuredNrc,
                    InsuredMarriedStatus = Utils.GetMaritalStatus(policyInsured?.MaritalStatus),
                    InsuredGender = Utils.GetGender(policyInsured?.Gender),
                    InsuredFather = policyInsured?.FatherName,
                    InsuredOccupation = insuredOccupation,
                    InsuredEmail = policyInsured?.Email,
                    InsuredPhone = policyInsured?.PhoneNo,
                    InsuredAddress =
                                        policyInsured?.Address1
                                        + ", " + policyInsured?.Address2
                                        + ", " + policyInsured?.Address3
                                        + ", " + policyInsured?.Address4
                                        + ", " + policyInsured?.Address5
                                        ,
                };

                response.InsuredInfo = insuredInfo;

                #endregion

                #region BeneficiaryInfo

                var beneficiaries = unitOfWork.GetRepository<Beneficiary>().Query(x => x.PolicyNo == policyNumber)
                    .OrderByDescending(x => x.Percentage)
                    .ToList();

                if (beneficiaries != null && beneficiaries.Any())
                {
                    var beneInfoList = new List<BeneficiaryInfo>();
                    foreach (var beneficiary in beneficiaries)
                    {
                        var client = unitOfWork.GetRepository<Client>().Query(x => x.ClientNo == beneficiary.BeneficiaryClientNo).FirstOrDefault();

                        var beneficiaryNrc = string.IsNullOrEmpty(client?.Nrc)
                                                ? (string.IsNullOrEmpty(client?.PassportNo)
                                                ? (client?.Other)
                                                : client?.PassportNo)
                                                : client?.Nrc;


                        var percentage = 0;
                        if (beneficiary.Percentage != null)
                        {
                            percentage = Convert.ToInt32(beneficiary.Percentage);
                        }


                        var relationship = unitOfWork.GetRepository<Entities.Relationship>()
                            .Query(x => x.Code == beneficiary.Relationship)
                            .FirstOrDefault();

                        var relationName = beneficiary.Relationship;

                        if (relationship != null && !string.IsNullOrEmpty(relationship.Name))
                        {
                            relationName = relationship.Name;
                        }

                        var beneficiaryInfo = new BeneficiaryInfo()
                        {
                            BeneficiaryName = client?.Name,
                            BeneficiaryNrc = beneficiaryNrc,
                            BeneficiaryDob = client?.Dob,
                            BeneficiaryEmail = client?.Email,
                            BeneficiaryGender = Utils.GetGender(client?.Gender),
                            BeneficiaryPhone = client?.PhoneNo,
                            BeneficiaryRelationship = relationName,
                            BeneficiarySharedPercent = percentage
                        };

                        beneInfoList.Add(beneficiaryInfo);
                    }

                    response.BeneficiaryInfo = beneInfoList;
                }





                #endregion


                #region PaymentInfo 
                var payment = new PaymentInfo()
                {
                    PaymentFrequency = Utils.GetPaymentFrequency(insuredPolicy?.PaymentFrequency),
                    InstallmentPremium = (insuredPolicy?.InstallmentPremium != null) 
                    ? Convert.ToDouble(insuredPolicy?.InstallmentPremium) : 0,
                };

                response.PaymentInfo = payment;
                #endregion

                #region PolicyDocuments 
                
                var documents = new PolicyDocuments()
                {
                    Document = ""
                };

                response.PolicyDocuments = new List<PolicyDocuments> { documents };
                #endregion


                MobileErrorLog($"GetPolicyDetail => Response", $"{JsonConvert.SerializeObject(response)}", "", httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<PolicyDetailResponse>(ErrorCode.E0, response);

            }
            catch (Exception ex)
            {
                MobileErrorLog("GetPolicyDetail Ex =>",ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<PolicyDetailResponse>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<List<UpcomingPremiumList>>> GetUpcomingPremiums(bool? isAuthPassed)
        {
            try
            {
                //return errorCodeProvider.GetResponseModel<List<UpcomingPremiumList>>(ErrorCode.E0, new List<UpcomingPremiumList>());

                var memberGuid = GetMemberIDFromToken();
                var memberId = memberGuid?.ToString();

                if(isAuthPassed == false)
                {
                    if (CheckAuthorization(memberGuid, null)?.ViewMyPolicies == false)
                        return new ResponseModel<List<UpcomingPremiumList>> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };
                }
                


                #region "Upcoming Premium"
                var upcomingPremiumList = new List<UpcomingPremiumList>();

                List<string>? clientNoList = GetClientNoListByIdValue(memberGuid.Value);

                if (!clientNoList.IsNullOrEmpty())
                {
                    var currentDate = Utils.GetDefaultDate();
                    var limitDays = DefaultConstants.LimitDaysForUpcomingAndOverdueForULI;

                    var upcoming = currentDate.AddDays(limitDays);
                    var overdued = currentDate.AddDays(-limitDays);

                    //&& ((policy.PolicyStatus == "IF" && policy.PremiumStatus != "PU") || (policy.PolicyStatus == "LA"))                    

                    var query = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => x.ProductType != "PAI" /*Mya Kyay Hmone Request 12/03/2024*/
                        ////&& x.AcpModeFlag != "1" /*MKM & TMA Request 02/07/2024 ms team meeting */ MKM asked to use to move this checking on 30/05/2025 Friday!
                        && (clientNoList.Contains(x.PolicyHolderClientNo) || clientNoList.Contains(x.InsuredPersonClientNo))
                        && (x.PaidToDate >= overdued && x.PaidToDate <= upcoming
                        && ((x.PolicyStatus == "IF" && x.PremiumStatus != "PU") || x.PolicyStatus == "LA"))
                        );

                    var excludedProductCodeList = unitOfWork
                                 .GetRepository<PolicyExcludedList>()
                                 .Query()
                                 .Select(x => x.ProductCode)
                                 .ToList();

                    if (excludedProductCodeList != null && excludedProductCodeList.Any())
                    {
                        query = query.Where(x => excludedProductCodeList.Contains(x.ProductType) == false);
                    }

                    var policies = query
                        .OrderBy(x => x.PaidToDate)
                        .ToList();

                    if (policies.Any())
                    {
                        foreach (var policy in policies)
                        {
                            #region Due Date Checking
                            var isDued = false;
                            var isUpcoming = false;
                            var dueInDays = 0;

                            var actualDueInDays = Utils.GetNumberOfDaysForPolicyDue(policy.PaidToDate.Value);

                            if(policy.ProductType == "ULI")
                            {
                                if (actualDueInDays < 0
                                && actualDueInDays >= ((-1) * DefaultConstants.LimitDaysForUpcomingAndOverdueForULI))
                                {
                                    isDued = true;
                                }


                                if (actualDueInDays >= 0
                                    && actualDueInDays <= DefaultConstants.LimitDaysForUpcomingAndOverdueForULI)
                                {
                                    isUpcoming = true;
                                }
                            }
                            else
                            {
                                if (actualDueInDays < 0
                                && actualDueInDays >= ((-1) * DefaultConstants.LimitDaysForUpcomingAndOverdue))
                                {
                                    isDued = true;
                                }


                                if (actualDueInDays >= 0
                                    && actualDueInDays <= DefaultConstants.LimitDaysForUpcomingAndOverdue)
                                {
                                    isUpcoming = true;
                                }
                            } 

                            dueInDays = (actualDueInDays < 0)
                                ? ((-1) * actualDueInDays) : actualDueInDays;

                            #endregion
                            var policyAdditionalAmt = unitOfWork.GetRepository<Entities.PolicyAdditionalAmt>().Query(x => x.PolicyNo == policy.PolicyNo).FirstOrDefault();

                            if ((isDued || isUpcoming) && policyAdditionalAmt?.PremiumDueAmount > 0)
                            {
                                var upcomingPremium = new UpcomingPremiumList();

                                var insuredPerson = unitOfWork.GetRepository<Client>().Query(x => x.ClientNo == policy.InsuredPersonClientNo).FirstOrDefault();
                                var product = unitOfWork.GetRepository<Entities.Product>()
                                    .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false).FirstOrDefault();

                                var insuredNrc = string.IsNullOrEmpty(insuredPerson?.Nrc)
                                    ? (string.IsNullOrEmpty(insuredPerson?.PassportNo)
                                    ? (insuredPerson?.Other)
                                    : insuredPerson?.PassportNo)
                                    : insuredPerson?.Nrc;

                                upcomingPremium.PolicyNumber = policy.PolicyNo;
                                upcomingPremium.PremiumDue = Convert.ToDouble(policyAdditionalAmt.PremiumDueAmount);
                                upcomingPremium.ProductName = product?.TitleEn;
                                upcomingPremium.ProductNameMM = product?.TitleMm;
                                upcomingPremium.ProductLogo = product?.LogoImage;
                                upcomingPremium.ProductLogo = string.IsNullOrEmpty(product?.LogoImage) ? product?.LogoImage : commonRepository.GetFileFullUrl(EnumFileType.Product, product?.LogoImage);


                                upcomingPremium.NumberOfDaysForDue = dueInDays;
                                upcomingPremium.IsDued = isDued;
                                upcomingPremium.IsUpcoming = isUpcoming;
                                upcomingPremium.DueDate = policy.PaidToDate;

                                upcomingPremiumList.Add(upcomingPremium);

                            }
                        }

                        
                    }
                }

                return errorCodeProvider.GetResponseModel(ErrorCode.E0, upcomingPremiumList);

                #endregion
            }
            catch (Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

            }

            return errorCodeProvider.GetResponseModel<List<UpcomingPremiumList>>(ErrorCode.E500);
        }

        public async Task<ResponseModel<UpcomingPremiumDetail>> GetUpcomingPremiumDetail(string policyNumber)
        {
            try
            {
                var memberGuid = GetMemberIDFromToken();
                var memberId = memberGuid?.ToString();

                if (CheckAuthorization(memberGuid, null)?.ViewMyPolicies == false)
                    return new ResponseModel<UpcomingPremiumDetail> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var query = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => x.PolicyNo == policyNumber && ((x.PolicyStatus == "IF" && x.PremiumStatus != "PU") || x.PolicyStatus == "LA")
                    && x.ProductType != "PAI" /*Mya Kyay Hmone Request 12/03/2024*/
                    ////&& x.AcpModeFlag != "1" /*MKM & TMA Request 02/07/2024 ms team meeting */ MKM asked to use to move this checking on 30/05/2025 Friday!
                    );

                var excludedProductCodeList = unitOfWork
                             .GetRepository<PolicyExcludedList>()
                             .Query()
                             .Select(x => x.ProductCode)
                             .ToList();

                if (excludedProductCodeList != null && excludedProductCodeList.Any())
                {
                    query = query.Where(x => excludedProductCodeList.Contains(x.ProductType) == false);
                }

                var policy = await query
                    .FirstOrDefaultAsync();

                var clientNoList = GetClientNoListByIdValue(memberGuid);
                if (policy != null && clientNoList != null)
                {
                    if (clientNoList.Contains(policy.PolicyHolderClientNo) == false && clientNoList.Contains(policy.InsuredPersonClientNo) == false)
                    {
                        return errorCodeProvider.GetResponseModel<UpcomingPremiumDetail>(ErrorCode.E403);
                    }
                }


                Entities.PolicyAdditionalAmt? policyAdditionalAmt = null;

                if (policy != null)
                    policyAdditionalAmt = unitOfWork.GetRepository<Entities.PolicyAdditionalAmt>()
                        .Query(x => x.PolicyNo == policy.PolicyNo).FirstOrDefault();

                if (policy != null && policyAdditionalAmt?.PremiumDueAmount > 0)
                {
                    #region Due Date Checking
                    var isDued = false;
                    var isUpcoming = false;
                    var dueInDays = 0;

                    var actualDueInDays = Utils.GetNumberOfDaysForPolicyDue(policy.PaidToDate.Value);

                    if(policy.ProductType == "ULI")
                    {
                        if (actualDueInDays < 0
                        && actualDueInDays >= ((-1) * DefaultConstants.LimitDaysForUpcomingAndOverdueForULI))
                        {
                            isDued = true;
                        }

                        if (actualDueInDays >= 0
                            && actualDueInDays <= DefaultConstants.LimitDaysForUpcomingAndOverdueForULI)
                        {
                            isUpcoming = true;
                        }
                    }
                    else
                    {
                        if (actualDueInDays < 0
                        && actualDueInDays >= ((-1) * DefaultConstants.LimitDaysForUpcomingAndOverdue))
                        {
                            isDued = true;
                        }

                        if (actualDueInDays >= 0
                            && actualDueInDays <= DefaultConstants.LimitDaysForUpcomingAndOverdue)
                        {
                            isUpcoming = true;
                        }
                    }
                    

                    dueInDays = (actualDueInDays < 0)
                        ? ((-1) * actualDueInDays) : actualDueInDays;

                    #endregion

                    if (isDued || isUpcoming)
                    {
                        var upcomingPremium = new UpcomingPremiumDetail();
                        var insuredPerson = unitOfWork.GetRepository<Client>().Query(x => x.ClientNo == policy.InsuredPersonClientNo).FirstOrDefault();
                        var product = unitOfWork.GetRepository<Entities.Product>()
                            .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false).FirstOrDefault();

                        var insuredNrc = string.IsNullOrEmpty(insuredPerson?.Nrc)
                            ? (string.IsNullOrEmpty(insuredPerson?.PassportNo)
                            ? (insuredPerson?.Other)
                            : insuredPerson?.PassportNo)
                            : insuredPerson?.Nrc;

                        upcomingPremium.InsuredName = insuredPerson?.Name;
                        upcomingPremium.InsuredId = policy.InsuredPersonClientNo;
                        upcomingPremium.InsuredNrc = insuredNrc;
                        upcomingPremium.DueDate = policy.PaidToDate;
                        upcomingPremium.PolicyNumber = policy.PolicyNo;
                        upcomingPremium.PremiumDue = Convert.ToDouble(policyAdditionalAmt.PremiumDueAmount);
                        upcomingPremium.ProductName = product?.TitleEn;
                        upcomingPremium.ProductNameMm = product?.TitleMm;

                        upcomingPremium.ProductLogo = string.IsNullOrEmpty(product?.LogoImage) ? product?.LogoImage : commonRepository.GetFileFullUrl(EnumFileType.Product, product?.LogoImage);



                        upcomingPremium.NumberOfDaysForDue = dueInDays;
                        upcomingPremium.IsDued = isDued;
                        upcomingPremium.IsUpcoming = isUpcoming;

                        var saleConsultant = unitOfWork.GetRepository<Entities.Client>().Query(x => x.AgentCode == policy.AgentCode)
                            .FirstOrDefault();

                        if (saleConsultant != null)
                        {
                            var policyAgentNrc = string.IsNullOrEmpty(saleConsultant?.Nrc)
                            ? (string.IsNullOrEmpty(saleConsultant?.PassportNo)
                            ? (saleConsultant?.Other)
                            : saleConsultant?.PassportNo)
                            : saleConsultant?.Nrc;

                            var agentOccupation = unitOfWork.GetRepository<Entities.Occupation>()
                    .Query(x => x.Code== saleConsultant.AgentCode)
                    .FirstOrDefault()?.Description;

                            var agentInfo = new PolicyAgentInfo()
                            {
                                AgentNrc = policyAgentNrc,
                                AgentPhone = saleConsultant?.PhoneNo,
                                AgentEmail = saleConsultant?.Email,
                                AgentName = saleConsultant?.Name,
                                AgentFather = saleConsultant?.FatherName,
                                AgentGender = Utils.GetGender(saleConsultant?.Gender),
                                AgentMarriedStatus = Utils.GetMaritalStatus(saleConsultant?.MaritalStatus),
                                AgentOccupation = agentOccupation,
                                AgentAddress = saleConsultant?.Address1
                                    + ", " + saleConsultant?.Address2
                                    + ", " + saleConsultant?.Address3
                                    + ", " + saleConsultant?.Address4
                                    + ", " + saleConsultant?.Address5
                            ,
                            };

                            upcomingPremium.agentInfo = agentInfo;
                        }
                        

                        


                        return errorCodeProvider.GetResponseModel(ErrorCode.E0, upcomingPremium);
                    }

                    
                }

                var message = $"Premium Due notification period is over for this policy {policyNumber}. Please check in My Policies";

                return new ResponseModel<UpcomingPremiumDetail> { Code = 400, Message = message };
            }
            catch (Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

            }

            return errorCodeProvider.GetResponseModel<UpcomingPremiumDetail>(ErrorCode.E500);
        }


        public async Task<ResponseModel<UpcomingPremiumDetail>> TestGetUpcomingPremiumDetail(string policyNumber, Guid? _memberId, string otp)
        {
            try
            {
                if (ValidateTestEndpointsOtp(otp) == false)
                {
                    return errorCodeProvider.GetResponseModel<UpcomingPremiumDetail>(ErrorCode.E403);
                }

                var memberGuid = _memberId;
                var memberId = memberGuid?.ToString();

                if (CheckAuthorization(memberGuid, null)?.ViewMyPolicies == false)
                    return new ResponseModel<UpcomingPremiumDetail> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var policy = await unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => x.PolicyNo == policyNumber && ((x.PolicyStatus == "IF" && x.PremiumStatus != "PU") || x.PolicyStatus == "LA")
                    && x.ProductType != "PAI" /*Mya Kyay Hmone Request 12/03/2024*/
                    && x.AcpModeFlag != "1" /*MKM & TMA Request 02/07/2024 ms team meeting */
                    )
                    .FirstOrDefaultAsync();

                var clientNoList = GetClientNoListByIdValue(memberGuid);
                if (policy != null && clientNoList != null)
                {
                    if (clientNoList.Contains(policy.PolicyHolderClientNo) == false && clientNoList.Contains(policy.InsuredPersonClientNo) == false)
                    {
                        return errorCodeProvider.GetResponseModel<UpcomingPremiumDetail>(ErrorCode.E403);
                    }
                }


                Entities.PolicyAdditionalAmt? policyAdditionalAmt = null;

                if (policy != null)
                    policyAdditionalAmt = unitOfWork.GetRepository<Entities.PolicyAdditionalAmt>()
                        .Query(x => x.PolicyNo == policy.PolicyNo).FirstOrDefault();

                if (policy != null && policyAdditionalAmt?.PremiumDueAmount > 0)
                {
                    #region Due Date Checking
                    var isDued = false;
                    var isUpcoming = false;
                    var dueInDays = 0;

                    var actualDueInDays = Utils.GetNumberOfDaysForPolicyDue(policy.PaidToDate.Value);

                    if (actualDueInDays < 0
                        && actualDueInDays >= ((-1) * DefaultConstants.LimitDaysForUpcomingAndOverdue))
                    {
                        isDued = true;
                    }

                    if (actualDueInDays >= 0
                        && actualDueInDays <= DefaultConstants.LimitDaysForUpcomingAndOverdue)
                    {
                        isUpcoming = true;
                    }

                    dueInDays = (actualDueInDays < 0)
                        ? ((-1) * actualDueInDays) : actualDueInDays;

                    #endregion

                    if (isDued || isUpcoming)
                    {
                        var upcomingPremium = new UpcomingPremiumDetail();
                        var insuredPerson = unitOfWork.GetRepository<Client>().Query(x => x.ClientNo == policy.InsuredPersonClientNo).FirstOrDefault();
                        var product = unitOfWork.GetRepository<Entities.Product>()
                            .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false).FirstOrDefault();

                        var insuredNrc = string.IsNullOrEmpty(insuredPerson?.Nrc)
                            ? (string.IsNullOrEmpty(insuredPerson?.PassportNo)
                            ? (insuredPerson?.Other)
                            : insuredPerson?.PassportNo)
                            : insuredPerson?.Nrc;

                        upcomingPremium.InsuredName = insuredPerson?.Name;
                        upcomingPremium.InsuredId = policy.InsuredPersonClientNo;
                        upcomingPremium.InsuredNrc = insuredNrc;
                        upcomingPremium.DueDate = policy.PaidToDate;
                        upcomingPremium.PolicyNumber = policy.PolicyNo;
                        upcomingPremium.PremiumDue = Convert.ToDouble(policyAdditionalAmt.PremiumDueAmount);
                        upcomingPremium.ProductName = product?.TitleEn;
                        upcomingPremium.ProductNameMm = product?.TitleMm;

                        upcomingPremium.ProductLogo = string.IsNullOrEmpty(product?.LogoImage) ? product?.LogoImage : commonRepository.GetFileFullUrl(EnumFileType.Product, product?.LogoImage);



                        upcomingPremium.NumberOfDaysForDue = dueInDays;
                        upcomingPremium.IsDued = isDued;
                        upcomingPremium.IsUpcoming = isUpcoming;

                        var saleConsultant = unitOfWork.GetRepository<Entities.Client>().Query(x => x.AgentCode == policy.AgentCode)
                            .FirstOrDefault();

                        if (saleConsultant != null)
                        {
                            var policyAgentNrc = string.IsNullOrEmpty(saleConsultant?.Nrc)
                            ? (string.IsNullOrEmpty(saleConsultant?.PassportNo)
                            ? (saleConsultant?.Other)
                            : saleConsultant?.PassportNo)
                            : saleConsultant?.Nrc;

                            var agentOccupation = unitOfWork.GetRepository<Entities.Occupation>()
                    .Query(x => x.Code == saleConsultant.AgentCode)
                    .FirstOrDefault()?.Description;

                            var agentInfo = new PolicyAgentInfo()
                            {
                                AgentNrc = policyAgentNrc,
                                AgentPhone = saleConsultant?.PhoneNo,
                                AgentEmail = saleConsultant?.Email,
                                AgentName = saleConsultant?.Name,
                                AgentFather = saleConsultant?.FatherName,
                                AgentGender = Utils.GetGender(saleConsultant?.Gender),
                                AgentMarriedStatus = Utils.GetMaritalStatus(saleConsultant?.MaritalStatus),
                                AgentOccupation = agentOccupation,
                                AgentAddress = saleConsultant?.Address1
                                    + ", " + saleConsultant?.Address2
                                    + ", " + saleConsultant?.Address3
                                    + ", " + saleConsultant?.Address4
                                    + ", " + saleConsultant?.Address5
                            ,
                            };

                            upcomingPremium.agentInfo = agentInfo;
                        }





                        return errorCodeProvider.GetResponseModel(ErrorCode.E0, upcomingPremium);
                    }


                }

                var message = $"Premium Due notification period is over for this policy {policyNumber}. Please check in My Policies";

                return new ResponseModel<UpcomingPremiumDetail> { Code = 400, Message = message };
            }
            catch (Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

            }

            return errorCodeProvider.GetResponseModel<UpcomingPremiumDetail>(ErrorCode.E500);
        }


        public async Task<ResponseModel<List<PolicyCoveragesResponse>>> TestGetCoverages(string insuredId, bool active, string nIRC, string otp)
        {
            var coveragesResponse = new List<PolicyCoveragesResponse>();

            try
            {
                #region #Customized
                if (ValidateTestEndpointsOtp(otp) == false)
                {
                    return errorCodeProvider.GetResponseModel<List<PolicyCoveragesResponse>>(ErrorCode.E403);
                }

                var memberGuid = unitOfWork.GetRepository<Entities.Member>()
                    .Query(x => (x.Nrc == nIRC || x.Passport == nIRC || x.Others == nIRC) && x.IsActive == true && x.IsVerified == true)
                    .Select(x => x.MemberId)
                    .FirstOrDefault();
                #endregion


                if (CheckAuthorization(memberGuid, null)?.ViewMyPolicies == false)
                    return new ResponseModel<List<PolicyCoveragesResponse>> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };

                var checkInsured = unitOfWork.GetRepository<Entities.Client>()
                    .Query(x => x.ClientNo == insuredId).Any();

                if (!checkInsured)
                {
                    return errorCodeProvider.GetResponseModel<List<PolicyCoveragesResponse>>(ErrorCode.E700);
                }




                var clientNoList = GetClientNoListByIdValue(memberGuid);
                List<Entities.Policy>? policies = null;

                MobileErrorLog("GetCoverages"
                            , $"active => {active}"
                            , $""
                            , httpContext?.HttpContext.Request.Path);

                if (active)
                {
                    policies = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => x.InsuredPersonClientNo == insuredId                        
                        && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                        .ToList();
                }
                else
                {
                    policies = unitOfWork.GetRepository<Entities.Policy>()
                            .Query(x => x.InsuredPersonClientNo == insuredId                            
                            && Utils.GetPolicyStatus().Contains(x.PolicyStatus)
                            && !Utils.GetActivePolicyStatus().Contains(x.PolicyStatus)

                            )
                            .ToList();
                }


                var coverages = await unitOfWork.GetRepository<Entities.Coverage>()
                    .Query(x => x.IsDelete == false && x.IsActive == true)
                    .Include(x => x.ProductCoverages).ThenInclude(x => x.Product)
                    .ToListAsync();

                foreach (var coverage in coverages)
                {


                    var policyCoverage = new PolicyCoveragesResponse()
                    {
                        CoverageId = coverage.CoverageId,
                        CoverageNameEn = coverage.CoverageNameEn,
                        CoverageNameMM = coverage.CoverageNameMm,
                    };

                    if (coverage.CoverageIcon != null)
                    {
                        policyCoverage.CoverageIcon = commonRepository.GetFileFullUrl(EnumFileType.Product, coverage.CoverageIcon);
                    }

                    MobileErrorLog("GetCoverages"
                            , $"Policy count => {policies?.Count ?? 0}"
                            , $""
                            , httpContext?.HttpContext.Request.Path);

                    if (policies != null && policies.Any())
                    {


                        var productCodeListByCoverage = coverage.ProductCoverages.Select(x => x.Product.ProductTypeShort).ToList();
                        var productCodeListByPolicy = policies.Select(x => x.ProductType).ToList();

                        MobileErrorLog("productCodeListByCoverage"
                            , $"CoverageName => {coverage.CoverageNameEn}"
                            , $"ProductCode => {string.Join(",", productCodeListByCoverage)}"
                            , httpContext?.HttpContext.Request.Path);

                        MobileErrorLog("productCodeListByPolicy"
                            , $"CoverageName => {coverage.CoverageNameEn}"
                            , $"ProductCode => {string.Join(",", productCodeListByPolicy)}"
                            , httpContext?.HttpContext.Request.Path);

                        policyCoverage.IsCovered = productCodeListByCoverage.Intersect(productCodeListByPolicy).Any();

                        MobileErrorLog("productCodeListBy"
                            , $"CoverageName => {coverage.CoverageNameEn}"
                            , $"IsCovered => {policyCoverage.IsCovered}"
                            , httpContext?.HttpContext.Request.Path);
                    }
                    else
                    {
                        policyCoverage.IsCovered = false;
                    }


                    coveragesResponse.Add(policyCoverage);
                }

                return errorCodeProvider.GetResponseModel<List<PolicyCoveragesResponse>>(ErrorCode.E0, coveragesResponse);

            }
            catch (Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<List<PolicyCoveragesResponse>>(ErrorCode.E500);
            }
        }
    }

}
