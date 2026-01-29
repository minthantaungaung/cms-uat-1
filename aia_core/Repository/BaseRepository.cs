using aia_core.Repository.Mobile;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.Transactions;
using Newtonsoft.Json;
using aia_core.Model.Mobile.Response;
using aia_core.Model.Cms;
using aia_core.Entities;
using System.Web;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Bibliography;
using Azure;
using static Google.Apis.Requests.BatchRequest;
using aia_core.Model.Mobile.Request;
using DocumentFormat.OpenXml.Drawing.Charts;
using static Google.Apis.Auth.OAuth2.Web.AuthorizationCodeWebApp;
using aia_core.Model.Mobile.Response.MemberPolicyResponse;
using aia_core.Model.Mobile.Servicing.Data.Response;
using Microsoft.AspNetCore.Authorization;
using AuthorizationResult = aia_core.Model.Mobile.Response.AuthorizationResult;
using System.Text.RegularExpressions;

namespace aia_core.Repository
{
    public class BaseRepository : IDisposable
    {
        protected readonly IHttpContextAccessor httpContext;
        protected readonly IAzureStorageService azureStorage;
        protected readonly IErrorCodeProvider errorCodeProvider;
        protected readonly IUnitOfWork<Entities.Context> unitOfWork;

        public BaseRepository(IHttpContextAccessor httpContext,
            IAzureStorageService azureStorage,
            IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork)
        {
            this.httpContext = httpContext;
            this.azureStorage = azureStorage;
            this.errorCodeProvider = errorCodeProvider;
            this.unitOfWork = unitOfWork;
        }

        protected string AuthorizationOtpToken
        {
            get
            {
                var token = this.httpContext?.HttpContext?.Request?.Headers["Authorization"].ToString() ?? "";
                return token?.Replace("Bearer ", "") ?? "";
            }
        }

        protected string GetValueFromJWTClaims(EnumOtpClaims type)
        {
            try
            {
                return this.httpContext?.HttpContext?.User?.Claims?.FirstOrDefault(x => x.Type == $"{type}")?.Value ?? "";
            }
            catch { }
            return string.Empty;
        }

        public string GetFileFullUrl(EnumFileType fileType, string fileName)
        {
            var rawUrl = this.azureStorage.GetUrlFromPrivate(fileName).Result;
            var url = rawUrl;

            Console.WriteLine($"GetFileFullUrl > rawUrl {rawUrl}");

            if (rawUrl.Contains(" "))
            {
                url = rawUrl.Replace(" ", "%20");
                //MobileErrorLog("GetFileFullUrl", rawUrl, url, httpContext?.HttpContext.Request.Path);
            }

            return url;
        }


        public string GetFileFullUrl(string fileName)
        {

            return GetFileFullUrl(EnumFileType.Profile, fileName);
        }

        public Guid? GetMemberIDFromToken()
        {
            try
            {
                var oktaID = httpContext?.HttpContext.User.Claims.FirstOrDefault(r => r.Type == "uid")?.Value;
                if (!string.IsNullOrEmpty(oktaID))
                {
                    var user = unitOfWork.GetRepository<Entities.Member>().Query(
                        expression: r => r.Auth0Userid == oktaID).FirstOrDefault();
                    return (Guid?)user?.MemberId;
                }
            }
            catch { }

            //return Guid.Parse("f8a19132-9223-417b-ba56-abfa03bb30a1");
            return (Guid?)null;
        }

        public EnumIndividualMemberType? GetMemberType()
        {

            try
            {
                var memberId = GetMemberIDFromToken();

                if (memberId != null)
                {
                    var clientNoList = GetClientNoListByIdValue(memberId);


                    var isRuby = unitOfWork.GetRepository<Entities.Client>().Query(x => clientNoList.Contains(x.ClientNo) && x.VipFlag == "Y").Any();

                    if (isRuby)
                    {
                        return EnumIndividualMemberType.Ruby;
                    }


                    return EnumIndividualMemberType.Member;
                }
            }
            catch { }

            return null;
        }

        public EnumIndividualMemberType? GetMemberTypeByID(Guid? memberId)
        {

            try
            {
                var clientNoList = GetClientNoListByIdValue(memberId);


                var isRuby = unitOfWork.GetRepository<Entities.Client>().Query(x => clientNoList.Contains(x.ClientNo) && x.VipFlag == "Y").Any();

                if (isRuby)
                {
                    return EnumIndividualMemberType.Ruby;
                }


                return EnumIndividualMemberType.Member;
            }
            catch { }

            return null;
        }

        public string? GetPolicyOwnerOrInsuredById(Guid? memberId)
        {

            try
            {
                var clientNoList = GetClientNoListByIdValue(memberId);
                var isOwner = unitOfWork.GetRepository<Entities.Policy>().Query(x => clientNoList.Contains(x.PolicyHolderClientNo)).Any();
                var isInsured = unitOfWork.GetRepository<Entities.Policy>().Query(x => clientNoList.Contains(x.InsuredPersonClientNo)).Any();

                if (isOwner)
                {
                    return "Policy Owner";
                }
                else if (isInsured)
                {
                    return "Life Insured";
                }


            }
            catch { }

            return "";
        }

        public string? GetPolicyHolderOrInsuredPerson(Guid? memberId)
        {

            try
            {
                var clientNoList = GetClientNoListByIdValue(memberId);
                var isOwner = unitOfWork.GetRepository<Entities.Policy>().Query(x => clientNoList.Contains(x.PolicyHolderClientNo)).Any();
                var isInsured = unitOfWork.GetRepository<Entities.Policy>().Query(x => clientNoList.Contains(x.InsuredPersonClientNo)).Any();

                if (isOwner)
                {
                    return "Policy Holder";
                }
                else if (isInsured)
                {
                    return "Insured Person";
                }


            }
            catch { }

            return "";
        }


        protected async Task CmsAuditLog(
            EnumObjectGroup objectGroup,
            EnumObjectAction objectAction,
            Guid? objectId = null,
            string? objectName = null,
            string? oldData = null,
            string? newData = null)
        {
            try
            {
                using (var scope = new TransactionScope(
                        scopeOption: TransactionScopeOption.Suppress,
                        scopeTimeout: TimeSpan.FromMinutes(1),
                        asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled
                        ))
                {
                    await unitOfWork.GetRepository<Entities.AuditLog>().AddAsync(new Entities.AuditLog
                    {
                        Id = Guid.NewGuid(),
                        ObjectGroup = $"{objectGroup}",
                        Action = $"{objectAction}",
                        ObjectId = objectId,
                        ObjectName = objectName,
                        OldData = oldData,
                        NewData = newData,
                        LogDate = Utils.GetDefaultDate(),
                        StaffId = new Guid(GetCmsUser().ID)
                    });
                    await unitOfWork.SaveChangesAsync();
                    scope.Complete();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"CmsAudit.Error => {ex?.Message} | {ex?.InnerException?.Message}");
            }
        }

        protected async Task CmsAuditLogLogin(
            EnumObjectGroup objectGroup,
            EnumObjectAction objectAction,
            string? email = null,
            Guid? objectId = null,
            string? objectName = null,
            string? oldData = null,
            string? newData = null)
        {
            try
            {
                using (var scope = new TransactionScope(
                        scopeOption: TransactionScopeOption.Suppress,
                        scopeTimeout: TimeSpan.FromMinutes(1),
                        asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled
                        ))
                {
                    await unitOfWork.GetRepository<Entities.AuditLog>().AddAsync(new Entities.AuditLog
                    {
                        Id = Guid.NewGuid(),
                        ObjectGroup = $"{objectGroup}",
                        Action = $"{objectAction}",
                        ObjectId = objectId,
                        ObjectName = email,
                        OldData = oldData,
                        NewData = newData,
                        LogDate = Utils.GetDefaultDate(),
                        StaffId = objectId
                    });
                    await unitOfWork.SaveChangesAsync();
                    scope.Complete();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"CmsAudit.Error => {ex?.Message} | {ex?.InnerException?.Message}");
            }
        }

        public void CmsErrorLog(
            string? LogMessage = null,
            string? ExceptionMessage = null,
            string? Exception = null,
            string? EndPoint = null,
            string? UserID = null)
        {

            var errorlog = $"CmsErrorLog => LogMessage => {LogMessage} ExceptionMessage => {ExceptionMessage}" +
                $"Exception => {Exception} EndPoint => {EndPoint} UserID => {UserID}";
            Console.WriteLine(errorlog);

            //try
            //{
            //    unitOfWork.GetRepository<Entities.ErrorLogCms>().Add(new Entities.ErrorLogCms
            //    {
            //        ID = Guid.NewGuid(),
            //        LogMessage = LogMessage,
            //        ExceptionMessage = ExceptionMessage,
            //        Exception = Exception,
            //        EndPoint = EndPoint,
            //        LogDate = Utils.GetDefaultDate(),
            //        UserID = UserID
            //    });
            //    unitOfWork.SaveChanges();
            //}
            //catch (Exception ex)
            //{
            //    Console.Error.WriteLine($"CmsErrorLog.Error => {ex?.Message} | {ex?.InnerException?.Message}");
            //}
        }

        public void MobileErrorLog(
            string? LogMessage = null,
            string? ExceptionMessage = null,
            string? Exception = null,
            string? EndPoint = null,
            string? UserID = null)
        {

            var errorlog = $"MobileErrorLog => LogMessage => {LogMessage} ExceptionMessage => {ExceptionMessage}" +
                $"Exception => {Exception} EndPoint => {EndPoint} UserID => {UserID}";
            Console.WriteLine(errorlog);

            //try
            //{
            //    unitOfWork.GetRepository<Entities.ErrorLogMobile>().Add(new Entities.ErrorLogMobile
            //    {
            //        ID = Guid.NewGuid(),
            //        LogMessage = LogMessage,
            //        ExceptionMessage = ExceptionMessage,
            //        Exception = Exception,
            //        EndPoint = EndPoint,
            //        LogDate = Utils.GetDefaultDate(),
            //        UserID = UserID
            //    });
            //    unitOfWork.SaveChanges();
            //}
            //catch (Exception ex)
            //{
            //    Console.Error.WriteLine($"MobileErrorLog.Error => {ex?.Message} | {ex?.InnerException?.Message}");
            //}
        }

