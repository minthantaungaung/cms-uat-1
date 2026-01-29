using aia_core.Entities;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response;
using aia_core.Model.Cms.Response.MemberPolicyResponse;
using aia_core.Repository.Mobile;
using aia_core.Services;
using aia_core.UnitOfWork;
using DocumentFormat.OpenXml.Vml.Office;
using FastMember;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SqlServer.Server;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;
using CsvHelper.Configuration;
using System.Data;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace aia_core.Repository.Cms
{   
    public interface IMemberPolicyRepository
    {
        Task<ResponseModel<PagedList<MemberPolicyListResponse>>> List(MemberPolicyListRequest model);
        Task<ResponseModel<MemberPolicyResponse>> GetPolicies(string memberId);
        Task<ResponseModel<PolicyDetailResponse>> GetPolicyDetail(string insuredId, string policyNo);
        Task<ResponseModel<List<PolicyCoveragesResponse>>> GetCoverages(string memberId);

        Task<ResponseModel<List<PolicyCoveragesResponse>>> GetCoveragesByClientNo(string clientno);
    }
    public class MemberPolicyRepository : BaseRepository, IMemberPolicyRepository
    {
        
        public MemberPolicyRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider, IUnitOfWork<Context> unitOfWork) 
            :base(httpContext, azureStorage, errorCodeProvider, unitOfWork) 
        {
            
        }

        public async Task<ResponseModel<PagedList<MemberPolicyListResponse>>> List(MemberPolicyListRequest model)
        {
            var memberResponses = new List<MemberPolicyListResponse>();

            try
            {
                model.QueryType = Model.Cms.Request.Common.EnumSqlQueryType.List;
                var queryStrings = PrepareListQuery(model);

                var count = unitOfWork.GetRepository<GetCountByRawQuery>()
                    .FromSqlRaw(queryStrings?.CountQuery, null, CommandType.Text)
                    .FirstOrDefault();

                var list = unitOfWork.GetRepository<MemberPolicyListResponse>()
                    .FromSqlRaw(queryStrings.ListQuery, null, CommandType.Text)
                    .ToList();


                var data = new PagedList<MemberPolicyListResponse>(
                source: list,
                totalCount: count.SelectCount,
                pageNumber: (int)model.Page,
                pageSize: (int)model.Size);

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.Members,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<PagedList<MemberPolicyListResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog("Member Policy List", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<MemberPolicyListResponse>>(ErrorCode.E500);
            }
        }

        private aia_core.Repository.QueryStrings PrepareListQuery(MemberPolicyListRequest model)
        {
            #region #CountQuery
            var countQuery = @"SELECT COUNT(Member.Member_ID) AS SelectCount ";
            var asQuery = @"";
            #endregion

            #region #DataQuery
            var dataQuery = @"SELECT 
                            Member.Member_ID AS AppRegMemberId,
                            Member.Name AS MemberName,
                            Member.Mobile AS MemberPhone,
                            Member.Email AS MemberEmail,
                            Member.NRC AS MemberIdNrc,
                            Member.Passport AS MemberIdPassport,
                            Member.Others AS MemberIdOther,
                            Member.Gender AS MemberGender,
                            Member.DOB AS MemberDob,
                            Member.Is_Active AS MemberIsActive,
                            Member.Register_Date AS RegisterDate,
                            Member.Last_Active_Date AS LastActiveDate,
                            Member.MemberType AS MemberType,
                            Member.GroupMemberID AS GroupMemberID,
                            Member.IndividualMemberID AS MemberId,
                            Member.IndividualMemberID AS IndividualMemberID ";
            #endregion

            #region #FromQuery
            var fromQuery = @"FROM Member ";
            #endregion

            #region #GroupQuery

            var groupQuery = @"";


            var groupQueryForCount = @"";
            #endregion

            #region #OrderQuery
            var orderQuery = @"ORDER BY Member.Register_Date DESC ";
            var orderQueryForCount = @" ";
            #endregion



            #region #FilterQuery

            var filterQuery = @"where 1 = 1 ";

            if (!string.IsNullOrEmpty(model.MemberId))
            {
                filterQuery += $@"AND (Member.IndividualMemberID LIKE '%{model.MemberId}%' OR Member.GroupMemberID LIKE '%{model.MemberId}%') ";
            }

            if (!string.IsNullOrEmpty(model.MemberName))
            {
                filterQuery += $@"AND Member.Name LIKE '%{model.MemberName}%' ";
            }

            if (!string.IsNullOrEmpty(model.MemberPhone))
            {
                filterQuery += $@"AND Member.Mobile LIKE '%{model.MemberPhone}%' ";
            }

            if (!string.IsNullOrEmpty(model.MemberEmail))
            {
                filterQuery += $@"AND Member.Email LIKE '%{model.MemberEmail}%' ";
            }

            if (!string.IsNullOrEmpty(model.MemberIden))
            {
                filterQuery += $@"AND (Member.NRC LIKE '%{model.MemberIden}%' OR Member.Passport LIKE '%{model.MemberIden}%' OR Member.Others LIKE '%{model.MemberIden}%') ";
            }

            //if (model.MemberIdenType != null && model.MemberIdenType.Any())
            //{


            //    var subQuery = new List<string>();

            //    foreach (var iden in model.MemberIdenType)
            //    {
            //        if (iden == EnumIdenType.Nrc)
            //        {
            //            subQuery.Add(@"Member.NRC IS NOT NULL AND Member.NRC <> '' ");
            //        }
            //        else if (iden == EnumIdenType.Passport)
            //        {
            //            subQuery.Add(@"Member.Passport IS NOT NULL AND Member.Passport <> '' ");
            //        }
            //        else if (iden == EnumIdenType.Others)
            //        {
            //            subQuery.Add(@"Member.Others IS NOT NULL AND Member.Others <> '' ");
            //        }
            //    }

            //    var subQueryString = string.Join("OR ", subQuery);

            //    filterQuery += $@"AND ({subQueryString})";
            //}


            if (!string.IsNullOrEmpty(model.MemberIdenType))
            {
                if (model.MemberIdenType == "Nrc")
                {
                    filterQuery += $@"AND Member.NRC IS NOT NULL AND Member.NRC <> '' ";
                }
                else if (model.MemberIdenType == "Passport")
                {
                    filterQuery += $@"AND Member.Passport IS NOT NULL AND Member.Passport <> '' ";
                }
                else if (model.MemberIdenType == "Others")
                {
                    filterQuery += $@"AND Member.Others IS NOT NULL AND Member.Others <> '' ";
                }
            }
            if (model.MemberType != null)
            {
                if (model.MemberType == EnumIndividualMemberType.Ruby)
                {
                    filterQuery += $@"AND Lower(Member.MemberType) = '{EnumIndividualMemberType.Ruby.ToString().ToLower()}' ";
                }
                else if (model.MemberType == EnumIndividualMemberType.Member)
                {
                    filterQuery += $@"AND Lower(Member.MemberType) = '{EnumIndividualMemberType.Member.ToString().ToLower()}' ";
                }
            }

            if (model.MemberIsActive != null)
            {
                if (model.MemberIsActive == true)
                {
                    filterQuery += $@"AND Member.Is_Active = 1 ";
                }
                else if (model.MemberIsActive == false)
                {
                    filterQuery += $@"AND Member.Is_Active = 0 ";
                }
                
            }



            #endregion

            #region #OffsetQuery

            #endregion
            var offsetQuery = "";
            if (model.QueryType == Model.Cms.Request.Common.EnumSqlQueryType.List)
            {
                offsetQuery = $"OFFSET {(model.Page - 1) * model.Size} ROWS FETCH NEXT {model.Size} ROWS ONLY";
            }

            countQuery = $"{countQuery}{fromQuery}{filterQuery}{groupQueryForCount}{asQuery}";
            var listQuery = $"{dataQuery}{fromQuery}{filterQuery}{groupQuery}{orderQuery}{offsetQuery}";

            return new aia_core.Repository.QueryStrings { CountQuery = countQuery, ListQuery = listQuery };
        }

        public async Task<ResponseModel<MemberPolicyResponse>> GetPolicies(string memberId)
        {
            try
            {

                var member = unitOfWork.GetRepository<Entities.Member>()
                    .Query(x => x.IndividualMemberID == memberId || x.GroupMemberID == memberId)
                    .FirstOrDefault();

                if (member == null)
                    return errorCodeProvider.GetResponseModel<MemberPolicyResponse>(ErrorCode.E400);

                var appRegistration = new AppRegistrationInfo(member);
                appRegistration.MemberId = memberId;
                appRegistration.MemberType = member.MemberType;

                var clientNoList = GetClientNoListByIdValueCms(member.MemberId);

                var policiesList = new List<Policies>();
                var policies = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => clientNoList.Contains(x.PolicyHolderClientNo) || clientNoList.Contains(x.InsuredPersonClientNo))
                    .ToList();

                if (policies != null && policies.Any())
                {

                    var insuredPolicies = new List<InsuredPolicies>();
                    foreach (var policy in policies)
                    {
                        var insuredPolicy = new InsuredPolicies();

                        var insuredPerson = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == policy.InsuredPersonClientNo).FirstOrDefault();
                        var product = unitOfWork.GetRepository<Entities.Product>()
                            .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false).FirstOrDefault();

                        insuredPolicy.InsuredName = insuredPerson?.Name;
                        insuredPolicy.InsuredId = policy.InsuredPersonClientNo;
                        insuredPolicy.PolicyDate = policy.PolicyIssueDate;
                        insuredPolicy.PolicyId = policy.PolicyNo;
                        insuredPolicy.PolicyUnits = policy.NumberOfUnit;
                        insuredPolicy.SumAssured = Convert.ToDouble(policy.SumAssured);
                        insuredPolicy.ProductName = product?.TitleEn;
                        insuredPolicy.ProductCode = policy.ProductType;
                        insuredPolicy.ComponentCodes = policy.Components;

                        if (product != null && product.LogoImage != null)
                        {
                            insuredPolicy.ProductLogo = GetFileFullUrl(EnumFileType.Product, product.LogoImage);
                        }

                        if (policy.PaidToDate != null)
                            insuredPolicy.NumberOfDaysForDue = Utils.GetNumberOfDaysForPolicyDue(policy.PaidToDate.Value);

                        insuredPolicy.IsPolicyActive = policy.PolicyStatus == EnumPolicyStatus.IF.ToString() ? true : false;

                        insuredPolicies.Add(insuredPolicy);
                    }

                    policiesList = insuredPolicies.GroupBy(
                    p => p.InsuredId,
                    (key, g) => new Policies
                    {
                        InsuredName = g.Select(x => x.InsuredName).FirstOrDefault()
                    ,
                        InsuredId = g.Select(x => x.InsuredId).FirstOrDefault()
                    ,
                        InsuredPolicies = g.ToList()
                    })
                        .ToList();
                }

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.MemberPolicy,
                        objectAction: EnumObjectAction.View,
                        //objectId: appRegistration?.MemberId != null ? Guid.Parse(appRegistration?.MemberId) : null,
                        objectName: appRegistration?.MemberName);
                return errorCodeProvider.GetResponseModel<MemberPolicyResponse>(ErrorCode.E0, new MemberPolicyResponse()
                {
                    AppRegistrationInfo = appRegistration,
                    Policies = policiesList
                });

            }
            catch (Exception ex)
            {
                CmsErrorLog("GetPolicies => Ex", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<MemberPolicyResponse>(ErrorCode.E500);
            }
        }
        

        async Task<ResponseModel<List<PolicyCoveragesResponse>>> IMemberPolicyRepository.GetCoverages(string memberId)
        {
            var coveragesResponse = new List<PolicyCoveragesResponse>();

            try
            {
                var memberClients = GetClientNoListByIdValue(Guid.Parse(memberId));

                if (!memberClients.Any())
                {
                    return errorCodeProvider.GetResponseModel<List<PolicyCoveragesResponse>>(ErrorCode.E400);
                }

                var policies = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => memberClients.Contains(x.PolicyHolderClientNo) || memberClients.Contains(x.InsuredPersonClientNo)).ToList();

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
                        CoverageIcon = GetFileFullUrl(EnumFileType.Product, coverage.CoverageIcon),
                };

                    if (policies.Any())
                    {
                        var isCoveredByProduct = coverage.ProductCoverages
                            .Where(x => policies.Select(x => x.ProductType).Contains(x.Product.ProductTypeShort)).Any();


                        policyCoverage.IsCovered = isCoveredByProduct;

                    }
                    else
                    {
                        policyCoverage.IsCovered = false;
                    }
                    

                    coveragesResponse.Add(policyCoverage);
                }

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.MemberPolicy,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<List<PolicyCoveragesResponse>>(ErrorCode.E0, coveragesResponse);

            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<PolicyCoveragesResponse>>(ErrorCode.E500);
            }
        }

        async Task<ResponseModel<PolicyDetailResponse>> IMemberPolicyRepository.GetPolicyDetail(string insuredId, string policyNo)
        {
            try
            {
                var response = new PolicyDetailResponse();

                var insuredPolicy = await unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => x.PolicyNo == policyNo && x.InsuredPersonClientNo == insuredId)                    
                    .FirstOrDefaultAsync();

                if (insuredPolicy == null)
                    return errorCodeProvider.GetResponseModel<PolicyDetailResponse>(ErrorCode.E400);

                var policyHolder = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == insuredPolicy.PolicyHolderClientNo).FirstOrDefault();
                var policyAgent = unitOfWork.GetRepository<Entities.Client>().Query(x => x.AgentCode == insuredPolicy.AgentCode).FirstOrDefault();
                var policyInsured = unitOfWork.GetRepository<Entities.Client>().Query(x => x.ClientNo == insuredId).FirstOrDefault();
                var product = unitOfWork.GetRepository<Entities.Product>()
                    .Query(x => x.ProductTypeShort == insuredPolicy.ProductType && x.IsActive == true && x.IsDelete == false).FirstOrDefault();

                response.insuredName = policyInsured?.Name;
                response.policyNumber = insuredPolicy?.PolicyNo;
                response.policyName = product?.TitleEn;

                #region PolicyInfo

                var aCP = "";
                var policyStatus = "";

                if (insuredPolicy != null)
                {
                    aCP = insuredPolicy.AcpModeFlag == "1" ? "ACP" : "";

                    policyStatus = unitOfWork.GetRepository<Entities.PolicyStatus>().Query(x => x.ShortDesc == insuredPolicy.PolicyStatus)
                        .FirstOrDefault()?.LongDesc;
                }

                var policyInfo = new PolicyInfo
                {
                    PolicyUnits = insuredPolicy?.NumberOfUnit,
                    PolicyNumber = insuredPolicy?.PolicyNo,
                    SumAssured = Convert.ToDouble(insuredPolicy?.SumAssured),
                    PaymentFrequency = Utils.GetPaymentFrequency(insuredPolicy?.PaymentFrequency),
                    PolicyACP = aCP,
                    OutstandingInterest = Convert.ToDouble(insuredPolicy?.OutstandingInterest),
                    OutstandingPremium = Convert.ToDouble(insuredPolicy?.OutstandingPremium),
                    PolicyHolder = policyHolder?.Name,
                    AgentEmail = policyAgent?.Email,
                    AgentPhone = policyAgent?.PhoneNo,
                    AgentName = policyAgent?.Name,
                    PolicyStatus = policyStatus,
                    PremiumDueDate = insuredPolicy?.PaidToDate,
                };
                response.PolicyInfo= policyInfo;

                #endregion


                #region PolicyHolderInfo

                var policyHolderOccupation = unitOfWork.GetRepository<Entities.Occupation>()
                    .Query(x => policyHolder != null ? (x.Code == policyHolder.Occupation) : (x.Code == string.Empty))
                    .FirstOrDefault()?.Description;

                var policyHolderInfo = new PolicyHolderInfo()
                {
                    PolicyHolder = policyHolder?.Name,
                    PolicyHolderNrc = policyHolder?.Nrc,
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

                    InsuredNrc = policyInsured?.Nrc,
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

                var beneficiaries = unitOfWork.GetRepository<Beneficiary>().Query(x => x.PolicyNo == policyNo)
                    .OrderByDescending(x => x.Percentage)
                    .ToList();

                if (beneficiaries.Any())
                {
                    var beneInfoList = new List<BeneficiaryInfo>();
                    foreach (var beneficiary in beneficiaries)
                    {
                        var client = unitOfWork.GetRepository<Client>().Query(x => x.ClientNo == beneficiary.BeneficiaryClientNo).FirstOrDefault();

                        var relationship = unitOfWork.GetRepository<Entities.Relationship>()
                            .Query(x => x.Code == beneficiary.Relationship)
                            .FirstOrDefault();

                        var beneficiaryInfo = new BeneficiaryInfo()
                        {
                            BeneficiaryName = client?.Name,
                            BeneficiaryNrc= client?.Nrc,
                            BeneficiaryDob = client?.Dob, 
                            BeneficiaryEmail= client?.Email,
                            BeneficiaryGender= Utils.GetGender(client?.Gender) ,
                            BeneficiaryPhone = client?.PhoneNo,
                            BeneficiaryRelationship = relationship?.Name,
                            BeneficiarySharedPercent = Convert.ToInt32(beneficiary.Percentage)
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
                    InstallmentPremium = Convert.ToDouble(insuredPolicy?.InstallmentPremium) 
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


                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.MemberPolicy,
                        objectAction: EnumObjectAction.View,
                        objectName: insuredPolicy.PolicyNo);
                return errorCodeProvider.GetResponseModel<PolicyDetailResponse>(ErrorCode.E0, response);

            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PolicyDetailResponse>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<List<PolicyCoveragesResponse>>> GetCoveragesByClientNo(string clientno)
        {
            var coveragesResponse = new List<PolicyCoveragesResponse>();
            List<string> memberClients = new List<string> { clientno };

            try
            {

                var checkInsured = unitOfWork.GetRepository<Entities.Client>()
                    .Query(x => x.ClientNo == clientno).Any();

                if (!checkInsured)
                {
                    return errorCodeProvider.GetResponseModel<List<PolicyCoveragesResponse>>(ErrorCode.E400);
                }

                var policies = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => x.InsuredPersonClientNo == clientno).ToList();

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
                        CoverageIcon = GetFileFullUrl(EnumFileType.Product, coverage.CoverageIcon),
                    };

                    if (policies.Any())
                    {
                        var isCoveredByProduct = coverage.ProductCoverages
                            .Where(x => policies.Select(x => x.ProductType).Contains(x.Product.ProductTypeShort)).Any();


                        policyCoverage.IsCovered = isCoveredByProduct;

                    }
                    else
                    {
                        policyCoverage.IsCovered = false;
                    }


                    coveragesResponse.Add(policyCoverage);
                }

                await CmsAuditLog(
                        objectGroup: EnumObjectGroup.MemberPolicy,
                        objectAction: EnumObjectAction.View);
                return errorCodeProvider.GetResponseModel<List<PolicyCoveragesResponse>>(ErrorCode.E0, coveragesResponse);

            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<PolicyCoveragesResponse>>(ErrorCode.E500);
            }
        }
    }
}
