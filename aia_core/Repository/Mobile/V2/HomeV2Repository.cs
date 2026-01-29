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
    public interface IHomeV2Repository
    {
        ResponseModel<List<CardListResponse>> GetCardList();
    }
    public class HomeV2Repository : BaseRepository, IHomeV2Repository
    {
        #region "const"
        private readonly IMemberPolicyRepository memberPolicyRepository;
        private readonly ICommonRepository commonRepository;

        public HomeV2Repository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
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

     
    }
}