        protected async Task SendOTPCode(string refNumber, string message, string apiKey, string username, string otp)
        {
            try
            {
                if (Utils.IsEmailAddress(refNumber))
                {
                    await Utils.SendOtpEmail(refNumber, username, otp);
                }
                else
                {
                    Utils.SendSms(refNumber, message, apiKey);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{ex?.Message} {ex?.InnerException?.Message}");
            }
        }

        public CmsAccessUser GetCmsUser()
        {
            var currentUser = httpContext.HttpContext.User;

            #region "Get CMS User from accessToken"
            CmsAccessUser user = new CmsAccessUser();
            if (currentUser.HasClaim(c => c.Type == CMSClaim.ID))
            {
                user.ID = currentUser.Claims.FirstOrDefault(c => c.Type == CMSClaim.ID).Value;
                user.Email = currentUser.Claims.FirstOrDefault(c => c.Type == CMSClaim.Email).Value;
                user.Name = currentUser.Claims.FirstOrDefault(c => c.Type == CMSClaim.Name).Value;
                user.RoleID = currentUser.Claims.FirstOrDefault(c => c.Type == CMSClaim.RoleID).Value;
                user.GenerateToken = currentUser.Claims.FirstOrDefault(c => c.Type == CMSClaim.GenerateToken).Value;
            }

            return user;
            #endregion
        }
        public void Dispose()
        {

        }

        public List<Entities.AuthorizationStatus> GetAuthByPolicyStatus(List<string> matchedProductCodeList, List<ProductCodePolicyStat> userPolicyList,
            out List<ProductCodePolicyStat> matchedPolicyList)
        {
            var policyList = userPolicyList.Where(x => matchedProductCodeList.Contains(x.ProductCode))
                .Select(x => new { x.ProductCode, x.PolicyStatus })
                .ToList();

            matchedPolicyList = policyList
                .Select(x => new ProductCodePolicyStat { ProductCode = x.ProductCode, PolicyStatus = x.PolicyStatus })
                .ToList();

            var policyStatusList = policyList.Select(x => x.PolicyStatus).ToList().Distinct().ToList();

            var policyResultList = unitOfWork.GetRepository<Entities.AuthorizationStatus>()
                                .Query(x => policyStatusList.Contains(x.Status) && x.StatusType == EnumStatusType.Policy.ToString())
                                .ToList();



            return policyResultList;
        }

        public List<Entities.AuthorizationStatus> GetAuthByPremiumStatus(List<ProductCodePolicyStat> userPolicyList, List<string> matchedProductCodeList
            , List<string> matchedPolicyStatusList)
        {
            var premiumStatusList = userPolicyList.Where(x => matchedProductCodeList.Contains(x.ProductCode) && matchedPolicyStatusList.Contains(x.PolicyStatus))
                .Select(x => x.PremiumStatus)
                .ToList()
                .Distinct()
                .ToList();

            var premiumResultList = unitOfWork.GetRepository<Entities.AuthorizationStatus>()
                            .Query(x => premiumStatusList.Contains(x.Status) && x.StatusType == EnumStatusType.Premium.ToString())
                            .ToList();

            return premiumResultList;
        }


        public AuthorizationResult GetAuthorizationsByPerson(List<string> clientNoList)
        {
            try
            {

                var policyHolder = unitOfWork.GetRepository<Entities.Policy>().Query(x => clientNoList.Contains(x.PolicyHolderClientNo)).Any();
                var policyInsured = unitOfWork.GetRepository<Entities.Policy>().Query(x => clientNoList.Contains(x.InsuredPersonClientNo)).Any();

                if (policyHolder)
                {
                    var authorizationPerson = unitOfWork.GetRepository<Entities.AuthorizationPerson>()
                        .Query(x => x.PersonType == EnumPolicyPersonType.PolicyHolder.ToString())
                        .Select(x => new AuthorizationResult(x))
                        .FirstOrDefault();

                    return authorizationPerson;
                }

                if (policyInsured)
                {
                    var authorizationPerson = unitOfWork.GetRepository<Entities.AuthorizationPerson>()
                            .Query(x => x.PersonType == EnumPolicyPersonType.Insured.ToString())
                            .Select(x => new AuthorizationResult(x))
                            .FirstOrDefault();

                    return authorizationPerson;
                }
            }
            catch (Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
            }

            return null;
        }

        public AuthorizationResult GetAuthorizationsByProduct(List<string> clientNoList, List<string> productCodeList)
        {
            try
            {

                var authorizationProducts = unitOfWork.GetRepository<Entities.AuthorizationProduct>()
                        .Query(x => productCodeList.Contains(x.ProductCode))
                        .ToList();

                var authorizationResult = new AuthorizationResult();

                if (!authorizationProducts.IsNullOrEmpty())
                {
                    authorizationResult.Registration = authorizationProducts.Any(x => x.Registration == true);
                    authorizationResult.Login = authorizationProducts.Any(x => x.Login == true);
                    authorizationResult.ViewMyPolicies = authorizationProducts.Any(x => x.ViewMyPolicies == true);
                    authorizationResult.Proposition = authorizationProducts.Any(x => x.Proposition == true);
                    authorizationResult.Claim = authorizationProducts.Any(x => x.Claim == true);

                    authorizationResult.PolicyHolderDetails = authorizationProducts.Any(x => x.PolicyHolderDetails == true);
                    authorizationResult.InsuredDetails = authorizationProducts.Any(x => x.InsuredDetails == true);
                    authorizationResult.BeneficiaryInfo = authorizationProducts.Any(x => x.BeneficiaryInfo == true);
                    authorizationResult.LapseReinstatement = authorizationProducts.Any(x => x.LapseReinstatement == true);
                    authorizationResult.HealthRenewal = authorizationProducts.Any(x => x.HealthRenewal == true);
                    authorizationResult.PolicyLoanRepayment = authorizationProducts.Any(x => x.PolicyLoanRepayment == true);
                    authorizationResult.ACP = authorizationProducts.Any(x => x.Acp == true);
                    authorizationResult.AdhocTopup = authorizationProducts.Any(x => x.AdhocTopup == true);
                    authorizationResult.PartialWithdrawal = authorizationProducts.Any(x => x.PartialWithdrawal == true);
                    authorizationResult.PolicyLoan = authorizationProducts.Any(x => x.PolicyLoan == true);
                    authorizationResult.PolicyPaidup = authorizationProducts.Any(x => x.PolicyPaidup == true);
                    authorizationResult.PolicySurrender = authorizationProducts.Any(x => x.PolicySurrender == true);
                    authorizationResult.PaymentFrequency = authorizationProducts.Any(x => x.PaymentFrequency == true);
                    authorizationResult.SumAssuredChange = authorizationProducts.Any(x => x.SumAssuredChange == true);
                    authorizationResult.RefundofPayment = authorizationProducts.Any(x => x.RefundofPayment == true);
                }

                return authorizationResult;

            }
            catch (Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

            }

            return null;
        }

        public AuthorizationResult GetAuthorizationsByStatus(List<string> clientNoList, List<ProductCodePolicyStat> userPolicyList)
        {
            try
            {
                var authorizationResults = new List<AuthorizationResult>();

                var productCodeList = userPolicyList.Select(x => x.ProductCode).ToList().Distinct().ToList();
                var authorizationProducts = unitOfWork.GetRepository<Entities.AuthorizationProduct>()
                        .Query(x => productCodeList.Contains(x.ProductCode))
                        .ToList();

                var policyResult = new AuthorizationResult();
                var premiumResult = new AuthorizationResult();

                if (authorizationProducts != null && authorizationProducts.Any())
                {
                    var matchedProductCodeList = new List<string>();
                    var policyStatusList = new List<Entities.AuthorizationStatus>();
                    var premiumStatusList = new List<Entities.AuthorizationStatus>();

                    if (authorizationProducts.Any(x => x.PolicyPaidup == true))
                    {
                        #region #policyResult & premiumResult
                        matchedProductCodeList = new List<string>();
                        matchedProductCodeList = authorizationProducts
                            .Where(x => x.PolicyPaidup == true).Select(x => x.ProductCode).ToList();

                        var matchedPolicyList = new List<ProductCodePolicyStat>();

                        policyStatusList = new List<AuthorizationStatus>();
                        policyStatusList = GetAuthByPolicyStatus(matchedProductCodeList, userPolicyList, out matchedPolicyList);

                        if (policyStatusList != null)
                        {
                            policyResult.PolicyPaidup = policyStatusList.Any(x => x.PolicyPaidup == true);

                            if (policyResult.PolicyPaidup)
                            {
                                var matchedPolicyStatusList = policyStatusList
                                    .Where(x => x.PolicyPaidup == true)
                                    .Select(x => x.Status)
                                    .ToList();


                                var matchedList = matchedPolicyList
                                    .Where(x => matchedPolicyStatusList.Contains(x.PolicyStatus))
                                    .Select(x => new { x.ProductCode, x.PolicyStatus })
                                    .ToList();

                                premiumStatusList = new List<AuthorizationStatus>();
                                premiumStatusList = GetAuthByPremiumStatus(userPolicyList
                                    , matchedList.Select(x => x.ProductCode).ToList()
                                    , matchedList.Select(x => x.PolicyStatus).ToList());

                                if (premiumStatusList != null)
                                {
                                    premiumResult.PolicyPaidup = premiumStatusList.Any(x => x.PolicyPaidup == true);
                                }
                            }
                        }


                        #endregion
                    }
                    if (authorizationProducts.Any(x => x.Registration == true))
                    {
                        #region #policyResult & premiumResult
                        matchedProductCodeList = new List<string>();
                        matchedProductCodeList = authorizationProducts
                            .Where(x => x.Registration == true).Select(x => x.ProductCode).ToList();

                        var matchedPolicyList = new List<ProductCodePolicyStat>();

                        policyStatusList = new List<AuthorizationStatus>();
                        policyStatusList = GetAuthByPolicyStatus(matchedProductCodeList, userPolicyList, out matchedPolicyList);

                        if (policyStatusList != null)
                        {
                            policyResult.Registration = policyStatusList.Any(x => x.Registration == true);

                            if (policyResult.Registration)
                            {
                                var matchedPolicyStatusList = policyStatusList
                                    .Where(x => x.Registration == true)
                                    .Select(x => x.Status)
                                    .ToList();


                                var matchedList = matchedPolicyList
                                    .Where(x => matchedPolicyStatusList.Contains(x.PolicyStatus))
                                    .Select(x => new { x.ProductCode, x.PolicyStatus })
                                    .ToList();

                                premiumStatusList = new List<AuthorizationStatus>();
                                premiumStatusList = GetAuthByPremiumStatus(userPolicyList
                                    , matchedList.Select(x => x.ProductCode).ToList()
                                    , matchedList.Select(x => x.PolicyStatus).ToList());

                                if (premiumStatusList != null)
                                {
                                    premiumResult.Registration = premiumStatusList.Any(x => x.Registration == true);
                                }
                            }
                        }


                        #endregion
                    }
                    if (authorizationProducts.Any(x => x.Login == true))
                    {
                        #region #policyResult & premiumResult
                        matchedProductCodeList = new List<string>();
                        matchedProductCodeList = authorizationProducts
                            .Where(x => x.Login == true).Select(x => x.ProductCode).ToList();

                        var matchedPolicyList = new List<ProductCodePolicyStat>();

                        policyStatusList = new List<AuthorizationStatus>();
                        policyStatusList = GetAuthByPolicyStatus(matchedProductCodeList, userPolicyList, out matchedPolicyList);

                        if (policyStatusList != null)
                        {
                            policyResult.Login = policyStatusList.Any(x => x.Login == true);

                            Console.WriteLine($"clientNoList > {string.Join(",", clientNoList)} > policyResult.Login > {policyResult.Login}");

                            if (policyResult.Login)
                            {
                                var matchedPolicyStatusList = policyStatusList
                                    .Where(x => x.Login == true)
                                    .Select(x => x.Status)
                                    .ToList();


                                var matchedList = matchedPolicyList
                                    .Where(x => matchedPolicyStatusList.Contains(x.PolicyStatus))
                                    .Select(x => new { x.ProductCode, x.PolicyStatus })
                                    .ToList();

                                premiumStatusList = new List<AuthorizationStatus>();
                                premiumStatusList = GetAuthByPremiumStatus(userPolicyList
                                    , matchedList.Select(x => x.ProductCode).ToList()
                                    , matchedList.Select(x => x.PolicyStatus).ToList());

                                if (premiumStatusList != null)
                                {
                                    premiumResult.Login = premiumStatusList.Any(x => x.Login == true);

                                    Console.WriteLine($"clientNoList > {string.Join(",", clientNoList)} > premiumResult.Login > {premiumResult.Login}");
                                }
                            }
                        }
                        #endregion
                    }
                    if (authorizationProducts.Any(x => x.ViewMyPolicies == true))
                    {
                        #region #policyResult & premiumResult
                        matchedProductCodeList = new List<string>();
                        matchedProductCodeList = authorizationProducts
                            .Where(x => x.ViewMyPolicies == true).Select(x => x.ProductCode).ToList();

                        var matchedPolicyList = new List<ProductCodePolicyStat>();

                        policyStatusList = new List<AuthorizationStatus>();
                        policyStatusList = GetAuthByPolicyStatus(matchedProductCodeList, userPolicyList, out matchedPolicyList);

                        if (policyStatusList != null)
                        {
                            policyResult.ViewMyPolicies = policyStatusList.Any(x => x.ViewMyPolicies == true);

                            Console.WriteLine($"clientNoList > {string.Join(",", clientNoList)} > policyResult.ViewMyPolicies > {policyResult.ViewMyPolicies}");

                            if (policyResult.ViewMyPolicies)
                            {
                                var matchedPolicyStatusList = policyStatusList
                                    .Where(x => x.ViewMyPolicies == true)
                                    .Select(x => x.Status)
                                    .ToList();


                                var matchedList = matchedPolicyList
                                    .Where(x => matchedPolicyStatusList.Contains(x.PolicyStatus))
                                    .Select(x => new { x.ProductCode, x.PolicyStatus })
                                    .ToList();

                                premiumStatusList = new List<AuthorizationStatus>();
                                premiumStatusList = GetAuthByPremiumStatus(userPolicyList
                                    , matchedList.Select(x => x.ProductCode).ToList()
                                    , matchedList.Select(x => x.PolicyStatus).ToList());

                                if (premiumStatusList != null)
                                {
                                    premiumResult.ViewMyPolicies = premiumStatusList.Any(x => x.ViewMyPolicies == true);

                                    Console.WriteLine($"clientNoList > {string.Join(",", clientNoList)} > premiumResult.ViewMyPolicies > {premiumResult.ViewMyPolicies}");
                                }
                            }
                        }
                        #endregion
                    }
                    if (authorizationProducts.Any(x => x.Proposition == true))
                    {
                        #region #policyResult & premiumResult
                        matchedProductCodeList = new List<string>();
                        matchedProductCodeList = authorizationProducts
                            .Where(x => x.Proposition == true).Select(x => x.ProductCode).ToList();

                        var matchedPolicyList = new List<ProductCodePolicyStat>();

                        policyStatusList = new List<AuthorizationStatus>();
                        policyStatusList = GetAuthByPolicyStatus(matchedProductCodeList, userPolicyList, out matchedPolicyList);


                        if (policyStatusList != null)
                        {
                            policyResult.Proposition = policyStatusList.Any(x => x.Proposition == true);

                            if (policyResult.Proposition)
                            {
                                var matchedPolicyStatusList = policyStatusList
                                    .Where(x => x.Proposition == true)
                                    .Select(x => x.Status)
                                    .ToList();


                                var matchedList = matchedPolicyList
                                    .Where(x => matchedPolicyStatusList.Contains(x.PolicyStatus))
                                    .Select(x => new { x.ProductCode, x.PolicyStatus })
                                    .ToList();

                                premiumStatusList = new List<AuthorizationStatus>();
                                premiumStatusList = GetAuthByPremiumStatus(userPolicyList
                                    , matchedList.Select(x => x.ProductCode).ToList()
                                    , matchedList.Select(x => x.PolicyStatus).ToList());

                                if (premiumStatusList != null)
                                {
                                    premiumResult.Proposition = premiumStatusList.Any(x => x.Proposition == true);
                                }
                            }
                        }

                        #endregion
                    }
                    if (authorizationProducts.Any(x => x.Claim == true))
                    {
                        #region #policyResult & premiumResult
                        matchedProductCodeList = new List<string>();
                        matchedProductCodeList = authorizationProducts
                            .Where(x => x.Claim == true).Select(x => x.ProductCode).ToList();

                        var matchedPolicyList = new List<ProductCodePolicyStat>();

                        policyStatusList = new List<AuthorizationStatus>();
                        policyStatusList = GetAuthByPolicyStatus(matchedProductCodeList, userPolicyList, out matchedPolicyList);

                        if (policyStatusList != null)
                        {
                            policyResult.Claim = policyStatusList.Any(x => x.Claim == true);

                            if (policyResult.Claim)
                            {
                                var matchedPolicyStatusList = policyStatusList
                                    .Where(x => x.Claim == true)
                                    .Select(x => x.Status)
                                    .ToList();


                                var matchedList = matchedPolicyList
                                    .Where(x => matchedPolicyStatusList.Contains(x.PolicyStatus))
                                    .Select(x => new { x.ProductCode, x.PolicyStatus })
                                    .ToList();

                                premiumStatusList = new List<AuthorizationStatus>();
                                premiumStatusList = GetAuthByPremiumStatus(userPolicyList
                                    , matchedList.Select(x => x.ProductCode).ToList()
                                    , matchedList.Select(x => x.PolicyStatus).ToList());

                                if (premiumStatusList != null)
                                {
                                    premiumResult.Claim = premiumStatusList.Any(x => x.Claim == true);
                                }
                            }
                        }


                        #endregion
                    }

                    if (authorizationProducts.Any(x => x.PolicyHolderDetails == true))
                    {
                        #region #policyResult & premiumResult
                        matchedProductCodeList = new List<string>();
                        matchedProductCodeList = authorizationProducts
                            .Where(x => x.PolicyHolderDetails == true).Select(x => x.ProductCode).ToList();

                        var matchedPolicyList = new List<ProductCodePolicyStat>();

                        policyStatusList = new List<AuthorizationStatus>();
                        policyStatusList = GetAuthByPolicyStatus(matchedProductCodeList, userPolicyList, out matchedPolicyList);

                        if (policyStatusList != null)
                        {
                            policyResult.PolicyHolderDetails = policyStatusList.Any(x => x.PolicyHolderDetails == true);

                            if (policyResult.PolicyHolderDetails)
                            {
                                var matchedPolicyStatusList = policyStatusList
                                    .Where(x => x.PolicyHolderDetails == true)
                                    .Select(x => x.Status)
                                    .ToList();


                                var matchedList = matchedPolicyList
                                    .Where(x => matchedPolicyStatusList.Contains(x.PolicyStatus))
                                    .Select(x => new { x.ProductCode, x.PolicyStatus })
                                    .ToList();

                                premiumStatusList = new List<AuthorizationStatus>();
                                premiumStatusList = GetAuthByPremiumStatus(userPolicyList
                                    , matchedList.Select(x => x.ProductCode).ToList()
                                    , matchedList.Select(x => x.PolicyStatus).ToList());

                                if (premiumStatusList != null)
                                {
                                    premiumResult.PolicyHolderDetails = premiumStatusList.Any(x => x.PolicyHolderDetails == true);
                                }
                            }
                        }


                        #endregion
                    }
                    if (authorizationProducts.Any(x => x.InsuredDetails == true))
                    {
                        #region #policyResult & premiumResult
                        matchedProductCodeList = new List<string>();
                        matchedProductCodeList = authorizationProducts
                            .Where(x => x.InsuredDetails == true).Select(x => x.ProductCode).ToList();

                        var matchedPolicyList = new List<ProductCodePolicyStat>();

                        policyStatusList = new List<AuthorizationStatus>();
                        policyStatusList = GetAuthByPolicyStatus(matchedProductCodeList, userPolicyList, out matchedPolicyList);


                        if (policyStatusList != null)
                        {
                            policyResult.InsuredDetails = policyStatusList.Any(x => x.InsuredDetails == true);

                            if (policyResult.InsuredDetails)
                            {
                                var matchedPolicyStatusList = policyStatusList
                                    .Where(x => x.InsuredDetails == true)
                                    .Select(x => x.Status)
                                    .ToList();


                                var matchedList = matchedPolicyList
                                    .Where(x => matchedPolicyStatusList.Contains(x.PolicyStatus))
                                    .Select(x => new { x.ProductCode, x.PolicyStatus })
                                    .ToList();

                                premiumStatusList = new List<AuthorizationStatus>();
                                premiumStatusList = GetAuthByPremiumStatus(userPolicyList
                                    , matchedList.Select(x => x.ProductCode).ToList()
                                    , matchedList.Select(x => x.PolicyStatus).ToList());

                                if (premiumStatusList != null)
                                {
                                    premiumResult.InsuredDetails = premiumStatusList.Any(x => x.InsuredDetails == true);
                                }
                            }
                        }

                        #endregion
                    }
                    if (authorizationProducts.Any(x => x.BeneficiaryInfo == true))
                    {
                        #region #policyResult & premiumResult
                        matchedProductCodeList = new List<string>();
                        matchedProductCodeList = authorizationProducts
                            .Where(x => x.BeneficiaryInfo == true).Select(x => x.ProductCode).ToList();

                        var matchedPolicyList = new List<ProductCodePolicyStat>();

                        policyStatusList = new List<AuthorizationStatus>();
                        policyStatusList = GetAuthByPolicyStatus(matchedProductCodeList, userPolicyList, out matchedPolicyList);

                        if (policyStatusList != null)
                        {
                            policyResult.BeneficiaryInfo = policyStatusList.Any(x => x.BeneficiaryInfo == true);

                            if (policyResult.BeneficiaryInfo)
                            {
                                var matchedPolicyStatusList = policyStatusList
                                    .Where(x => x.BeneficiaryInfo == true)
                                    .Select(x => x.Status)
                                    .ToList();


                                var matchedList = matchedPolicyList
                                    .Where(x => matchedPolicyStatusList.Contains(x.PolicyStatus))
                                    .Select(x => new { x.ProductCode, x.PolicyStatus })
                                    .ToList();

                                premiumStatusList = new List<AuthorizationStatus>();
                                premiumStatusList = GetAuthByPremiumStatus(userPolicyList
                                    , matchedList.Select(x => x.ProductCode).ToList()
                                    , matchedList.Select(x => x.PolicyStatus).ToList());

                                if (premiumStatusList != null)
                                {
                                    premiumResult.BeneficiaryInfo = premiumStatusList.Any(x => x.BeneficiaryInfo == true);
                                }
                            }
                        }


                        #endregion
                    }
                    if (authorizationProducts.Any(x => x.LapseReinstatement == true))
                    {
                        #region #policyResult & premiumResult
                        matchedProductCodeList = new List<string>();
                        matchedProductCodeList = authorizationProducts
                            .Where(x => x.LapseReinstatement == true).Select(x => x.ProductCode).ToList();

                        var matchedPolicyList = new List<ProductCodePolicyStat>();

                        policyStatusList = new List<AuthorizationStatus>();
                        policyStatusList = GetAuthByPolicyStatus(matchedProductCodeList, userPolicyList, out matchedPolicyList);

                        if (policyStatusList != null)
                        {
                            policyResult.LapseReinstatement = policyStatusList.Any(x => x.LapseReinstatement == true);

                            if (policyResult.LapseReinstatement)
                            {
                                var matchedPolicyStatusList = policyStatusList
                                    .Where(x => x.LapseReinstatement == true)
                                    .Select(x => x.Status)
                                    .ToList();


                                var matchedList = matchedPolicyList
                                    .Where(x => matchedPolicyStatusList.Contains(x.PolicyStatus))
                                    .Select(x => new { x.ProductCode, x.PolicyStatus })
                                    .ToList();

                                premiumStatusList = new List<AuthorizationStatus>();
                                premiumStatusList = GetAuthByPremiumStatus(userPolicyList
                                    , matchedList.Select(x => x.ProductCode).ToList()
                                    , matchedList.Select(x => x.PolicyStatus).ToList());

                                if (premiumStatusList != null)
                                {
                                    premiumResult.LapseReinstatement = premiumStatusList.Any(x => x.LapseReinstatement == true);
                                }
                            }
                        }


                        #endregion
                    }
                    if (authorizationProducts.Any(x => x.HealthRenewal == true))
                    {
                        #region #policyResult & premiumResult
                        matchedProductCodeList = new List<string>();
                        matchedProductCodeList = authorizationProducts
                            .Where(x => x.HealthRenewal == true).Select(x => x.ProductCode).ToList();

                        var matchedPolicyList = new List<ProductCodePolicyStat>();

                        policyStatusList = new List<AuthorizationStatus>();
                        policyStatusList = GetAuthByPolicyStatus(matchedProductCodeList, userPolicyList, out matchedPolicyList);

                        if (policyStatusList != null)
                        {
                            policyResult.HealthRenewal = policyStatusList.Any(x => x.HealthRenewal == true);

                            if (policyResult.HealthRenewal)
                            {
                                var matchedPolicyStatusList = policyStatusList
                                    .Where(x => x.HealthRenewal == true)
                                    .Select(x => x.Status)
                                    .ToList();


                                var matchedList = matchedPolicyList
                                    .Where(x => matchedPolicyStatusList.Contains(x.PolicyStatus))
                                    .Select(x => new { x.ProductCode, x.PolicyStatus })
                                    .ToList();

                                premiumStatusList = new List<AuthorizationStatus>();
                                premiumStatusList = GetAuthByPremiumStatus(userPolicyList
                                    , matchedList.Select(x => x.ProductCode).ToList()
                                    , matchedList.Select(x => x.PolicyStatus).ToList());

                                if (premiumStatusList != null)
                                {
                                    premiumResult.HealthRenewal = premiumStatusList.Any(x => x.HealthRenewal == true);
                                }
                            }
                        }


                        #endregion
                    }
                    if (authorizationProducts.Any(x => x.PolicyLoanRepayment == true))
                    {
                        #region #policyResult & premiumResult
                        matchedProductCodeList = new List<string>();
                        matchedProductCodeList = authorizationProducts
                            .Where(x => x.PolicyLoanRepayment == true).Select(x => x.ProductCode).ToList();

                        var matchedPolicyList = new List<ProductCodePolicyStat>();

                        policyStatusList = new List<AuthorizationStatus>();
                        policyStatusList = GetAuthByPolicyStatus(matchedProductCodeList, userPolicyList, out matchedPolicyList);

                        if (policyStatusList != null)
                        {
                            policyResult.PolicyLoanRepayment = policyStatusList.Any(x => x.PolicyLoanRepayment == true);

                            if (policyResult.PolicyLoanRepayment)
                            {
                                var matchedPolicyStatusList = policyStatusList
                                    .Where(x => x.PolicyLoanRepayment == true)
                                    .Select(x => x.Status)
                                    .ToList();


                                var matchedList = matchedPolicyList
                                    .Where(x => matchedPolicyStatusList.Contains(x.PolicyStatus))
                                    .Select(x => new { x.ProductCode, x.PolicyStatus })
                                    .ToList();

                                premiumStatusList = new List<AuthorizationStatus>();
                                premiumStatusList = GetAuthByPremiumStatus(userPolicyList
                                    , matchedList.Select(x => x.ProductCode).ToList()
                                    , matchedList.Select(x => x.PolicyStatus).ToList());

                                if (premiumStatusList != null)
                                {
                                    premiumResult.PolicyLoanRepayment = premiumStatusList.Any(x => x.PolicyLoanRepayment == true);
                                }
                            }
                        }


                        #endregion
                    }
                    if (authorizationProducts.Any(x => x.Acp == true))
                    {
                        #region #policyResult & premiumResult
                        matchedProductCodeList = new List<string>();
                        matchedProductCodeList = authorizationProducts
                            .Where(x => x.Acp == true).Select(x => x.ProductCode).ToList();

                        var matchedPolicyList = new List<ProductCodePolicyStat>();

                        policyStatusList = new List<AuthorizationStatus>();
                        policyStatusList = GetAuthByPolicyStatus(matchedProductCodeList, userPolicyList, out matchedPolicyList);

                        if (policyStatusList != null)
                        {
                            policyResult.ACP = policyStatusList.Any(x => x.Acp == true);

                            if (policyResult.ACP)
                            {
                                var matchedPolicyStatusList = policyStatusList
                                    .Where(x => x.Acp == true)
                                    .Select(x => x.Status)
                                    .ToList();


                                var matchedList = matchedPolicyList
                                    .Where(x => matchedPolicyStatusList.Contains(x.PolicyStatus))
                                    .Select(x => new { x.ProductCode, x.PolicyStatus })
                                    .ToList();

                                premiumStatusList = new List<AuthorizationStatus>();
                                premiumStatusList = GetAuthByPremiumStatus(userPolicyList
                                    , matchedList.Select(x => x.ProductCode).ToList()
                                    , matchedList.Select(x => x.PolicyStatus).ToList());

                                if (premiumStatusList != null)
                                {
                                    premiumResult.ACP = premiumStatusList.Any(x => x.Acp == true);
                                }
                            }
                        }


                        #endregion
                    }
                    if (authorizationProducts.Any(x => x.AdhocTopup == true))
                    {
                        #region #policyResult & premiumResult
                        matchedProductCodeList = new List<string>();
                        matchedProductCodeList = authorizationProducts
                            .Where(x => x.AdhocTopup == true).Select(x => x.ProductCode).ToList();

                        var matchedPolicyList = new List<ProductCodePolicyStat>();

                        policyStatusList = new List<AuthorizationStatus>();
                        policyStatusList = GetAuthByPolicyStatus(matchedProductCodeList, userPolicyList, out matchedPolicyList);

                        if (policyStatusList != null)
                        {
                            policyResult.AdhocTopup = policyStatusList.Any(x => x.AdhocTopup == true);

                            if (policyResult.AdhocTopup)
                            {
                                var matchedPolicyStatusList = policyStatusList
                                    .Where(x => x.AdhocTopup == true)
                                    .Select(x => x.Status)
                                    .ToList();


                                var matchedList = matchedPolicyList
                                    .Where(x => matchedPolicyStatusList.Contains(x.PolicyStatus))
                                    .Select(x => new { x.ProductCode, x.PolicyStatus })
                                    .ToList();

                                premiumStatusList = new List<AuthorizationStatus>();
                                premiumStatusList = GetAuthByPremiumStatus(userPolicyList
                                    , matchedList.Select(x => x.ProductCode).ToList()
                                    , matchedList.Select(x => x.PolicyStatus).ToList());

                                if (premiumStatusList != null)
                                {
                                    premiumResult.AdhocTopup = premiumStatusList.Any(x => x.AdhocTopup == true);
                                }
                            }
                        }


                        #endregion
                    }
                    if (authorizationProducts.Any(x => x.PartialWithdrawal == true))
                    {
                        #region #policyResult & premiumResult
                        matchedProductCodeList = new List<string>();
                        matchedProductCodeList = authorizationProducts
                            .Where(x => x.PartialWithdrawal == true).Select(x => x.ProductCode).ToList();

                        var matchedPolicyList = new List<ProductCodePolicyStat>();

                        policyStatusList = new List<AuthorizationStatus>();
                        policyStatusList = GetAuthByPolicyStatus(matchedProductCodeList, userPolicyList, out matchedPolicyList);

                        if (policyStatusList != null)
                        {
                            policyResult.PartialWithdrawal = policyStatusList.Any(x => x.PartialWithdrawal == true);

                            if (policyResult.PartialWithdrawal)
                            {
                                var matchedPolicyStatusList = policyStatusList
                                    .Where(x => x.PartialWithdrawal == true)
                                    .Select(x => x.Status)
                                    .ToList();


                                var matchedList = matchedPolicyList
                                    .Where(x => matchedPolicyStatusList.Contains(x.PolicyStatus))
                                    .Select(x => new { x.ProductCode, x.PolicyStatus })
                                    .ToList();

                                premiumStatusList = new List<AuthorizationStatus>();
                                premiumStatusList = GetAuthByPremiumStatus(userPolicyList
                                    , matchedList.Select(x => x.ProductCode).ToList()
                                    , matchedList.Select(x => x.PolicyStatus).ToList());

                                if (premiumStatusList != null)
                                {
                                    premiumResult.PartialWithdrawal = premiumStatusList.Any(x => x.PartialWithdrawal == true);
                                }
                            }
                        }


                        #endregion
                    }
                    if (authorizationProducts.Any(x => x.PolicyLoan == true))
                    {
                        #region #policyResult & premiumResult
                        matchedProductCodeList = new List<string>();
                        matchedProductCodeList = authorizationProducts
                            .Where(x => x.PolicyLoan == true).Select(x => x.ProductCode).ToList();

                        var matchedPolicyList = new List<ProductCodePolicyStat>();

                        policyStatusList = new List<AuthorizationStatus>();
                        policyStatusList = GetAuthByPolicyStatus(matchedProductCodeList, userPolicyList, out matchedPolicyList);

                        if (policyStatusList != null)
                        {
                            policyResult.PolicyLoan = policyStatusList.Any(x => x.PolicyLoan == true);

                            if (policyResult.PolicyLoan)
                            {
                                var matchedPolicyStatusList = policyStatusList
                                    .Where(x => x.PolicyLoan == true)
                                    .Select(x => x.Status)
                                    .ToList();


                                var matchedList = matchedPolicyList
                                    .Where(x => matchedPolicyStatusList.Contains(x.PolicyStatus))
                                    .Select(x => new { x.ProductCode, x.PolicyStatus })
                                    .ToList();

                                premiumStatusList = new List<AuthorizationStatus>();
                                premiumStatusList = GetAuthByPremiumStatus(userPolicyList
                                    , matchedList.Select(x => x.ProductCode).ToList()
                                    , matchedList.Select(x => x.PolicyStatus).ToList());

                                if (premiumStatusList != null)
                                {
                                    premiumResult.PolicyLoan = premiumStatusList.Any(x => x.PolicyLoan == true);
                                }
                            }
                        }


                        #endregion
                    }

                    if (authorizationProducts.Any(x => x.PolicySurrender == true))
                    {
                        #region #policyResult & premiumResult
                        matchedProductCodeList = new List<string>();
                        matchedProductCodeList = authorizationProducts
                            .Where(x => x.PolicySurrender == true).Select(x => x.ProductCode).ToList();

                        var matchedPolicyList = new List<ProductCodePolicyStat>();

                        policyStatusList = new List<AuthorizationStatus>();
                        policyStatusList = GetAuthByPolicyStatus(matchedProductCodeList, userPolicyList, out matchedPolicyList);

                        if (policyStatusList != null)
                        {
                            policyResult.PolicySurrender = policyStatusList.Any(x => x.PolicySurrender == true);

                            if (policyResult.PolicySurrender)
                            {
                                var matchedPolicyStatusList = policyStatusList
                                    .Where(x => x.PolicySurrender == true)
                                    .Select(x => x.Status)
                                    .ToList();


                                var matchedList = matchedPolicyList
                                    .Where(x => matchedPolicyStatusList.Contains(x.PolicyStatus))
                                    .Select(x => new { x.ProductCode, x.PolicyStatus })
                                    .ToList();

                                premiumStatusList = new List<AuthorizationStatus>();
                                premiumStatusList = GetAuthByPremiumStatus(userPolicyList
                                    , matchedList.Select(x => x.ProductCode).ToList()
                                    , matchedList.Select(x => x.PolicyStatus).ToList());

                                if (premiumStatusList != null)
                                {
                                    premiumResult.PolicySurrender = premiumStatusList.Any(x => x.PolicySurrender == true);
                                }
                            }
                        }


                        #endregion
                    }
                    if (authorizationProducts.Any(x => x.PaymentFrequency == true))
                    {
                        #region #policyResult & premiumResult
                        matchedProductCodeList = new List<string>();
                        matchedProductCodeList = authorizationProducts
                            .Where(x => x.PaymentFrequency == true).Select(x => x.ProductCode).ToList();

                        var matchedPolicyList = new List<ProductCodePolicyStat>();

                        policyStatusList = new List<AuthorizationStatus>();
                        policyStatusList = GetAuthByPolicyStatus(matchedProductCodeList, userPolicyList, out matchedPolicyList);

                        if (policyStatusList != null)
                        {
                            policyResult.PaymentFrequency = policyStatusList.Any(x => x.PaymentFrequency == true);

                            if (policyResult.PaymentFrequency)
                            {
                                var matchedPolicyStatusList = policyStatusList
                                    .Where(x => x.PaymentFrequency == true)
                                    .Select(x => x.Status)
                                    .ToList();


                                var matchedList = matchedPolicyList
                                    .Where(x => matchedPolicyStatusList.Contains(x.PolicyStatus))
                                    .Select(x => new { x.ProductCode, x.PolicyStatus })
                                    .ToList();

                                premiumStatusList = new List<AuthorizationStatus>();
                                premiumStatusList = GetAuthByPremiumStatus(userPolicyList
                                    , matchedList.Select(x => x.ProductCode).ToList()
                                    , matchedList.Select(x => x.PolicyStatus).ToList());

                                if (premiumStatusList != null)
                                {
                                    premiumResult.PaymentFrequency = premiumStatusList.Any(x => x.PaymentFrequency == true);
                                }
                            }
                        }


                        #endregion
                    }
                    if (authorizationProducts.Any(x => x.SumAssuredChange == true))
                    {
                        #region #policyResult & premiumResult
                        matchedProductCodeList = new List<string>();
                        matchedProductCodeList = authorizationProducts
                            .Where(x => x.SumAssuredChange == true).Select(x => x.ProductCode).ToList();

                        var matchedPolicyList = new List<ProductCodePolicyStat>();

                        policyStatusList = new List<AuthorizationStatus>();
                        policyStatusList = GetAuthByPolicyStatus(matchedProductCodeList, userPolicyList, out matchedPolicyList);

                        if (policyStatusList != null)
                        {
                            policyResult.SumAssuredChange = policyStatusList.Any(x => x.SumAssuredChange == true);

                            if (policyResult.SumAssuredChange)
                            {
                                var matchedPolicyStatusList = policyStatusList
                                    .Where(x => x.SumAssuredChange == true)
                                    .Select(x => x.Status)
                                    .ToList();


                                var matchedList = matchedPolicyList
                                    .Where(x => matchedPolicyStatusList.Contains(x.PolicyStatus))
                                    .Select(x => new { x.ProductCode, x.PolicyStatus })
                                    .ToList();

                                premiumStatusList = new List<AuthorizationStatus>();
                                premiumStatusList = GetAuthByPremiumStatus(userPolicyList
                                    , matchedList.Select(x => x.ProductCode).ToList()
                                    , matchedList.Select(x => x.PolicyStatus).ToList());

                                if (premiumStatusList != null)
                                {
                                    premiumResult.SumAssuredChange = premiumStatusList.Any(x => x.SumAssuredChange == true);
                                }
                            }
                        }


                        #endregion
                    }
                    if (authorizationProducts.Any(x => x.RefundofPayment == true))
                    {
                        #region #policyResult & premiumResult
                        matchedProductCodeList = new List<string>();
                        matchedProductCodeList = authorizationProducts
                            .Where(x => x.RefundofPayment == true).Select(x => x.ProductCode).ToList();

                        var matchedPolicyList = new List<ProductCodePolicyStat>();

                        policyStatusList = new List<AuthorizationStatus>();
                        policyStatusList = GetAuthByPolicyStatus(matchedProductCodeList, userPolicyList, out matchedPolicyList);

                        if (policyStatusList != null)
                        {
                            policyResult.RefundofPayment = policyStatusList.Any(x => x.RefundofPayment == true);

                            if (policyResult.RefundofPayment)
                            {
                                var matchedPolicyStatusList = policyStatusList
                                    .Where(x => x.RefundofPayment == true)
                                    .Select(x => x.Status)
                                    .ToList();


                                var matchedList = matchedPolicyList
                                    .Where(x => matchedPolicyStatusList.Contains(x.PolicyStatus))
                                    .Select(x => new { x.ProductCode, x.PolicyStatus })
                                    .ToList();

                                premiumStatusList = new List<AuthorizationStatus>();
                                premiumStatusList = GetAuthByPremiumStatus(userPolicyList
                                    , matchedList.Select(x => x.ProductCode).ToList()
                                    , matchedList.Select(x => x.PolicyStatus).ToList());

                                if (premiumStatusList != null)
                                {
                                    premiumResult.RefundofPayment = premiumStatusList.Any(x => x.RefundofPayment == true);
                                }
                            }
                        }


                        #endregion
                    }
                }


                var compositeResult = new AuthorizationResult();
                if (premiumResult != null && policyResult != null)
                {
                    compositeResult.Registration = (policyResult.Registration && premiumResult.Registration);
                    compositeResult.Login = (policyResult.Login && premiumResult.Login);
                    compositeResult.ViewMyPolicies = (policyResult.ViewMyPolicies && premiumResult.ViewMyPolicies);
                    compositeResult.Proposition = (policyResult.Proposition && premiumResult.Proposition);
                    compositeResult.Claim = (policyResult.Claim && premiumResult.Claim);


                    compositeResult.PolicyHolderDetails = (policyResult.PolicyHolderDetails && premiumResult.PolicyHolderDetails);
                    compositeResult.InsuredDetails = (policyResult.InsuredDetails && premiumResult.InsuredDetails);
                    compositeResult.BeneficiaryInfo = (policyResult.BeneficiaryInfo && premiumResult.BeneficiaryInfo);
                    compositeResult.LapseReinstatement = (policyResult.LapseReinstatement && premiumResult.LapseReinstatement);
                    compositeResult.HealthRenewal = (policyResult.HealthRenewal && premiumResult.HealthRenewal);
                    compositeResult.PolicyLoanRepayment = (policyResult.PolicyLoanRepayment && premiumResult.PolicyLoanRepayment);
                    compositeResult.ACP = (policyResult.ACP && premiumResult.ACP);
                    compositeResult.AdhocTopup = (policyResult.AdhocTopup && premiumResult.AdhocTopup);
                    compositeResult.PartialWithdrawal = (policyResult.PartialWithdrawal && premiumResult.PartialWithdrawal);
                    compositeResult.PolicyLoan = (policyResult.PolicyLoan && premiumResult.PolicyLoan);
                    compositeResult.PolicyPaidup = (policyResult.PolicyPaidup && premiumResult.PolicyPaidup);
                    compositeResult.PolicySurrender = (policyResult.PolicySurrender && premiumResult.PolicySurrender);
                    compositeResult.PaymentFrequency = (policyResult.PaymentFrequency && premiumResult.PaymentFrequency);
                    compositeResult.SumAssuredChange = (policyResult.SumAssuredChange && premiumResult.SumAssuredChange);
                    compositeResult.RefundofPayment = (policyResult.RefundofPayment && premiumResult.RefundofPayment);
                }


                return compositeResult;
            }
            catch (Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

            }

            return null;
        }

        public AuthorizationResult CheckAuthorization(Guid? memberId, string? IdValue)
        {

            var clientNoList = new List<string>();

            if (!string.IsNullOrEmpty(IdValue))
            {
                clientNoList = unitOfWork.GetRepository<Entities.Client>()
                            .Query(x => x.Nrc == IdValue
                            || x.PassportNo == IdValue
                            || x.Other == IdValue)
                            .Select(x => x.ClientNo)
                            .ToList();
            }
            else if (memberId != null)
            {
                clientNoList = GetClientNoListByIdValue(memberId);
            }

            if (clientNoList != null && clientNoList.Any())
            {
                var authMatrixPerson = unitOfWork.GetRepository<Entities.AuthorizationPerson>()
                    .Query()
                    .ToList();

                Console.WriteLine($"CheckAuthorization => IdValue  => {IdValue} clientNoList  => {string.Join(",", clientNoList)}");

                var authPerson = GetAuthorizationsByPerson(clientNoList);

                Console.WriteLine($"CheckAuthorization => authPerson => {JsonConvert.SerializeObject(authPerson)}");



                //var userPolicyList = unitOfWork.GetRepository<Entities.Policy>()
                //        .Query(x => clientNoList.Contains(x.PolicyHolderClientNo) || clientNoList.Contains(x.InsuredPersonClientNo))
                //        .Select(x => new ProductCodePolicyStat
                //        {
                //            ProductCode = x.ProductType,
                //            PolicyStatus = x.PolicyStatus,
                //            PremiumStatus = x.PremiumStatus,
                //            PolicyNumber = x.PolicyNo,
                //        })
                //        .ToList();


                var userPolicyList = new List<ProductCodePolicyStat>();

                var ownerPolicyList = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => clientNoList.Contains(x.PolicyHolderClientNo))
                        .Select(x => new ProductCodePolicyStat
                        {
                            ProductCode = x.ProductType,
                            PolicyStatus = x.PolicyStatus,
                            PremiumStatus = x.PremiumStatus,
                            PolicyNumber = x.PolicyNo,
                        })
                        .ToList();

                var insuredPolicyList = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => clientNoList.Contains(x.InsuredPersonClientNo))
                        .Select(x => new ProductCodePolicyStat
                        {
                            ProductCode = x.ProductType,
                            PolicyStatus = x.PolicyStatus,
                            PremiumStatus = x.PremiumStatus,
                            PolicyNumber = x.PolicyNo,
                        })
                        .ToList();

                #region #userPolicyList
                if (ownerPolicyList?.Any() == true || insuredPolicyList?.Any() == true)
                {
                    if (ownerPolicyList?.Any() == true)
                    {
                        userPolicyList.AddRange(ownerPolicyList);
                    }

                    if (insuredPolicyList?.Any() == true)
                    {
                        userPolicyList.AddRange(insuredPolicyList);
                    }
                }
                #endregion


                if (userPolicyList != null && userPolicyList.Any())
                {
                    var productCodeList = userPolicyList.Select(x => x.ProductCode).ToList().Distinct().ToList();


                    var authProduct = GetAuthorizationsByProduct(clientNoList, productCodeList);

                    var authStatus = GetAuthorizationsByStatus(clientNoList, userPolicyList);

                    if (authPerson == null || authProduct == null || authStatus == null)
                    {
                        return new AuthorizationResult();
                    }



                    if (memberId == new Guid("a6101190-eae0-4701-903d-2e676784629b"))
                    {
                        Console.WriteLine($"CheckAuthorization => productCodeList => {string.Join("", productCodeList)}");
                        Console.WriteLine($"CheckAuthorization => authProduct => {JsonConvert.SerializeObject(authProduct)}");
                        Console.WriteLine($"CheckAuthorization => authStatus => {JsonConvert.SerializeObject(authStatus)}");
                    }

                    var authResult = new AuthorizationResult();



                    var ownerProductCodeList = new List<string>();
                    var ownerAuthProduct = new AuthorizationResult();
                    var ownerAuthStatus = new AuthorizationResult();
                    if (ownerPolicyList?.Any() == true)
                    {
                        ownerProductCodeList = ownerPolicyList.Select(x => x.ProductCode).ToList().Distinct().ToList();



                        ownerAuthProduct = GetAuthorizationsByProduct(clientNoList, ownerProductCodeList);
                        ownerAuthStatus = GetAuthorizationsByStatus(clientNoList, ownerPolicyList);


                        if (memberId == new Guid("a6101190-eae0-4701-903d-2e676784629b"))
                        {
                            Console.WriteLine($"CheckAuthorization => ownerProductCodeList => {string.Join("", ownerProductCodeList)}");
                            Console.WriteLine($"CheckAuthorization => ownerAuthProduct => {JsonConvert.SerializeObject(ownerAuthProduct)}");
                            Console.WriteLine($"CheckAuthorization => ownerAuthStatus => {JsonConvert.SerializeObject(ownerAuthStatus)}");
                        }


                    }

                    #region #Registration
                    var personType = authMatrixPerson.Where(x => x.Registration == true).Select(x => x.PersonType).ToList();

                    if (memberId == new Guid("a6101190-eae0-4701-903d-2e676784629b"))
                    {
                        Console.WriteLine($"CheckAuthorization => Registration => personType => {string.Join("", personType)}");
                    }

                    if (personType?.Contains(EnumAuthPersonType.Insured.ToString()) == true
                        && personType?.Contains(EnumAuthPersonType.PolicyHolder.ToString()) == true)
                    {
                        authResult.Registration = (authPerson.Registration
                            && authProduct.Registration
                            && authStatus.Registration);
                    }
                    else
                    {
                        authResult.Registration = (authPerson.Registration
                            && ownerAuthProduct?.Registration == true
                            && ownerAuthStatus?.Registration == true);
                    }
                    #endregion

                    #region #Login
                    personType = authMatrixPerson.Where(x => x.Login == true).Select(x => x.PersonType).ToList();
                    if (personType?.Contains(EnumAuthPersonType.Insured.ToString()) == true
                        && personType?.Contains(EnumAuthPersonType.PolicyHolder.ToString()) == true)
                    {
                        authResult.Login = (authPerson.Login
                            && authProduct.Login
                            && authStatus.Login);
                    }
                    else
                    {
                        authResult.Login = (authPerson.Login
                            && ownerAuthProduct?.Login == true
                            && ownerAuthStatus?.Login == true);
                    }

                    #endregion

                    #region #ViewMyPolicies
                    personType = authMatrixPerson.Where(x => x.ViewMyPolicies == true).Select(x => x.PersonType).ToList();

                    if (memberId == new Guid("a6101190-eae0-4701-903d-2e676784629b"))
                    {
                        Console.WriteLine($"CheckAuthorization => ViewMyPolicies => personType => {string.Join("", personType)}");
                    }
                    if (personType?.Contains(EnumAuthPersonType.Insured.ToString()) == true
                        && personType?.Contains(EnumAuthPersonType.PolicyHolder.ToString()) == true)
                    {
                        authResult.ViewMyPolicies = (authPerson.ViewMyPolicies
                            && authProduct.ViewMyPolicies
                            && authStatus.ViewMyPolicies);
                    }
                    else
                    {
                        authResult.ViewMyPolicies = (authPerson.ViewMyPolicies
                            && ownerAuthProduct?.ViewMyPolicies == true
                            && ownerAuthStatus?.ViewMyPolicies == true);
                    }

                    #endregion


                    #region #Proposition
                    personType = authMatrixPerson.Where(x => x.Proposition == true).Select(x => x.PersonType).ToList();
                    if (personType?.Contains(EnumAuthPersonType.Insured.ToString()) == true
                        && personType?.Contains(EnumAuthPersonType.PolicyHolder.ToString()) == true)
                    {
                        authResult.Proposition = (authPerson.Proposition
                            && authProduct.Proposition
                            && authStatus.Proposition);
                    }
                    else
                    {
                        authResult.Proposition = (authPerson.Proposition
                            && ownerAuthProduct?.Proposition == true
                            && ownerAuthStatus?.Proposition == true);
                    }
                    #endregion

                    #region #Claim

                    personType = authMatrixPerson.Where(x => x.Claim == true).Select(x => x.PersonType).ToList();

                    if (memberId == new Guid("a6101190-eae0-4701-903d-2e676784629b"))
                    {
                        Console.WriteLine($"CheckAuthorization => Claim => personType => {string.Join("", personType)}");
                    }
                    if (personType?.Contains(EnumAuthPersonType.Insured.ToString()) == true
                        && personType?.Contains(EnumAuthPersonType.PolicyHolder.ToString()) == true)
                    {
                        authResult.Claim = (authPerson.Claim
                            && authProduct.Claim
                            && authStatus.Claim);
                    }
                    else
                    {
                        authResult.Claim = (authPerson.Claim
                            && ownerAuthProduct?.Claim == true
                            && ownerAuthStatus?.Claim == true);

                        if (memberId == new Guid("a6101190-eae0-4701-903d-2e676784629b"))
                        {
                            Console.WriteLine($"CheckAuthorization => ownerAuthProduct?.Claim => {ownerAuthProduct?.Claim}" +
                                $", ownerAuthStatus?.Claim => {ownerAuthStatus?.Claim}");

                        }

                    }

                    #endregion




                    personType = authMatrixPerson.Where(x => x.PolicyHolderDetails == true).Select(x => x.PersonType).ToList();
                    if (memberId == new Guid("a6101190-eae0-4701-903d-2e676784629b"))
                    {
                        Console.WriteLine($"CheckAuthorization => PolicyHolderDetails => personType => {string.Join("", personType)}");
                    }

                    if (personType?.Contains(EnumAuthPersonType.Insured.ToString()) == true
                        && personType?.Contains(EnumAuthPersonType.PolicyHolder.ToString()) == true)
                    {
                        authResult.PolicyHolderDetails = (authPerson.PolicyHolderDetails
                            && authProduct.PolicyHolderDetails
                            && authStatus.PolicyHolderDetails);

                    }
                    else
                    {
                        authResult.PolicyHolderDetails = (authPerson.PolicyHolderDetails
                            && ownerAuthProduct?.PolicyHolderDetails == true
                            && ownerAuthStatus?.PolicyHolderDetails == true);

                    }


                    personType = authMatrixPerson.Where(x => x.InsuredDetails == true).Select(x => x.PersonType).ToList();

                    if (personType?.Contains(EnumAuthPersonType.Insured.ToString()) == true
                        && personType?.Contains(EnumAuthPersonType.PolicyHolder.ToString()) == true)
                    {
                        authResult.InsuredDetails = (authPerson.InsuredDetails
                            && authProduct.InsuredDetails
                            && authStatus.InsuredDetails);

                    }
                    else
                    {
                        authResult.InsuredDetails = (authPerson.InsuredDetails
                            && ownerAuthProduct?.InsuredDetails == true &&
                            ownerAuthStatus?.InsuredDetails == true);

                    }

                    personType = authMatrixPerson.Where(x => x.BeneficiaryInfo == true).Select(x => x.PersonType).ToList();

                    if (personType?.Contains(EnumAuthPersonType.Insured.ToString()) == true
                        && personType?.Contains(EnumAuthPersonType.PolicyHolder.ToString()) == true)
                    {
                        authResult.BeneficiaryInfo = (authPerson.BeneficiaryInfo
                            && authProduct.BeneficiaryInfo
                            && authStatus.BeneficiaryInfo);

                    }
                    else
                    {
                        authResult.BeneficiaryInfo = (authPerson.BeneficiaryInfo
                            && ownerAuthProduct?.BeneficiaryInfo == true
                            && ownerAuthStatus?.BeneficiaryInfo == true);

                    }


                    personType = authMatrixPerson.Where(x => x.LapseReinstatement == true).Select(x => x.PersonType).ToList();

                    if (personType?.Contains(EnumAuthPersonType.Insured.ToString()) == true
                        && personType?.Contains(EnumAuthPersonType.PolicyHolder.ToString()) == true)
                    {
                        authResult.LapseReinstatement = (authPerson.LapseReinstatement
                            && authProduct.LapseReinstatement
                            && authStatus.LapseReinstatement);

                    }
                    else
                    {
                        authResult.LapseReinstatement = (authPerson.LapseReinstatement
                            && ownerAuthProduct?.LapseReinstatement == true
                            && ownerAuthStatus?.LapseReinstatement == true);

                    }


                    personType = authMatrixPerson.Where(x => x.HealthRenewal == true).Select(x => x.PersonType).ToList();

                    if (personType?.Contains(EnumAuthPersonType.Insured.ToString()) == true
                        && personType?.Contains(EnumAuthPersonType.PolicyHolder.ToString()) == true)
                    {
                        authResult.HealthRenewal = (authPerson.HealthRenewal
                            && authProduct.HealthRenewal
                            && authStatus.HealthRenewal);

                    }
                    else
                    {
                        authResult.HealthRenewal = (authPerson.HealthRenewal
                            && ownerAuthProduct?.HealthRenewal == true
                            && ownerAuthStatus?.HealthRenewal == true);

                    }

                    personType = authMatrixPerson.Where(x => x.PolicyLoanRepayment == true).Select(x => x.PersonType).ToList();

                    if (personType?.Contains(EnumAuthPersonType.Insured.ToString()) == true
                        && personType?.Contains(EnumAuthPersonType.PolicyHolder.ToString()) == true)
                    {
                        authResult.PolicyLoanRepayment = (authPerson.PolicyLoanRepayment
                            && authProduct.PolicyLoanRepayment
                            && authStatus.PolicyLoanRepayment);

                    }
                    else
                    {
                        authResult.PolicyLoanRepayment = (authPerson.PolicyLoanRepayment
                            && ownerAuthProduct?.PolicyLoanRepayment == true
                            && ownerAuthStatus?.PolicyLoanRepayment == true);

                    }

                    personType = authMatrixPerson.Where(x => x.Acp == true).Select(x => x.PersonType).ToList();

                    if (personType?.Contains(EnumAuthPersonType.Insured.ToString()) == true
                        && personType?.Contains(EnumAuthPersonType.PolicyHolder.ToString()) == true)
                    {
                        authResult.ACP = (authPerson.ACP
                            && authProduct.ACP
                            && authStatus.ACP);

                    }
                    else
                    {
                        authResult.ACP = (authPerson.ACP
                            && ownerAuthProduct?.ACP == true
                            && ownerAuthStatus?.ACP == true);

                    }

                    personType = authMatrixPerson.Where(x => x.AdhocTopup == true).Select(x => x.PersonType).ToList();

                    if (personType?.Contains(EnumAuthPersonType.Insured.ToString()) == true
                        && personType?.Contains(EnumAuthPersonType.PolicyHolder.ToString()) == true)
                    {
                        authResult.AdhocTopup = (authPerson.AdhocTopup
                            && authProduct.AdhocTopup
                            && authStatus.AdhocTopup);

                    }
                    else
                    {
                        authResult.AdhocTopup = (authPerson.AdhocTopup
                            && ownerAuthProduct?.AdhocTopup == true
                            && ownerAuthStatus?.AdhocTopup == true);

                    }

                    personType = authMatrixPerson.Where(x => x.PartialWithdrawal == true).Select(x => x.PersonType).ToList();

                    if (personType?.Contains(EnumAuthPersonType.Insured.ToString()) == true
                        && personType?.Contains(EnumAuthPersonType.PolicyHolder.ToString()) == true)
                    {
                        authResult.PartialWithdrawal = (authPerson.PartialWithdrawal
                            && authProduct.PartialWithdrawal
                            && authStatus.PartialWithdrawal);

                    }
                    else
                    {
                        authResult.PartialWithdrawal = (authPerson.PartialWithdrawal
                            && ownerAuthProduct?.PartialWithdrawal == true
                            && ownerAuthStatus?.PartialWithdrawal == true);

                    }

                    personType = authMatrixPerson.Where(x => x.PolicyLoan == true).Select(x => x.PersonType).ToList();

                    if (personType?.Contains(EnumAuthPersonType.Insured.ToString()) == true
                        && personType?.Contains(EnumAuthPersonType.PolicyHolder.ToString()) == true)
                    {
                        authResult.PolicyLoan = (authPerson.PolicyLoan
                            && authProduct.PolicyLoan
                            && authStatus.PolicyLoan);

                    }
                    else
                    {
                        authResult.PolicyLoan = (authPerson.PolicyLoan
                            && ownerAuthProduct?.PolicyLoan == true
                            && ownerAuthStatus?.PolicyLoan == true);

                    }

                    personType = authMatrixPerson.Where(x => x.PolicyPaidup == true).Select(x => x.PersonType).ToList();

                    if (personType?.Contains(EnumAuthPersonType.Insured.ToString()) == true
                        && personType?.Contains(EnumAuthPersonType.PolicyHolder.ToString()) == true)
                    {
                        authResult.PolicyPaidup = (authPerson.PolicyPaidup
                            && authProduct.PolicyPaidup
                            && authStatus.PolicyPaidup);

                    }
                    else
                    {
                        authResult.PolicyPaidup = (authPerson.PolicyPaidup
                            && ownerAuthProduct?.PolicyPaidup == true
                            && ownerAuthStatus?.PolicyPaidup == true);

                    }

                    personType = authMatrixPerson.Where(x => x.PolicySurrender == true).Select(x => x.PersonType).ToList();

                    if (personType?.Contains(EnumAuthPersonType.Insured.ToString()) == true
                        && personType?.Contains(EnumAuthPersonType.PolicyHolder.ToString()) == true)
                    {
                        authResult.PolicySurrender = (authPerson.PolicySurrender
                            && authProduct.PolicySurrender
                            && authStatus.PolicySurrender);

                    }
                    else
                    {
                        authResult.PolicySurrender = (authPerson.PolicySurrender
                            && ownerAuthProduct?.PolicySurrender == true
                            && ownerAuthStatus?.PolicySurrender == true);

                    }

                    personType = authMatrixPerson.Where(x => x.PaymentFrequency == true).Select(x => x.PersonType).ToList();

                    if (personType?.Contains(EnumAuthPersonType.Insured.ToString()) == true
                        && personType?.Contains(EnumAuthPersonType.PolicyHolder.ToString()) == true)
                    {
                        authResult.PaymentFrequency = (authPerson.PaymentFrequency
                            && authProduct.PaymentFrequency
                            && authStatus.PaymentFrequency);

                    }
                    else
                    {
                        authResult.PaymentFrequency = (authPerson.PaymentFrequency
                            && ownerAuthProduct?.PaymentFrequency == true
                            && ownerAuthStatus?.PaymentFrequency == true);

                    }

                    personType = authMatrixPerson.Where(x => x.SumAssuredChange == true).Select(x => x.PersonType).ToList();

                    if (personType?.Contains(EnumAuthPersonType.Insured.ToString()) == true
                        && personType?.Contains(EnumAuthPersonType.PolicyHolder.ToString()) == true)
                    {
                        authResult.SumAssuredChange = (authPerson.SumAssuredChange
                            && authProduct.SumAssuredChange
                            && authStatus.SumAssuredChange);

                    }
                    else
                    {
                        authResult.SumAssuredChange = (authPerson.SumAssuredChange
                            && ownerAuthProduct?.SumAssuredChange == true
                            && ownerAuthStatus?.SumAssuredChange == true);

                    }

                    personType = authMatrixPerson.Where(x => x.RefundofPayment == true).Select(x => x.PersonType).ToList();

                    if (personType?.Contains(EnumAuthPersonType.Insured.ToString()) == true
                        && personType?.Contains(EnumAuthPersonType.PolicyHolder.ToString()) == true)
                    {
                        authResult.RefundofPayment = (authPerson.RefundofPayment
                            && authProduct.RefundofPayment
                            && authStatus.RefundofPayment);

                    }
                    else
                    {
                        authResult.RefundofPayment = (authPerson.RefundofPayment
                            && ownerAuthProduct?.RefundofPayment == true
                            && ownerAuthStatus?.RefundofPayment == true);

                    }


                    authResult.Servicing =
                           authResult.PolicyHolderDetails
                        || authResult.InsuredDetails
                        || authResult.BeneficiaryInfo
                        || authResult.LapseReinstatement
                        || authResult.HealthRenewal
                        || authResult.PolicyLoanRepayment
                        || authResult.ACP
                        || authResult.AdhocTopup
                        || authResult.PartialWithdrawal
                        || authResult.PolicyLoan
                        || authResult.PolicyPaidup
                        || authResult.PolicySurrender
                        || authResult.PaymentFrequency
                        || authResult.SumAssuredChange
                        || authResult.RefundofPayment;

                    if (memberId == new Guid("a6101190-eae0-4701-903d-2e676784629b"))
                    {
                        Console.WriteLine($"CheckAuthorization => FianlAuthResult => {JsonConvert.SerializeObject(authResult)}");

                    }

                    return authResult;
                }


            }


