using aia_core.Entities;
using aia_core.Model.Mobile.Response;
using aia_core.Services;
using aia_core.UnitOfWork;
using FastMember;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Newtonsoft.Json;
using aia_core.Model.Mobile.Request;
using Microsoft.IdentityModel.Tokens;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Data;
using System.Collections.Generic;

namespace aia_core.Repository.Mobile
{
    public interface IHomeRepository
    {
        ResponseModel<List<CardListResponse>> GetCardList();
        ResponseModel<HomeDataResponse> GetHomeData();
        ResponseModel<List<HomeRecentRequestResponse>> GetRecentRequestList();

        ResponseModel<List<CardListResponse>> GetCardListByNrc(string nrc, string otp);
    }
    public class HomeRepository : BaseRepository, IHomeRepository
    {
        #region "const"
        private readonly IMemberPolicyRepository memberPolicyRepository;
        private readonly ICommonRepository commonRepository;

        public HomeRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork, IMemberPolicyRepository memberPolicyRepository, ICommonRepository commonRepository)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.memberPolicyRepository = memberPolicyRepository;
            this.commonRepository = commonRepository;
        }
        #endregion

        #region "Get Card List"
        public ResponseModel<List<CardListResponse>> GetCardList()
        {
            try
            {
                var clients = new List<Client>();
                List<CardListResponse> cardlist = new List<CardListResponse>();

                var memberIdGuid = GetMemberIDFromToken();
                var memberId = memberIdGuid.ToString();

                var allClientNoList = GetClientNoListByIdValue(memberIdGuid);

                ////DC Contract Declined
                ////IN  Incomplete
                ////NT  Not taken Up
                ////PO  Contract Postponed
                ////PS Contract Proposal
                ////UW  Underwriting Approval
                ////WD Contract Withdrawn

                var excludedPolicyStatusList = new string[] { "DC", "IN", "NT", "PO", "PS", "UW", "WD" };

                #region #Insert Card



                allClientNoList?.ForEach(clientNo =>
                {
                    var memberClient = unitOfWork.GetRepository<Entities.MemberClient>()
                                .Query(x => x.ClientNo == clientNo && x.MemberId == Guid.Parse(memberId)).FirstOrDefault();

                    if (memberClient == null)
                    {
                        MobileErrorLog($"Insert Card memberId => {memberId}, clientNo => {clientNo}", $"", $"", httpContext?.HttpContext.Request.Path);
                        unitOfWork.GetRepository<Entities.MemberClient>().Add(new MemberClient()
                        {
                            Id = Guid.NewGuid(),
                            MemberId = Guid.Parse(memberId),
                            ClientNo = clientNo,
                        });

                        unitOfWork.SaveChanges();

                    }
                });
                #endregion


                #region #Update MemberType & GroupMemberID
                try
                {
                    var member = unitOfWork.GetRepository<Entities.Member>().Query(x => x.MemberId == memberIdGuid).FirstOrDefault();
                    if (member != null)
                    {
                        (string? membertype, string? memberID, string? groupMemberId) clientInfo = GetClientInfo(memberIdGuid);
                        member.MemberType = clientInfo.membertype;
                        member.GroupMemberID = clientInfo.groupMemberId;
                        //member.IndividualMemberID = clientInfo.memberID;

                        unitOfWork.SaveChanges();
                    }
                }
                catch { }
                #endregion

                var hasActivePolicy = unitOfWork.GetRepository<Policy>()
                    .Query(x => (allClientNoList.Contains(x.PolicyHolderClientNo) || allClientNoList.Contains(x.InsuredPersonClientNo))
                    && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus))
                    .Any();


                #region # corporate
                var earliestCorporatePolicy = unitOfWork.GetRepository<Policy>()
                    .Query(x => (allClientNoList.Contains(x.PolicyHolderClientNo) || allClientNoList.Contains(x.InsuredPersonClientNo))
                    && x.PolicyNo.Length > DefaultConstants.IndividualPolicyNoLength
                    && excludedPolicyStatusList.Contains(x.PolicyStatus) == false
                    && x.PolicyIssueDate != null
                    )                    
                    .Select(x => new { x.PolicyHolderClientNo, x.InsuredPersonClientNo, x.PolicyIssueDate })
                    .ToList() // raw list
                    .OrderBy(x => x.PolicyIssueDate)
                    .FirstOrDefault(); //earliest

                if (earliestCorporatePolicy != null && hasActivePolicy)
                {
                    var earliestCorporateClientNo = allClientNoList.Where(x => x == earliestCorporatePolicy.PolicyHolderClientNo || x == earliestCorporatePolicy.InsuredPersonClientNo).FirstOrDefault();
                    var earliestCorporateClient = unitOfWork.GetRepository<Client>().Query(x => x.ClientNo == earliestCorporateClientNo).FirstOrDefault();

                    if (earliestCorporateClient != null)
                    {
                        CardListResponse corporateCard = new CardListResponse();
                        corporateCard.ClientNo = earliestCorporateClient.ClientNo;
                        corporateCard.Name = earliestCorporateClient?.Name;
                        corporateCard.MemberType = EnumMemberType.corporate.ToString();
                        corporateCard.MemberSince = (earliestCorporatePolicy.PolicyIssueDate != null) ? earliestCorporatePolicy.PolicyIssueDate.Value.ToString("MM/yyyy") : "";
                        corporateCard.Sort = 3;

                        #region # policies
                        corporateCard.policyList = new List<PolicyData>();

                        var policyList = unitOfWork.GetRepository<Entities.Policy>()
                                .Query(x => (allClientNoList.Contains(x.PolicyHolderClientNo) || allClientNoList.Contains(x.InsuredPersonClientNo))
                                && x.PolicyNo.Length > DefaultConstants.IndividualPolicyNoLength
                                && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus) 
                                && x.PolicyIssueDate != null
                                )
                                .ToList()
                                .OrderBy(x => x.PolicyIssueDate)
                                .ToList();

                        foreach (var policy in policyList)
                        {
                            var product = unitOfWork.GetRepository<Entities.Product>()
                                .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false).FirstOrDefault();

                            if (product != null)
                            {
                                corporateCard.policyList.Add(
                                new PolicyData()
                                {
                                    ProductName = product.TitleEn,
                                    ProductNameMm = product.TitleMm,
                                    PolicyNumber = policy.PolicyNo,
                                });
                            }
                            

                        }

                        #endregion

                        
                        cardlist.Add(corporateCard);

                        
                    }

                    

                }
                #endregion




                #region # ruby               


                var earliestRubyClient = unitOfWork.GetRepository<Client>()
                    .Query(x => allClientNoList.Contains(x.ClientNo) && (x.VipFlag == "R" || x.VipFlag == "E"))
                    .ToList() // raw list
                    .OrderBy(x => x.VipEffectiveDate)
                    .FirstOrDefault(); // earliest

                

                if (earliestRubyClient != null && hasActivePolicy)
                {
                    

                    CardListResponse rubyCard = new CardListResponse();
                    //rubyCard.ClientNo = earliestRubyClient.ClientNo;
                    rubyCard.ClientNo = earliestRubyClient.MasterClientNo;



                    var masterClient = unitOfWork.GetRepository<Client>()
                    .Query(x => x.ClientNo == earliestRubyClient.MasterClientNo)
                    .FirstOrDefault();

                    if (masterClient != null)
                    {
                        rubyCard.Name = masterClient.Name;
                        rubyCard.MemberSince = (masterClient.VipEffectiveDate != null) ? masterClient.VipEffectiveDate.Value.ToString("MM/yyyy") : "";

                    }

                    if (earliestRubyClient.VipFlag == "R")
                        rubyCard.MemberType = EnumMemberType.ruby.ToString();
                    else if (earliestRubyClient.VipFlag == "E")
                        rubyCard.MemberType = EnumMemberType.rubyelite.ToString();

                    rubyCard.Sort = 2;

                    #region # policies
                    rubyCard.policyList = new List<PolicyData>();

                    var policyList = unitOfWork.GetRepository<Entities.Policy>()
                            .Query(x => (allClientNoList.Contains(x.PolicyHolderClientNo) || allClientNoList.Contains(x.InsuredPersonClientNo))
                            && x.PolicyNo.Length == DefaultConstants.IndividualPolicyNoLength
                            && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus)
                            && x.PolicyIssueDate != null
                            )
                            .ToList()
                             .OrderBy(x => x.PolicyIssueDate)
                             .ToList();

                    foreach (var policy in policyList)
                    {
                        var product = unitOfWork.GetRepository<Entities.Product>()
                            .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false).FirstOrDefault();
                        rubyCard.policyList.Add(new PolicyData()
                        {
                            ProductName = product?.TitleEn,
                            ProductNameMm = product?.TitleMm,
                            PolicyNumber = policy?.PolicyNo,
                        });

                    }

                    #endregion

                    
                    cardlist.Add(rubyCard);

                }
                #endregion

                #region # individual

                if (earliestRubyClient == null && hasActivePolicy)
                {
                    
                    var earliestIndividualPolicy = unitOfWork.GetRepository<Policy>()
                        .Query(x => (allClientNoList.Contains(x.PolicyHolderClientNo) || allClientNoList.Contains(x.InsuredPersonClientNo))
                        && x.PolicyNo.Length == DefaultConstants.IndividualPolicyNoLength
                        && excludedPolicyStatusList.Contains(x.PolicyStatus) == false
                        && x.PolicyIssueDate != null
                        )
                        .Select(x => new { x.PolicyHolderClientNo, x.InsuredPersonClientNo, x.PolicyIssueDate , x.PolicyNo})
                        .ToList() // raw list
                        .OrderBy(x => x.PolicyIssueDate)
                        .FirstOrDefault(); // earliest


                    
                    if (earliestIndividualPolicy != null)
                    {
                        var earliestIndividualClientNo = allClientNoList.Where(x => x == earliestIndividualPolicy.PolicyHolderClientNo || x == earliestIndividualPolicy.InsuredPersonClientNo).FirstOrDefault();
                        var earliestIndividualClient = unitOfWork.GetRepository<Client>().Query(x => x.ClientNo == earliestIndividualClientNo).FirstOrDefault();

                        
                        if (earliestIndividualClient != null)
                        {
                            CardListResponse individualCard = new CardListResponse();
                            //individualCard.ClientNo = earliestIndividualClient.ClientNo;
                            individualCard.ClientNo = earliestIndividualClient.MasterClientNo;
                            individualCard.MemberType = EnumMemberType.individual.ToString();
                            individualCard.MemberSince = (earliestIndividualPolicy.PolicyIssueDate != null) 
                                ? earliestIndividualPolicy.PolicyIssueDate.Value.ToString("MM/yyyy") : "";


                            var masterClient = unitOfWork.GetRepository<Client>()
                    .Query(x => x.ClientNo == earliestIndividualClient.MasterClientNo)
                    .FirstOrDefault();

                            if (masterClient != null)
                            {
                                individualCard.Name = masterClient.Name;

                            }

                            individualCard.Sort = 1;

                            #region # policies
                            individualCard.policyList = new List<PolicyData>();

                            var individualPolicyList = unitOfWork.GetRepository<Entities.Policy>()
                                    .Query(x => (allClientNoList.Contains(x.PolicyHolderClientNo) || allClientNoList.Contains(x.InsuredPersonClientNo))
                                    && x.PolicyNo.Length == DefaultConstants.IndividualPolicyNoLength
                                    && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus)
                                    && x.PolicyIssueDate != null

                                    )
                                    .ToList()
                                    .OrderBy(x => x.PolicyIssueDate)
                                    .ToList();

                            foreach (var policy in individualPolicyList)
                            {
                                var product = unitOfWork.GetRepository<Entities.Product>()
                                    .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false).FirstOrDefault();
                                


                                if (product != null)
                                {
                                    individualCard.policyList.Add(
                                     new PolicyData()
                                     {
                                         ProductName = product.TitleEn,
                                         ProductNameMm = product.TitleMm,
                                         PolicyNumber = policy.PolicyNo,
                                     });
                                }

                            }

                            #endregion

                            cardlist.Add(individualCard);
                        }
                        
                    }
                    
                }
                #endregion

                cardlist = cardlist.OrderBy(x => x.Sort).ToList();

                return errorCodeProvider.GetResponseModel<List<CardListResponse>>(ErrorCode.E0, cardlist);
            }
            catch (Exception ex)
            {
                MobileErrorLog("GetCardList Ex => ", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<CardListResponse>>(ErrorCode.E500);
            }

        }

        public ResponseModel<List<CardListResponse>> GetCardListByNrc(string nrc, string otp)
        {
            try
            {
                //==========================================
                // THIS IS TEST METHOD NOT UPDATE HERE
                //==========================================
                if(ValidateTestEndpointsOtp(otp) == false)
                {
                    return errorCodeProvider.GetResponseModel<List<CardListResponse>>(ErrorCode.E403);
                }

                var clients = new List<Client>();
                List<CardListResponse> cardlist = new List<CardListResponse>();


                var entityMember = unitOfWork.GetRepository<Entities.Member>().
                    Query(x => (x.Nrc == nrc || x.Passport == nrc || x.Others == nrc) && x.IsActive == true && x.IsVerified == true)
                    .FirstOrDefault();

                if (entityMember == null)
                    return errorCodeProvider.GetResponseModel<List<CardListResponse>>(ErrorCode.E404);

                

                var memberIdGuid = entityMember.MemberId;
                var memberId = memberIdGuid.ToString();

                var allClientNoList = GetClientNoListByIdValue(memberIdGuid);

                ////DC Contract Declined
                ////IN  Incomplete
                ////NT  Not taken Up
                ////PO  Contract Postponed
                ////PS Contract Proposal
                ////UW  Underwriting Approval
                ////WD Contract Withdrawn

                var excludedPolicyStatusList = new string[] { "DC", "IN", "NT", "PO", "PS", "UW", "WD" };

                #region #Insert Card



                allClientNoList?.ForEach(clientNo =>
                {
                    var memberClient = unitOfWork.GetRepository<Entities.MemberClient>()
                                .Query(x => x.ClientNo == clientNo && x.MemberId == Guid.Parse(memberId)).FirstOrDefault();

                    if (memberClient == null)
                    {
                        MobileErrorLog($"Insert Card memberId => {memberId}, clientNo => {clientNo}", $"", $"", httpContext?.HttpContext.Request.Path);
                        unitOfWork.GetRepository<Entities.MemberClient>().Add(new MemberClient()
                        {
                            Id = Guid.NewGuid(),
                            MemberId = Guid.Parse(memberId),
                            ClientNo = clientNo,
                        });

                        unitOfWork.SaveChanges();

                    }
                });
                #endregion


                #region #Update MemberType & GroupMemberID
                try
                {
                    var member = unitOfWork.GetRepository<Entities.Member>().Query(x => x.MemberId == memberIdGuid).FirstOrDefault();
                    if (member != null)
                    {
                        (string? membertype, string? memberID, string? groupMemberId) clientInfo = GetClientInfo(memberIdGuid);
                        member.MemberType = clientInfo.membertype;
                        member.GroupMemberID = clientInfo.groupMemberId;
                        //member.IndividualMemberID = clientInfo.memberID;

                        unitOfWork.SaveChanges();
                    }
                }
                catch { }
                #endregion




                #region # corporate
                var earliestCorporatePolicy = unitOfWork.GetRepository<Policy>()
                    .Query(x => (allClientNoList.Contains(x.PolicyHolderClientNo) || allClientNoList.Contains(x.InsuredPersonClientNo))
                    && x.PolicyNo.Length > DefaultConstants.IndividualPolicyNoLength
                    && excludedPolicyStatusList.Contains(x.PolicyStatus) == false
                    && x.PolicyIssueDate != null
                    )
                    .Select(x => new { x.PolicyHolderClientNo, x.InsuredPersonClientNo, x.PolicyIssueDate })
                    .ToList() // raw list
                    .OrderBy(x => x.PolicyIssueDate)
                    .FirstOrDefault(); //earliest

                if (earliestCorporatePolicy != null)
                {
                    var earliestCorporateClientNo = allClientNoList.Where(x => x == earliestCorporatePolicy.PolicyHolderClientNo || x == earliestCorporatePolicy.InsuredPersonClientNo).FirstOrDefault();
                    var earliestCorporateClient = unitOfWork.GetRepository<Client>().Query(x => x.ClientNo == earliestCorporateClientNo).FirstOrDefault();

                    if (earliestCorporateClient != null)
                    {
                        CardListResponse corporateCard = new CardListResponse();
                        corporateCard.ClientNo = earliestCorporateClient.ClientNo;
                        corporateCard.Name = earliestCorporateClient?.Name;
                        corporateCard.MemberType = EnumMemberType.corporate.ToString();
                        corporateCard.MemberSince = (earliestCorporatePolicy.PolicyIssueDate != null) ? earliestCorporatePolicy.PolicyIssueDate.Value.ToString("MM/yyyy") : "";
                        corporateCard.Sort = 3;

                        #region # policies
                        corporateCard.policyList = new List<PolicyData>();

                        var policyList = unitOfWork.GetRepository<Entities.Policy>()
                                .Query(x => (allClientNoList.Contains(x.PolicyHolderClientNo) || allClientNoList.Contains(x.InsuredPersonClientNo))
                                && x.PolicyNo.Length > DefaultConstants.IndividualPolicyNoLength
                                && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus)
                                && x.PolicyIssueDate != null
                                )
                                .ToList()
                                .OrderBy(x => x.PolicyIssueDate)
                                .ToList();

                        foreach (var policy in policyList)
                        {
                            var product = unitOfWork.GetRepository<Entities.Product>()
                                .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false).FirstOrDefault();

                            if (product != null)
                            {
                                corporateCard.policyList.Add(
                                new PolicyData()
                                {
                                    ProductName = product.TitleEn,
                                    ProductNameMm = product.TitleMm,
                                    PolicyNumber = policy.PolicyNo,
                                });
                            }


                        }

                        #endregion


                        cardlist.Add(corporateCard);


                    }



                }
                #endregion




                #region # ruby               


                var earliestRubyClient = unitOfWork.GetRepository<Client>()
                    .Query(x => allClientNoList.Contains(x.ClientNo) && x.VipFlag == "Y")
                    .ToList() // raw list
                    .OrderBy(x => x.VipEffectiveDate)
                    .FirstOrDefault(); // earliest



                if (earliestRubyClient != null)
                {


                    CardListResponse rubyCard = new CardListResponse();
                    //rubyCard.ClientNo = earliestRubyClient.ClientNo;
                    rubyCard.ClientNo = earliestRubyClient.MasterClientNo;



                    var masterClient = unitOfWork.GetRepository<Client>()
                    .Query(x => x.ClientNo == earliestRubyClient.MasterClientNo)
                    .FirstOrDefault();

                    if (masterClient != null)
                    {
                        rubyCard.Name = masterClient.Name;
                        rubyCard.MemberSince = (masterClient.VipEffectiveDate != null) ? masterClient.VipEffectiveDate.Value.ToString("MM/yyyy") : "";

                    }

                    rubyCard.MemberType = EnumMemberType.ruby.ToString();
                    rubyCard.Sort = 2;

                    #region # policies
                    rubyCard.policyList = new List<PolicyData>();

                    var policyList = unitOfWork.GetRepository<Entities.Policy>()
                            .Query(x => (allClientNoList.Contains(x.PolicyHolderClientNo) || allClientNoList.Contains(x.InsuredPersonClientNo))
                            && x.PolicyNo.Length == DefaultConstants.IndividualPolicyNoLength
                            && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus)
                            && x.PolicyIssueDate != null
                            )
                            .ToList()
                             .OrderBy(x => x.PolicyIssueDate)
                             .ToList();

                    foreach (var policy in policyList)
                    {
                        var product = unitOfWork.GetRepository<Entities.Product>()
                            .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false).FirstOrDefault();
                        rubyCard.policyList.Add(new PolicyData()
                        {
                            ProductName = product?.TitleEn,
                            ProductNameMm = product?.TitleMm,
                            PolicyNumber = policy?.PolicyNo,
                        });

                    }

                    #endregion


                    cardlist.Add(rubyCard);

                }
                #endregion

                #region # individual

                if (earliestRubyClient == null)
                {

                    var earliestIndividualPolicy = unitOfWork.GetRepository<Policy>()
                        .Query(x => (allClientNoList.Contains(x.PolicyHolderClientNo) || allClientNoList.Contains(x.InsuredPersonClientNo))
                        && x.PolicyNo.Length == DefaultConstants.IndividualPolicyNoLength
                        && excludedPolicyStatusList.Contains(x.PolicyStatus) == false
                        && x.PolicyIssueDate != null
                        )
                        .Select(x => new { x.PolicyHolderClientNo, x.InsuredPersonClientNo, x.PolicyIssueDate, x.PolicyNo })
                        .ToList() // raw list
                        .OrderBy(x => x.PolicyIssueDate)
                        .FirstOrDefault(); // earliest



                    if (earliestIndividualPolicy != null)
                    {
                        var earliestIndividualClientNo = allClientNoList.Where(x => x == earliestIndividualPolicy.PolicyHolderClientNo || x == earliestIndividualPolicy.InsuredPersonClientNo).FirstOrDefault();
                        var earliestIndividualClient = unitOfWork.GetRepository<Client>().Query(x => x.ClientNo == earliestIndividualClientNo).FirstOrDefault();


                        if (earliestIndividualClient != null)
                        {
                            CardListResponse individualCard = new CardListResponse();
                            //individualCard.ClientNo = earliestIndividualClient.ClientNo;
                            individualCard.ClientNo = earliestIndividualClient.MasterClientNo;
                            individualCard.MemberType = EnumMemberType.individual.ToString();
                            individualCard.MemberSince = (earliestIndividualPolicy.PolicyIssueDate != null)
                                ? earliestIndividualPolicy.PolicyIssueDate.Value.ToString("MM/yyyy") : "";


                            var masterClient = unitOfWork.GetRepository<Client>()
                    .Query(x => x.ClientNo == earliestIndividualClient.MasterClientNo)
                    .FirstOrDefault();

                            if (masterClient != null)
                            {
                                individualCard.Name = masterClient.Name;

                            }

                            individualCard.Sort = 1;

                            #region # policies
                            individualCard.policyList = new List<PolicyData>();

                            var individualPolicyList = unitOfWork.GetRepository<Entities.Policy>()
                                    .Query(x => (allClientNoList.Contains(x.PolicyHolderClientNo) || allClientNoList.Contains(x.InsuredPersonClientNo))
                                    && x.PolicyNo.Length == DefaultConstants.IndividualPolicyNoLength
                                    && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus)
                                    && x.PolicyIssueDate != null

                                    )
                                    .ToList()
                                    .OrderBy(x => x.PolicyIssueDate)
                                    .ToList();

                            foreach (var policy in individualPolicyList)
                            {
                                var product = unitOfWork.GetRepository<Entities.Product>()
                                    .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false).FirstOrDefault();



                                if (product != null)
                                {
                                    individualCard.policyList.Add(
                                     new PolicyData()
                                     {
                                         ProductName = product.TitleEn,
                                         ProductNameMm = product.TitleMm,
                                         PolicyNumber = policy.PolicyNo,
                                     });
                                }

                            }

                            #endregion

                            cardlist.Add(individualCard);
                        }

                    }

                }
                #endregion

                cardlist = cardlist.OrderBy(x => x.Sort).ToList();

                return errorCodeProvider.GetResponseModel<List<CardListResponse>>(ErrorCode.E0, cardlist);
            }
            catch (Exception ex)
            {
                MobileErrorLog("GetCardList Ex => ", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<CardListResponse>>(ErrorCode.E500);
            }

        }
        #endregion

        #region "GetHomeData"
        public ResponseModel<HomeDataResponse> GetHomeData()
        {
            try
            {
                var memberGuid = GetMemberIDFromToken();
                var memberID = memberGuid?.ToString();
                var memberType = GetMemberType()?.ToString().ToLower();

                List<string> list = GetClientNoListByIdValue(memberGuid);

                


                var memberId = memberGuid?.ToString();

                List<PropositionsCategoryResponse> propositionCategories = new List<PropositionsCategoryResponse>();

                var authResult = CheckAuthorization(memberGuid, null);

                if (authResult?.Proposition == true)
                {
                    #region "Get Proposition"
                    var propositionList = unitOfWork.GetRepository<Proposition>()
                    .Query(x => x.IsActive == true && x.IsDelete == false && (x.Eligibility == memberType || x.Eligibility == EnumPropositionBenefit.both.ToString()))
                    .Include(i => i.PropositionCategory)
                    .Include(i => i.PropositionBenefits)
                    .GroupBy(x => x.PropositionCategoryId)
                    .ToList();


                    foreach (var item in propositionList)
                    {
                        propositionCategories.Add(new PropositionsCategoryResponse()
                        {
                            Eligibility = item.FirstOrDefault().Eligibility,
                            ID = item.FirstOrDefault().PropositionCategoryId?.ToString(),
                            Name_EN = item.FirstOrDefault().PropositionCategory?.NameEn,
                            Name_MM = item.FirstOrDefault().PropositionCategory?.NameMm,
                            IconImage = string.IsNullOrEmpty(item.FirstOrDefault().PropositionCategory?.IconImage)
                            ? null : commonRepository.GetFileFullUrl(EnumFileType.Proposition, item.FirstOrDefault().PropositionCategory?.IconImage),
                            BackgroudImage = string.IsNullOrEmpty(item.FirstOrDefault().PropositionCategory?.BackgroundImage)
                            ? null : commonRepository.GetFileFullUrl(EnumFileType.Proposition, item.FirstOrDefault().PropositionCategory?.BackgroundImage),
                            TotalBenefits = item.Sum(x => x.PropositionBenefits.Where(x => x.Type == EnumPropositionBenefit.both.ToString() || x.Type == memberType).Count()),
                            TotalPropositions = item.Count(),
                            CreatedOn = item.FirstOrDefault().PropositionCategory.CreatedOn,
                        });
                    }

                    propositionCategories = propositionCategories
                        .OrderBy(x => x.CreatedOn)
                        .Skip(0)
                        .Take(5)
                        .ToList();

                    #endregion
                }



                #region "Get Blog (Promotion & Activity)"
                List<Blog> blog = unitOfWork.GetRepository<Blog>()
               .Query(x => x.IsActive == true && x.IsDelete == false
               && (x.CategoryType == EnumCategoryType.activity.ToString() || (x.CategoryType == EnumCategoryType.promotion.ToString() && (x.PromotionStart.Value < Utils.GetDefaultDate() && x.PromotionEnd.Value > Utils.GetDefaultDate())))
               )
               .OrderBy(o => o.Sort)
               .Skip(0)
               .Take(5)
               .ToList();

                List<PromotionResponse> blogList = new List<PromotionResponse>();
                foreach (var item in blog)
                {
                    PromotionResponse data = new PromotionResponse();
                    data.ID = item.Id.ToString();
                    data.Title_EN = item.TitleEn;
                    data.Title_MM = item.TitleMm;
                    data.Topic_EN = item.TopicEn;
                    data.Topic_MM = item.TopicMm;
                    data.CoverImage = string.IsNullOrEmpty(item.CoverImage) ? item.CoverImage : commonRepository.GetFileFullUrl(EnumFileType.Blog, item.CoverImage);
                    data.ReadMin_EN = item.ReadMinEn;
                    data.ReadMin_MM = item.ReadMinMm;
                    data.PromotionEnd = item.PromotionEnd;
                    data.CategoryType = item.CategoryType;
                    blogList.Add(data);
                }

                #endregion

                #region "Get Product"
                List<Product> products = unitOfWork.GetRepository<Product>()
               .Query(x => x.IsActive == true && x.IsDelete == false && (x.NotAllowedInProductList == null || x.NotAllowedInProductList == false))
               .OrderBy(o => o.CreatedDate)
               .Skip(0)
               .Take(5)
               .ToList();

                List<ProductResponse> productList = new List<ProductResponse>();
                foreach (var item in products)
                {
                    ProductResponse data = new ProductResponse();
                    data.ID = item.ProductId.ToString();
                    data.ProductName_EN = item.TitleEn;
                    data.ProductName_MM = item.TitleMm;
                    data.TagLine_EN = item.TaglineEn;
                    data.TagLine_MM = item.TaglineMm;
                    data.IssuedAgeFrom_EN = item.IssuedAgeFrom;
                    data.IssuedAgeFrom_MM = item.IssuedAgeFromMm;
                    data.IssuedAgeEnd_EN = item.IssuedAgeTo;
                    data.IssuedAgeEnd_MM = item.IssuedAgeToMm;
                    data.LogoImage = string.IsNullOrEmpty(item.LogoImage) ? item.LogoImage : commonRepository.GetFileFullUrl(EnumFileType.Product, item.LogoImage);
                    data.CoverImage = string.IsNullOrEmpty(item.CoverImage) ? item.CoverImage : commonRepository.GetFileFullUrl(EnumFileType.Product, item.CoverImage);
                    data.IconImage = string.IsNullOrEmpty(item.LogoImage) ? item.LogoImage : commonRepository.GetFileFullUrl(EnumFileType.Product, item.LogoImage);

                    data.ProductCode = item.ProductTypeShort;

                    productList.Add(data);
                }

                #endregion


                var upcomingPremiumList = new List<UpcomingPremiumList>();

                #region "Upcoming Premium"

                if (authResult?.ViewMyPolicies == true)
                {
                    var responseModel = memberPolicyRepository.GetUpcomingPremiums(true).Result;

                    if (responseModel.Code == (long)ErrorCode.E0)
                    {
                        upcomingPremiumList = responseModel.Data.OrderBy(x => x.DueDate).Take(5).ToList();
                    }
                }

                #endregion

                var saleConsultantResponse = new SaleConsultantResponse();

                var agentCodeList = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => list.Contains(x.PolicyHolderClientNo) || list.Contains(x.InsuredPersonClientNo)).Select(x => x.AgentCode).Distinct().ToList();

                if(agentCodeList != null && agentCodeList.Count == 1) 
                {
                    var saleConsultant = new SaleConsultant();

                    var agent = unitOfWork.GetRepository<Entities.Client>().Query(x => x.AgentCode == agentCodeList.First()).FirstOrDefault();
                    saleConsultant.Email = agent?.Email;
                    saleConsultant.Mobile = agent?.PhoneNo;
                    saleConsultant.Name = agent?.Name;

                    saleConsultantResponse.hasSaleConsultant = true;
                    saleConsultantResponse.SaleConsultant = saleConsultant;

                }

                return errorCodeProvider.GetResponseModel<HomeDataResponse>(ErrorCode.E0, new HomeDataResponse
                {
                    PropositionsCategory = propositionCategories,
                    Promotion = blogList,
                    Product = productList,
                    UpcomingPremium = upcomingPremiumList,
                    SaleConsultantInfo = saleConsultantResponse,
                });
            }
            catch (Exception ex)
            {
                MobileErrorLog("GetHomeData Ex =>", ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<HomeDataResponse>(ErrorCode.E500);
            }
        }

        ResponseModel<List<HomeRecentRequestResponse>> IHomeRepository.GetRecentRequestList()
        {
            var response = new List<HomeRecentRequestResponse>();

            var memberGuid = GetMemberIDFromToken(); //new Guid("0A10D00B-B364-4A50-9242-66F7243A3B79");//

            try
            {
                var queryString = $@"select top 3  
                                    TBL3.RequestType,
	                                TBL3.RequestType2,
                                    TBL3.RequestId,
                                    TBL3.RequestNameEn,
                                    TBL3.RequestNameMm,
                                    TBL3.Status,
                                    TBL3.CreatedDate,
                                    TBL3.PolicyNumber,
                                        CASE
                                            WHEN TBL3.RequestType = 'Claim' THEN TBL3.RequestType2
                                            ELSE NULL
                                        END AS ClaimType,
                                        CASE
                                            WHEN TBL3.RequestType = 'Service' THEN TBL3.RequestType2
                                            ELSE NULL
                                        END AS ServiceType
                                    from(
                                    select * from
                                    ((SELECT TOP 3
	                                    'Claim' as RequestType,
                                        ClaimTran.ClaimId AS RequestId,
                                        '' AS RequestNameEn,
	                                    '' AS RequestNameMm,
                                        ClaimTran.ClaimFormType AS RequestType2,
	                                    ClaimTran.ClaimStatus AS Status,
                                        ClaimTran.CreatedDate,
	                                    ClaimTran.PolicyNo AS PolicyNumber
                                    FROM
                                        ClaimTran 
	                                    --LEFT JOIN InsuranceBenefit on InsuranceBenefit.BenefitFormType = ClaimTran.ClaimFormType
	                                    WHERE ClaimTran.AppMemberId = '{memberGuid}'
                                    ORDER BY TransactionDate DESC)) AS TBL1

                                    UNION ALL

                                    select * from
                                    (SELECT TOP 3
	                                    'Service' as RequestType,
                                        ServiceMain.ServiceID AS RequestId,
                                        ServiceType.ServiceTypeNameEn AS RequestNameEn,
	                                    ServiceType.ServiceTypeNameMm AS RequestNameMm,
                                        ServiceMain.ServiceType AS RequestType2,
	                                    ServiceMain.ServiceStatus AS Status,
                                        ServiceMain.CreatedDate,
	                                    ServiceMain.PolicyNumber
                                    FROM
                                        ServiceMain 
	                                    LEFT JOIN ServiceType on ServiceType.ServiceTypeEnum = ServiceMain.ServiceType
	                                    WHERE ServiceMain.LoginMemberID = '{memberGuid}'
                                    ORDER BY CreatedDate DESC) AS TBL2
                                    ) AS TBL3 Order by CreatedDate desc ";

                var list = unitOfWork.GetRepository<HomeRecentRequestResponse>()
                    .FromSqlRaw(queryString, null, CommandType.Text)
                    .ToList();

                list?.ForEach(item =>
                {
                    if (item.RequestType == "Claim")
                    {
                        var insuranceBenefit = unitOfWork.GetRepository<Entities.InsuranceBenefit>()
                        .Query(x => x.BenefitFormType == item.RequestType2).FirstOrDefault();

                        if (insuranceBenefit != null)
                        {
                            item.RequestNameEn = insuranceBenefit.BenefitNameEn;
                            item.RequestNameMm = insuranceBenefit.BenefitNameMm;
                        }

                    }
                }
                );

                return errorCodeProvider.GetResponseModel<List<HomeRecentRequestResponse>>(ErrorCode.E0, list);
            }
            catch (Exception ex)
            {
                MobileErrorLog("GetHomeData Ex =>", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<HomeRecentRequestResponse>>(ErrorCode.E500);
            }

        }
        #endregion
    }
}