            return new AuthorizationResult();


        }




        public List<string>? GetClientNoListByIdValue(Guid? appMemberId)
        {

            var idValue = unitOfWork.GetRepository<Entities.Member>()
                .Query(x => x.MemberId == appMemberId && x.IsVerified == true && x.IsActive == true)
                .Select(x => new { x.Nrc, x.Passport, x.Others })
                .FirstOrDefault();

            if (idValue != null)
            {

                var clientNoList = unitOfWork.GetRepository<Entities.Client>()
                       .Query(x => (!string.IsNullOrEmpty(idValue.Nrc) && x.Nrc == idValue.Nrc)
                       || (!string.IsNullOrEmpty(idValue.Passport) && x.PassportNo == idValue.Passport)
                       || (!string.IsNullOrEmpty(idValue.Others) && x.Other == idValue.Others))
                       .Select(x => x.ClientNo).ToList();

                return clientNoList;
            }

            return null;

        }

        public List<string>? GetClientNoListByIdValueCms(Guid? appMemberId)
        {

            var idValue = unitOfWork.GetRepository<Entities.Member>()
                .Query(x => x.MemberId == appMemberId)
                .Select(x => new { x.Nrc, x.Passport, x.Others })
                .FirstOrDefault();

            if (idValue != null)
            {

                var clientNoList = unitOfWork.GetRepository<Entities.Client>()
                       .Query(x => (!string.IsNullOrEmpty(idValue.Nrc) && x.Nrc == idValue.Nrc)
                       || (!string.IsNullOrEmpty(idValue.Passport) && x.PassportNo == idValue.Passport)
                       || (!string.IsNullOrEmpty(idValue.Others) && x.Other == idValue.Others))
                       .Select(x => x.ClientNo).ToList();

                return clientNoList;
            }

            return null;

        }

        public List<string>? GetAllClientNoListByClientNo(string clientNo)
        {

            var idValue = unitOfWork.GetRepository<Entities.Client>()
                .Query(x => x.ClientNo == clientNo)
                .Select(x => new { x.Nrc, x.PassportNo, x.Other })
                .FirstOrDefault();

            if (idValue != null)
            {

                var clientNoList = unitOfWork.GetRepository<Entities.Client>()
                       .Query(x => (!string.IsNullOrEmpty(idValue.Nrc) && x.Nrc == idValue.Nrc)
                       || (!string.IsNullOrEmpty(idValue.PassportNo) && x.PassportNo == idValue.PassportNo)
                       || (!string.IsNullOrEmpty(idValue.Other) && x.Other == idValue.Other))
                       .Select(x => x.ClientNo).ToList();

                return clientNoList;
            }

            return null;

        }



        public List<string>? GetActivePolicyNoListByHolder(string clientNo)
        {

            var policyNoList = unitOfWork.GetRepository<Entities.Policy>()
                .Query(x => x.PolicyHolderClientNo == clientNo && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                .Select(x => x.PolicyNo)
                .ToList();

            return policyNoList;

        }

        public List<string>? GetActivePolicyNoListByInsured(string clientNo)
        {

            var policyNoList = unitOfWork.GetRepository<Entities.Policy>()
                .Query(x => x.InsuredPersonClientNo == clientNo && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                .Select(x => x.PolicyNo)
                .ToList();

            return policyNoList;

        }


        public List<string>? GetPolicyStatusList()
        {
            var eligibleStatus = unitOfWork.GetRepository<Entities.AuthorizationStatus>()
                            .Query(x => x.ViewMyPolicies == true && x.StatusType == EnumStatusType.Policy.ToString())
                            .Select(x => x.Status)
                            .ToList();

            return eligibleStatus;
        }


        public DateTime? GetILCoastClaimDate()
        {
            var appConfig = unitOfWork.GetRepository<Entities.AppConfig>().Query().FirstOrDefault();

            if (appConfig != null && appConfig.Coast_Claim_IsSystemDate != null)
                return appConfig.Coast_Claim_IsSystemDate == false ? (appConfig.Coast_Claim_CustomDate ?? Utils.GetDefaultDate()) : Utils.GetDefaultDate();

            return Utils.GetDefaultDate();
        }

        public DateTime? GetILCoastServicingDate()
        {
            var appConfig = unitOfWork.GetRepository<Entities.AppConfig>().Query().FirstOrDefault();

            if (appConfig != null && appConfig.Coast_Servicing_IsSystemDate != null)
                return appConfig.Coast_Servicing_IsSystemDate == false ? (appConfig.Coast_Servicing_CustomDate ?? Utils.GetDefaultDate()) : Utils.GetDefaultDate();

            return Utils.GetDefaultDate();
        }

        private List<DateTime> GenerateHoliday(DateTime fromDate, DateTime toDate)
        {
            var _fromDate = fromDate;
            var holidayList = new List<DateTime>();

            while (fromDate <= toDate)
            {
                if (fromDate.DayOfWeek == DayOfWeek.Saturday || fromDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    holidayList.Add(fromDate.Date);
                }

                fromDate = fromDate.AddDays(1);
            }


            var customHolidayList = unitOfWork.GetRepository<Entities.Holiday>()
            .Query(x => (x.HolidayDate >= _fromDate.Date && x.HolidayDate <= toDate.Date) && x.IsDelete == false && x.IsActive == true)
            .Select(x => x.HolidayDate)
            .ToList();

            if (customHolidayList != null)
            {
                holidayList.AddRange(customHolidayList);
            }

            holidayList = holidayList
                .Distinct()
                .ToList();

            return holidayList;
        }
        private bool IsHolidayOrWeekend(DateTime applied)
        {
            var isTodayCustomHoliday = unitOfWork.GetRepository<Entities.Holiday>()
                .Query(x => x.HolidayDate == applied.Date && x.IsDelete == false && x.IsActive == true)
                .Any();

            return isTodayCustomHoliday || applied.DayOfWeek == DayOfWeek.Saturday || applied.DayOfWeek == DayOfWeek.Sunday;
        }

        public ClaimContact GetProgressAndContactHour(DateTime applied, EnumProgressType? progressType = null)
        {
            var response = new ClaimContact();
            var orginalAppliedDate = applied;

            try
            {

                var originalApplied = applied;

                var deadline = applied.AddHours(DefaultConstants.ClaimContactHours);

                // Adjust the deadline to account for non-working days (weekends and holidays)
                while (applied < deadline)
                {
                    applied = applied.AddHours(1);

                    if (IsHolidayOrWeekend(applied))
                    {
                        deadline = deadline.AddHours(1);
                        continue;
                    }
                }

                if (IsHolidayOrWeekend(orginalAppliedDate))
                {
                    deadline = deadline.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                }

                response.AppliedDate = originalApplied;
                response.CompletedDate = deadline;

                // Calculate the remaining hours from now
                var currentDate = Utils.GetDefaultDate();
                response.CurrentDate = currentDate;

                #region #GenerateHoliday
                var holidayList = GenerateHoliday(currentDate, deadline);
                double deductBufferTime = 0;

                if (IsHolidayOrWeekend(currentDate))
                {
                    var bufferTime = (currentDate - currentDate.Date).TotalMinutes;
                    deductBufferTime = (bufferTime * (-1)) + (-1);
                }


                currentDate = currentDate.AddDays(holidayList.Count)
                    .AddMinutes(deductBufferTime);

                if (holidayList?.Any() == true)
                {
                    response.HolidayList.AddRange(holidayList);
                }

                #endregion

                double remainingMinutes = (deadline - currentDate).TotalMinutes;

                if (remainingMinutes > 0)
                {
                    var totalInMinutes = DefaultConstants.ClaimContactHours * 60; //4320

                    if (remainingMinutes > totalInMinutes)
                    {
                        response.Percent = 100;
                        response.Hours = "72:00 Hours";
                    }
                    else
                    {
                        var consumedInMinutes = totalInMinutes - remainingMinutes;
                        response.Percent = Convert.ToInt32(100 - Convert.ToInt32((consumedInMinutes / totalInMinutes) * 100)); ;


                        TimeSpan timeSpan = TimeSpan.FromMinutes(remainingMinutes);
                        int hours = (int)timeSpan.TotalHours;
                        int minutes = timeSpan.Minutes;

                        response.Hours = $"{hours:D2}:{minutes:D2} Hours";
                    }


                }
                else
                {
                    response.Percent = 0;
                    response.Hours = "00:00 Hours";

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetProgressAndContactHour Ex {JsonConvert.SerializeObject(ex)}");
            }



            return response;
        }



        public List<Model.Mobile.Response.ClaimStatus> GetPredefinedClaimStatusList()
        {
            var claimStatusList = new List<Model.Mobile.Response.ClaimStatus>();
            claimStatusList.Add(new Model.Mobile.Response.ClaimStatus
            {
                StatusCode = EnumClaimStatus.RC.ToString(), //"RC",
                Status = "Received",
                IsCompleted = false,
                StatusChangedDt = DateTime.MinValue,
                Remove = false,
                Sort = 1
            });

            claimStatusList.Add(new Model.Mobile.Response.ClaimStatus
            {
                StatusCode = EnumClaimStatus.FU.ToString(), //"FU",
                Status = "Followed-up",
                IsCompleted = false,
                StatusChangedDt = DateTime.MinValue,
                Remove = true,
                Sort = 2
            });

            claimStatusList.Add(new Model.Mobile.Response.ClaimStatus
            {
                StatusCode = EnumClaimStatus.BT.ToString(), //"AL",
                Status = "Approved",
                IsCompleted = false,
                StatusChangedDt = DateTime.MinValue,
                Remove = false,
                Sort = 3
            });

            claimStatusList.Add(new Model.Mobile.Response.ClaimStatus
            {
                StatusCode = EnumClaimStatus.RJ.ToString(), //"RJ",
                Status = "Rejected",
                IsCompleted = false,
                StatusChangedDt = DateTime.MinValue,
                Remove = true,
                Sort = 4
            });

            claimStatusList.Add(new Model.Mobile.Response.ClaimStatus
            {
                StatusCode = EnumClaimStatus.WD.ToString(), //"WD",
                Status = "Withdrawn",
                IsCompleted = false,
                StatusChangedDt = DateTime.MinValue,
                Remove = true,
                Sort = 5
            });

            claimStatusList.Add(new Model.Mobile.Response.ClaimStatus
            {
                StatusCode = EnumClaimStatus.CS.ToString(), //"CS",
                Status = "Closed",
                IsCompleted = false,
                StatusChangedDt = DateTime.MinValue,
                Remove = true,
                Sort = 6
            });

            claimStatusList.Add(new Model.Mobile.Response.ClaimStatus
            {
                StatusCode = EnumClaimStatus.PD.ToString(), //"PD",
                Status = "Paid",
                IsCompleted = false,
                StatusChangedDt = DateTime.MinValue,
                Remove = false,
                Sort = 7
            });

            return claimStatusList;
        }

        public List<Model.Mobile.Response.ClaimStatusTmp> GetDummyClaimStatusList()
        {
            var claimStatusList = new List<Model.Mobile.Response.ClaimStatusTmp>();
            claimStatusList.Add(new Model.Mobile.Response.ClaimStatusTmp
            {
                NewStatus = EnumClaimStatus.RC.ToString(), //"RC",
                IsCompleted = false,
            });
            claimStatusList.Add(new Model.Mobile.Response.ClaimStatusTmp
            {
                NewStatus = EnumClaimStatus.FU.ToString(), //"FU",
                IsCompleted = false,
                NewStatusDesc = "Follow-up"
            });

            return claimStatusList;
        }

        public DateTime GetILCustomDate(EnumILCustomDate enumILCustomDate = EnumILCustomDate.Claim)
        {
            DateTime? iLCustomDate = null;

            var iLCustomDates = unitOfWork.GetRepository<Entities.AppConfig>()
                .Query()
                .Select(x => new
                {
                    x.Coast_Claim_IsSystemDate,
                    x.Coast_Claim_CustomDate,
                    x.Coast_Servicing_IsSystemDate,
                    x.Coast_Servicing_CustomDate
                })
                .FirstOrDefault();

            if (enumILCustomDate == EnumILCustomDate.Claim && iLCustomDates != null)
            {
                iLCustomDate = iLCustomDates.Coast_Claim_IsSystemDate == true
                    ? iLCustomDates.Coast_Claim_CustomDate
                    : null;
            }
            else if (enumILCustomDate == EnumILCustomDate.Servicing && iLCustomDates != null)
            {
                iLCustomDate = iLCustomDates.Coast_Servicing_IsSystemDate == true
                    ? iLCustomDates.Coast_Servicing_CustomDate
                    : null;
            }


            if (iLCustomDate != null)
                return iLCustomDate.Value;

            return DateTime.UtcNow.AddHours(6).AddMinutes(30);
        }

        public bool IsHoliday()
        {
            var isTodayHoliday = false;
            isTodayHoliday = unitOfWork.GetRepository<Entities.Holiday>()
                .Query(x => x.HolidayDate.Date == Utils.GetDefaultDate().Date && x.IsDelete == false && x.IsActive == true)
                .Any();

            if (Utils.GetDefaultDate().DayOfWeek == DayOfWeek.Saturday || Utils.GetDefaultDate().DayOfWeek == DayOfWeek.Sunday)
            {
                isTodayHoliday = true;
            }

            return isTodayHoliday;
        }


        public (string? membertype, string? memberID, string? groupMemberId) GetClientInfo(Guid? appMemberId)
        {

            var idValue = unitOfWork.GetRepository<Entities.Member>()
                .Query(x => x.MemberId == appMemberId)
                .Select(x => new { x.Nrc, x.Passport, x.Others })
                .FirstOrDefault();

            if (idValue != null)
            {

                var clientNoList = unitOfWork.GetRepository<Entities.Client>()
                       .Query(x => (!string.IsNullOrEmpty(idValue.Nrc) && x.Nrc == idValue.Nrc)
                       || (!string.IsNullOrEmpty(idValue.Passport) && x.PassportNo == idValue.Passport)
                       || (!string.IsNullOrEmpty(idValue.Others) && x.Other == idValue.Others))
                       .Select(x => x.ClientNo).ToList();

                var client = unitOfWork.GetRepository<Entities.Client>()
                    .Query(x => clientNoList.Contains(x.ClientNo))
                    .ToList();

                var isruby = client.Any(x => x.VipFlag == "Y");
                var membertype = isruby == true ? EnumIndividualMemberType.Ruby.ToString() : EnumIndividualMemberType.Member.ToString();


                var groupClientNo = unitOfWork.GetRepository<Entities.Policy>()
                   .Query(x => (clientNoList.Contains(x.PolicyHolderClientNo) || clientNoList.Contains(x.InsuredPersonClientNo))
                   && x.PolicyNo.Length > DefaultConstants.IndividualPolicyNoLength)
                   .Select(x => x.PolicyHolderClientNo)
                   .FirstOrDefault();

                return (membertype, clientNoList?.FirstOrDefault(), groupClientNo);
            }


            return (null, null, null);

        }


        public (string? membertype, string? memberID, string? groupMemberId) GetClientInfoByIdValue(string idValue)
        {

            if (!string.IsNullOrEmpty(idValue))
            {
                var clientNoList = unitOfWork.GetRepository<Entities.Client>()
                       .Query(x => x.Nrc == idValue || x.PassportNo == idValue || x.Other == idValue)
                       .Select(x => x.ClientNo).ToList();

                if (clientNoList != null && clientNoList.Any())
                {
                    var client = unitOfWork.GetRepository<Entities.Client>()
                        .Query(x => clientNoList.Contains(x.ClientNo))
                        .ToList();

                    var isruby = client.Any(x => x.VipFlag == "Y");
                    var membertype = isruby == true ? EnumIndividualMemberType.Ruby.ToString() : EnumIndividualMemberType.Member.ToString();


                    var groupClientNo = unitOfWork.GetRepository<Entities.Policy>()
                       .Query(x => (clientNoList.Contains(x.PolicyHolderClientNo) || clientNoList.Contains(x.InsuredPersonClientNo))
                       && x.PolicyNo.Length > DefaultConstants.IndividualPolicyNoLength)
                       .Select(x => x.PolicyHolderClientNo)
                       .FirstOrDefault();

                    return (membertype, clientNoList?.FirstOrDefault(), groupClientNo);
                }

            }


            return (null, null, null);

        }


        public (string? producttype, string? productname) GetProductInfo(string policyNo)
        {

            var policy = unitOfWork.GetRepository<Entities.Policy>().Query(x => x.PolicyNo == policyNo).FirstOrDefault();

            var product = unitOfWork.GetRepository<Entities.Product>().Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false)
                .Select(x => new { x.ProductTypeShort, x.TitleEn })
                .FirstOrDefault();

            return (product?.ProductTypeShort, product?.TitleEn);
        }


        public string GetServicingFormId(EnumServiceType serviceType, bool isPdf = false, bool isNrcFront = false, bool isNrcBack = false)
        {
            var formId = "";

            try
            {
                if (isPdf)
                {
                    switch (serviceType)
                    {
                        #region #POSFRM1
                        case EnumServiceType.PolicyHolderInformation: formId = "POSPPM1"; break;
                        case EnumServiceType.InsuredPersonInformation: formId = "POSPPM1"; break;
                        #endregion

                        #region #POSBFM1
                        case EnumServiceType.BeneficiaryInformation: formId = "POSBFM1"; break;
                        #endregion

                        #region #POSBLM1
                        case EnumServiceType.PaymentFrequency: formId = "POSBLM1"; break;
                        #endregion

                        #region #POSFRM1
                        case EnumServiceType.LapseReinstatement: formId = "POSFRM1"; break;
                        case EnumServiceType.HealthRenewal: formId = "POSFRM1"; break;
                        case EnumServiceType.PolicyLoanRepayment: formId = "POSFRM1"; break;
                        case EnumServiceType.AcpLoanRepayment: formId = "POSFRM1"; break;
                        case EnumServiceType.AdHocTopup: formId = "POSFRM1"; break;
                        case EnumServiceType.PartialWithdraw: formId = "POSFRM1"; break;
                        case EnumServiceType.PolicyLoan: formId = "POSFRM1"; break;
                        case EnumServiceType.PolicyPaidUp: formId = "POSFRM1"; break;
                        case EnumServiceType.PolicySurrender: formId = "POSFRM1"; break;
                        case EnumServiceType.SumAssuredChange: formId = "POSFRM1"; break;
                        case EnumServiceType.RefundOfPayment: formId = "POSFRM1"; break;
                            #endregion
                    }
                }
                else
                {
                    switch (serviceType)
                    {
                        case EnumServiceType.PolicyHolderInformation: formId = ""; break;
                        case EnumServiceType.InsuredPersonInformation: formId = ""; break;
                        case EnumServiceType.PaymentFrequency: formId = ""; break;

                        #region #POSDOC1
                        case EnumServiceType.LapseReinstatement: formId = "POSDOC1"; break;
                        case EnumServiceType.HealthRenewal: formId = "POSDOC1"; break;
                        case EnumServiceType.PolicyLoanRepayment: formId = "POSDOC1"; break;
                        case EnumServiceType.AcpLoanRepayment: formId = "POSDOC1"; break;
                        case EnumServiceType.AdHocTopup: formId = "POSDOC1"; break;
                        case EnumServiceType.PartialWithdraw: formId = "POSDOC1"; break;
                        case EnumServiceType.PolicyLoan: formId = "POSDOC1"; break;
                        case EnumServiceType.PolicyPaidUp: formId = "POSDOC1"; break;
                        case EnumServiceType.PolicySurrender: formId = "POSDOC1"; break;
                        case EnumServiceType.SumAssuredChange: formId = "POSDOC1"; break;
                        case EnumServiceType.RefundOfPayment: formId = "POSDOC1"; break;
                            #endregion
                    }

                    if (serviceType == EnumServiceType.BeneficiaryInformation && isNrcFront)
                    {
                        formId = "BFID1";
                    }

                    if (serviceType == EnumServiceType.BeneficiaryInformation && isNrcBack)
                    {
                        formId = "BFID2";
                    }
                }
            }
            catch { }

            return formId;
        }


        public string GetPolicyPlanName(string productCode, string componentCode, string? policyNo = "")
        {
            var planName = "";
            try
            {


                if (productCode == "OHI" || productCode == "OHG")
                {
                    var ohiCodeList = new List<string> { "OHI1", "OHI2", "OHI3", "OHI4", "OHI5", "OHI6", "OHI7" };
                    var ohgCodeList = new List<string> { "OHG1", "OHG2", "OHG3", "OHG4", "OHG5", "OHG6", "OHG7" };


                    var componentCodeList = componentCode.Split(",").ToList();


                    var isMatched = ohiCodeList.Intersect(componentCodeList).Any() || ohgCodeList.Intersect(componentCodeList).Any();



                    if (isMatched)
                    {
                        //var planData = unitOfWork.GetRepository<Entities.PlanData>()
                        //.Query(x => componentCodeList.Contains(x.PlanCode))
                        //.Select(x => x.PlanDesc)
                        //.ToList();

                        //if (planData?.Any() == true)
                        //{
                        //    planName = string.Join(", ", planData);
                        //}

                        var planData = unitOfWork.GetRepository<Entities.PlanData>()
                        .Query(x => componentCodeList.Contains(x.PlanCode))
                        .FirstOrDefault();

                        if (planData != null)
                        {
                            planName = planData.PlanDesc;
                        }

                    }

                    //Console.WriteLine($"GetPolicyPlanName => " +
                    //    $"policyNo => {policyNo} productCode => {productCode} componentCode => {componentCode} " +
                    //    $"isMatched => {ohiCodeList.Intersect(componentCodeList).Any()} || {ohgCodeList.Intersect(componentCodeList).Any()} " +
                    //    $"planName => {planName}");


                }
            }
            catch
            { }

            return planName;
        }

        public static string NormalizeMyanmarPhoneNumber(string phoneNumber)
        {
            try
            {
                if (!string.IsNullOrEmpty(phoneNumber) &&
                (phoneNumber.StartsWith("+959") || phoneNumber.StartsWith("959")
                || phoneNumber.StartsWith("09") || phoneNumber.StartsWith("9")))
                {
                    // Remove all non-digit characters
                    string digitsOnly = Regex.Replace(phoneNumber, @"\D", "");

                    // If the number starts with country code +95 or 95
                    if (digitsOnly.StartsWith("959"))
                    {
                        return "0" + digitsOnly.Substring(2); // Remove the country code and add prefix 0
                    }
                    else if (digitsOnly.StartsWith("0"))
                    {
                        return digitsOnly; // Already in the correct format
                    }
                    else
                    {
                        return "0" + digitsOnly; // No country code, no prefix 0, so add prefix 0
                    }
                }
            }
            catch
            {

            }

            return phoneNumber;
        }

        public bool ValidateTestEndpointsOtp(string otp)
        {
            #region CustomOtp
            var onetimeToken = unitOfWork.GetRepository<Entities.OnetimeToken>()
                .Query(x => x.Otp == otp)
                .FirstOrDefault();

            if (onetimeToken == null)
            {
                return false;
            }

            unitOfWork.GetRepository<Entities.OnetimeToken>().Delete(onetimeToken);
            unitOfWork.SaveChanges();
            #endregion

            return true;
        }

        public string GetMimeType(string fileName)
        {
            var fileExtension = Path.GetExtension(fileName).ToLower();
            switch (fileExtension)
            {
                case ".pdf":
                    return "application/pdf";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".txt":
                    return "text/plain";
                default:
                    return "application/octet-stream"; // Default for unknown file types
            }
        }

        #region #OtpRateLimit && BruteForceAttemps Preventions
        public string GetClientIpAddress()
        {
            return "i am ip address";
        }
        public bool IsOtpRateLimitExceeded(OtpRateLimitModel model)
        {

            var otpCountPerUser = unitOfWork
                .GetRepository<RateLimitControlOtpAttempts>()
                .Query(x => x.UserIdentifier == model.UserIdentifier && x.CreatedAt >= (Utils.GetDefaultDate().AddSeconds((-1) * model.LimitIntervalPerUserInSeconds)))
                .Count();

            if (otpCountPerUser >= model.LimitCountPerUser)
            {
                return true;
            }

            //var otpCountPerIp = unitOfWork
            //    .GetRepository<RateLimitControl>()
            //    .Query(x => x.IpAddress == GetClientIpAddress() && x.CreatedAt >= DateTime.UtcNow.AddMinutes((-1) * model.LimitIntervalPerIpInMinutes))
            //    .Count();

            //if (otpCountPerIp >= model.LimitCountPerIp)
            //{
            //    return true;
            //}

            var otpCountGlobal = unitOfWork
                .GetRepository<RateLimitControlOtpAttempts>()
                .Query(x => x.CreatedAt >= (Utils.GetDefaultDate().AddSeconds((-1) * model.LimitIntervalPerGlobalInSeconds)))
                .Count();

            if (otpCountGlobal >= model.LimitCountGlobal)
            {
                return true;
            }

            unitOfWork.GetRepository<RateLimitControlOtpAttempts>()
                .Add(new RateLimitControlOtpAttempts
                {
                    UserIdentifier = model.UserIdentifier,
                    IpAddress = GetClientIpAddress(),
                    CreatedAt = Utils.GetDefaultDate(),
                    RateLimitOtpType = model.RateLimitOtpType.ToString(),
                    Id = Guid.NewGuid(),
                });
            unitOfWork.SaveChanges();

            return false;
        }        

        public void AddOtpBruteForceAttempt(OtpBruteForceAttemptsRateLimitModel model)
        {
            unitOfWork.GetRepository<RateLimitOtpBruteForceAttempts>()
                .Add(new RateLimitOtpBruteForceAttempts
                {
                    UserIdentifier = model.UserIdentifier,
                    IpAddress = GetClientIpAddress(),
                    CreatedAt = Utils.GetDefaultDate(),
                    IsSuccess = model.IsSuccess,
                    Id = Guid.NewGuid(),
                    OtpCode = model.OtpCode,
                    RateLimitOtpType = model.RateLimitOtpType.ToString(),
                });
            unitOfWork.SaveChanges();
        }

        public bool IsOtpBruteForceAttemptDetacted(OtpBruteForceAttemptsRateLimitModel model)
        {
            var now = Utils.GetDefaultDate();

            // Count failed attempts per user in the time window
            var failedPerUser = unitOfWork
                .GetRepository<RateLimitOtpBruteForceAttempts>()
                .Query(x =>
                    x.UserIdentifier == model.UserIdentifier &&
                    x.CreatedAt >= now.AddSeconds(-model.IntervalInSecondsPerUser) &&
                    x.IsSuccess == false)
                .Count();

            if (failedPerUser >= model.MaxAttemptsPerUser)
            {
                return true;
            }

            // Count failed attempts globally
            var failedGlobal = unitOfWork
                .GetRepository<RateLimitOtpBruteForceAttempts>()
                .Query(x =>
                    x.CreatedAt >= now.AddSeconds(-model.IntervalInSecondsPerGlobal) &&
                    x.IsSuccess == false)
                .Count();

            if (failedGlobal >= model.MaxAttemptsPerGlobal)
            {
                return true;
            }            

            return false;
        }

        #endregion

        public List<string>? SubmitNoInsertNoAPICalls_GetAllClientNoList2(string clientNo, SubmitNoInsertNoAPICallsResponse response)
        {

            var idValueQuery = unitOfWork.GetRepository<Entities.Client>()
                .Query(x => x.ClientNo == clientNo)
                .Select(x => new
                 {
                     Nrc = (x.Nrc ?? "").Trim(),
                     PassportNo = (x.PassportNo ?? "").Trim(),
                     Other = (x.Other ?? "").Trim()
                 });

            // Print SQL for first query
            var formattedSql = idValueQuery.ToQueryString();

            Console.WriteLine("SubmitNoInsertNoAPICalls_GetAllClientNoList2 => Get idValueQuery SQL: " + formattedSql);

            var idValue = idValueQuery.FirstOrDefault();

            response.SQLQueryList.Add(new SQLQueryList
            {
                QueryName = "Get idValueQuery",
                Query = formattedSql,
                Result = new List<string>
                {
                    $"Nrc: {idValue?.Nrc}, PassportNo: {idValue?.PassportNo}, Other: {idValue?.Other}"
                }
                ,
                ResultCount = idValue == null ? 0 : 1
            });

            if (idValue != null)
            {

                var clientNoQuery = unitOfWork.GetRepository<Entities.Client>()
                       .Query(x => (!string.IsNullOrEmpty(idValue.Nrc) && x.Nrc == idValue.Nrc)
                       || (!string.IsNullOrEmpty(idValue.PassportNo) && x.PassportNo == idValue.PassportNo)
                       || (!string.IsNullOrEmpty(idValue.Other) && x.Other == idValue.Other))
                       .Select(x => x.ClientNo);

                // Print SQL for second query

                formattedSql = clientNoQuery.ToQueryString();

                Console.WriteLine("SubmitNoInsertNoAPICalls_GetAllClientNoList2 => AllClientNoQuery SQL: " + formattedSql);



                // Execute the query
                var clientNoList = clientNoQuery.ToList();


                response.SQLQueryList.Add(new SQLQueryList
                {
                    QueryName = "Get clientNoQuery",
                    Query = formattedSql,
                    Result = clientNoList,
                    ResultCount = clientNoList.Count
                });

                Console.WriteLine($"SubmitNoInsertNoAPICalls_GetAllClientNoList2 => {clientNoList.Count} {string.Join(Environment.NewLine, clientNoList)}");

                return clientNoList;
            }

            return null;

        }

        public List<string>? SubmitNoInsertNoAPICalls_GetAllClientNoList(string clientNo, SubmitNoInsertNoAPICallsResponse response)
        {

            var idValueQuery = unitOfWork.GetRepository<Entities.Client>()
                .Query(x => x.ClientNo == clientNo)
                .Select(x => new { x.Nrc, x.PassportNo, x.Other });

            // Print SQL for first query
            var formattedSql = idValueQuery.ToQueryString();

            Console.WriteLine("SubmitNoInsertNoAPICalls_GetAllClientNoList => Get idValueQuery SQL: " + formattedSql);

            var idValue = idValueQuery.FirstOrDefault();

            response.SQLQueryList.Add(new SQLQueryList
            {
                QueryName = "Get idValueQuery",
                Query = formattedSql,
                Result = new List<string>
                {
                    $"Nrc: {idValue?.Nrc}, PassportNo: {idValue?.PassportNo}, Other: {idValue?.Other}"
                }
                ,
                ResultCount = idValue == null ? 0 : 1
            });

            if (idValue != null)
            {

                var clientNoQuery = unitOfWork.GetRepository<Entities.Client>()
                       .Query(x => (!string.IsNullOrEmpty(idValue.Nrc) && x.Nrc == idValue.Nrc)
                       || (!string.IsNullOrEmpty(idValue.PassportNo) && x.PassportNo == idValue.PassportNo)
                       || (!string.IsNullOrEmpty(idValue.Other) && x.Other == idValue.Other))
                       .Select(x => x.ClientNo);

                // Print SQL for second query
                formattedSql = clientNoQuery.ToQueryString();
                Console.WriteLine("SubmitNoInsertNoAPICalls_GetAllClientNoList => AllClientNoQuery SQL: " + formattedSql);               

                
                // Execute the query
                var clientNoList = clientNoQuery.ToList();

                response.SQLQueryList.Add(new SQLQueryList
                {
                    QueryName = "Get clientNoQuery",
                    Query = formattedSql,
                    Result = clientNoList,
                    ResultCount = clientNoList.Count
                });

                Console.WriteLine($"SubmitNoInsertNoAPICalls_GetAllClientNoList => {clientNoList.Count} {string.Join(Environment.NewLine, clientNoList)}");

                return clientNoList;
            }

            return null;

        }

        public string GetMasterClientIdByMemberId(Guid appMemberId)
        {
            string masterClientId = "";
            var idValue = unitOfWork.GetRepository<Entities.Member>()
                .Query(x => x.MemberId == appMemberId)
                .Select(x => new { x.Nrc, x.Passport, x.Others })
                .FirstOrDefault();

            if (idValue != null)
            {
                masterClientId = unitOfWork.GetRepository<Entities.Client>()
                       .Query(x => (!string.IsNullOrEmpty(idValue.Nrc) && x.Nrc.Trim() == idValue.Nrc.Trim())
                       || (!string.IsNullOrEmpty(idValue.Passport) && x.PassportNo.Trim() == idValue.Passport.Trim())
                       || (!string.IsNullOrEmpty(idValue.Others) && x.Other.Trim() == idValue.Others.Trim()))
                       .Select(x => x.MasterClientNo)
                       .FirstOrDefault() ?? "";
                
            }

            return masterClientId;
        }

    }


    public class GetCountByRawQuery
    {
        public long SelectCount { get; set; }
    }

    public class QueryStrings
    {
        public string? CountQuery { get; set; }
        public string? ListQuery { get; set; }
    }

    public class ProductCodePolicyStat
    {
        public string? ProductCode { get; set; }
        public string? PolicyStatus { get; set; }
        public string? PremiumStatus { get; set; }
        public string? PolicyNumber { get; set; }
    }
}
